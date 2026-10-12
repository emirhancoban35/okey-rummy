using System;

namespace Okey.Core.Rooms
{
    public sealed class RoomOptions
    {
        public int RuleIndex { get; }
        public int Hands { get; }
        public int Seats { get; }
        public bool Partnerships { get; }
        public bool Humanlike { get; }
        public bool Rising { get; }
        public bool Reclaim { get; }

        public RoomOptions(int ruleIndex, int hands, int seats, bool partnerships, bool humanlike, bool rising, bool reclaim)
        {
            if (ruleIndex < 0 || hands < 1 || seats < 2 || seats > 4 || partnerships && seats != 4)
                throw new ArgumentOutOfRangeException(nameof(hands));
            RuleIndex = ruleIndex;
            Hands = hands;
            Seats = seats;
            Partnerships = partnerships;
            Humanlike = humanlike;
            Rising = rising;
            Reclaim = reclaim;
        }
    }
}
