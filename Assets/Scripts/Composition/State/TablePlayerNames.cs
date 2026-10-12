using System;
using Okey.Core.Tables;
using Okey.Presentation.Text;

namespace Okey.Composition
{
    public sealed class TablePlayerNames
    {
        private readonly TableTextCatalog _texts;
        private readonly BotPreferences _bots;
        private readonly LocalPlayerContext _local;
        public event Action Changed
        {
            add => _bots.Changed += value;
            remove => _bots.Changed -= value;
        }

        public TablePlayerNames(TableTextCatalog texts, BotPreferences bots, LocalPlayerContext local)
        {
            _texts = texts;
            _bots = bots;
            _local = local;
        }

        public string ForSeat(int seat)
        {
            var viewSeat = _local.ViewSeat(seat);
            return _bots.Humanlike ? _texts.PlayerName(viewSeat) : _texts.SeatName(viewSeat);
        }
    }
}
