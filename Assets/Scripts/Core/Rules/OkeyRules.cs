using System;

namespace Okey.Core.Rules
{
    public sealed class OkeyRules
    {
        public bool AllowsHighOne { get; }
        public bool AllowsIndicatorPairs { get; }
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
        public int JokerReclaimPenalty { get; }
        public int DiscardPenalty { get; }
        public DiscardPenalties DiscardPenalties { get; }
        public int PairMultiplier { get; }
        public int JokerFinishMultiplier { get; }

        public OkeyRules(int playerCount, int handSize, int minimumValue, int maximumValue,
            int seriesThreshold, int pairThreshold, int invalidOpeningPenalty, int unopenedScore,
            int jokerScore, int winnerScore, int discardPenalty, int pairMultiplier, int jokerFinishMultiplier, bool allowsHighOne = false, DiscardPenalties discardPenalties = null, int jokerReclaimPenalty = 101, bool allowsIndicatorPairs = false)
        {
            if (playerCount < 2 || playerCount > 4 || handSize < 1 || minimumValue < 1 ||
                maximumValue < minimumValue || seriesThreshold < 1 || pairThreshold < 1 ||
                jokerReclaimPenalty < 0 || invalidOpeningPenalty < 0 || unopenedScore < 0 || jokerScore < 0 || discardPenalty < 0 || pairMultiplier < 1 || jokerFinishMultiplier < 1)
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Invalid table rules.");
            AllowsHighOne = allowsHighOne;
            AllowsIndicatorPairs = allowsIndicatorPairs;
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
            JokerReclaimPenalty = jokerReclaimPenalty;
            DiscardPenalty = discardPenalty;
            DiscardPenalties = discardPenalties ?? new DiscardPenalties(discardPenalty, discardPenalty);
            PairMultiplier = pairMultiplier;
            JokerFinishMultiplier = jokerFinishMultiplier;
        }
    }
}
