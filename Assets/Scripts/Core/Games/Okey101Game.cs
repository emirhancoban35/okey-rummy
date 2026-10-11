using System;
using System.Collections.Generic;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;

namespace Okey.Core.Games
{
    public sealed class Okey101Game : ITableGame
    {
        public bool AllowsPublicMelds => true;
        public bool RequiresOpenDiscardPickup => true;
        public bool AllowsEmptyStockPlay => false;
        public bool PenalizesPlayableDiscards => true;
        public bool TryFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules, out MeldPlan plan, out OpeningMode mode)
        {
            plan = new MeldPlan(Array.Empty<Meld>());
            mode = OpeningMode.Closed;
            return hand.Count == 0;
        }
        public bool TryFindFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules, out int discardId)
        {
            discardId = -1;
            return false;
        }
        public IRoundScoring CreateScoring(OkeyRules rules, IMeldRules meldRules) => new Okey101Scoring(rules, meldRules);
    }
}
