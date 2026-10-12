using System;
using Okey.Core.Rooms;
using Okey.Core.Tables;
using Okey.Presentation.Tables;
using Okey.Configuration;
using Okey.Core.Players;

namespace Okey.Composition
{
    public sealed class TablePlayerNames
    {
        private readonly TablePlayersView _view;
        private readonly PlayerIdentity[] _roster;
        private readonly BotPreferences _bots;
        private readonly LocalPlayerContext _local;
        private readonly LocalRoomDirectory _rooms;
        public event Action Changed
        {
            add => _bots.Changed += value;
            remove => _bots.Changed -= value;
        }

        public TablePlayerNames(TablePlayersView view, BotPreferences bots, LocalPlayerContext local, LocalRoomDirectory rooms, RoomCatalog profiles)
        {
            _view = view;
            _roster = profiles.CreateRoster();
            _bots = bots;
            _local = local;
            _rooms = rooms;
        }

        public string ForSeat(int seat)
        {
            var viewSeat = _local.ViewSeat(seat);
            if (_bots.Humanlike && _rooms.Current != null) return _rooms.Current.Members[viewSeat].DisplayName;
            return _bots.Humanlike ? _roster[viewSeat].DisplayName : _view.SeatName(viewSeat);
        }
    }
}
