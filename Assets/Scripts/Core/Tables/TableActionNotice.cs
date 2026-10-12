namespace Okey.Core.Tables
{
    public sealed class TableActionNotice
    {
        public PlayerActionKind Kind { get; }
        public int Seat { get; }
        public int PenaltySeat { get; }
        public TablePenaltyKind PenaltyKind { get; }
        public int Value { get; }
        public int Penalty { get; }
        public bool InitialOpening { get; }
        public TableActionNotice(PlayerActionKind kind, int seat, int value = 0, int penalty = 0, bool initialOpening = false, TablePenaltyKind penaltyKind = TablePenaltyKind.None, int penaltySeat = -1)
        {
            Kind = kind;
            Seat = seat;
            PenaltySeat = penaltySeat < 0 ? seat : penaltySeat;
            PenaltyKind = penaltyKind;
            Value = value;
            Penalty = penalty;
            InitialOpening = initialOpening;
        }
    }
}
