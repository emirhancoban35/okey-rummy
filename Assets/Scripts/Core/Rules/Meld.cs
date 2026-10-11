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
        public Meld(MeldKind kind, IEnumerable<MeldTile> tiles)
        {
            if (!Enum.IsDefined(typeof(MeldKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            var snapshot = tiles.ToArray();
            if (snapshot.Length == 0 || snapshot.Any(tile => tile == null) ||
                snapshot.Select(tile => tile.Stone.Id).Distinct().Count() != snapshot.Length)
                throw new ArgumentException("A meld needs distinct physical tiles.", nameof(tiles));
            Kind = kind;
            Tiles = Array.AsReadOnly(snapshot);
            Points = snapshot.Sum(tile => tile.Value);
        }
    }
}
