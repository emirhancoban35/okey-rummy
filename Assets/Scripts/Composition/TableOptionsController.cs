using System;
using Okey.Configuration;
using Okey.Core.Dealing;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using UnityEngine.SceneManagement;
using Zenject;
namespace Okey.Composition
{
    public sealed class TableOptionsController : IInitializable, IDisposable
    {
        private readonly TableSession _table;
        private readonly TableSettings _settings;
        private readonly TurnControlsView _controls;
        private bool _loading;
        public TableOptionsController(TableSession table, TableSettings settings, TurnControlsView controls)
        {
            _table = table;
            _settings = settings;
            _controls = controls;
        }
        public void Initialize()
        {
            _controls.BotModeRequested += ToggleBots;
            _controls.PartnershipRequested += TogglePartners;
            _controls.NewHandRequested += NextHand;
            _table.Changed += Render;
            Render();
        }
        private void ToggleBots() { _settings.ToggleBotMode(); Render(); }
        private void TogglePartners() { _settings.TogglePartnerships(); Render(); }
        private void Render() => _controls.RenderOptions(_settings.HumanlikeBots, _settings.Partnerships, _table.Phase == TurnPhase.Finished);
        private void NextHand()
        {
            if (_loading) return;
            _loading = true;
            SceneManager.LoadSceneAsync(_controls.gameObject.scene.name, LoadSceneMode.Single);
        }
        public void Dispose()
        {
            _controls.BotModeRequested -= ToggleBots;
            _controls.PartnershipRequested -= TogglePartners;
            _controls.NewHandRequested -= NextHand;
            _table.Changed -= Render;
        }
    }
}
