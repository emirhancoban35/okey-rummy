using Okey.Configuration;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private TableSettings _tableSettings;
        [SerializeField] private ShuffleSettings _shuffleSettings;
        [SerializeField] private SceneNavigationSettings _navigationSettings;

        public override void InstallBindings()
        {
            Container.BindInstance(_tableSettings);
            Container.BindInstance(_tableSettings.Rooms);
            Container.BindInstance(_shuffleSettings);
            Container.BindInstance(_navigationSettings);
            Container.Bind<GameFlowState>().AsSingle();
        }
    }
}
