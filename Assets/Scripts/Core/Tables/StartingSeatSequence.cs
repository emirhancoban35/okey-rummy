namespace Okey.Core.Tables
{
    public sealed class StartingSeatSequence
    {
        private readonly IRandomSource _random;
        private int _lastSeat = -1;

        public StartingSeatSequence(IRandomSource random) => _random = random;

        public int Next(int playerCount, bool randomStart)
        {
            if (!randomStart) return 0;
            _lastSeat = _lastSeat < 0 ? _random.Next(playerCount) : (_lastSeat + 1) % playerCount;
            return _lastSeat;
        }
    }
}
