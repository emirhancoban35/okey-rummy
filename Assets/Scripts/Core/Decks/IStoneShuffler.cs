using System.Collections.Generic;
using Okey.Core.Stones;

namespace Okey.Core.Decks
{
    public interface IStoneShuffler
    {
        IReadOnlyList<StoneInstance> Shuffle(IReadOnlyList<StoneInstance> stones, int seed);
    }
}
