namespace Okey.Core.Tables
{
    public sealed class TableActionNotice
    {
        public PlayerActionKind Kind { get; }
        public int Seat { get; }
        public int Value { get; }
        public int Penalty { get; }
        public bool InitialOpening { get; }
        public TableActionNotice(PlayerActionKind kind, int seat, int value = 0, int penalty = 0, bool initialOpening = false)
        {
            Kind = kind;
            Seat = seat;
            Value = value;
            Penalty = penalty;
            InitialOpening = initialOpening;
        }
    }
}
