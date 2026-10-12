using System;
namespace Okey.Core.Tables
{
    public sealed class TurnClock
    {
        private readonly TableSession _table;
        private int _turn = -1;
        private int _opening = -1;
        private bool _expired;
        public double Remaining { get; private set; }
        public TurnClock(TableSession table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            Advance(0);
        }
        public bool Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (_table.Phase == TurnPhase.Finished) { Remaining = 0; return false; }
            if (_turn != _table.TurnRevision || _opening != _table.OpeningRevision)
            {
                _turn = _table.TurnRevision;
                _opening = _table.OpeningRevision;
                Remaining = _table.Options.TurnSeconds;
                _expired = false;
            }
            Remaining = Math.Max(0, Remaining - seconds);
            if (Remaining > 0 || _expired) return false;
            _expired = true;
            return true;
        }
    }
}
