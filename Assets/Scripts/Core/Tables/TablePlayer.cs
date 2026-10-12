using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class TablePlayer
    {
        private readonly List<StoneInstance> _discards = new List<StoneInstance>();
        internal RackArrangement OwnedHand { get; }
        public IReadOnlyRack Hand => Layout;
        public IRackLayout Layout { get; }
        public IReadOnlyList<StoneInstance> Discards { get; }
        public OpeningMode Opening { get; internal set; }
        public int OpeningValue { get; internal set; }
        public int Penalty { get; internal set; }
        public int Score { get; internal set; }
        internal TablePlayer(RackArrangement hand)
        {
            OwnedHand = hand;
            Layout = new RackLayout(hand);
            Discards = _discards.AsReadOnly();
        }
        internal StoneInstance TakeDiscard()
        {
            var stone = _discards[_discards.Count - 1];
            _discards.RemoveAt(_discards.Count - 1);
            return stone;
        }
        internal void Discard(StoneInstance stone) => _discards.Add(stone);
    }
}
