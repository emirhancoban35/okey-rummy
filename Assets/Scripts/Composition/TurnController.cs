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
        private readonly TurnControlsView _controls;
        private readonly TablePresenter _table;
        private readonly RackMeldEvaluator _evaluator;
        private readonly RackView _rack;
        private int[] _handIds;
        private MeldPlan _series;
        private MeldPlan _pairs;
        private int _finishingDiscard = -1;
        private Stone _drawnDragView;
        private bool _initialized;
        private bool _disposed;

        public TurnController(TableSession session, HandPresenter hand, RackInteractionController interaction,
            TurnControlsView controls, TablePresenter table, RackMeldEvaluator evaluator, RackView rack)
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
            _controls.DrawRequested += Draw;
            foreach (var area in _controls.DiscardAreas) area.Clicked += TakeDiscard;
            _controls.DrawSource.DragStarted += BeginDrawDrag;
            _controls.DrawSource.Dragged += DragDraw;
            _controls.DrawSource.DragEnded += EndDrawDrag;
            _controls.DrawSource.DragCancelled += CancelDrawDrag;
            _controls.SeriesRequested += OpenSeries;
            _controls.PairsRequested += OpenPairs;
            _interaction.DropRequested += Drop;
            _interaction.DiscardRequested += Discard;
            _interaction.DragMoved += PreviewLayOff;
            _interaction.Changed += Render;
            _session.Changed += Render;
            Render();
        }

        private void TakeDiscard(DiscardAreaView area)
        {
            if (_disposed || _interaction.IsDragging || area != _controls.DiscardAreas[_session.Players.Count - 1]) return;
            if (!_session.Game.RequiresOpenDiscardPickup && _session.TryTakeDiscard(0)) return;
            for (var meld = 0; meld < _session.Melds.Count; meld++)
                if (_session.TryTakeDiscard(0, meld)) return;
        }

        public void Draw()
        {
            if (!_disposed && !_interaction.IsDragging && _session.TryDraw(0, out var stone))
                _hand.AnimateDraw(stone.Id, _controls.DrawSource.transform.position);
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
            UpdatePlans();
            if (!_session.Game.AllowsPublicMelds)
            {
                if (_finishingDiscard >= 0) TryDiscard(_hand.Views.Single(view => view.Instance.Id == _finishingDiscard));
                return;
            }
            var before = _session.Players[0].Penalty;
            var opened = _session.TryOpen(0, mode, (mode == OpeningMode.Pairs ? _pairs : _series).Melds);
            var penalty = _session.Players[0].Penalty - before;
            if (opened || penalty > 0) _controls.ShowOpeningResult(opened, penalty);
        }

        private void Discard(Stone stone)
        {
            if (!_interaction.IsDragging) TryDiscard(stone);
        }

        private bool TryDiscard(Stone stone)
        {
            if (_disposed || stone == null || stone.Instance == null) return false;
            var origin = _controls.DiscardAreas[0].StoneRoot.InverseTransformPoint(stone.transform.position);
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
            if (_controls.DiscardAreas[0].Contains(position)) return TryDiscard(stone);
            var origin = stone.transform.position;
            if (!_table.TryFindMeld(stone.Instance, position, out var index) || !_session.TryLayOff(0, stone.Instance.Id, index)) return false;
            _table.AnimateLayOff(stone.Instance.Id, origin);
            return true;
        }

        private void UpdatePlans()
        {
            var hand = _session.Players[0].Hand.Slots;
            var ids = hand.Select(stone => stone?.Id ?? -1).ToArray();
            if (_handIds != null && _handIds.SequenceEqual(ids)) return;
            _handIds = ids;
            _finishingDiscard = -1;
            if (!_session.Game.AllowsPublicMelds)
                _session.Game.TryFindFinish(hand.Where(stone => stone != null).ToArray(), _session.MeldRules, _session.Rules, out _finishingDiscard);
            _series = _evaluator.Find(hand, _rack.RowCapacity, _session.MeldRules, OpeningMode.Series);
            _pairs = _evaluator.Find(hand, _rack.RowCapacity, _session.MeldRules, OpeningMode.Pairs);
        }

        private void Render()
        {
            if (_disposed) return;
            if (!_interaction.IsDragging) _table.ClearPreview();
            _hand.Refresh();
            UpdatePlans();
            var local = _session.Players[0];
            _controls.ConfigureGame(_session.Game.AllowsPublicMelds);

            _controls.DiscardAreas[_session.Players.Count - 1].SetAvailable(!_interaction.IsDragging && _session.CanTakeDiscard(0));
            _controls.Render(_session.Phase, _session.ActiveSeat, local.Hand.Count, _session.RemainingCount,
                _series.Points, _pairs.Melds.Count, _session.SeriesThreshold, _session.PairThreshold,
                _session.Phase == TurnPhase.Finished ? local.Score : local.Penalty,
                !_interaction.IsDragging && (_session.CanDraw(0) || _session.CanEndDrawnRound(0)),
                !_interaction.IsDragging && _session.ActiveSeat == 0 && _session.Phase == TurnPhase.AwaitingDiscard);
            if (_session.Game.AllowsPublicMelds) _controls.RenderGroups(_series, _pairs, local.Opening);
            else _controls.RenderClosedHand(_series, _pairs, _session.Rules.HandSize,
                !_interaction.IsDragging && _finishingDiscard >= 0 && _session.ActiveSeat == 0 && _session.Phase == TurnPhase.AwaitingDiscard);
            if (_session.Phase == TurnPhase.Finished) _controls.RenderRoundResult(_session.Winner);
            var grouped = local.Opening == OpeningMode.Pairs || local.Opening == OpeningMode.Closed &&
                _pairs.Melds.Count >= _session.PairThreshold && _series.Points < _session.SeriesThreshold ? _pairs : _series;
            var ids = grouped.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            foreach (var view in _hand.Views) view.Selection.SetGrouped(ids.Contains(view.Instance.Id));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelDrawDrag();
            _controls.DrawRequested -= Draw;
            foreach (var area in _controls.DiscardAreas) area.Clicked -= TakeDiscard;
            _controls.DrawSource.DragStarted -= BeginDrawDrag;
            _controls.DrawSource.Dragged -= DragDraw;
            _controls.DrawSource.DragEnded -= EndDrawDrag;
            _controls.DrawSource.DragCancelled -= CancelDrawDrag;
            _controls.SeriesRequested -= OpenSeries;
            _controls.PairsRequested -= OpenPairs;
            _interaction.DropRequested -= Drop;
            _interaction.DiscardRequested -= Discard;
            _interaction.DragMoved -= PreviewLayOff;
            _interaction.Changed -= Render;
            _session.Changed -= Render;
        }
    }
}
