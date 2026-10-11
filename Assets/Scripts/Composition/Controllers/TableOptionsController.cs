using Okey.Presentation.Tables;
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
        private readonly TableView _controls;
        private readonly ITableNavigation _navigation;
        public TableOptionsController(TableSession table, TableSettings settings, TableView controls, ITableNavigation navigation)
        {
            _table = table;
            _settings = settings;
            _controls = controls;
            _navigation = navigation;
        }
        public void Initialize()
        {
            _controls.Options.BotModeRequested += ToggleBots;
            _controls.Options.PartnershipRequested += TogglePartners;
            _controls.Options.EscalatingRequested += ToggleEscalating;
            _controls.Options.NewHandRequested += NextHand;
            _table.Changed += Render;
            Render();
        }
        private void ToggleBots() { _settings.ToggleBotMode(); Render(); }
        private void ToggleEscalating() { _settings.ToggleEscalatingOpenings(); Render(); }
        private void TogglePartners() { _settings.TogglePartnerships(); Render(); }
        private void Render()
        {
            var text = _controls.Texts;
            _controls.Options.Render(_settings.HumanlikeBots ? text.HumanlikeBotLabel : text.FastBotLabel,
                _settings.Partnerships ? text.NextPartnerLabel : text.NextIndividualLabel,
                _settings.EscalatingOpenings ? text.EscalatingOnLabel : text.EscalatingOffLabel, _table.Game.AllowsPublicMelds);
        }
        private void NextHand() => _navigation.OpenMenu();
        public void Dispose()
        {
            _controls.Options.BotModeRequested -= ToggleBots;
            _controls.Options.PartnershipRequested -= TogglePartners;
            _controls.Options.EscalatingRequested -= ToggleEscalating;
            _controls.Options.NewHandRequested -= NextHand;
            _table.Changed -= Render;
        }
    }
}
