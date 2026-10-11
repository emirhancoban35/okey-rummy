using System;
using System.Collections.Generic;
using System.Linq;

namespace Okey.Core.Rules
{
    public sealed class MeldPlan
    {
        public IReadOnlyList<Meld> Melds { get; }
        public int Points { get; }
        public int TileCount { get; }
        public MeldPlan(IEnumerable<Meld> melds)
        {
            if (melds == null) throw new ArgumentNullException(nameof(melds));
            var snapshot = melds.ToArray();
            if (snapshot.Any(meld => meld == null)) throw new ArgumentException("Melds must be assigned.", nameof(melds));
            Melds = Array.AsReadOnly(snapshot);
            Points = snapshot.Sum(meld => meld.Points);
            TileCount = snapshot.Sum(meld => meld.Tiles.Count);
        }
    }
}
