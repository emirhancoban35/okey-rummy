using System;
using Okey.Core.Stones;

namespace Okey.Core.Decks
{
    public sealed class StoneSetDefinition
    {
        public StoneType Type { get; }
        public StoneColor Color { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get; }
        public int CopiesPerValue { get; }
        public int Count { get; }

        public StoneSetDefinition(StoneType type, StoneColor color,
            int minimumValue, int maximumValue, int copiesPerValue)
        {
            if (!Enum.IsDefined(typeof(StoneType), type))
                throw new ArgumentOutOfRangeException(nameof(type));

            if (!Enum.IsDefined(typeof(StoneColor), color))
                throw new ArgumentOutOfRangeException(nameof(color));

            if (minimumValue < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumValue));

            if (maximumValue < minimumValue)
                throw new ArgumentOutOfRangeException(nameof(maximumValue));

            if (copiesPerValue < 1)
                throw new ArgumentOutOfRangeException(nameof(copiesPerValue));

            Count = checked((maximumValue - minimumValue + 1) * copiesPerValue);
            Type = type;
            Color = color;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
            CopiesPerValue = copiesPerValue;
        }
    }
}
