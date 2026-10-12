using System;
using System.Collections.Generic;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;

namespace Okey.Core.Games
{
    public sealed class Okey101Game : ITableGame
    {
        public IOpeningPolicy OpeningPolicy { get; } = new Okey101OpeningPolicy();
        public bool AllowsPublicMelds => true;
        public bool RequiresDiscardUse => true;
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
        public RackActionState PlanRackActions(RackActionRequest request)
        {
            var evaluator = new RackMeldEvaluator();
            var series = evaluator.Find(request.Slots, request.RowCapacity, request.MeldRules, OpeningMode.Series);
            var pairs = evaluator.Find(request.Slots, request.RowCapacity, request.MeldRules, OpeningMode.Pairs);
            var closed = request.Opening == OpeningMode.Closed;
            var grouped = request.Opening == OpeningMode.Pairs || closed && pairs.Melds.Count >= request.PairThreshold &&
                series.Points < request.SeriesThreshold ? pairs : series;
            return new RackActionState(new[]
            {
                new RackActionPlan(RackActionKind.OpenMelds, OpeningMode.Series, series,
                    closed ? series.Points : series.TileCount, request.SeriesThreshold, request.Opening != OpeningMode.Pairs && (closed || series.Melds.Count > 0), !closed),
                new RackActionPlan(RackActionKind.OpenMelds, OpeningMode.Pairs, pairs,
                    pairs.Melds.Count, request.PairThreshold, (request.Opening != OpeningMode.Series || request.PairBoardOpen) && (closed || pairs.Melds.Count > 0), !closed)
            }, grouped);
        }

        public IRoundScoring CreateScoring(OkeyRules rules, IMeldRules meldRules) => new Okey101Scoring(rules, meldRules);
    }
}
