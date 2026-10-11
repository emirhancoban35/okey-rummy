using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using UnityEngine;
using Zenject;
namespace Okey.Composition
{
    public sealed class TurnClockController : ITickable
    {
        private readonly TableSession _table;
        private readonly TurnClock _clock;
        private readonly IPlayerStrategy _strategy;
        private readonly TurnControlsView _controls;
        private readonly RackInteractionController _interaction;
        public TurnClockController(TableSession table, IPlayerStrategy strategy, TurnControlsView controls, RackInteractionController interaction)
        {
            _table = table;
            _clock = new TurnClock(table);
            _strategy = strategy;
            _controls = controls;
            _interaction = interaction;
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
                        _table.Players[seat].Opening, _table.Rules, _table.MeldRules)));
            }
            _controls.RenderClock(_table.ActiveSeat, _clock.Remaining, _table.Options.Partnerships, _table.TeamScore(0));
        }
    }
}
