using System;
using Okey.Configuration;
using Okey.Core.Dealing;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using Zenject;
namespace Okey.Composition
{
    public sealed class TableOptionsController : IInitializable, IDisposable
    {
        private readonly TableSession _table;
        private readonly TableSettings _settings;
        private readonly TurnControlsView _controls;
        private readonly ITableNavigation _navigation;
        public TableOptionsController(TableSession table, TableSettings settings, TurnControlsView controls, ITableNavigation navigation)
        {
            _table = table;
            _settings = settings;
            _controls = controls;
            _navigation = navigation;
        }
        public void Initialize()
        {
            _controls.BotModeRequested += ToggleBots;
            _controls.PartnershipRequested += TogglePartners;
            _controls.EscalatingRequested += ToggleEscalating;
            _controls.NewHandRequested += NextHand;
            _table.Changed += Render;
            Render();
        }
        private void ToggleBots() { _settings.ToggleBotMode(); Render(); }
        private void ToggleEscalating() { _settings.ToggleEscalatingOpenings(); Render(); }
        private void TogglePartners() { _settings.TogglePartnerships(); Render(); }
        private void Render() => _controls.RenderOptions(_settings.HumanlikeBots, _settings.Partnerships, _table.Phase == TurnPhase.Finished, _settings.EscalatingOpenings);
        private void NextHand() => _navigation.OpenMenu();
        public void Dispose()
        {
            _controls.BotModeRequested -= ToggleBots;
            _controls.PartnershipRequested -= TogglePartners;
            _controls.EscalatingRequested -= ToggleEscalating;
            _controls.NewHandRequested -= NextHand;
            _table.Changed -= Render;
        }
    }
}
