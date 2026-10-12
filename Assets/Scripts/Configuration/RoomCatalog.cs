using System;
using System.Linq;
using Okey.Core.Rooms;
using Okey.Core.Players;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Room Catalog")]
    public sealed class RoomCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            public int Number;
            public string Name;
            public string Password;
            public int RuleIndex;
            public int Hands;
            public bool Partnerships;
            public bool Rising;
            public bool Humanlike = true;
            public bool Reclaim = true;
            public PlayerProfile[] Players = Array.Empty<PlayerProfile>();
        }
        [SerializeField] private Entry[] _rooms = Array.Empty<Entry>();
        [SerializeField] private int[] _handChoices = { 1, 3, 5, 7, 11 };
        [SerializeField, Min(1)] private int _firstRoomNumber = 100001;
        [SerializeField] private PlayerProfile _localPlayer;
        [SerializeField] private PlayerProfile[] _botPlayers = Array.Empty<PlayerProfile>();
        public PlayerIdentity LocalPlayer => _localPlayer.CreateIdentity();
        public PlayerIdentity AvailableBot(LocalRoom room) => _botPlayers.Select(profile => profile.CreateIdentity())
            .FirstOrDefault(player => room.Members.All(member => member.Id != player.Id));
        public PlayerIdentity[] CreateRoster() => new[] { LocalPlayer }.Concat(
            _botPlayers.Select(profile => profile.CreateIdentity())).ToArray();
        public int NextHands(int current)
        {
            var index = Array.IndexOf(_handChoices, current);
            return _handChoices[(index + 1) % _handChoices.Length];
        }
        public LocalRoomDirectory CreateDirectory(int seats) => new LocalRoomDirectory(_rooms.Select(room =>
            new LocalRoom(room.Number, room.Name, room.Password,
                new RoomOptions(room.RuleIndex, room.Hands, seats, room.Partnerships, room.Humanlike, room.Rising, room.Reclaim), room.Players.Select(profile => profile.CreateIdentity()))), _firstRoomNumber);
    }
}
