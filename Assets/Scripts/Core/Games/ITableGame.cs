using System.Collections.Generic;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;

namespace Okey.Core.Games
{
    public interface ITableGame
    {
        IOpeningPolicy OpeningPolicy { get; }
        RackActionState PlanRackActions(RackActionRequest request);
        bool AllowsPublicMelds { get; }
        bool RequiresDiscardUse { get; }
        bool AllowsEmptyStockPlay { get; }
        bool PenalizesPlayableDiscards { get; }
        bool TryFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules, out MeldPlan plan, out OpeningMode mode);
        bool TryFindFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules, out int discardId);
        IRoundScoring CreateScoring(OkeyRules rules, IMeldRules meldRules);
    }
}
