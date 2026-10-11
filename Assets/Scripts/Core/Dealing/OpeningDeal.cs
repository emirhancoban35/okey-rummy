using System.Collections.Generic;
using Okey.Core.Decks;
using Okey.Core.Stones;

namespace Okey.Core.Dealing
{
    public sealed class OpeningDeal
    {
        public int Seed { get; }
        public IReadOnlyList<StoneInstance> InitialHand { get; }
        public DrawPile DrawPile { get; }

        internal OpeningDeal(int seed, IReadOnlyList<StoneInstance> initialHand, DrawPile drawPile)
        {
            Seed = seed;
            InitialHand = initialHand;
            DrawPile = drawPile;
        }
    }
}
