using Okey.Presentation.Tables;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Tables;
using UnityEngine;
using Zenject;
namespace Okey.Composition
{
    public sealed class TurnClockController : ITickable
    {
        private readonly LocalPlayerContext _local;
        private readonly TableSession _table;
        private readonly TurnClock _clock;
        private readonly IPlayerStrategy _strategy;
        private readonly TableView _controls;
        private readonly RackInteractionController _interaction;
        public TurnClockController(TableSession table, IPlayerStrategy strategy, TableView controls, RackInteractionController interaction, LocalPlayerContext local)
        {
            _table = table;
            _local = local;
            _clock = new TurnClock(table);
            _strategy = strategy;
            _controls = controls;
            _interaction = interaction;
        }
        private int _lastSeat = -1;
        private int _lastSecond = -1;
        private bool _lastFinished;
        private int _lastModeScore = int.MinValue;
        private bool _lastPartners;
        public void RenderClock(int seat, double remaining, bool partners, int score, double duration, bool finished = false)
        {
            var text = _controls.Texts;
            var seconds = Mathf.CeilToInt((float)remaining);
            _controls.Clock.RenderProgress(_local.ViewSeat(seat), (float)(remaining / duration), !finished);
            if (seat != _lastSeat || seconds != _lastSecond || finished != _lastFinished)
            {
                _lastSeat = seat;
                _lastSecond = seconds;
                _lastFinished = finished;
                _controls.Clock.RenderText(_local.ViewSeat(seat),
                    seat == _local.Seat ? string.Format(text.ClockFormat, text.LocalSeatName, seconds) : string.Format(text.SecondsFormat, seconds), !finished);
            }
            if (score == _lastModeScore && partners == _lastPartners) return;
            _lastModeScore = score;
            _lastPartners = partners;
            _controls.Hud.RenderMode(partners ? string.Format(text.PartnerModeFormat, text.SeatName(0), text.SeatName(_local.ViewSeat((_local.Seat + _table.Players.Count / 2) % _table.Players.Count)), score) : text.IndividualModeLabel);
        }
        public void Tick() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if (_clock.Advance(seconds))
            {
                _interaction.CancelActiveDrag();
                var seat = _table.ActiveSeat;
                if (_table.Phase == TurnPhase.AwaitingDraw) _table.TryDraw(seat);
                if (_table.Phase == TurnPhase.AwaitingDiscard)
                    _table.TryDiscard(seat, _strategy.ChooseDiscard(new PlayerObservation(
                        _table.Players[seat].Hand.Slots.Where(stone => stone != null), _table.Melds,
                        _table.Players[seat].Opening, _table.Rules, _table.MeldRules,
                        seriesThreshold: _table.SeriesThreshold, pairThreshold: _table.PairThreshold)));
            }
            RenderClock(_table.ActiveSeat, _clock.Remaining, _table.Options.Partnerships, _table.TeamScore(_local.Seat), _table.Options.TurnSeconds, _table.Phase == TurnPhase.Finished);
        }
    }
}
