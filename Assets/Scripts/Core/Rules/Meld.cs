using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public sealed class Meld
    {
        public MeldKind Kind { get; }
        public IReadOnlyList<MeldTile> Tiles { get; }
        public int Points { get; }
        internal Meld(MeldKind kind, IEnumerable<MeldTile> tiles)
        {
            Kind = kind;
            var snapshot = tiles.ToArray();
            Tiles = Array.AsReadOnly(snapshot);
            Points = snapshot.Sum(tile => tile.Value);
        }
    }
}
