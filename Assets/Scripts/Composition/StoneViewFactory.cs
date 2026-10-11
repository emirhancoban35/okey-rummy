using System;
using Okey.Core.Stones;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class StoneViewFactory
    {
        private readonly DiContainer _container;
        private readonly Stone _prefab;

        public StoneViewFactory(DiContainer container, Stone prefab)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public Stone Create(StoneInstance stone, Transform parent)
        {
            if (stone == null)
                throw new ArgumentNullException(nameof(stone));

            var view = _container.InstantiatePrefabForComponent<Stone>(_prefab, parent);
            view.Initialize(stone);
            return view;
        }
    }
}
