using System;
using Okey.Presentation.Menus;
using UnityEngine;
using Zenject;
namespace Okey.Composition
{
    public sealed class NewGameMenuInstaller : MonoInstaller
    {
        [Inject] private GameFlowState _flow;
        [SerializeField] private NewGameMenuView _menu;
        [SerializeField] private RoomMenuView _rooms;
        public override void InstallBindings()
        {
            if (_menu == null || _rooms == null)
                throw new InvalidOperationException("Menu and room views must be assigned.");
            Container.BindInstance(_flow.Rooms);
            Container.BindInstance(_flow.Setup);
            Container.BindInstance(_flow.Matches);
            Container.BindInstance(_menu);
            Container.BindInstance(_rooms);
            Container.Bind<ITableNavigation>().To<SceneTableNavigation>().AsSingle();
            Container.BindInterfacesAndSelfTo<NewGameMenuController>().AsSingle();
            Container.BindInterfacesAndSelfTo<RoomMenuController>().AsSingle();
        }
    }
}
