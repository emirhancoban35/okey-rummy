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
            _view.PlayRequested += _navigation.StartTable;
            _view.PartnershipRequested += TogglePartnership;
            _view.BotModeRequested += ToggleBots;
            _view.EscalatingRequested += ToggleEscalating;
            Render();
        }
        private void TogglePartnership() { _settings.TogglePartnerships(); Render(); }
        private void ToggleBots() { _settings.ToggleBotMode(); Render(); }
        private void ToggleEscalating() { _settings.ToggleEscalatingOpenings(); Render(); }
        private void Render() => _view.Render(_settings.Partnerships, _settings.HumanlikeBots, _settings.EscalatingOpenings);
        public void Dispose()
        {
            _view.PlayRequested -= _navigation.StartTable;
            _view.PartnershipRequested -= TogglePartnership;
            _view.BotModeRequested -= ToggleBots;
            _view.EscalatingRequested -= ToggleEscalating;
        }
    }
}
