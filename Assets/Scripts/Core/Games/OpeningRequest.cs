using System.Collections.Generic;
using Okey.Core.Rules;
using Okey.Core.Tables;

namespace Okey.Core.Games
{
    public sealed class OpeningRequest
    {
        public OpeningMode Mode { get; }
        public OpeningMode CurrentMode { get; }
        public IReadOnlyList<Meld> Melds { get; }
        public int HandCount { get; }
        public bool HasValidTiles { get; }
        public int SeriesThreshold { get; }
        public int PairThreshold { get; }
        public OkeyRules Rules { get; }
        public TableOptions Options { get; }
        public bool PairBoardOpen { get; }

        public OpeningRequest(OpeningMode mode, OpeningMode currentMode, IReadOnlyList<Meld> melds,
            int handCount, bool hasValidTiles, int seriesThreshold, int pairThreshold, OkeyRules rules, TableOptions options, bool pairBoardOpen = false)
        {
            PairBoardOpen = pairBoardOpen;
            Mode = mode;
            CurrentMode = currentMode;
            Melds = melds;
            HandCount = handCount;
            HasValidTiles = hasValidTiles;
            SeriesThreshold = seriesThreshold;
            PairThreshold = pairThreshold;
            Rules = rules;
            Options = options;
        }
    }
}
