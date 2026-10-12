using System;
using System.Linq;
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
            if (publicMelds)
            {
                _view.Surface.SeriesBoard.RenderThreshold(_table.SeriesThreshold.ToString());
                _view.Surface.PairsBoard.RenderThreshold(_table.PairThreshold.ToString());
            }
            _view.Surface.Render(publicMelds, _table.Melds.Count == 0, !publicMelds && _table.Winner < 0,
                canDraw, _table.ActiveSeat == _local.Seat && _table.Phase == TurnPhase.AwaitingDiscard && _table.PendingDiscard == null,
                !_interaction.IsDragging && (_table.CanTakeDiscard(_local.Seat) || hasBorrowedTile), _table.Players.Count);
            _view.Hud.ShowHandValue(publicMelds);
            RenderStatus();
            _view.Hud.RenderValues(_table.RemainingHandValue(_local.Seat), _table.RemainingCount, penalty);
            _view.Panels.RenderHelp(publicMelds);
            if (_actions.Actions.Count > _view.Actions.Count)
                throw new InvalidOperationException("The table prefab needs an action view for every game action.");
            for (var index = 0; index < _view.Actions.Count; index++)
            {
                var visible = index < _actions.Actions.Count;
                var view = _view.Actions[index];
                view.gameObject.SetActive(visible);
                if (!visible) continue;
                var action = _actions.Actions[index];
                var usesBorrowed = !hasBorrowedTile || action.Kind == RackActionKind.OpenMelds &&
                    action.Plan.Melds.SelectMany(meld => meld.Tiles).Any(tile => tile.Stone.Id == _table.PendingDiscard.Id);
                view.Render(action.Kind, action.AdditionalGroups, action.Progress, action.Required, canAct && action.CanExecute && usesBorrowed);
            }
            var groupedIds = _rackGroups.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            foreach (var stone in _hand.Views) stone.Selection.SetGrouped(groupedIds.Contains(stone.Instance.Id));
            _interaction.SetGroups(_rackGroups.Melds);
        }
        private void RenderStatus()
        {
            var hud = _view.Hud;
            if (_table.Phase == TurnPhase.Finished)
            {
                if (_table.Winner < 0) hud.ShowDrawnRound();
                else hud.ShowWinner(_names.ForSeat(_table.Winner));
            }
            else if (_table.ActiveSeat != _local.Seat) hud.ShowWaiting(_names.ForSeat(_table.ActiveSeat));
            else if (_table.PendingDiscard != null) hud.ShowBorrowedTile();
            else if (_table.Phase == TurnPhase.AwaitingDraw && _table.CanEndDrawnRound(_local.Seat)) hud.ShowEmptyStock();
            else if (_table.Phase == TurnPhase.AwaitingDiscard && !_table.Game.AllowsPublicMelds) hud.ShowClassicDiscard();
            else hud.ShowTurn();
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
