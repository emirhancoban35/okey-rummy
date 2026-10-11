using System;
using Okey.Configuration;
using Okey.Core.Decks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private DeckSettings _deckSettings;
        [SerializeField] private Stone _stonePrefab;

        public override void InstallBindings()
        {
            if (_deckSettings == null || _stonePrefab == null)
                throw new InvalidOperationException("Deck settings and a stone prefab must be assigned to the game installer.");

            Container.BindInstance(_deckSettings.CreateDefinition());
            Container.Bind<DeckFactory>().AsSingle();
            Container.Bind<Stone>().FromInstance(_stonePrefab).WhenInjectedInto<StoneViewFactory>();
            Container.Bind<StoneViewFactory>().AsSingle();
        }
    }
}
