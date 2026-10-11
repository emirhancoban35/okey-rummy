using System;
using Okey.Core.Dealing;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class TurnController : IInitializable, IDisposable
    {
        private readonly SinglePlayerSession _session;
        private readonly HandPresenter _hand;
        private readonly RackInteractionController _interaction;
        private readonly TurnControlsView _controls;
        private readonly StoneViewFactory _factory;
        private Stone _discardView;
        private bool _initialized;
        private bool _disposed;

        public TurnController(SinglePlayerSession session, HandPresenter hand, RackInteractionController interaction,
            TurnControlsView controls, StoneViewFactory factory)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _hand = hand ?? throw new ArgumentNullException(nameof(hand));
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _controls = controls != null ? controls : throw new ArgumentNullException(nameof(controls));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TurnController));
            if (_initialized)
                return;
            _initialized = true;
            _controls.DrawRequested += Draw;
            _controls.DiscardRequested += DiscardSelected;
            _interaction.Changed += Render;
            Render();
        }

        public void Draw()
        {
            if (_disposed || _interaction.IsDragging || !_session.TryDraw(out _))
                return;
            _hand.Refresh();
            Render();
        }

        public void DiscardSelected()
        {
            var selected = _interaction.Selected;
            if (_disposed || _interaction.IsDragging || selected == null ||
                !_session.TryDiscard(selected.Instance.Id, out var stone))
                return;

            var origin = selected.transform.position;
            if (_discardView == null)
                _discardView = _factory.Create(stone, _controls.DiscardRoot);
            else
                _discardView.Initialize(stone);
            _discardView.DragHandler.enabled = false;
            _discardView.Selection.SetSelected(false);
            _discardView.SetFace(true);
            _discardView.transform.localScale = _controls.DiscardScale;
            _discardView.Motion.SnapTo(_controls.DiscardRoot.InverseTransformPoint(origin));
            _discardView.Motion.MoveTo(Vector3.zero);
            _hand.Refresh();
            Render();
        }

        private void Render()
        {
            _controls.Render(_session.Phase, _session.Hand.Count, _session.RemainingCount,
                !_interaction.IsDragging && _session.CanDraw,
                !_interaction.IsDragging && _session.Phase == TurnPhase.AwaitingDiscard && _interaction.Selected != null);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _controls.DrawRequested -= Draw;
            _controls.DiscardRequested -= DiscardSelected;
            _interaction.Changed -= Render;
            if (_discardView != null)
            {
                _discardView.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_discardView.gameObject);
            }
        }
    }
}
