using System;
using Okey.Configuration;
using Okey.Core.Decks;
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
        [SerializeField] private TurnControlsView _controlsView;
        [SerializeField] private TableSettings _tableSettings;

        public override void InstallBindings()
        {
            if (_deckSettings == null || _stonePrefab == null || _openingDealSettings == null || _rackView == null || _controlsView == null || _tableSettings == null)
                throw new InvalidOperationException("Deck settings, opening deal settings, stone prefab, rack view, turn controls and table settings must be assigned to the game installer.");

            Container.BindInstance(_deckSettings.CreateDefinition());
            Container.Bind<DeckFactory>().AsSingle();
            Container.Bind<IStoneShuffler>().To<SeededStoneShuffler>().AsSingle();
            Container.Bind<OpeningDealFactory>().AsSingle();
            Container.BindInstance(_tableSettings);
            Container.BindInstance(_tableSettings.CreateRules());
            Container.Bind<MeldPlanner>().AsSingle();
            Container.Bind<IRandomSource>().To<SeededRandomSource>().AsSingle().WithArguments(_openingDealSettings.CreateSeed());
            Container.Bind<IPlayerStrategy>().To<MeldPlayerStrategy>().AsSingle();
            Container.Bind<TableSession>().FromMethod(context => new TableSession(
                context.Container.Resolve<OkeyRules>(), context.Container.Resolve<IStoneShuffler>().Shuffle(
                    context.Container.Resolve<DeckFactory>().Create(context.Container.Resolve<DeckDefinition>()),
                    _openingDealSettings.CreateSeed()), _rackView.Capacity, startingSeat: _tableSettings.NextStartingSeat(),
                    options: _tableSettings.CreateOptions())).AsSingle();
            Container.Bind<Stone>().FromInstance(_stonePrefab).WhenInjectedInto<StoneViewFactory>();
            Container.Bind<StoneViewFactory>().AsSingle();
            Container.BindInstance(_rackView);
            Container.Bind<RackArrangement>().FromMethod(context =>
                context.Container.Resolve<TableSession>().Players[0].Hand).AsSingle();
            Container.Bind<RackInteractionController>().AsSingle();
            Container.BindInstance(_controlsView);
            Container.BindInterfacesAndSelfTo<HandPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TablePresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TurnController>().AsSingle();
            Container.BindInterfacesAndSelfTo<BotTurnController>().AsSingle();
            Container.BindInterfacesAndSelfTo<TurnClockController>().AsSingle();
            Container.BindInterfacesAndSelfTo<TableOptionsController>().AsSingle();
            Container.BindExecutionOrder<HandPresenter>(-10);
            Container.BindExecutionOrder<TablePresenter>(-5);
        }

    }
}
