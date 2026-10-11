using System;
using System.Collections.Generic;
using Okey.Core.Stones;

namespace Okey.Core.Decks
{
    public sealed class SeededStoneShuffler : IStoneShuffler
    {
        public IReadOnlyList<StoneInstance> Shuffle(IReadOnlyList<StoneInstance> stones, int seed)
        {
            if (stones == null)
                throw new ArgumentNullException(nameof(stones));

            var shuffled = new StoneInstance[stones.Count];
            for (var index = 0; index < stones.Count; index++)
                shuffled[index] = stones[index];

            var random = new Random(seed);
            for (var index = shuffled.Length - 1; index > 0; index--)
            {
                var other = random.Next(index + 1);
                var stone = shuffled[index];
                shuffled[index] = shuffled[other];
                shuffled[other] = stone;
            }

            return Array.AsReadOnly(shuffled);
        }
    }
}
