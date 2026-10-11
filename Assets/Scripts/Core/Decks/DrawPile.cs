using System;
using System.Collections.Generic;
using Okey.Core.Stones;

namespace Okey.Core.Decks
{
    public sealed class DrawPile
    {
        private readonly StoneInstance[] _stones;
        private int _nextIndex;

        public int RemainingCount => _stones.Length - _nextIndex;

        public DrawPile(IEnumerable<StoneInstance> stones)
        {
            if (stones == null)
                throw new ArgumentNullException(nameof(stones));

            _stones = new List<StoneInstance>(stones).ToArray();
            var ids = new HashSet<int>();
            foreach (var stone in _stones)
            {
                if (stone == null)
                    throw new ArgumentException("A draw pile cannot contain a null stone.", nameof(stones));

                if (!ids.Add(stone.Id))
                    throw new ArgumentException("Physical stone IDs must be unique within a draw pile.", nameof(stones));
            }
        }

        public bool TryDraw(out StoneInstance stone)
        {
            if (RemainingCount == 0)
            {
                stone = null;
                return false;
            }

            stone = _stones[_nextIndex++];
            return true;
        }

        public bool Contains(int stoneId)
        {
            for (var index = _nextIndex; index < _stones.Length; index++)
                if (_stones[index].Id == stoneId)
                    return true;

            return false;
        }

        public IReadOnlyList<StoneInstance> Draw(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count > RemainingCount)
                throw new InvalidOperationException("The draw pile does not contain enough stones.");

            var drawn = new StoneInstance[count];
            Array.Copy(_stones, _nextIndex, drawn, 0, count);
            _nextIndex += count;
            return Array.AsReadOnly(drawn);
        }
    }
}
