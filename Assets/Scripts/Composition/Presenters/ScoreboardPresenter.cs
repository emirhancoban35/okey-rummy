using System;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using Okey.Core.Tables;
using Okey.Presentation.Tables;
using Zenject;
namespace Okey.Composition
{
    public sealed class ScoreboardPresenter : IInitializable, IDisposable
    {
        private readonly TableSession _table;
        private readonly MatchScoreLedger _ledger;
        private readonly ScoreboardView _view;
        private readonly TablePlayerNames _names;
        private readonly LocalPlayerContext _local;
        private readonly SeatColorSettings _colors;
        private readonly ITableNavigation _navigation;
        private bool _shown;
        public ScoreboardPresenter(TableSession table, MatchScoreLedger ledger, ScoreboardView view,
            TablePlayerNames names, LocalPlayerContext local, SeatColorSettings colors, ITableNavigation navigation)
        {
            _table = table; _ledger = ledger; _view = view; _names = names; _local = local;
            _colors = colors; _navigation = navigation;
        }
        public void Initialize()
        {
            _table.Changed += Refresh;
            _names.Changed += Refresh;
            _view.ContinueRequested += Continue;
            _view.SetPlayerCount(_table.Players.Count);
            Refresh();
        }
        private void Refresh()
        {
            var finished = _table.Phase == TurnPhase.Finished;
            if (finished) _ledger.Record(_table);
            var winner = finished && _table.Winner >= 0 ? _names.ForSeat(_table.Winner) : string.Empty;
            if (_ledger.Complete)
            {
                var best = Enumerable.Range(0, _table.Players.Count).Min(seat => _ledger.TeamTotal(seat, _table.Options.Partnerships));
                winner = string.Join(" + ", Enumerable.Range(0, _table.Players.Count)
                    .Where(seat => _ledger.TeamTotal(seat, _table.Options.Partnerships) == best).Select(_names.ForSeat));
            }
            var history = string.Join("\n", _ledger.Rounds.Select((round, index) => _view.HistoryLine(index + 1,
                string.Join("  /  ", Enumerable.Range(0, _table.Players.Count).Select(slot => round.Scores[_local.TableSeat(slot)].ToString())))));
            _view.RenderHeader(_ledger.Rounds.Count, _ledger.RoundLimit, finished, _ledger.Complete, winner, history);
            for (var slot = 0; slot < _table.Players.Count; slot++)
            {
                var seat = _local.TableSeat(slot);
                var player = _table.Players[seat];
                var total = _ledger.TeamTotal(seat, _table.Options.Partnerships);
                _view.RenderRow(slot, _names.ForSeat(seat), player.Opening == OpeningMode.Closed ? "—" : player.OpeningValue.ToString(),
                    player.Penalty.ToString(), finished ? player.Score.ToString() : "—", total.ToString(),
                    _colors.For(_table.TeamOf(seat), _table.Options.Partnerships));
            }
            if (!finished || _shown) return;
            _shown = true;
            _view.Show();
        }
        private void Continue()
        {
            if (_ledger.Complete) _navigation.OpenMenu();
            else _navigation.StartTable();
        }
        public void Dispose()
        {
            _table.Changed -= Refresh;
            _names.Changed -= Refresh;
            _view.ContinueRequested -= Continue;
        }
    }
}
