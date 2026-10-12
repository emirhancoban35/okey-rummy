using System;
using Okey.Configuration;
using Okey.Presentation.Menus;
using Zenject;
namespace Okey.Composition
{
    public sealed class NewGameMenuController : IInitializable, IDisposable
    {
        private readonly GameSetup _settings;
        private readonly MatchHistory _matches;
        private readonly NewGameMenuView _view;
        private readonly ITableNavigation _navigation;
        public NewGameMenuController(GameSetup settings, NewGameMenuView view, ITableNavigation navigation, MatchHistory matches)
        {
            _settings = settings;
            _matches = matches;
            _view = view;
            _navigation = navigation;
        }
        public void Initialize()
        {
            _view.JokerReclaimRequested += ToggleJokerReclaim;
            _view.RulesRequested += ToggleRules;
            _view.PlayRequested += StartMatch;
            _view.PartnershipRequested += TogglePartnership;
            _view.BotModeRequested += ToggleBots;
            _view.EscalatingRequested += ToggleEscalating;
            Render();
        }
        private void StartMatch() { _matches.Reset(_settings); _navigation.StartTable(); }
        private void ToggleJokerReclaim() { _settings.ToggleJokerReclaim(); Render(); }
        private void ToggleRules() { _settings.NextRules(); Render(); }
        private void TogglePartnership() { _settings.TogglePartnerships(); Render(); }
        private void ToggleBots() { _settings.ToggleBotMode(); Render(); }
        private void ToggleEscalating() { _settings.ToggleEscalatingOpenings(); Render(); }
        private void Render()
        {
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
            _view.JokerReclaimRequested -= ToggleJokerReclaim;
            _view.RulesRequested -= ToggleRules;
            _view.PlayRequested -= StartMatch;
            _view.PartnershipRequested -= TogglePartnership;
            _view.BotModeRequested -= ToggleBots;
            _view.EscalatingRequested -= ToggleEscalating;
        }
    }
}
