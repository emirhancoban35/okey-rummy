using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public enum OpeningMode { Closed, Series, Pairs }
    public enum MeldKind { Run, Set, Pair }

    public sealed class MeldTile
    {
        public StoneInstance Stone { get; }
        public StoneColor Color { get; }
        public int Value { get; }
        public MeldTile(StoneInstance stone, StoneColor color, int value)
        {
            Stone = stone ?? throw new ArgumentNullException(nameof(stone));
            Color = color;
            Value = value;
        }
    }

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
