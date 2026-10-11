using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class TablePlayer
    {
        private readonly List<StoneInstance> _discards = new List<StoneInstance>();
        public RackArrangement Hand { get; }
        public IReadOnlyList<StoneInstance> Discards { get; }
        public OpeningMode Opening { get; internal set; }
        public int Penalty { get; internal set; }
        public int Score { get; internal set; }
        internal TablePlayer(RackArrangement hand)
        {
            Hand = hand;
            Discards = _discards.AsReadOnly();
        }
        internal void Discard(StoneInstance stone) => _discards.Add(stone);
    }
}
