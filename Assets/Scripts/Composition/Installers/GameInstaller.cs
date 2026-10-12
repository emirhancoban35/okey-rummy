using Okey.Presentation.Tables;
using System;
using System.Linq;
using Okey.Configuration;
using Okey.Core.Decks;
using Okey.Core.Games;
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
        [SerializeField] private Camera _tableCamera;
        [SerializeField] private ScoreboardBinding[] _scoreboards;
        [SerializeField] private SeatColorSettings _seatColors;
        [SerializeField] private Stone _stonePrefab;
        [Inject] private ShuffleSettings _shuffleSettings;
        [Inject] private GameFlowState _flow;
        [SerializeField] private RackView _rackView;
        [SerializeField] private TableView _controlsView;
        [SerializeField, Min(0)] private int _localSeat;

        public override void InstallBindings()
        {
            if (_tableCamera == null || _deckSettings == null || _stonePrefab == null || _rackView == null || _controlsView == null)
                throw new InvalidOperationException("Table camera, deck settings, stone prefab, rack view, table controls must be assigned to the game installer.");

            var seed = _shuffleSettings.CreateSeed();
            Container.BindInstance(_deckSettings.CreateDefinition());
            Container.Bind<DeckFactory>().AsSingle();
            Container.Bind<IStoneShuffler>().To<SeededStoneShuffler>().AsSingle();
            Container.BindInstance(_flow.Rooms);
            var setup = _flow.Setup;
            Container.BindInstance(setup);
            Container.BindInstance(_flow.Matches.For(setup));
            Container.BindInstance(_seatColors);
            Container.BindInstance(new LocalPlayerContext(_localSeat, setup.PlayerCount));
            Container.BindInstance(setup.CreateBotPreferences());
            Container.Bind<ITableNavigation>().To<SceneTableNavigation>().AsSingle();
            Container.BindInstance(setup.CreateRules());
            Container.Bind<MeldPlanner>().AsSingle();
            Container.Bind<TimeoutDiscardPolicy>().AsSingle();
            Container.Bind<RackMeldEvaluator>().AsSingle();
            Container.Bind<IBotDecisionPolicy>().FromMethod(context => setup.ActiveProfile.CreateBotDecisions(
                context.Container.Resolve<IPlayerStrategy>(), context.Container.Resolve<IRandomSource>())).AsSingle();
            Container.Bind<IRandomSource>().To<SeededRandomSource>().AsSingle().WithArguments(seed);
            Container.Bind<ITableGame>().FromMethod(context => setup.ActiveProfile.CreateGame(context.Container.Resolve<MeldPlanner>())).AsSingle();
            Container.Bind<IPlayerStrategy>().FromMethod(context => setup.ActiveProfile.CreateStrategy(context.Container.Resolve<MeldPlanner>())).AsSingle();
            Container.Bind<TableSession>().FromMethod(context => new TableSession(
                context.Container.Resolve<OkeyRules>(), context.Container.Resolve<IStoneShuffler>().Shuffle(
                    context.Container.Resolve<DeckFactory>().Create(context.Container.Resolve<DeckDefinition>()),
                    seed), _rackView.Capacity, startingSeat: _flow.StartingSeats.Next(setup.PlayerCount, setup.RandomStartingSeat),
                    options: setup.CreateOptions(), game: context.Container.Resolve<ITableGame>())).AsSingle();
            Container.Bind<Stone>().FromInstance(_stonePrefab).WhenInjectedInto<StoneViewFactory>();
            Container.Bind<StoneViewFactory>().AsSingle();
            Container.BindInstance(_rackView);
            Container.Bind<IRackLayout>().FromMethod(context =>
                context.Container.Resolve<TableSession>().Players[_localSeat].Layout).AsSingle();
            Container.Bind<IReadOnlyRack>().FromMethod(context => context.Container.Resolve<IRackLayout>()).AsSingle();
            Container.Bind<RackInteractionController>().AsSingle();
            var scorePrefab = _scoreboards.Single(binding => binding.Profile == setup.ActiveProfile).Prefab;
            var scores = Container.InstantiatePrefabForComponent<ScoreboardView>(scorePrefab, _controlsView.Canvas.transform);
            Container.BindInstance(scores);
            Container.BindInterfacesAndSelfTo<ScoreboardPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TableExperiencePresenter>().AsSingle();
            Container.BindInstance(_controlsView);
            Container.BindInstance(_tableCamera);
            Container.BindInstance(_controlsView.Players);
            Container.Bind<TablePlayerNames>().AsSingle();
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
