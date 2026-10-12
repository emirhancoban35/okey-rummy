using System;
using Okey.Core.Tables;
using Okey.Presentation.Tables;
using Zenject;
namespace Okey.Composition
{
    public sealed class TableExperiencePresenter : IInitializable, IDisposable
    {
        private readonly TableSession _table;
        private readonly TableView _view;
        private readonly TablePlayerNames _names;
        private readonly LocalPlayerContext _local;
        private readonly SeatColorSettings _colors;
        private int _turn = -1;
        private bool _finished;
        public TableExperiencePresenter(TableSession table, TableView view, TablePlayerNames names, LocalPlayerContext local, SeatColorSettings colors)
        {
            _table = table; _view = view; _names = names; _local = local; _colors = colors;
        }
        public void Initialize()
        {
            _table.Changed += Refresh;
            _table.ActionCompleted += Action;
            _view.Hud.SetIdentity(_colors.For(_table.TeamOf(_local.Seat), _table.Options.Partnerships));
            for (var seat = 0; seat < _table.Players.Count; seat++)
                if (seat != _local.Seat) _view.Players.SetIdentity(_local.ViewSeat(seat), _colors.For(_table.TeamOf(seat), _table.Options.Partnerships));
            Refresh();
        }
        private void Refresh()
        {
            if (_table.Phase == TurnPhase.Finished)
            {
                if (!_finished) _view.Audio.Play(TableSoundCue.Finish);
                _finished = true;
                return;
            }
            if (_turn == _table.TurnRevision) return;
            _turn = _table.TurnRevision;
            if (_table.ActiveSeat != _local.Seat) return;
            _view.Audio.Play(TableSoundCue.YourTurn);
            _view.Feedback.ShowTurn();
        }
        private void Action(TableActionNotice notice)
        {
            if (notice.PenaltyKind != TablePenaltyKind.None)
            {
                _view.Audio.Play(TableSoundCue.Penalty);
                _view.Feedback.ShowPenalty(notice.PenaltyKind, _names.ForSeat(notice.PenaltySeat),
                    notice.Penalty, _names.ForSeat(notice.Seat));
                return;
            }
            switch (notice.Kind)
            {
                case PlayerActionKind.TakeDiscard:
                    if (notice.Seat == _local.Seat && _table.PendingDiscard != null) _view.Feedback.ShowDiscardTrialHelp();
                    _view.Audio.Play(TableSoundCue.Draw);
                    break;
                case PlayerActionKind.Draw: _view.Audio.Play(TableSoundCue.Draw); break;
                case PlayerActionKind.Discard: _view.Audio.Play(TableSoundCue.Discard); break;
                case PlayerActionKind.LayOff: _view.Audio.Play(TableSoundCue.LayOff); break;
                case PlayerActionKind.Open:
                    _view.Audio.Play(TableSoundCue.Open);
                    if (notice.InitialOpening) _view.Feedback.ShowOpening(_names.ForSeat(notice.Seat), notice.Value);
                    break;
            }
        }
        public void Dispose()
        {
            _table.Changed -= Refresh;
            _table.ActionCompleted -= Action;
        }
    }
}
