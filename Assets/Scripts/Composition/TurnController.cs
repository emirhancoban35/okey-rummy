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
        private readonly MeldPlanner _planner;
        private int[] _handIds;
        private MeldPlan _series;
        private MeldPlan _pairs;
        private bool _initialized;
        private bool _disposed;

        public TurnController(TableSession session, HandPresenter hand, RackInteractionController interaction,
            TurnControlsView controls, TablePresenter table, MeldPlanner planner)
        {
            _session = session;
            _hand = hand;
            _interaction = interaction;
            _controls = controls;
            _table = table;
            _planner = planner;
        }

        public void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TurnController));
            if (_initialized) return;
            _initialized = true;
            _controls.DrawRequested += Draw;
            _controls.SeriesRequested += OpenSeries;
            _controls.PairsRequested += OpenPairs;
            _interaction.DropRequested += Drop;
            _interaction.Changed += Render;
            _session.Changed += Render;
            Render();
        }

        public void Draw()
        {
            if (!_disposed && !_interaction.IsDragging && _session.TryDraw(0, out var stone))
                _hand.AnimateDraw(stone.Id, _controls.DrawSource.transform.position);
        }

        public void OpenSeries() => Open(OpeningMode.Series);
        public void OpenPairs() => Open(OpeningMode.Pairs);

        private void Open(OpeningMode mode)
        {
            if (_disposed || _interaction.IsDragging) return;
            UpdatePlans();
            _session.TryOpen(0, mode, (mode == OpeningMode.Pairs ? _pairs : _series).Melds);
        }

        private bool Drop(Stone stone, Vector3 position)
        {
            if (_disposed) return false;
            if (_controls.DiscardAreas[0].Contains(position))
            {
                var origin = _controls.DiscardAreas[0].StoneRoot.InverseTransformPoint(stone.transform.position);
                if (!_session.TryDiscard(0, stone.Instance.Id)) return false;
                _table.AnimateDiscard(0, origin);
                return true;
            }
            return _table.TryFindMeld(position, out var index) && _session.TryLayOff(0, stone.Instance.Id, index);
        }

        private void UpdatePlans()
        {
            var hand = _session.Players[0].Hand.Slots.Where(stone => stone != null).OrderBy(stone => stone.Id).ToArray();
            var ids = hand.Select(stone => stone.Id).ToArray();
            if (_handIds != null && _handIds.SequenceEqual(ids)) return;
            _handIds = ids;
            _series = _planner.Find(hand, _session.MeldRules, _session.Rules, OpeningMode.Series);
            _pairs = _planner.Find(hand, _session.MeldRules, _session.Rules, OpeningMode.Pairs);
        }

        private void Render()
        {
            if (_disposed) return;
            _hand.Refresh();
            UpdatePlans();
            var local = _session.Players[0];
            _controls.Render(_session.Phase, _session.ActiveSeat, local.Hand.Count, _session.RemainingCount,
                _series.Points, _pairs.Melds.Count, _session.Rules.SeriesThreshold, _session.Rules.PairThreshold,
                _session.Phase == TurnPhase.Finished ? local.Score : local.Penalty,
                !_interaction.IsDragging && _session.CanDraw(0),
                !_interaction.IsDragging && _session.ActiveSeat == 0 && _session.Phase == TurnPhase.AwaitingDiscard);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _controls.DrawRequested -= Draw;
            _controls.SeriesRequested -= OpenSeries;
            _controls.PairsRequested -= OpenPairs;
            _interaction.DropRequested -= Drop;
            _interaction.Changed -= Render;
            _session.Changed -= Render;
        }
    }
}
