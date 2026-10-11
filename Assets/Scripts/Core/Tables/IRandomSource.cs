using System;
namespace Okey.Core.Tables
{
    public interface IRandomSource
    {
        double NextDouble();
        int Next(int maximum);
    }
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;
        public SeededRandomSource(int seed) => _random = new Random(seed);
        public double NextDouble() => _random.NextDouble();
        public int Next(int maximum) => _random.Next(maximum);
    }
}
