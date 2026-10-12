using System.Linq;
using Okey.Core.Rules;

namespace Okey.Core.Games
{
    public sealed class Okey101OpeningPolicy : IOpeningPolicy
    {
        public OpeningDecision Evaluate(OpeningRequest request)
        {
            var pairs = request.Mode == OpeningMode.Pairs;
            var initial = request.CurrentMode == OpeningMode.Closed;
            var points = request.Melds.Sum(meld => meld.Points);
            var valid = request.HasValidTiles && request.Melds.Count > 0 &&
                request.Melds.Sum(meld => meld.Tiles.Count) < request.HandCount &&
                (request.Mode == OpeningMode.Series || pairs) &&
                request.Melds.All(meld => pairs ? meld.Kind == MeldKind.Pair : meld.Kind != MeldKind.Pair) &&
                (initial || request.CurrentMode == request.Mode ||
                    request.CurrentMode == OpeningMode.Series && pairs && request.PairBoardOpen) &&
                (!initial || (pairs ? request.Melds.Count >= request.PairThreshold : points >= request.SeriesThreshold));
            var series = request.SeriesThreshold;
            var pairCount = request.PairThreshold;
            if (valid && initial && request.Options.EscalatingOpenings)
            {
                if (pairs) pairCount = request.Melds.Count + request.Options.OpeningIncrement;
                else series = points + request.Options.OpeningIncrement;
            }
            return new OpeningDecision(valid, valid || !initial ? 0 : request.Rules.InvalidOpeningPenalty, series, pairCount);
        }

        public bool CanLayOff(OpeningMode opening, int handCount) => opening != OpeningMode.Closed && handCount > 1;
    }
}
