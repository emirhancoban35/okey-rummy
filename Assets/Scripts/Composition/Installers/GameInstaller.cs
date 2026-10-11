using Okey.Presentation.Tables;
using System;
using Okey.Configuration;
using Okey.Core.Decks;
using Okey.Core.Games;
using Okey.Core.Dealing;
using Okey.Core.Stones;
using Okey.Core.Tables;
using Okey.Core.Rules;
using Okey.Presentation.Stones;
using Okey.Presentation.Racks;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private DeckSettings _deckSettings;
        [SerializeField] private Stone _stonePrefab;
        [SerializeField] private OpeningDealSettings _openingDealSettings;
        [SerializeField] private RackView _rackView;
        [SerializeField] private TableView _controlsView;
        [SerializeField] private TableSettings _tableSettings;
        [SerializeField, Min(0)] private int _localSeat;
        [SerializeField] private SceneNavigationSettings _navigationSettings;

        public override void InstallBindings()
        {
            if (_deckSettings == null || _stonePrefab == null || _openingDealSettings == null || _rackView == null || _controlsView == null || _tableSettings == null || _navigationSettings == null)
                throw new InvalidOperationException("Deck settings, opening deal settings, stone prefab, rack view, turn controls, table settings and navigation settings must be assigned to the game installer.");

            Container.BindInstance(_deckSettings.CreateDefinition());
            Container.Bind<DeckFactory>().AsSingle();
            Container.Bind<IStoneShuffler>().To<SeededStoneShuffler>().AsSingle();
            var flow = GameFlowState.Resolve(_tableSettings);
            var setup = flow.Setup;
            Container.BindInstance(setup);
            Container.BindInstance(new LocalPlayerContext(_localSeat, setup.PlayerCount));
            Container.BindInstance(setup.CreateBotPreferences());
            Container.BindInstance(_navigationSettings);
            Container.Bind<ITableNavigation>().To<SceneTableNavigation>().AsSingle();
            Container.BindInstance(setup.CreateRules());
            Container.Bind<MeldPlanner>().AsSingle();
            Container.Bind<RackMeldEvaluator>().AsSingle();
            Container.Bind<IBotDecisionPolicy>().FromMethod(context => setup.ActiveProfile.CreateBotDecisions(
                context.Container.Resolve<IPlayerStrategy>(), context.Container.Resolve<IRandomSource>())).AsSingle();
            Container.Bind<IRandomSource>().To<SeededRandomSource>().AsSingle().WithArguments(_openingDealSettings.CreateSeed());
            Container.Bind<ITableGame>().FromMethod(context => setup.ActiveProfile.CreateGame(context.Container.Resolve<MeldPlanner>())).AsSingle();
            Container.Bind<IPlayerStrategy>().FromMethod(context => setup.ActiveProfile.CreateStrategy(context.Container.Resolve<MeldPlanner>())).AsSingle();
            Container.Bind<TableSession>().FromMethod(context => new TableSession(
                context.Container.Resolve<OkeyRules>(), context.Container.Resolve<IStoneShuffler>().Shuffle(
                    context.Container.Resolve<DeckFactory>().Create(context.Container.Resolve<DeckDefinition>()),
                    _openingDealSettings.CreateSeed()), _rackView.Capacity, startingSeat: flow.StartingSeats.Next(setup.PlayerCount, setup.RandomStartingSeat),
                    options: setup.CreateOptions(), game: context.Container.Resolve<ITableGame>())).AsSingle();
            Container.Bind<Stone>().FromInstance(_stonePrefab).WhenInjectedInto<StoneViewFactory>();
            Container.Bind<StoneViewFactory>().AsSingle();
            Container.BindInstance(_rackView);
            Container.Bind<IRackLayout>().FromMethod(context =>
                context.Container.Resolve<TableSession>().Players[_localSeat].Layout).AsSingle();
            Container.Bind<IReadOnlyRack>().FromMethod(context => context.Container.Resolve<IRackLayout>()).AsSingle();
            Container.Bind<RackInteractionController>().AsSingle();
            Container.BindInstance(_controlsView);
            Container.BindInterfacesAndSelfTo<HandPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TablePresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TableHudPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TurnController>().AsSingle();
            Container.BindInterfacesAndSelfTo<BotTurnController>().AsSingle();
            Container.BindInterfacesAndSelfTo<TurnClockController>().AsSingle();
            Container.BindInterfacesAndSelfTo<TableOptionsController>().AsSingle();
            Container.BindExecutionOrder<HandPresenter>(-10);
            Container.BindExecutionOrder<TablePresenter>(-5);
            Container.BindExecutionOrder<TableHudPresenter>(-2);
        }

    }
}
