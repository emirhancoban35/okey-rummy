using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Players;

namespace Okey.Core.Rooms
{
    public sealed class LocalRoom
    {
        private readonly string _password;
        private readonly List<PlayerIdentity> _members;
        public int Number { get; }
        public string Name { get; }
        public RoomOptions Options { get; }
        public bool Locked => _password.Length > 0;
        public IReadOnlyList<PlayerIdentity> Members { get; }
        public bool Full => _members.Count == Options.Seats;
        public bool Joined { get; private set; }

        public LocalRoom(int number, string name, string password, RoomOptions options, IEnumerable<PlayerIdentity> members)
        {
            if (number < 1 || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A room needs a number and name.");
            Number = number;
            Name = name.Trim();
            _password = password ?? string.Empty;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _members = members.ToList();
            if (_members.Count > options.Seats || _members.Any(player => player == null) ||
                _members.Select(player => player.Id).Distinct().Count() != _members.Count)
                throw new ArgumentException("Invalid room members.", nameof(members));
            Members = _members.AsReadOnly();
        }

        internal bool Accepts(string password) => string.Equals(_password, password ?? string.Empty, StringComparison.Ordinal);
        internal void Join(PlayerIdentity player)
        {
            _members.Insert(0, player);
            Joined = true;
        }

        internal void Leave()
        {
            _members.RemoveAt(0);
            Joined = false;
        }

        public bool RemovePlayer(string id)
        {
            if (!Joined) return false;
            var index = _members.FindIndex(player => player.Id == id);
            if (index <= 0) return false;
            _members.RemoveAt(index);
            return true;
        }

        public bool AddBot(PlayerIdentity player)
        {
            if (!Joined || Full) return false;
            if (_members.Any(member => member.Id == player.Id)) return false;
            _members.Add(player);
            return true;
        }
    }
}
