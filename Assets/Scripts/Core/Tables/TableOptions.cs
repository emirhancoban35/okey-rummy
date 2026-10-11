using System;
namespace Okey.Core.Tables
{
    public sealed class TableOptions
    {
        public bool Partnerships { get; }
        public bool AllowDiscardPickup { get; }
        public double TurnSeconds { get; }
        public TableOptions(bool partnerships = false, bool allowDiscardPickup = true, double turnSeconds = 45)
        {
            if (double.IsNaN(turnSeconds) || double.IsInfinity(turnSeconds) || turnSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(turnSeconds));
            Partnerships = partnerships;
            AllowDiscardPickup = allowDiscardPickup;
            TurnSeconds = turnSeconds;
        }
    }
}
