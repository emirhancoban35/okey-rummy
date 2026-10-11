using Okey.Presentation.Tables;
using System;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class TurnController : IInitializable, IDisposable
    {
        private readonly TableSession _session;
        private readonly HandPresenter _hand;
        private readonly RackInteractionController _interaction;
        private readonly TableView _controls;
        private readonly TablePresenter _table;
        private readonly RackMeldEvaluator _evaluator;
        private readonly RackView _rack;
        private Stone _drawnDragView;
        private bool _initialized;
        private bool _disposed;

        public TurnController(TableSession session, HandPresenter hand, RackInteractionController interaction,
            TableView controls, TablePresenter table, RackMeldEvaluator evaluator, RackView rack)
        {
            _session = session;
            _hand = hand;
            _interaction = interaction;
            _controls = controls;
            _table = table;
            _evaluator = evaluator;
            _rack = rack;
        }

        public void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TurnController));
            if (_initialized) return;
            _initialized = true;
            _controls.Surface.DrawSource.DrawRequested += Draw;
            foreach (var area in _controls.Surface.DiscardAreas) area.Clicked += TakeDiscard;
            _controls.Surface.DrawSource.DragStarted += BeginDrawDrag;
            _controls.Surface.DrawSource.Dragged += DragDraw;
            _controls.Surface.DrawSource.DragEnded += EndDrawDrag;
            _controls.Surface.DrawSource.DragCancelled += CancelDrawDrag;
            _controls.SeriesAction.Requested += OpenSeries;
            _controls.PairAction.Requested += OpenPairs;
            _interaction.DropRequested += Drop;
            _interaction.DiscardRequested += Discard;
            _interaction.DragMoved += PreviewLayOff;

        }

        private void TakeDiscard(DiscardAreaView area)
        {
            if (_disposed || _interaction.IsDragging || area != _controls.Surface.DiscardAreas[_session.Players.Count - 1]) return;
            if (!_session.Game.RequiresOpenDiscardPickup && _session.TryTakeDiscard(0)) return;
            for (var meld = 0; meld < _session.Melds.Count; meld++)
                if (_session.TryTakeDiscard(0, meld)) return;
        }

        public void Draw()
        {
            if (!_disposed && !_interaction.IsDragging && _session.TryDraw(0, out var stone))
                _hand.AnimateDraw(stone.Id, _controls.Surface.DrawSource.transform.position);
        }

        private bool BeginDrawDrag(Vector3 position)
        {
            if (_disposed || _interaction.IsDragging || !_session.TryDraw(0, out var stone)) return false;
            _drawnDragView = _hand.Views.Single(view => view.Instance.Id == stone.Id);
            _interaction.TryBeginDrag(_drawnDragView);
            _interaction.DragTo(_drawnDragView, position);
            return true;
        }

        private void DragDraw(Vector3 position)
        {
            if (_drawnDragView != null) _interaction.DragTo(_drawnDragView, position);
        }

        private void EndDrawDrag(Vector3 position)
        {
            var view = _drawnDragView;
            _drawnDragView = null;
            if (view != null) _interaction.EndDrag(view, position);
        }

        private void CancelDrawDrag()
        {
            var view = _drawnDragView;
            _drawnDragView = null;
            if (view != null) _interaction.CancelDrag(view);
        }

        public void OpenSeries() => Open(OpeningMode.Series);
        public void OpenPairs() => Open(OpeningMode.Pairs);

        private void Open(OpeningMode mode)
        {
            if (_disposed || _interaction.IsDragging) return;
            if (!_session.Game.AllowsPublicMelds)
            {
                if (_session.TryFindFinish(0, out var discardId)) TryDiscard(_hand.Views.Single(view => view.Instance.Id == discardId));
                return;
            }
            var plan = _evaluator.Find(_session.Players[0].Hand.Slots, _rack.RowCapacity, _session.MeldRules, mode);
            var before = _session.Players[0].Penalty;
            var opened = _session.TryOpen(0, mode, plan.Melds);
            var penalty = _session.Players[0].Penalty - before;
            if (opened || penalty > 0) _controls.Feedback.Show(opened ? _controls.Texts.OpeningSuccessText :
                string.Format(_controls.Texts.OpeningFailureFormat, penalty), opened);
        }

        private void Discard(Stone stone)
        {
            if (!_interaction.IsDragging) TryDiscard(stone);
        }

        private bool TryDiscard(Stone stone)
        {
            if (_disposed || stone == null || stone.Instance == null) return false;
            var origin = _controls.Surface.DiscardAreas[0].StoneRoot.InverseTransformPoint(stone.transform.position);
            if (!_session.TryDiscard(0, stone.Instance.Id)) return false;
            _table.AnimateDiscard(0, origin);
            return true;
        }

        private void PreviewLayOff(Stone stone, Vector3 position)
        {
            _interaction.SetDragScale(stone, _table.PreviewLayOff(stone.Instance, position, out var scale) ? scale : null);
        }

        private bool Drop(Stone stone, Vector3 position)
        {
            _table.ClearPreview();
            if (_disposed) return false;
            if (_controls.Surface.DiscardAreas[0].Contains(position)) return TryDiscard(stone);
            var origin = stone.transform.position;
            if (!_table.TryFindMeld(stone.Instance, position, out var index) || !_session.TryLayOff(0, stone.Instance.Id, index)) return false;
            _table.AnimateLayOff(stone.Instance.Id, origin);
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelDrawDrag();
            _controls.Surface.DrawSource.DrawRequested -= Draw;
            foreach (var area in _controls.Surface.DiscardAreas) area.Clicked -= TakeDiscard;
            _controls.Surface.DrawSource.DragStarted -= BeginDrawDrag;
            _controls.Surface.DrawSource.Dragged -= DragDraw;
            _controls.Surface.DrawSource.DragEnded -= EndDrawDrag;
            _controls.Surface.DrawSource.DragCancelled -= CancelDrawDrag;
            _controls.SeriesAction.Requested -= OpenSeries;
            _controls.PairAction.Requested -= OpenPairs;
            _interaction.DropRequested -= Drop;
            _interaction.DiscardRequested -= Discard;
            _interaction.DragMoved -= PreviewLayOff;

        }
    }
}
