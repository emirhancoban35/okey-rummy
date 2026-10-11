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
        private readonly TableSession _session;
        private readonly TurnControlsView _controls;
        private readonly StoneViewFactory _factory;
        private readonly List<Stone> _owned = new List<Stone>();
        private readonly Dictionary<int, Stone> _meldViews = new Dictionary<int, Stone>();
        private readonly List<List<Stone>> _meldGroups = new List<List<Stone>>();
        private readonly Stone[] _discards;
        private bool _initialized;
        private bool _disposed;

        public TablePresenter(TableSession session, TurnControlsView controls, StoneViewFactory factory)
        {
            _session = session;
            _controls = controls;
            _factory = factory;
            _discards = new Stone[session.Players.Count];
        }

        public void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TablePresenter));
            if (_initialized) return;
            _initialized = true;
            _controls.SetPlayerCount(_session.Players.Count);
            CreatePublicStone(_session.Indicator, _controls.IndicatorRoot, _controls.DiscardScale);
            _session.Changed += Render;
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
                if (_meldGroups[index].Any(view => Vector2.Distance(view.transform.position, position) <= _controls.MeldDropRadius))
                    return true;
            index = -1;
            return false;
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
            _controls.SetMeldCount(_session.Melds.Count);
            for (var seat = 0; seat < _session.Players.Count; seat++)
            {
                var player = _session.Players[seat];
                if (seat > 0) _controls.RenderOpponent(seat, player.Hand.Count, player.Penalty, player.Score,
                    seat == _session.ActiveSeat && _session.Phase != TurnPhase.Finished);
                if (player.Discards.Count == 0)
                {
                    if (_discards[seat] != null) _discards[seat].gameObject.SetActive(false);
                    continue;
                }
                if (_discards[seat] != null) _discards[seat].gameObject.SetActive(true);
                var last = player.Discards[player.Discards.Count - 1];
                if (_discards[seat] == null)
                    _discards[seat] = CreatePublicStone(last, _controls.DiscardAreas[seat].StoneRoot, _controls.DiscardScale);
                else if (_discards[seat].Instance.Id != last.Id) _discards[seat].Initialize(last);
            }
            _meldGroups.Clear();
            var column = 0;
            var row = 0;
            var pair = 0;
            var pairCount = _session.Melds.Count(meld => meld.Kind == MeldKind.Pair);
            foreach (var meld in _session.Melds)
            {
                if (meld.Kind != MeldKind.Pair && column + meld.Tiles.Count > _controls.MeldColumns) { row++; column = 0; }
                var group = new List<Stone>();
                var tileIndex = 0;
                foreach (var tile in meld.Tiles)
                {
                    if (!_meldViews.TryGetValue(tile.Stone.Id, out var view))
                    {
                        view = CreatePublicStone(tile.Stone, meld.Kind == MeldKind.Pair ? _controls.PairsRoot : _controls.MeldRoot, _controls.MeldScale);
                        _meldViews.Add(tile.Stone.Id, view);
                    }
                    var target = meld.Kind == MeldKind.Pair ? _controls.GetPairPosition(pair, tileIndex++, pairCount) : _controls.GetMeldPosition(column++, row);
                    view.transform.localScale = meld.Kind == MeldKind.Pair ? _controls.GetPairScale(pairCount) : _controls.MeldScale;
                    if (view.transform.localPosition != target) view.Motion.MoveTo(target);
                    group.Add(view);
                }
                _meldGroups.Add(group);
                if (meld.Kind == MeldKind.Pair) pair++; else column++;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _session.Changed -= Render;
            foreach (var view in _owned)
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(view.gameObject);
                }
        }
    }
}
