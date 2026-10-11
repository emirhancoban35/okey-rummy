using System;
using Okey.Configuration;
using Okey.Core.Decks;
using Okey.Core.Dealing;
using Okey.Core.Stones;
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

        public override void InstallBindings()
        {
            if (_deckSettings == null || _stonePrefab == null || _openingDealSettings == null || _rackView == null || _controlsView == null)
                throw new InvalidOperationException("Deck settings, opening deal settings, stone prefab, rack view and turn controls must be assigned to the game installer.");

            Container.BindInstance(_deckSettings.CreateDefinition());
            Container.Bind<DeckFactory>().AsSingle();
            Container.Bind<IStoneShuffler>().To<SeededStoneShuffler>().AsSingle();
            Container.Bind<OpeningDealFactory>().AsSingle();
            Container.Bind<OpeningDeal>().FromMethod(CreateOpeningDeal).AsSingle();
            Container.Bind<Stone>().FromInstance(_stonePrefab).WhenInjectedInto<StoneViewFactory>();
            Container.Bind<StoneViewFactory>().AsSingle();
            Container.BindInstance(_rackView);
            Container.Bind<RackArrangement>().FromMethod(context =>
                new RackArrangement(context.Container.Resolve<OpeningDeal>().InitialHand, _rackView.Capacity)).AsSingle();
            Container.Bind<RackInteractionController>().AsSingle();
            Container.BindInstance(_controlsView);
            Container.Bind<SinglePlayerSession>().FromMethod(context => new SinglePlayerSession(
                context.Container.Resolve<RackArrangement>(), context.Container.Resolve<OpeningDeal>().DrawPile,
                _openingDealSettings.InitialPhase)).AsSingle();
            Container.BindInterfacesAndSelfTo<HandPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TurnController>().AsSingle();
            Container.BindExecutionOrder<HandPresenter>(-10);
        }

        private OpeningDeal CreateOpeningDeal(InjectContext context)
        {
            var factory = context.Container.Resolve<OpeningDealFactory>();
            var definition = context.Container.Resolve<DeckDefinition>();
            return factory.Create(definition, _openingDealSettings.InitialHandSize, _openingDealSettings.CreateSeed());
        }
    }
}
