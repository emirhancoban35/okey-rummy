using System;
using System.Linq;

namespace Okey.Core.Tables
{
    public sealed class DiscardWeights
    {
        public double Adjacent { get; }
        public double Gap { get; }
        public double Set { get; }
        public double Duplicate { get; }
        public double LowTile { get; }

        public DiscardWeights(double adjacent = 4, double gap = 1, double set = 2, double duplicate = 1, double lowTile = .2)
        {
            if (new[] { adjacent, gap, set, duplicate, lowTile }.Any(value => value < 0 || double.IsNaN(value) || double.IsInfinity(value)))
                throw new ArgumentOutOfRangeException(nameof(adjacent));
            Adjacent = adjacent;
            Gap = gap;
            Set = set;
            Duplicate = duplicate;
            LowTile = lowTile;
        }
    }
}
