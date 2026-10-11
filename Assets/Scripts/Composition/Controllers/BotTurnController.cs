using Okey.Core.Dealing;
using Okey.Core.Tables;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class BotTurnController : ITickable
    {
        private readonly LocalPlayerContext _local;
        private readonly TableSession _table;
        private readonly IBotDecisionPolicy _decisions;
        private readonly BotPreferences _preferences;
        private readonly IRandomSource _random;
        private float _elapsed;
        private float _delay;
        private int _revision = -1;
        private bool _holdOpening;

        public BotTurnController(TableSession table, IBotDecisionPolicy decisions, BotPreferences preferences, IRandomSource random, LocalPlayerContext local)
        {
            _table = table;
            _local = local;
            _decisions = decisions;
            _preferences = preferences;
            _random = random;
        }

        public void Tick()
        {
            if (_table.ActiveSeat == _local.Seat || _table.Phase == TurnPhase.Finished)
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
            _holdOpening = _decisions.ShouldHoldOpening(_preferences);
            ScheduleDelay();
        }

        private void ScheduleDelay()
        {
            _delay = (float)(_preferences.Humanlike ? _preferences.MinimumThinkingSeconds +
                (_preferences.MaximumThinkingSeconds - _preferences.MinimumThinkingSeconds) * _random.NextDouble() :
                _preferences.FastActionSeconds);
        }

        public void Act()
        {
            var seat = _table.ActiveSeat;
            if (seat == _local.Seat || _table.Phase == TurnPhase.Finished) return;
            PrepareTurn();
            _table.TryApply(seat, _decisions.Decide(_table.ObserveTurn(seat), _preferences, _holdOpening));
        }
    }
}
