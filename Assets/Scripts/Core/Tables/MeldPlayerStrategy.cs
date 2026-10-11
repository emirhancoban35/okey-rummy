using System.Linq;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class MeldPlayerStrategy : IPlayerStrategy
    {
        private readonly MeldPlanner _planner;
        public MeldPlayerStrategy(MeldPlanner planner) => _planner = planner;

        public MeldPlan PlanOpening(PlayerObservation observation, out OpeningMode mode)
        {
            mode = observation.Opening;
            if (mode == OpeningMode.Closed)
            {
                var pairs = _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, OpeningMode.Pairs);
                if (pairs.Melds.Count >= observation.Rules.PairThreshold)
                {
                    mode = OpeningMode.Pairs;
                    return pairs;
                }
                mode = OpeningMode.Series;
            }
            return _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, mode);
        }

        public int ChooseDiscard(PlayerObservation observation)
        {
            var plan = PlanOpening(observation, out _);
            var protectedIds = plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            return observation.Hand.OrderBy(stone => observation.MeldRules.IsWild(stone) ||
                    observation.Melds.Any(meld => observation.MeldRules.TryExtend(meld, stone, out _)))
                .ThenBy(stone => observation.MeldRules.IsWild(stone))
                .ThenBy(stone => protectedIds.Contains(stone.Id))
                .ThenBy(stone => observation.Hand.Count(other => other.Id != stone.Id &&
                    (observation.MeldRules.Resolve(other).Value == observation.MeldRules.Resolve(stone).Value ||
                    (observation.MeldRules.Resolve(other).Color == observation.MeldRules.Resolve(stone).Color &&
                    System.Math.Abs(observation.MeldRules.Resolve(other).Value - observation.MeldRules.Resolve(stone).Value) <= 2))))
                .ThenBy(stone => observation.NextPlayerDiscards.Count == 0 ? 0 :
                    observation.NextPlayerDiscards.Reverse().Take(3).Any(discard =>
                        observation.MeldRules.Resolve(discard).Value == observation.MeldRules.Resolve(stone).Value ||
                        observation.MeldRules.Resolve(discard).Color == observation.MeldRules.Resolve(stone).Color) ? 0 : 1)
                .ThenBy(stone => observation.MeldRules.Resolve(stone).Value).First().Id;
        }
    }
}
