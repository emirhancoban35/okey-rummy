using Okey.Presentation.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Tables;
using Okey.Core.Stones;
using Okey.Core.Rules;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class TablePresenter : IInitializable, IDisposable
    {
        private readonly LocalPlayerContext _local;
        private readonly TablePlayerNames _names;
        private readonly TableSession _session;
        private readonly TableView _controls;
        private readonly StoneViewFactory _factory;
        private readonly List<Stone> _owned = new List<Stone>();
        private readonly Dictionary<int, Stone> _meldViews = new Dictionary<int, Stone>();
        private readonly Dictionary<int, Vector3> _meldPositions = new Dictionary<int, Vector3>();
        private readonly List<List<Stone>> _meldGroups = new List<List<Stone>>();
        private readonly List<(MeldBoardView Board, int Group)> _boardGroups = new List<(MeldBoardView, int)>();
        private readonly List<(int Index, MeldBoardView Board, MeldBoardCell Cell)> _layOffTargets = new List<(int, MeldBoardView, MeldBoardCell)>();
        private int _targetStoneId = -1;
        private readonly Stone[] _discards;
        private bool _winningHandShown;
        private bool _initialized;
        private bool _disposed;

        public TablePresenter(TableSession session, TableView controls, StoneViewFactory factory, LocalPlayerContext local, TablePlayerNames names)
        {
            _session = session;
            _local = local;
            _names = names;
            _controls = controls;
            _factory = factory;
            _discards = new Stone[session.Players.Count];
        }

        public void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TablePresenter));
            if (_initialized) return;
            _initialized = true;
            _controls.Surface.SetPlayerCount(_session.Players.Count);
            _controls.Players.SetPlayerCount(_session.Players.Count);
            _controls.Surface.SeriesBoard.Configure(_session.Rules.MinimumValue, _session.Rules.MaximumValue, false);
            _controls.Surface.PairsBoard.Configure(_session.Rules.MinimumValue, _session.Rules.MaximumValue, true);
            _controls.Surface.SeriesBoard.PageChanged += Render;
            _controls.Surface.PairsBoard.PageChanged += Render;
            CreatePublicStone(_session.Indicator, _controls.Surface.IndicatorRoot, _controls.Surface.IndicatorScale);
            _session.Changed += Render;
            _names.Changed += Render;
            Render();
        }

        public void AnimateDiscard(int seat, Vector3 localOrigin)
        {
            _discards[seat].Motion.SnapTo(localOrigin);
            _discards[seat].Motion.MoveTo(Vector3.zero);
        }

        public bool TryFindMeld(Vector3 position, out int index)
        {
            for (index = 0; index < _meldGroups.Count; index++)
                if (_meldGroups[index].Any(view => view.gameObject.activeSelf && Vector2.Distance(view.transform.position, position) <= _controls.Surface.MeldDropRadius))
                    return true;
            index = -1;
            return false;
        }

        public bool TryFindMeld(StoneInstance stone, Vector3 position, out int index)
        {
            index = -1;
            if (_session.ActiveSeat != _local.Seat || _session.Phase != TurnPhase.AwaitingDiscard ||
                _session.Players[_local.Seat].Hand.FindSlot(stone.Id) < 0) return false;
            CacheLayOffTargets(stone);
            var nearest = float.MaxValue;
            foreach (var target in _layOffTargets)
            {
                if (!target.Board.ContainsTarget(position, target.Cell)) continue;
                var worldTarget = target.Board.transform.TransformPoint(target.Board.GetPosition(target.Cell));
                var distance = Vector2.Distance(worldTarget, position);
                if (distance >= nearest) continue;
                nearest = distance;
                index = target.Index;
            }
            return index >= 0;
        }

        private void CacheLayOffTargets(StoneInstance stone)
        {
            if (_targetStoneId == stone.Id) return;
            _targetStoneId = stone.Id;
            _layOffTargets.Clear();
            for (var index = 0; index < _session.Melds.Count; index++)
            {
                if (!_session.CanReclaimJoker(_local.Seat, stone.Id, index, out var extended, out _) &&
                    !_session.CanLayOff(_local.Seat, stone.Id, index, out extended)) continue;
                var placement = _boardGroups[index];
                for (var tile = 0; tile < extended.Tiles.Count; tile++)
                    if (extended.Tiles[tile].Stone.Id == stone.Id)
                    {
                        _layOffTargets.Add((index, placement.Board, placement.Board.Locate(extended, placement.Group, tile)));
                        break;
                    }
            }
        }

        public void PreviewLayOff(StoneInstance stone, Vector3 position) => PreviewLayOff(stone, position, out _);

        public bool PreviewLayOff(StoneInstance stone, Vector3 position, out Vector3 worldScale)
        {
            worldScale = default;
            ClearPreview();
            if (!TryFindMeld(stone, position, out var index)) return false;
            foreach (var view in _meldGroups[index]) view.Selection.SetSelected(true);
            foreach (var target in _layOffTargets)
                if (target.Index == index)
                {
                    target.Board.ShowTarget(target.Cell);
                    worldScale = Vector3.Scale(target.Board.TileScale, target.Board.transform.lossyScale);
                    return true;
                }
            return false;
        }

        public void ClearPreview()
        {
            foreach (var view in _meldViews.Values) view.Selection.SetSelected(false);
            _controls.Surface.SeriesBoard.HideTarget();
            _controls.Surface.PairsBoard.HideTarget();
        }

        public void AnimateLayOff(int stoneId, Vector3 worldOrigin)
        {
            var view = _meldViews[stoneId];
            var target = view.transform.parent.InverseTransformPoint(_meldPositions[stoneId]);
            view.Motion.SnapTo(view.transform.parent.InverseTransformPoint(worldOrigin));
            view.Motion.MoveTo(target);
        }

        private Stone CreatePublicStone(StoneInstance stone, Transform root, Vector3 scale)
        {
            var view = _factory.Create(stone, root);
            view.DragHandler.enabled = false;
            var collider = view.GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            view.Selection.enabled = false;
            view.SetFace(true);
            view.transform.localScale = scale;
            view.Motion.SnapTo(Vector3.zero);
            _owned.Add(view);
            return view;
        }

        private void Render()
        {
            if (_disposed) return;
            if (!_winningHandShown && !_session.Game.AllowsPublicMelds && _session.WinningHand != null)
            {
                _winningHandShown = true;
                for (var group = 0; group < _session.WinningHand.Melds.Count; group++)
                {
                    var meld = _session.WinningHand.Melds[group];
                    for (var tile = 0; tile < meld.Tiles.Count; tile++)
                    {
                        var view = CreatePublicStone(meld.Tiles[tile].Stone, _controls.Surface.WinningHandRoot, _controls.Surface.WinningTileScale);
                        view.Motion.SnapTo(_controls.Surface.WinningTilePosition(group, tile, meld.Kind == MeldKind.Pair));
                    }
                }
            }
            for (var seat = 0; seat < _session.Players.Count; seat++)
            {
                var player = _session.Players[seat];
                if (seat != _local.Seat)
                {
                    var active = seat == _session.ActiveSeat && _session.Phase != TurnPhase.Finished;
                    _controls.Players.RenderOpponent(_local.ViewSeat(seat), string.Format(_controls.Texts.OpponentFormat,
                        _names.ForSeat(seat), player.Penalty, player.Score,
                        active ? _controls.Texts.ActiveSeatText : string.Empty), active);
                }
                if (player.Discards.Count == 0)
                {
                    if (_discards[seat] != null) _discards[seat].gameObject.SetActive(false);
                    continue;
                }
                if (_discards[seat] != null) _discards[seat].gameObject.SetActive(true);
                var last = player.Discards[player.Discards.Count - 1];
                if (_discards[seat] == null)
                    _discards[seat] = CreatePublicStone(last, _controls.Surface.DiscardAreas[_local.ViewSeat(seat)].StoneRoot, _controls.Surface.DiscardScale);
                else if (_discards[seat].Instance.Id != last.Id) _discards[seat].Initialize(last);
            }
            ClearPreview();
            _targetStoneId = -1;
            _layOffTargets.Clear();
            var publicIds = _session.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            foreach (var id in _meldViews.Keys.Where(id => !publicIds.Contains(id)).ToArray())
            {
                var stale = _meldViews[id];
                stale.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(stale.gameObject);
                _owned.Remove(stale);
                _meldViews.Remove(id);
            }
            _meldPositions.Clear();
            _meldGroups.Clear();
            _boardGroups.Clear();
            var series = 0;
            var pair = 0;
            var pairCount = _session.Melds.Count(meld => meld.Kind == MeldKind.Pair);
            _controls.Surface.SeriesBoard.SetGroupCount(_session.Melds.Count - pairCount);
            _controls.Surface.PairsBoard.SetGroupCount(pairCount);
            foreach (var meld in _session.Melds)
            {
                var board = meld.Kind == MeldKind.Pair ? _controls.Surface.PairsBoard : _controls.Surface.SeriesBoard;
                var ordinal = meld.Kind == MeldKind.Pair ? pair++ : series++;
                _boardGroups.Add((board, ordinal));
                var group = new List<Stone>();
                for (var tileIndex = 0; tileIndex < meld.Tiles.Count; tileIndex++)
                {
                    var tile = meld.Tiles[tileIndex];
                    var cell = board.Locate(meld, ordinal, tileIndex);
                    if (!_meldViews.TryGetValue(tile.Stone.Id, out var view))
                    {
                        view = CreatePublicStone(tile.Stone, board.transform, board.TileScale);
                        _meldViews.Add(tile.Stone.Id, view);
                    }
                    var target = board.GetPosition(cell);
                    view.transform.localScale = board.TileScale;
                    _meldPositions[view.Instance.Id] = board.transform.TransformPoint(target);
                    var visible = cell.Page == board.Page;
                    var wasVisible = view.gameObject.activeSelf;
                    view.gameObject.SetActive(visible);
                    if (!wasVisible || !visible) view.Motion.SnapTo(target);
                    else if (view.transform.localPosition != target) view.Motion.MoveTo(target);
                    group.Add(view);
                }
                _meldGroups.Add(group);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _session.Changed -= Render;
            _names.Changed -= Render;
            _controls.Surface.SeriesBoard.PageChanged -= Render;
            _controls.Surface.PairsBoard.PageChanged -= Render;
            foreach (var view in _owned)
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(view.gameObject);
                }
        }
    }
}
