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

        public override void InstallBindings()
        {
            if (_deckSettings == null || _stonePrefab == null || _openingDealSettings == null || _rackView == null)
                throw new InvalidOperationException("Deck settings, opening deal settings a stone prefab and a rack view must be assigned to the game installer.");

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
            Container.BindInterfacesAndSelfTo<OpeningHandPresenter>().AsSingle();
        }

        private OpeningDeal CreateOpeningDeal(InjectContext context)
        {
            var factory = context.Container.Resolve<OpeningDealFactory>();
            var definition = context.Container.Resolve<DeckDefinition>();
            return factory.Create(definition, _openingDealSettings.InitialHandSize, _openingDealSettings.CreateSeed());
        }
    }
}
