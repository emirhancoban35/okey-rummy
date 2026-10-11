using Okey.Core.Rules;

namespace Okey.Core.Games
{
    public interface IOpeningPolicy
    {
        OpeningDecision Evaluate(OpeningRequest request);
        bool CanLayOff(OpeningMode opening, int handCount);
    }
}
