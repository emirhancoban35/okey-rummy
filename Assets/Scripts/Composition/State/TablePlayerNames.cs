using System;
using Okey.Core.Rooms;
using Okey.Core.Tables;
using Okey.Presentation.Text;

namespace Okey.Composition
{
    public sealed class TablePlayerNames
    {
        private readonly TableTextCatalog _texts;
        private readonly BotPreferences _bots;
        private readonly LocalPlayerContext _local;
        private readonly LocalRoomDirectory _rooms;
        public event Action Changed
        {
            add => _bots.Changed += value;
            remove => _bots.Changed -= value;
        }

        public TablePlayerNames(TableTextCatalog texts, BotPreferences bots, LocalPlayerContext local, LocalRoomDirectory rooms)
        {
            _texts = texts;
            _bots = bots;
            _local = local;
            _rooms = rooms;
        }

        public string ForSeat(int seat)
        {
            var viewSeat = _local.ViewSeat(seat);
            if (_bots.Humanlike && _rooms.Current != null) return _rooms.Current.Members[viewSeat].DisplayName;
            return _bots.Humanlike ? _texts.PlayerName(viewSeat) : _texts.SeatName(viewSeat);
        }
    }
}
