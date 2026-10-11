using System;
using System.Collections.Generic;

namespace Okey.Core.Stones
{
    public sealed class RackArrangement : IRackLayout
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

        public int Add(StoneInstance stone)
        {
            if (stone == null)
                throw new ArgumentNullException(nameof(stone));
            if (_positions.ContainsKey(stone.Id))
                throw new ArgumentException("The stone already belongs to this rack.", nameof(stone));

            var slot = Array.FindIndex(_slots, value => value == null);
            if (slot < 0)
                throw new InvalidOperationException("The rack is full.");

            _slots[slot] = stone;
            _positions.Add(stone.Id, slot);
            return slot;
        }

        public StoneInstance Remove(int stoneId)
        {
            if (!_positions.TryGetValue(stoneId, out var slot))
                throw new ArgumentException("The stone does not belong to this rack.", nameof(stoneId));

            var stone = _slots[slot];
            _slots[slot] = null;
            _positions.Remove(stoneId);
            return stone;
        }

        public IReadOnlyList<StoneInstance> Move(int stoneId, int destination)
        {
            if (destination < 0 || destination >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(destination));
            if (!_positions.TryGetValue(stoneId, out var source))
                throw new ArgumentException("The stone does not belong to this rack.", nameof(stoneId));
            if (source == destination)
                return Array.Empty<StoneInstance>();

            var stone = _slots[source];
            _slots[source] = null;
            var gap = FindNearestGap(destination);
            var moved = new List<StoneInstance>(Math.Abs(gap - destination) + 1);
            var direction = gap < destination ? 1 : -1;
            while (gap != destination)
            {
                var next = gap + direction;
                var shifted = _slots[next];
                _slots[gap] = shifted;
                _positions[shifted.Id] = gap;
                moved.Add(shifted);
                gap = next;
            }

            _slots[destination] = stone;
            _positions[stoneId] = destination;
            moved.Add(stone);
            return moved.AsReadOnly();
        }

        private int FindNearestGap(int destination)
        {
            for (var distance = 0; distance < Capacity; distance++)
            {
                var right = destination + distance;
                if (right < Capacity && _slots[right] == null)
                    return right;

                var left = destination - distance;
                if (left >= 0 && _slots[left] == null)
                    return left;
            }

            throw new InvalidOperationException("The rack must contain a gap after lifting a stone.");
        }
    }
}
