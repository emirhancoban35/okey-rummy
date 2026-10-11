using System;
using System.Linq;
using Okey.Configuration;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using Okey.Core.Tables;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class BotTurnController : ITickable
    {
        private readonly TableSession _table;
        private readonly IPlayerStrategy _strategy;
        private readonly TableSettings _settings;
        private readonly IRandomSource _random;
        private float _elapsed;
        private float _delay;
        private int _revision = -1;
        private bool _holdOpening;
        public BotTurnController(TableSession table, IPlayerStrategy strategy, TableSettings settings, IRandomSource random)
        {
            _table = table;
            _strategy = strategy;
            _settings = settings;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        private PlayerObservation Observe(int seat) => new PlayerObservation(
            _table.Players[seat].Hand.Slots.Where(stone => stone != null), _table.Melds,
            _table.Players[seat].Opening, _table.Rules, _table.MeldRules, _table.Players[(seat + 1) % _table.Players.Count].Discards,
            _table.SeriesThreshold, _table.PairThreshold);

        public void Tick()
        {
            if (_table.ActiveSeat == 0 || _table.Phase == TurnPhase.Finished)
            {
                _elapsed = 0;
                return;
            }
            PrepareTurn();
            _elapsed += Time.deltaTime;
            if (_elapsed < _delay) return;
            _elapsed = 0;
            Act();
            ScheduleDelay();
        }

        private void PrepareTurn()
        {
            if (_revision == _table.TurnRevision) return;
            _revision = _table.TurnRevision;
            _elapsed = 0;
            _holdOpening = _settings.HumanlikeBots && _random.NextDouble() < _settings.OpeningPatience;
            ScheduleDelay();
        }

        private void ScheduleDelay()
        {
            var range = _settings.ThinkingSeconds;
            _delay = _settings.HumanlikeBots ? Mathf.Lerp(range.x, Mathf.Max(range.x, range.y), (float)_random.NextDouble()) : _settings.BotActionDelay;
        }

        public void Act()
        {
            var seat = _table.ActiveSeat;
            if (seat == 0 || _table.Phase == TurnPhase.Finished) return;
            PrepareTurn();
            if (_table.Phase == TurnPhase.AwaitingDraw)
            {
                if (_table.CanTakeDiscard(seat) && !_table.Game.RequiresOpenDiscardPickup)
                {
                    var previous = _table.Players[(seat + _table.Players.Count - 1) % _table.Players.Count];
                    var tile = previous.Discards[previous.Discards.Count - 1];
                    var hand = Observe(seat);
                    var extended = new PlayerObservation(hand.Hand.Append(tile), hand.Melds, hand.Opening, hand.Rules, hand.MeldRules);
                    var before = _strategy.PlanOpening(hand, out _);
                    var after = _strategy.PlanOpening(extended, out _);
                    if (after.TileCount > before.TileCount && after.Melds.SelectMany(meld => meld.Tiles).Any(used => used.Stone.Id == tile.Id) &&
                        _table.TryTakeDiscard(seat)) return;
                }
                if (_table.CanTakeDiscard(seat) && _table.Game.RequiresOpenDiscardPickup)
                    for (var index = 0; index < _table.Melds.Count; index++)
                        if (_table.TryTakeDiscard(seat, index)) return;
                _table.TryDraw(seat);
                return;
            }
            var player = _table.Players[seat];
            var observation = Observe(seat);
            var plan = _strategy.PlanOpening(observation, out var mode);
            var canOpen = plan.Melds.Count > 0 && (player.Opening != OpeningMode.Closed ||
                (mode == OpeningMode.Pairs ? plan.Melds.Count >= _table.PairThreshold : plan.Points >= _table.SeriesThreshold));
            var shouldOpen = player.Opening != OpeningMode.Closed || !_holdOpening || plan.TileCount >= player.Hand.Count - 2 || _table.RemainingCount <= _table.Players.Count;
            if (_table.Game.AllowsPublicMelds && canOpen && shouldOpen && _table.TryOpen(seat, mode, plan.Melds)) return;
            if (_table.TryFindFinish(seat, out var finishingDiscard))
            {
                _table.TryDiscard(seat, finishingDiscard);
                return;
            }
            var mistake = _random.NextDouble() < _settings.MistakeChance;
            if (!mistake)
                foreach (var stone in observation.Hand)
                    for (var meld = 0; meld < _table.Melds.Count; meld++)
                        if (_table.TryLayOff(seat, stone.Id, meld)) return;
            var discard = mistake ? observation.Hand[_random.Next(observation.Hand.Count)].Id : _strategy.ChooseDiscard(observation);
            _table.TryDiscard(seat, discard);
        }
    }
}
