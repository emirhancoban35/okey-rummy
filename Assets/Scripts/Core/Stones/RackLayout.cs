using System.Collections.Generic;

namespace Okey.Core.Stones
{
    public sealed class RackLayout : IRackLayout
    {
        private readonly RackArrangement _rack;

        internal RackLayout(RackArrangement rack) => _rack = rack;

        public IReadOnlyList<StoneInstance> Slots => _rack.Slots;
        public int Count => _rack.Count;
        public int Capacity => _rack.Capacity;
        public int FindSlot(int stoneId) => _rack.FindSlot(stoneId);
        public IReadOnlyList<StoneInstance> Move(int stoneId, int destination) => _rack.Move(stoneId, destination);
        public IReadOnlyList<StoneInstance> MoveGroup(IReadOnlyList<int> stoneIds, int destination, int rowCapacity) =>
            _rack.MoveGroup(stoneIds, destination, rowCapacity);
    }
}
