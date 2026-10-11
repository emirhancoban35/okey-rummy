using System;
using System.Collections.Generic;
using Okey.Core.Dealing;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class OpeningHandPresenter : IInitializable, IDisposable
    {
        private readonly OpeningDeal _deal;
        private readonly StoneViewFactory _factory;
        private readonly RackView _rack;
        private readonly List<Stone> _views;
        private bool _initialized;
        private bool _disposed;

        public IReadOnlyList<Stone> Views { get; }

        public OpeningHandPresenter(OpeningDeal deal, StoneViewFactory factory, RackView rack)
        {
            _deal = deal ?? throw new ArgumentNullException(nameof(deal));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _rack = rack != null ? rack : throw new ArgumentNullException(nameof(rack));
            _views = new List<Stone>(deal.InitialHand.Count);
            Views = _views.AsReadOnly();
        }

        public void Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OpeningHandPresenter));

            if (_initialized)
                return;

            _rack.ValidateCapacity(_deal.InitialHand.Count);
            try
            {
                for (var index = 0; index < _deal.InitialHand.Count; index++)
                {
                    var view = _factory.Create(_deal.InitialHand[index], _rack.StoneRoot);
                    _views.Add(view);
                    _rack.Place(view, index, true);
                }

                _initialized = true;
            }
            catch
            {
                ClearViews();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearViews();
        }

        private void ClearViews()
        {
            foreach (var view in _views)
            {
                if (view == null)
                    continue;

                view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(view.gameObject);
            }

            _views.Clear();
        }
    }
}
