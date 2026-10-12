using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Menus
{
    public sealed class RoomMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _home;
        [SerializeField] private GameObject _create;
        [SerializeField] private GameObject _join;
        [SerializeField] private GameObject _lobby;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _joinSubmit;
        [SerializeField] private Button[] _backButtons;
        [SerializeField] private Button _addBot;
        [SerializeField] private Button _start;
        [SerializeField] private TMP_InputField _roomName;
        [SerializeField] private TMP_InputField _createPassword;
        [SerializeField] private TMP_InputField _roomNumber;
        [SerializeField] private TMP_InputField _joinPassword;
        [SerializeField] private RectTransform _roomList;
        [SerializeField] private RoomListItemView _roomItemPrefab;
        private readonly List<RoomListItemView> _roomItems = new List<RoomListItemView>();
        [SerializeField] private TMP_Text _roomTitle;
        [SerializeField] private TMP_Text _roomDetails;
        [SerializeField] private RoomSeatView[] _members;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private string _roomTitleFormat = "{0}  ·  #{1}";
        [SerializeField] private string _roomDetailsFormat = "{0}  ·  {1} hands  ·  {2}";
        [SerializeField] private string _roomListFormat = "#{0}   {1}   {2}/{3}   {4}";
        [SerializeField] private string _seatFormat = "{0:00}   {1}";
        [SerializeField] private string _emptySeat = "Empty seat";
        [SerializeField] private string _locked = "Private";
        [SerializeField] private string _open = "Open";
        [SerializeField] private string _invalidName = "Choose a room name.";
        [SerializeField] private string _invalidNumber = "Enter a valid room number.";
        [SerializeField] private string _notFound = "Room not found.";
        [SerializeField] private string _wrongPassword = "That password does not match.";
        [SerializeField] private string _full = "This room is full.";
        public string RoomName => _roomName.text;
        public string CreatePassword => _createPassword.text;
        public string RoomNumber => _roomNumber.text;
        public string JoinPassword => _joinPassword.text;
        public string InvalidName => _invalidName;
        public string InvalidNumber => _invalidNumber;
        public string NotFound => _notFound;
        public string WrongPassword => _wrongPassword;
        public string Full => _full;
        public event Action CreateRequested;
        public event Action JoinRequested;
        public event Action JoinSubmitted;
        public event Action<int> RoomSelected;
        public event Action<int> RemovePlayerRequested;
        public event Action BackRequested;
        public event Action AddBotRequested;
        public event Action StartRequested;
        private void Show(GameObject panel)
        {
            _home.SetActive(panel == _home);
            _create.SetActive(panel == _create);
            _join.SetActive(panel == _join);
            _lobby.SetActive(panel == _lobby);
            _message.text = string.Empty;
        }

        public void ShowHome() => Show(_home);
        public void ShowCreate() => Show(_create);
        public void ShowJoin()
        {
            _joinPassword.text = string.Empty;
            Show(_join);
        }

        public void ShowLobby()
        {
            _createPassword.text = string.Empty;
            _joinPassword.text = string.Empty;
            Show(_lobby);
        }

        public void ShowMessage(string message) => _message.text = message;
        public string RoomListLine(int number, string name, int members, int capacity, bool locked) =>
            string.Format(_roomListFormat, number, name, members, capacity, locked ? _locked : _open);
        public void BeginRoomList()
        {
            foreach (var item in _roomItems) item.gameObject.SetActive(false);
        }

        public void RenderRoom(int index, int number, string name, int members, int capacity, bool locked)
        {
            if (index == _roomItems.Count)
            {
                var item = Instantiate(_roomItemPrefab, _roomList);
                item.Selected += SelectRoom;
                _roomItems.Add(item);
            }
            var row = _roomItems[index];
            row.Render(number, RoomListLine(number, name, members, capacity, locked), members < capacity);
            row.gameObject.SetActive(true);
        }

        public void SelectRoomNumber(int number)
        {
            _roomNumber.text = number.ToString();
            _joinPassword.text = string.Empty;
            _joinPassword.ActivateInputField();
        }

        private void SelectRoom(int number) => RoomSelected?.Invoke(number);
        private void RemovePlayer(RoomSeatView seat) => RemovePlayerRequested?.Invoke(Array.IndexOf(_members, seat));
        public void RenderLobby(int number, string name, string rules, int hands, bool locked, IReadOnlyList<string> members, bool full)
        {
            _roomTitle.text = string.Format(_roomTitleFormat, name, number);
            _roomDetails.text = string.Format(_roomDetailsFormat, rules, hands, locked ? _locked : _open);
            for (var seat = 0; seat < _members.Length; seat++)
                _members[seat].Render(string.Format(_seatFormat, seat + 1, seat < members.Count ? members[seat] : _emptySeat),
                    seat > 0 && seat < members.Count);
            _addBot.interactable = !full;
            _start.interactable = full;
        }

        private void Create() => CreateRequested?.Invoke();
        private void Join() => JoinRequested?.Invoke();
        private void Submit() => JoinSubmitted?.Invoke();
        private void Back() => BackRequested?.Invoke();
        private void AddBot() => AddBotRequested?.Invoke();
        private void StartRoom() => StartRequested?.Invoke();
        private void OnEnable()
        {
            foreach (var seat in _members) seat.RemoveRequested += RemovePlayer;
            _createButton.onClick.AddListener(Create);
            _joinButton.onClick.AddListener(Join);
            _joinSubmit.onClick.AddListener(Submit);
            _addBot.onClick.AddListener(AddBot);
            _start.onClick.AddListener(StartRoom);
            foreach (var button in _backButtons) button.onClick.AddListener(Back);
        }

        private void OnDestroy()
        {
            foreach (var item in _roomItems) item.Selected -= SelectRoom;
        }

        private void OnDisable()
        {
            foreach (var seat in _members) seat.RemoveRequested -= RemovePlayer;
            _createButton.onClick.RemoveListener(Create);
            _joinButton.onClick.RemoveListener(Join);
            _joinSubmit.onClick.RemoveListener(Submit);
            _addBot.onClick.RemoveListener(AddBot);
            _start.onClick.RemoveListener(StartRoom);
            foreach (var button in _backButtons) button.onClick.RemoveListener(Back);
        }
    }
}
