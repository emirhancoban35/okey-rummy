using System;
using Okey.Core.Rules;
using Okey.Core.Stones;
namespace Okey.Core.Tables
{
    public readonly struct MeldBoardCell
    {
        public int Page { get; }
        public int Lane { get; }
        public int Row { get; }
        public int Column { get; }
        public MeldBoardCell(int page, int lane, int row, int column)
        {
            Page = page;
            Lane = lane;
            Row = row;
            Column = column;
        }
    }
}
