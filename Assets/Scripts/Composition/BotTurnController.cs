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
        private float _elapsed;
        public BotTurnController(TableSession table, IPlayerStrategy strategy, TableSettings settings)
        {
            _table = table;
            _strategy = strategy;
            _settings = settings;
        }

        private PlayerObservation Observe(int seat) => new PlayerObservation(
            _table.Players[seat].Hand.Slots.Where(stone => stone != null), _table.Melds,
            _table.Players[seat].Opening, _table.Rules, _table.MeldRules);

        public void Tick()
        {
            if (_table.ActiveSeat == 0 || _table.Phase == TurnPhase.Finished)
            {
                _elapsed = 0;
                return;
            }
            _elapsed += Time.deltaTime;
            if (_elapsed < _settings.BotActionDelay) return;
            _elapsed = 0;
            Act();
        }

        public void Act()
        {
            var seat = _table.ActiveSeat;
            if (seat == 0 || _table.Phase == TurnPhase.Finished) return;
            if (_table.Phase == TurnPhase.AwaitingDraw) { _table.TryDraw(seat); return; }
            var plan = _strategy.PlanOpening(Observe(seat), out var mode);
            if (plan.Melds.Count > 0 && (_table.Players[seat].Opening != OpeningMode.Closed ||
                (mode == OpeningMode.Pairs ? plan.Melds.Count >= _table.Rules.PairThreshold : plan.Points >= _table.Rules.SeriesThreshold)))
                _table.TryOpen(seat, mode, plan.Melds);
            foreach (var stone in _table.Players[seat].Hand.Slots.Where(stone => stone != null).ToArray())
                for (var meld = 0; meld < _table.Melds.Count; meld++)
                    if (_table.TryLayOff(seat, stone.Id, meld)) break;
            _table.TryDiscard(seat, _strategy.ChooseDiscard(Observe(seat)));
        }
    }
}
