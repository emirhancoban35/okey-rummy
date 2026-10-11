using System;
using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class HandPresenter : IInitializable, IDisposable
    {
        private readonly RackArrangement _hand;
        private readonly StoneViewFactory _factory;
        private readonly RackView _rack;
        private readonly RackInteractionController _interaction;
        private readonly List<Stone> _views;
        private readonly Dictionary<int, Stone> _viewById = new Dictionary<int, Stone>();
        private bool _initialized;
        private bool _disposed;

        public IReadOnlyList<Stone> Views { get; }

        public HandPresenter(RackArrangement hand, StoneViewFactory factory, RackView rack,
            RackInteractionController interaction)
        {
            _hand = hand ?? throw new ArgumentNullException(nameof(hand));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _rack = rack != null ? rack : throw new ArgumentNullException(nameof(rack));
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _views = new List<Stone>(hand.Count);
            Views = _views.AsReadOnly();
        }

        public void Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HandPresenter));

            if (_initialized)
                return;

            Refresh();
            _initialized = true;
        }

        public void Refresh()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HandPresenter));
            _rack.ValidateCapacity(_hand.Count);
            for (var index = _views.Count - 1; index >= 0; index--)
            {
                var view = _views[index];
                if (_hand.FindSlot(view.Instance.Id) >= 0)
                    continue;
                _interaction.Unregister(view);
                _viewById.Remove(view.Instance.Id);
                _views.RemoveAt(index);
                view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(view.gameObject);
            }

            try
            {
                for (var index = 0; index < _hand.Capacity; index++)
                {
                    var stone = _hand.Slots[index];
                    if (stone == null || _viewById.ContainsKey(stone.Id))
                        continue;
                    var view = _factory.Create(stone, _rack.StoneRoot);
                    _views.Add(view);
                    _viewById.Add(stone.Id, view);
                    _rack.Place(view, index, true, !_initialized);
                    _interaction.Register(view);
                }
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
            _interaction.Clear();
            foreach (var view in _views)
            {
                if (view == null)
                    continue;

                view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(view.gameObject);
            }

            _views.Clear();
            _viewById.Clear();
        }
    }
}
