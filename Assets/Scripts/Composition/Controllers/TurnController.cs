using Okey.Presentation.Tables;
using System;
using System.Linq;
using System.Collections.Generic;
using Okey.Core.Games;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class TurnController : IInitializable, IDisposable
    {
        private readonly LocalPlayerContext _local;
        private readonly TableSession _session;
        private readonly HandPresenter _hand;
        private readonly RackInteractionController _interaction;
        private readonly TableView _controls;
        private readonly TablePresenter _table;
        private readonly List<Action> _actionHandlers = new List<Action>();
        private readonly RackView _rack;
        private Stone _drawnDragView;
        private bool _initialized;
        private bool _disposed;

        public TurnController(TableSession session, HandPresenter hand, RackInteractionController interaction,
            TableView controls, TablePresenter table, RackView rack, LocalPlayerContext local)
        {
            _session = session;
            _local = local;
            _hand = hand;
            _interaction = interaction;
            _controls = controls;
            _table = table;
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
            for (var index = 0; index < _controls.Actions.Count; index++)
            {
                var actionIndex = index;
                Action handler = () => ExecuteAction(actionIndex);
                _actionHandlers.Add(handler);
                _controls.Actions[index].Requested += handler;
            }
            _interaction.DropRequested += Drop;
            _interaction.DiscardRequested += Discard;
            _interaction.DragMoved += PreviewLayOff;

        }

        private void TakeDiscard(DiscardAreaView area)
        {
            if (_disposed || _interaction.IsDragging || area != _controls.Surface.DiscardAreas[_session.Players.Count - 1]) return;
            if (!_session.Game.RequiresOpenDiscardPickup && _session.TryTakeDiscard(_local.Seat)) return;
            for (var meld = 0; meld < _session.Melds.Count; meld++)
                if (_session.TryTakeDiscard(_local.Seat, meld)) return;
        }

        public void Draw()
        {
            if (!_disposed && !_interaction.IsDragging && _session.TryDraw(_local.Seat, out var stone))
                _hand.AnimateDraw(stone.Id, _controls.Surface.DrawSource.transform.position);
        }

        private bool BeginDrawDrag(Vector3 position)
        {
            if (_disposed || _interaction.IsDragging || !_session.TryDraw(_local.Seat, out var stone)) return false;
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

        private void ExecuteAction(int index)
        {
            if (_disposed || _interaction.IsDragging) return;
            var player = _session.Players[_local.Seat];
            var actions = _session.Game.PlanRackActions(new RackActionRequest(player.Hand.Slots, _rack.RowCapacity,
                _session.MeldRules, _session.Rules, player.Opening, _session.SeriesThreshold, _session.PairThreshold));
            if (index >= actions.Actions.Count) return;
            var action = actions.Actions[index];
            if (!action.CanExecute) return;
            if (action.Kind == RackActionKind.Finish)
            {
                if (_session.TryFindFinish(_local.Seat, out var discardId)) TryDiscard(_hand.Views.Single(view => view.Instance.Id == discardId));
                return;
            }
            if (action.Kind != RackActionKind.OpenMelds) return;
            var before = _session.Players[_local.Seat].Penalty;
            var opened = _session.TryApply(_local.Seat, PlayerAction.Open(action.Mode, action.Plan.Melds));
            var penalty = _session.Players[_local.Seat].Penalty - before;
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
            if (!_session.TryDiscard(_local.Seat, stone.Instance.Id)) return false;
            _table.AnimateDiscard(_local.Seat, origin);
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
            if (!_table.TryFindMeld(stone.Instance, position, out var index) || !_session.TryLayOff(_local.Seat, stone.Instance.Id, index)) return false;
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
            for (var index = 0; index < _actionHandlers.Count; index++)
                _controls.Actions[index].Requested -= _actionHandlers[index];
            _actionHandlers.Clear();
            _interaction.DropRequested -= Drop;
            _interaction.DiscardRequested -= Discard;
            _interaction.DragMoved -= PreviewLayOff;

        }
    }
}
