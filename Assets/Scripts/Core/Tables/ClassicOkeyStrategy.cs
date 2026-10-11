using System;
using System.Linq;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class ClassicOkeyStrategy : IPlayerStrategy
    {
        private readonly MeldPlanner _planner;
        private readonly MeldPlayerStrategy _fallback;
        public ClassicOkeyStrategy(MeldPlanner planner)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _fallback = new MeldPlayerStrategy(planner);
        }
        public MeldPlan PlanOpening(PlayerObservation observation, out OpeningMode mode)
        {
            var series = _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, OpeningMode.Series, reserveDiscard: observation.Hand.Count > observation.Rules.HandSize, maximizeTiles: true);
            var pairs = _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, OpeningMode.Pairs, reserveDiscard: observation.Hand.Count > observation.Rules.HandSize, maximizeTiles: true);
            mode = pairs.TileCount >= series.TileCount ? OpeningMode.Pairs : OpeningMode.Series;
            return mode == OpeningMode.Pairs ? pairs : series;
        }
        public int ChooseDiscard(PlayerObservation observation)
        {
            var plan = PlanOpening(observation, out _);
            var used = plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            var spare = observation.Hand.Where(stone => !used.Contains(stone.Id)).ToArray();
            if (plan.TileCount == observation.Rules.HandSize && spare.Length == 1) return spare[0].Id;
            var preferred = _fallback.ChooseDiscard(observation);
            if (spare.Any(stone => stone.Id == preferred)) return preferred;
            return spare.OrderBy(stone => observation.MeldRules.IsWild(stone))
                .ThenBy(stone => observation.Hand.Count(other => other.Id != stone.Id &&
                    (observation.MeldRules.Resolve(other).Value == observation.MeldRules.Resolve(stone).Value ||
                    observation.MeldRules.Resolve(other).Color == observation.MeldRules.Resolve(stone).Color &&
                    Math.Abs(observation.MeldRules.Resolve(other).Value - observation.MeldRules.Resolve(stone).Value) <= 2)))
                .ThenBy(stone => observation.MeldRules.Resolve(stone).Value).First().Id;
        }
    }
}
