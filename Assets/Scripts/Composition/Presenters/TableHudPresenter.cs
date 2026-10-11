using System;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using Okey.Presentation.Tables;
using Zenject;

namespace Okey.Composition
{
    public sealed class TableHudPresenter : IInitializable, IDisposable
    {
        private readonly TableSession _table;
        private readonly TableView _view;
        private readonly HandPresenter _hand;
        private readonly RackInteractionController _interaction;
        private readonly TablePresenter _publicStones;
        private readonly RackMeldEvaluator _evaluator;
        private readonly RackView _rack;
        private int[] _handIds;
        private MeldPlan _series;
        private MeldPlan _pairs;
        private int _finishingDiscard;
        private bool _initialized;
        private bool _disposed;
        public TableHudPresenter(TableSession table, TableView view, HandPresenter hand, RackInteractionController interaction,
            TablePresenter publicStones, RackMeldEvaluator evaluator, RackView rack)
        {
            _table = table;
            _view = view;
            _hand = hand;
            _interaction = interaction;
            _publicStones = publicStones;
            _evaluator = evaluator;
            _rack = rack;
        }
        public void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TableHudPresenter));
            if (_initialized) return;
            _initialized = true;
            _table.Changed += Refresh;
            _interaction.Changed += Refresh;
            Refresh();
        }
        private void UpdatePlans()
        {
            var hand = _table.Players[0].Hand.Slots;
            var ids = hand.Select(stone => stone?.Id ?? -1).ToArray();
            if (_handIds != null && _handIds.SequenceEqual(ids)) return;
            _handIds = ids;
            _finishingDiscard = -1;
            if (!_table.Game.AllowsPublicMelds)
                _table.Game.TryFindFinish(hand.Where(stone => stone != null).ToArray(), _table.MeldRules, _table.Rules, out _finishingDiscard);
            _series = _evaluator.Find(hand, _rack.RowCapacity, _table.MeldRules, OpeningMode.Series);
            _pairs = _evaluator.Find(hand, _rack.RowCapacity, _table.MeldRules, OpeningMode.Pairs);
        }
        public void Refresh()
        {
            if (_disposed) return;
            if (!_interaction.IsDragging) _publicStones.ClearPreview();
            _hand.Refresh();
            UpdatePlans();
            var local = _table.Players[0];
            var publicMelds = _table.Game.AllowsPublicMelds;
            var finished = _table.Phase == TurnPhase.Finished;
            var canAct = !_interaction.IsDragging && _table.ActiveSeat == 0 && _table.Phase == TurnPhase.AwaitingDiscard;
            var canDraw = !_interaction.IsDragging && (_table.CanDraw(0) || _table.CanEndDrawnRound(0));
            var penalty = finished ? local.Score : local.Penalty;
            var text = _view.Texts;
            _view.Surface.Render(publicMelds, _table.Melds.Count == 0, !publicMelds && _table.Winner < 0,
                canDraw, _table.ActiveSeat == 0 && _table.Phase == TurnPhase.AwaitingDiscard,
                !_interaction.IsDragging && _table.CanTakeDiscard(0), _table.Players.Count);
            _view.Hud.Render(Status(), string.Format(text.HandFormat, local.Hand.Count), string.Format(text.PileFormat, _table.RemainingCount),
                string.Format(text.PenaltyFormat, penalty), penalty != 0);
            _view.Panels.RenderHelp(publicMelds ? text.OpenHelp : text.ClassicHelp);
            var closed = local.Opening == OpeningMode.Closed;
            var seriesValue = publicMelds ? _series.Points : _series.TileCount;
            var seriesThreshold = publicMelds ? _table.SeriesThreshold : _table.Rules.HandSize;
            _view.SeriesAction.Render(publicMelds ? text.OpenLabel : text.FinishLabel,
                publicMelds && !closed ? string.Format(text.AdditionalGroupFormat, _series.TileCount) : string.Format(text.ProgressFormat, seriesValue, seriesThreshold),
                publicMelds && !closed ? (_series.TileCount > 0 ? 1 : 0) : (float)seriesValue / seriesThreshold,
                canAct && (publicMelds ? local.Opening != OpeningMode.Pairs : _finishingDiscard >= 0));
            _view.PairAction.Render(publicMelds ? text.OpenLabel : text.PairsLabel,
                publicMelds && !closed ? string.Format(text.AdditionalGroupFormat, _pairs.Melds.Count) : string.Format(text.ProgressFormat, _pairs.Melds.Count, _table.PairThreshold),
                publicMelds && !closed ? (_pairs.Melds.Count > 0 ? 1 : 0) : (float)_pairs.Melds.Count / _table.PairThreshold,
                publicMelds && canAct && local.Opening != OpeningMode.Series);
            var grouped = local.Opening == OpeningMode.Pairs || closed && _pairs.Melds.Count >= _table.PairThreshold && _series.Points < _table.SeriesThreshold ? _pairs : _series;
            var groupedIds = grouped.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            foreach (var stone in _hand.Views) stone.Selection.SetGrouped(groupedIds.Contains(stone.Instance.Id));
        }
        private string Status()
        {
            var text = _view.Texts;
            if (_table.Phase == TurnPhase.Finished)
                return _table.Winner < 0 ? text.DrawnRoundLabel : string.Format(text.WinnerFormat, text.SeatName(_table.Winner));
            if (_table.ActiveSeat != 0) return string.Format(text.WaitingFormat, text.SeatName(_table.ActiveSeat));
            if (_table.Phase == TurnPhase.AwaitingDraw) return _table.CanEndDrawnRound(0) ? text.EmptyStockStatus : text.DrawStatus;
            return _table.Game.AllowsPublicMelds ? text.DiscardStatus : text.ClassicDiscardStatus;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _table.Changed -= Refresh;
            _interaction.Changed -= Refresh;
        }
    }
}
