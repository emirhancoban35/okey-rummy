using System;
using Okey.Configuration;
using Okey.Presentation.Menus;
using Zenject;
namespace Okey.Composition
{
    public sealed class NewGameMenuController : IInitializable, IDisposable
    {
        private readonly GameSetup _settings;
        private readonly RoomCatalog _rooms;
        private readonly NewGameMenuView _view;
        public NewGameMenuController(GameSetup settings, NewGameMenuView view, RoomCatalog rooms)
        {
            _settings = settings;
            _rooms = rooms;
            _view = view;
        }
        public void Initialize()
        {
            _settings.Changed += Render;
            _view.JokerReclaimRequested += ToggleJokerReclaim;
            _view.RulesRequested += ToggleRules;
            _view.HandsRequested += NextHands;
            _view.PartnershipRequested += TogglePartnership;
            _view.BotModeRequested += ToggleBots;
            _view.EscalatingRequested += ToggleEscalating;
            Render();
        }
        private void NextHands() { _settings.SetHands(_rooms.NextHands(_settings.MatchRounds)); }
        private void ToggleJokerReclaim() { _settings.ToggleJokerReclaim(); }
        private void ToggleRules() { _settings.NextRules(); }
        private void TogglePartnership() { _settings.TogglePartnerships(); }
        private void ToggleBots() { _settings.ToggleBotMode(); }
        private void ToggleEscalating() { _settings.ToggleEscalatingOpenings(); }
        private void Render()
        {
            _view.RenderHands(_settings.MatchRounds);
            var text = _view.Texts;
            var publicMelds = _settings.ActiveProfile.AllowsPublicMelds;
            _view.RenderJokerReclaim(!publicMelds ? text.MenuUnavailable :
                _settings.AllowJokerReclaim ? text.JokerReclaimOn : text.JokerReclaimOff, publicMelds);
            _view.Render(_settings.ActiveProfile.Label,
                _settings.Partnerships ? text.MenuPartners : text.MenuIndividual,
                _settings.HumanlikeBots ? text.MenuHumanlike : text.MenuFast,
                !_settings.ActiveProfile.AllowsPublicMelds ? text.MenuUnavailable : _settings.EscalatingOpenings ? text.MenuRising : text.MenuFixed,
                _settings.ActiveProfile.AllowsPublicMelds);
        }
        public void Dispose()
        {
            _settings.Changed -= Render;
            _view.JokerReclaimRequested -= ToggleJokerReclaim;
            _view.RulesRequested -= ToggleRules;
            _view.HandsRequested -= NextHands;
            _view.PartnershipRequested -= TogglePartnership;
            _view.BotModeRequested -= ToggleBots;
            _view.EscalatingRequested -= ToggleEscalating;
        }
    }
}
