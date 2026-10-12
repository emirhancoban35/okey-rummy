using System;
using Okey.Core.Rules;
using Okey.Core.Stones;
namespace Okey.Core.Tables
{
    public sealed class MeldBoardLayout
    {
        private readonly int _rows;
        private readonly int _lanes;
        private readonly int _minimumValue;
        public int Capacity => _rows * _lanes;
        public int SeriesColumns { get; }
        public MeldBoardLayout(int rows, int lanes, int minimumValue, int maximumValue)
        {
            if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));
            if (lanes < 1) throw new ArgumentOutOfRangeException(nameof(lanes));
            if (minimumValue < 1 || maximumValue < minimumValue) throw new ArgumentOutOfRangeException(nameof(maximumValue));
            _rows = rows;
            _lanes = lanes;
            _minimumValue = minimumValue;
            SeriesColumns = Math.Max(maximumValue - minimumValue + 1, Enum.GetValues(typeof(StoneColor)).Length - 1);
        }
        public int PageCount(int groupCount)
        {
            if (groupCount < 0) throw new ArgumentOutOfRangeException(nameof(groupCount));
            return Math.Max(1, (groupCount + Capacity - 1) / Capacity);
        }
        public MeldBoardCell Locate(Meld meld, int groupIndex, int tileIndex)
        {
            if (meld == null) throw new ArgumentNullException(nameof(meld));
            if (groupIndex < 0 || tileIndex < 0 || tileIndex >= meld.Tiles.Count)
                throw new ArgumentOutOfRangeException(nameof(tileIndex));
            var tile = meld.Tiles[tileIndex];
            var column = meld.Kind == MeldKind.Run ? tile.Value - _minimumValue :
                tileIndex;
            if (column < 0 || column >= SeriesColumns) throw new ArgumentOutOfRangeException(nameof(meld));
            return new MeldBoardCell(groupIndex / Capacity, groupIndex / _rows % _lanes, groupIndex % _rows, column);
        }
    }
}
