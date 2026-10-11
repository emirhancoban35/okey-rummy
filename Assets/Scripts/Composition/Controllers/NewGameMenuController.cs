using System;
using Okey.Configuration;
using Okey.Presentation.Menus;
using Zenject;
namespace Okey.Composition
{
    public sealed class NewGameMenuController : IInitializable, IDisposable
    {
        private readonly TableSettings _settings;
        private readonly NewGameMenuView _view;
        private readonly ITableNavigation _navigation;
        public NewGameMenuController(TableSettings settings, NewGameMenuView view, ITableNavigation navigation)
        {
            _settings = settings;
            _view = view;
            _navigation = navigation;
        }
        public void Initialize()
        {
            _view.RulesRequested += ToggleRules;
            _view.PlayRequested += _navigation.StartTable;
            _view.PartnershipRequested += TogglePartnership;
            _view.BotModeRequested += ToggleBots;
            _view.EscalatingRequested += ToggleEscalating;
            Render();
        }
        private void ToggleRules() { _settings.NextRules(); Render(); }
        private void TogglePartnership() { _settings.TogglePartnerships(); Render(); }
        private void ToggleBots() { _settings.ToggleBotMode(); Render(); }
        private void ToggleEscalating() { _settings.ToggleEscalatingOpenings(); Render(); }
        private void Render()
        {
            var text = _view.Texts;
            _view.Render(_settings.ActiveProfile.Label,
                _settings.Partnerships ? text.MenuPartners : text.MenuIndividual,
                _settings.HumanlikeBots ? text.MenuHumanlike : text.MenuFast,
                !_settings.ActiveProfile.AllowsPublicMelds ? text.MenuUnavailable : _settings.EscalatingOpenings ? text.MenuRising : text.MenuFixed,
                _settings.ActiveProfile.AllowsPublicMelds);
        }
        public void Dispose()
        {
            _view.RulesRequested -= ToggleRules;
            _view.PlayRequested -= _navigation.StartTable;
            _view.PartnershipRequested -= TogglePartnership;
            _view.BotModeRequested -= ToggleBots;
            _view.EscalatingRequested -= ToggleEscalating;
        }
    }
}
