using System;
using System.Collections.Generic;

namespace Okey.Core.Stones
{
    public sealed class RackArrangement
    {
        private readonly StoneInstance[] _slots;
        private readonly Dictionary<int, int> _positions = new Dictionary<int, int>();

        public IReadOnlyList<StoneInstance> Slots { get; }
        public int Count => _positions.Count;
        public int Capacity => _slots.Length;

        public RackArrangement(IEnumerable<StoneInstance> stones, int capacity)
        {
            if (stones == null)
                throw new ArgumentNullException(nameof(stones));
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _slots = new StoneInstance[capacity];
            foreach (var stone in stones)
            {
                if (stone == null || _positions.ContainsKey(stone.Id))
                    throw new ArgumentException("Rack stones must be non-null with unique physical IDs.", nameof(stones));
                if (Count == capacity)
                    throw new ArgumentException("The stones exceed rack capacity.", nameof(stones));

                _slots[Count] = stone;
                _positions.Add(stone.Id, Count);
            }

            Slots = Array.AsReadOnly(_slots);
        }

        public int FindSlot(int stoneId)
        {
            return _positions.TryGetValue(stoneId, out var slot) ? slot : -1;
        }

        public void Move(int stoneId, int destination)
        {
            if (destination < 0 || destination >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(destination));
            if (!_positions.TryGetValue(stoneId, out var source))
                throw new ArgumentException("The stone does not belong to this rack.", nameof(stoneId));
            if (source == destination)
                return;

            var displaced = _slots[destination];
            _slots[destination] = _slots[source];
            _slots[source] = displaced;
            _positions[stoneId] = destination;
            if (displaced != null)
                _positions[displaced.Id] = source;
        }
    }
}
