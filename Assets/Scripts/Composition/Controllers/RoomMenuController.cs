using System;
using System.Linq;
using Okey.Configuration;
using Okey.Core.Rooms;
using Okey.Presentation.Menus;
using Zenject;

namespace Okey.Composition
{
    public sealed class RoomMenuController : IInitializable, IDisposable
    {
        private readonly RoomMenuView _view;
        private readonly NewGameMenuView _settingsView;
        private readonly GameSetup _setup;
        private readonly RoomCatalog _catalog;
        private readonly LocalRoomDirectory _rooms;
        private readonly MatchHistory _matches;
        private readonly ITableNavigation _navigation;

        public RoomMenuController(RoomMenuView view, NewGameMenuView settingsView, GameSetup setup, RoomCatalog catalog,
            LocalRoomDirectory rooms, MatchHistory matches, ITableNavigation navigation)
        {
            _view = view;
            _settingsView = settingsView;
            _setup = setup;
            _catalog = catalog;
            _rooms = rooms;
            _matches = matches;
            _navigation = navigation;
        }

        public void Initialize()
        {
            _view.CreateRequested += _view.ShowCreate;
            _view.JoinRequested += ShowJoin;
            _view.JoinSubmitted += Join;
            _view.RoomSelected += SelectRoom;
            _view.RemovePlayerRequested += RemovePlayer;
            _view.BackRequested += Leave;
            _view.AddBotRequested += AddBot;
            _view.StartRequested += Start;
            _settingsView.CreateRoomRequested += Create;
            if (_rooms.Current == null) _view.ShowHome();
            else
            {
                _view.ShowLobby();
                RenderLobby();
            }
        }

        private void ShowJoin()
        {
            _view.BeginRoomList();
            for (var index = 0; index < _rooms.Rooms.Count; index++)
            {
                var room = _rooms.Rooms[index];
                _view.RenderRoom(index, room.Number, room.Name, room.Members.Count, room.Options.Seats, room.Locked);
            }
            _view.ShowJoin();
        }

        private void Create()
        {
            if (string.IsNullOrWhiteSpace(_view.RoomName))
            {
                _view.ShowMessage(_view.InvalidName);
                return;
            }
            _rooms.Create(_view.RoomName, _view.CreatePassword, _setup.CreateRoomOptions(), _catalog.LocalPlayer);
            _view.ShowLobby();
            RenderLobby();
        }

        private void Join()
        {
            if (!int.TryParse(_view.RoomNumber, out var number))
            {
                _view.ShowMessage(_view.InvalidNumber);
                return;
            }
            Join(number, _view.JoinPassword);
        }

        private void SelectRoom(int number)
        {
            var room = _rooms.Rooms.Single(item => item.Number == number);
            if (room.Locked) _view.SelectRoomNumber(number);
            else Join(number, string.Empty);
        }

        private void Join(int number, string password)
        {
            var result = _rooms.Join(number, password, _catalog.LocalPlayer);
            if (result != RoomJoinResult.Joined)
            {
                _view.ShowMessage(result == RoomJoinResult.NotFound ? _view.NotFound :
                    result == RoomJoinResult.WrongPassword ? _view.WrongPassword : _view.Full);
                return;
            }
            _setup.ApplyRoom(_rooms.Current.Options);
            _view.ShowLobby();
            RenderLobby();
        }

        private void AddBot()
        {
            var room = _rooms.Current;
            var bot = _catalog.AvailableBot(room);
            if (bot != null && room.AddBot(bot)) RenderLobby();
        }

        private void RemovePlayer(int seat)
        {
            var room = _rooms.Current;
            if (seat > 0 && seat < room.Members.Count && room.RemovePlayer(room.Members[seat].Id)) RenderLobby();
        }

        private void RenderLobby()
        {
            var room = _rooms.Current;
            _view.RenderLobby(room.Number, room.Name, _setup.ActiveProfile.Label, room.Options.Hands, room.Locked, room.Members.Select(player => player.DisplayName).ToArray(), room.Full);
        }

        private void Leave()
        {
            _rooms.Leave();
            _view.ShowHome();
        }

        private void Start()
        {
            if (_rooms.Current == null || !_rooms.Current.Full) return;
            _setup.ApplyRoom(_rooms.Current.Options);
            _matches.Reset(_setup);
            _navigation.StartTable();
        }

        public void Dispose()
        {
            _view.CreateRequested -= _view.ShowCreate;
            _view.JoinRequested -= ShowJoin;
            _view.RoomSelected -= SelectRoom;
            _view.RemovePlayerRequested -= RemovePlayer;
            _view.JoinSubmitted -= Join;
            _view.BackRequested -= Leave;
            _view.AddBotRequested -= AddBot;
            _view.StartRequested -= Start;
            _settingsView.CreateRoomRequested -= Create;
        }
    }
}
