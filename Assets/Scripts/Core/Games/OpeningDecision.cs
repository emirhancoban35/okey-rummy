namespace Okey.Core.Games
{
    public readonly struct OpeningDecision
    {
        public bool Accepted { get; }
        public int Penalty { get; }
        public int SeriesThreshold { get; }
        public int PairThreshold { get; }

        public OpeningDecision(bool accepted, int penalty, int seriesThreshold, int pairThreshold)
        {
            Accepted = accepted;
            Penalty = penalty;
            SeriesThreshold = seriesThreshold;
            PairThreshold = pairThreshold;
        }
    }
}
