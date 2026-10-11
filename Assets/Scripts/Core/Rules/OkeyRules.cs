using System;

namespace Okey.Core.Rules
{
    public sealed class OkeyRules
    {
        public int PlayerCount { get; }
        public int HandSize { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get; }
        public int SeriesThreshold { get; }
        public int PairThreshold { get; }
        public int InvalidOpeningPenalty { get; }
        public int UnopenedScore { get; }
        public int JokerScore { get; }
        public int WinnerScore { get; }

        public OkeyRules(int playerCount, int handSize, int minimumValue, int maximumValue,
            int seriesThreshold, int pairThreshold, int invalidOpeningPenalty, int unopenedScore,
            int jokerScore, int winnerScore)
        {
            if (playerCount < 2 || playerCount > 4 || handSize < 1 || minimumValue < 1 ||
                maximumValue < minimumValue || seriesThreshold < 1 || pairThreshold < 1 ||
                invalidOpeningPenalty < 0 || unopenedScore < 0 || jokerScore < 0)
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Invalid table rules.");
            PlayerCount = playerCount;
            HandSize = handSize;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
            SeriesThreshold = seriesThreshold;
            PairThreshold = pairThreshold;
            InvalidOpeningPenalty = invalidOpeningPenalty;
            UnopenedScore = unopenedScore;
            JokerScore = jokerScore;
            WinnerScore = winnerScore;
        }
    }
}
