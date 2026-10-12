using System;
using Okey.Configuration;
using Okey.Presentation.Menus;
using UnityEngine;
using Zenject;
namespace Okey.Composition
{
    public sealed class NewGameMenuInstaller : MonoInstaller
    {
        [SerializeField] private TableSettings _tableSettings;
        [SerializeField] private SceneNavigationSettings _navigationSettings;
        [SerializeField] private NewGameMenuView _menu;
        public override void InstallBindings()
        {
            if (_tableSettings == null || _navigationSettings == null || _menu == null)
                throw new InvalidOperationException("Menu, table settings and navigation settings must be assigned.");
            var flow = GameFlowState.Resolve(_tableSettings);
            Container.BindInstance(flow.Setup);
            Container.BindInstance(flow.Matches);
            Container.BindInstance(_navigationSettings);
            Container.BindInstance(_menu);
            Container.Bind<ITableNavigation>().To<SceneTableNavigation>().AsSingle();
            Container.BindInterfacesAndSelfTo<NewGameMenuController>().AsSingle();
        }
    }
}
