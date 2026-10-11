using System;
using Okey.Configuration;
using UnityEngine.SceneManagement;
namespace Okey.Composition
{
    public sealed class SceneTableNavigation : ITableNavigation
    {
        private readonly SceneNavigationSettings _settings;
        private bool _loading;
        public SceneTableNavigation(SceneNavigationSettings settings) => _settings = settings;
        public void OpenMenu() => Load(_settings.MenuScene);
        public void StartTable() => Load(_settings.TableScene);
        private void Load(string scene)
        {
            if (_loading) return;
            if (string.IsNullOrWhiteSpace(scene) || !UnityEngine.Application.CanStreamedLevelBeLoaded(scene))
                throw new InvalidOperationException("The navigation scene must be assigned and included in Build Settings.");
            _loading = true;
            try { SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single); }
            catch { _loading = false; throw; }
        }
    }
}
