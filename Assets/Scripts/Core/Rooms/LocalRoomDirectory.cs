using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Players;

namespace Okey.Core.Rooms
{
    public sealed class LocalRoomDirectory
    {
        private readonly List<LocalRoom> _rooms;
        private int _nextNumber;
        public IReadOnlyList<LocalRoom> Rooms { get; }
        public LocalRoom Current { get; private set; }

        public LocalRoomDirectory(IEnumerable<LocalRoom> rooms, int firstNumber)
        {
            _rooms = rooms.ToList();
            if (firstNumber < 1 || _rooms.Select(room => room.Number).Distinct().Count() != _rooms.Count)
                throw new ArgumentException("Room numbers must be positive and unique.");
            _nextNumber = firstNumber;
            Rooms = _rooms.AsReadOnly();
        }

        public LocalRoom Create(string name, string password, RoomOptions options, PlayerIdentity player)
        {
            while (_rooms.Any(room => room.Number == _nextNumber)) _nextNumber++;
            var room = new LocalRoom(_nextNumber++, name, password, options, Array.Empty<PlayerIdentity>());
            JoinLocal(room, player);
            _rooms.Add(room);
            return room;
        }

        public RoomJoinResult Join(int number, string password, PlayerIdentity player)
        {
            var room = _rooms.FirstOrDefault(item => item.Number == number);
            if (room == null) return RoomJoinResult.NotFound;
            if (!room.Accepts(password)) return RoomJoinResult.WrongPassword;
            if (room == Current) return RoomJoinResult.Joined;
            if (room.Full || room.Members.Any(member => member.Id == player.Id)) return RoomJoinResult.Full;
            JoinLocal(room, player);
            return RoomJoinResult.Joined;
        }

        private void JoinLocal(LocalRoom room, PlayerIdentity player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            Leave();
            room.Join(player);
            Current = room;
        }

        public void Leave()
        {
            if (Current == null) return;
            Current.Leave();
            Current = null;
        }
    }
}
