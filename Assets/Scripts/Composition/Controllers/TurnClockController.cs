using Okey.Presentation.Tables;
using System.Linq;
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
        private readonly TimeoutDiscardPolicy _timeout;
        private readonly TableView _controls;
        private readonly RackInteractionController _interaction;
        public TurnClockController(TableSession table, TimeoutDiscardPolicy timeout, TableView controls, RackInteractionController interaction, LocalPlayerContext local)
        {
            _table = table;
            _local = local;
            _clock = new TurnClock(table);
            _timeout = timeout;
            _controls = controls;
            _interaction = interaction;
        }
        private int _lastSeat = -1;
        private int _lastSecond = -1;
        private bool _lastFinished;
        public void RenderClock(int seat, double remaining, double duration, bool finished = false)
        {
            var seconds = Mathf.CeilToInt((float)remaining);
            _controls.Clock.RenderProgress(_local.ViewSeat(seat), (float)(remaining / duration), !finished);
            if (seat == _lastSeat && seconds == _lastSecond && finished == _lastFinished) return;
            _lastSeat = seat;
            _lastSecond = seconds;
            _lastFinished = finished;
            _controls.Clock.RenderText(_local.ViewSeat(seat), seconds, !finished);
        }
        public void Tick() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if (_clock.Advance(seconds))
            {
                _interaction.CancelActiveDrag();
                var seat = _table.ActiveSeat;
                _table.TryReturnDiscard(seat);
                if (_table.Phase == TurnPhase.AwaitingDraw) _table.TryDraw(seat);
                if (_table.Phase == TurnPhase.AwaitingDiscard)
                    _table.TryDiscard(seat, _timeout.ChooseDiscard(new PlayerObservation(
                        _table.Players[seat].Hand.Slots.Where(stone => stone != null), _table.Melds,
                        _table.Players[seat].Opening, _table.Rules, _table.MeldRules,
                        seriesThreshold: _table.SeriesThreshold, pairThreshold: _table.PairThreshold)));
            }
            RenderClock(_table.ActiveSeat, _clock.Remaining, _table.Options.TurnSeconds, _table.Phase == TurnPhase.Finished);
        }
    }
}
