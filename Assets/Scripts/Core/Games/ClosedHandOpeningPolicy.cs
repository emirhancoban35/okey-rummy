using Okey.Core.Rules;

namespace Okey.Core.Games
{
    public sealed class ClosedHandOpeningPolicy : IOpeningPolicy
    {
        public OpeningDecision Evaluate(OpeningRequest request) =>
            new OpeningDecision(false, 0, request.SeriesThreshold, request.PairThreshold);

        public bool CanLayOff(OpeningMode opening, int handCount) => false;
    }
}
