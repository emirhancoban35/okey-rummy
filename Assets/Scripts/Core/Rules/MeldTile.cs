using System;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
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
}
