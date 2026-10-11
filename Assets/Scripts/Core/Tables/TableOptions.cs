using System;
namespace Okey.Core.Tables
{
    public sealed class TableOptions
    {
        public bool Partnerships { get; }
        public bool AllowDiscardPickup { get; }
        public double TurnSeconds { get; }
        public bool EscalatingOpenings { get; }
        public int OpeningIncrement { get; }
        public TableOptions(bool partnerships = false, bool allowDiscardPickup = true, double turnSeconds = 45, bool escalatingOpenings = false, int openingIncrement = 1)
        {
            if (double.IsNaN(turnSeconds) || double.IsInfinity(turnSeconds) || turnSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(turnSeconds));
            if (openingIncrement < 1) throw new ArgumentOutOfRangeException(nameof(openingIncrement));
            EscalatingOpenings = escalatingOpenings;
            OpeningIncrement = openingIncrement;
            Partnerships = partnerships;
            AllowDiscardPickup = allowDiscardPickup;
            TurnSeconds = turnSeconds;
        }
    }
}
