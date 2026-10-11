using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public sealed class MeldPlan
    {
        public IReadOnlyList<Meld> Melds { get; }
        public int Points { get; }
        public int TileCount { get; }
        internal MeldPlan(IEnumerable<Meld> melds)
        {
            var snapshot = melds.ToArray();
            Melds = Array.AsReadOnly(snapshot);
            Points = snapshot.Sum(meld => meld.Points);
            TileCount = snapshot.Sum(meld => meld.Tiles.Count);
        }
    }
}
