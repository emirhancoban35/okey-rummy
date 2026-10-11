using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;

namespace Okey.Core.Games
{
    public sealed class ClassicOkeyGame : ITableGame
    {
        private readonly MeldPlanner _planner;
        public ClassicOkeyGame(MeldPlanner planner) => _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        public IOpeningPolicy OpeningPolicy { get; } = new ClosedHandOpeningPolicy();
        public bool AllowsPublicMelds => false;
        public bool RequiresOpenDiscardPickup => false;
        public bool AllowsEmptyStockPlay => true;
        public bool PenalizesPlayableDiscards => false;
        public bool TryFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules, out MeldPlan plan, out OpeningMode mode)
        {
            mode = OpeningMode.Pairs;
            plan = new MeldPlan(Array.Empty<Meld>());
            if (hand.Count != rules.HandSize) return false;
            plan = _planner.Find(hand, meldRules, rules, mode, reserveDiscard: false, maximizeTiles: true);
            if (plan.TileCount == rules.HandSize && plan.Melds.Count == rules.PairThreshold) return true;
            mode = OpeningMode.Series;
            plan = _planner.Find(hand, meldRules, rules, mode, reserveDiscard: false, maximizeTiles: true);
            return plan.TileCount == rules.HandSize;
        }
        public bool TryFindFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules, out int discardId)
        {
            discardId = -1;
            if (hand.Count != rules.HandSize + 1) return false;
            foreach (var mode in new[] { OpeningMode.Pairs, OpeningMode.Series })
            {
                var plan = _planner.Find(hand, meldRules, rules, mode, maximizeTiles: true);
                if (plan.TileCount != rules.HandSize || mode == OpeningMode.Pairs && plan.Melds.Count != rules.PairThreshold) continue;
                var ids = plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
                var spare = hand.Where(tile => !ids.Contains(tile.Id)).ToArray();
                if (spare.Length != 1) continue;
                discardId = spare[0].Id;
                return true;
            }
            return false;
        }

        public RackActionState PlanRackActions(RackActionRequest request)
        {
            var evaluator = new RackMeldEvaluator();
            var series = evaluator.Find(request.Slots, request.RowCapacity, request.MeldRules, OpeningMode.Series);
            var pairs = evaluator.Find(request.Slots, request.RowCapacity, request.MeldRules, OpeningMode.Pairs);
            var canFinish = TryFindFinish(request.Slots.Where(stone => stone != null).ToArray(), request.MeldRules, request.Rules, out _);
            var grouped = pairs.TileCount >= series.TileCount ? pairs : series;
            return new RackActionState(new[]
            {
                new RackActionPlan(RackActionKind.Finish, OpeningMode.Series, series,
                    series.TileCount, request.Rules.HandSize, canFinish),
                new RackActionPlan(RackActionKind.PreviewPairs, OpeningMode.Pairs, pairs,
                    pairs.Melds.Count, request.PairThreshold, false)
            }, grouped);
        }

        public IRoundScoring CreateScoring(OkeyRules rules, IMeldRules meldRules) => new ClassicOkeyScoring(rules);
    }
}
