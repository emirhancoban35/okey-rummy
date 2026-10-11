using System;

namespace Okey.Composition
{
    public sealed class LocalPlayerContext
    {
        public int Seat { get; }
        private readonly int _playerCount;

        public LocalPlayerContext(int seat, int playerCount)
        {
            if (seat < 0 || seat >= playerCount) throw new ArgumentOutOfRangeException(nameof(seat));
            Seat = seat;
            _playerCount = playerCount;
        }

        public int ViewSeat(int tableSeat) => (tableSeat - Seat + _playerCount) % _playerCount;
        public int TableSeat(int viewSeat) => (viewSeat + Seat) % _playerCount;
    }
}
