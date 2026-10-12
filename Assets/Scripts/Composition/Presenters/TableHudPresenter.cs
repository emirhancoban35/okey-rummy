using System;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Tables;
using Okey.Presentation.Racks;
using Okey.Presentation.Tables;
using Zenject;

namespace Okey.Composition
{
    public sealed class TableHudPresenter : IInitializable, IDisposable
    {
        private readonly LocalPlayerContext _local;
        private readonly TablePlayerNames _names;
        private readonly TableSession _table;
        private readonly TableView _view;
        private readonly HandPresenter _hand;
        private readonly RackInteractionController _interaction;
        private readonly TablePresenter _publicStones;
        private readonly RackView _rack;
        private int[] _handIds;
        private int _openingRevision = -1;
        private RackActionState _actions;
        private readonly RackMeldEvaluator _groups;
        private MeldPlan _rackGroups;
        private bool _initialized;
        private bool _disposed;
        public TableHudPresenter(TableSession table, TableView view, HandPresenter hand, RackInteractionController interaction,
            TablePresenter publicStones, RackView rack, LocalPlayerContext local, TablePlayerNames names, RackMeldEvaluator groups)
        {
            _groups = groups;
            _table = table;
            _local = local;
            _names = names;
            _view = view;
            _hand = hand;
            _interaction = interaction;
            _publicStones = publicStones;
            _rack = rack;
        }
        public void Initialize()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TableHudPresenter));
            if (_initialized) return;
            _initialized = true;
            _table.Changed += Refresh;
            _names.Changed += Refresh;
            _interaction.Changed += Refresh;
            Refresh();
        }
        private void UpdatePlans()
        {
            var hand = _table.Players[_local.Seat].Hand.Slots;
            var ids = hand.Select(stone => stone?.Id ?? -1).ToArray();
            if (_handIds != null && _handIds.SequenceEqual(ids) && _openingRevision == _table.OpeningRevision) return;
            _openingRevision = _table.OpeningRevision;
            _handIds = ids;
            _rackGroups = _groups.FindGroups(hand, _rack.RowCapacity, _table.MeldRules);
            var player = _table.Players[_local.Seat];
            _actions = _table.Game.PlanRackActions(new RackActionRequest(hand, _rack.RowCapacity,
                _table.MeldRules, _table.Rules, player.Opening, _table.SeriesThreshold, _table.PairThreshold, _table.PairBoardOpen));
        }
        public void Refresh()
        {
            if (_disposed) return;
            if (!_interaction.IsDragging) _publicStones.ClearPreview();
            _hand.Refresh();
            UpdatePlans();
            var local = _table.Players[_local.Seat];
            var publicMelds = _table.Game.AllowsPublicMelds;
            var finished = _table.Phase == TurnPhase.Finished;
            var canAct = !_interaction.IsDragging && _table.ActiveSeat == _local.Seat && _table.Phase == TurnPhase.AwaitingDiscard;
            var hasBorrowedTile = _table.ActiveSeat == _local.Seat && _table.PendingDiscard != null;
            var canDraw = !_interaction.IsDragging && (hasBorrowedTile && _table.RemainingCount > 0 || _table.CanDraw(_local.Seat) || _table.CanEndDrawnRound(_local.Seat));
            var penalty = finished ? local.Score : local.Penalty;
            var text = _view.Texts;
            _view.Surface.Render(publicMelds, _table.Melds.Count == 0, !publicMelds && _table.Winner < 0,
                canDraw, _table.ActiveSeat == _local.Seat && _table.Phase == TurnPhase.AwaitingDiscard && _table.PendingDiscard == null,
                !_interaction.IsDragging && (_table.CanTakeDiscard(_local.Seat) || hasBorrowedTile), _table.Players.Count);
            _view.Hud.Render(Status(), string.Empty, string.Format(text.PileFormat, _table.RemainingCount),
                string.Format(text.PenaltyFormat, penalty), penalty != 0);
            _view.Panels.RenderHelp(publicMelds ? text.OpenHelp : text.ClassicHelp);
            if (_actions.Actions.Count > _view.Actions.Count)
                throw new InvalidOperationException("The table prefab needs an action view for every game action.");
            for (var index = 0; index < _view.Actions.Count; index++)
            {
                var visible = index < _actions.Actions.Count;
                var view = _view.Actions[index];
                view.gameObject.SetActive(visible);
                if (!visible) continue;
                var action = _actions.Actions[index];
                var label = action.Kind == RackActionKind.OpenMelds ? text.OpenLabel :
                    action.Kind == RackActionKind.Finish ? text.FinishLabel : text.PairsLabel;
                var progress = publicMelds ? string.Format(text.ThresholdFormat, action.Required) :
                    action.AdditionalGroups ? string.Format(text.AdditionalGroupFormat, action.Progress) :
                    string.Format(text.ProgressFormat, action.Progress, action.Required);
                var fill = action.AdditionalGroups ? (action.Progress > 0 ? 1 : 0) : (float)action.Progress / action.Required;
                var usesBorrowed = !hasBorrowedTile || action.Kind == RackActionKind.OpenMelds &&
                    action.Plan.Melds.SelectMany(meld => meld.Tiles).Any(tile => tile.Stone.Id == _table.PendingDiscard.Id);
                view.Render(label, progress, fill, canAct && action.CanExecute && usesBorrowed);
            }
            var groupedIds = _rackGroups.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            foreach (var stone in _hand.Views) stone.Selection.SetGrouped(groupedIds.Contains(stone.Instance.Id));
            _interaction.SetGroups(_rackGroups.Melds);
        }
        private string Status()
        {
            var text = _view.Texts;
            if (_table.Phase == TurnPhase.Finished)
                return _table.Winner < 0 ? text.DrawnRoundLabel : string.Format(text.WinnerFormat, _names.ForSeat(_table.Winner));
            if (_table.ActiveSeat != _local.Seat) return string.Format(text.WaitingFormat, _names.ForSeat(_table.ActiveSeat));
            if (_table.PendingDiscard != null) return text.DiscardTrialStatus;
            if (_table.Phase == TurnPhase.AwaitingDraw) return _table.CanEndDrawnRound(_local.Seat) ? text.EmptyStockStatus : text.DrawStatus;
            return _table.Game.AllowsPublicMelds ? text.DiscardStatus : text.ClassicDiscardStatus;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _table.Changed -= Refresh;
            _names.Changed -= Refresh;
            _interaction.Changed -= Refresh;
        }
    }
}
