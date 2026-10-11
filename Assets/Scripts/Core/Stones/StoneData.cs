using System;

namespace Okey.Core.Stones
{
    public sealed class StoneData
    {
        public StoneType Type { get; }
        public StoneColor Color { get; }
        public int Value { get; }

        public StoneData(StoneType type, StoneColor color, int value)
        {
            if (!Enum.IsDefined(typeof(StoneType), type))
                throw new ArgumentOutOfRangeException(nameof(type));

            if (!Enum.IsDefined(typeof(StoneColor), color))
                throw new ArgumentOutOfRangeException(nameof(color));

            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            Type = type;
            Color = color;
            Value = value;
        }
    }
}
