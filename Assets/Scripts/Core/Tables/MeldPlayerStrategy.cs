using System;
using System.Linq;
using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class MeldPlayerStrategy : IPlayerStrategy
    {
        private readonly MeldPlanner _planner;
        private readonly DiscardWeights _weights;
        public MeldPlayerStrategy(MeldPlanner planner, DiscardWeights weights = null)
        {
            _planner = planner;
            _weights = weights ?? new DiscardWeights();
        }

        public MeldPlan PlanOpening(PlayerObservation observation, out OpeningMode mode)
        {
            mode = observation.Opening;
            if (mode == OpeningMode.Closed)
            {
                var pairs = _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, OpeningMode.Pairs);
                if (pairs.Melds.Count >= observation.PairThreshold)
                {
                    mode = OpeningMode.Pairs;
                    return pairs;
                }
                mode = OpeningMode.Series;
            }
            var plan = _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, mode);
            if (mode == OpeningMode.Series && observation.Opening == OpeningMode.Series && plan.TileCount == 0 &&
                observation.Melds.Any(meld => meld.Kind == MeldKind.Pair))
            {
                mode = OpeningMode.Pairs;
                return _planner.Find(observation.Hand, observation.MeldRules, observation.Rules, mode);
            }
            return plan;
        }

        private double RetentionValue(StoneInstance stone, PlayerObservation observation, HashSet<int> protectedIds)
        {
            var data = observation.MeldRules.Resolve(stone);
            var value = data.Value * _weights.LowTile;
            foreach (var other in observation.Hand.Where(other => other.Id != stone.Id && !protectedIds.Contains(other.Id)))
            {
                var candidate = observation.MeldRules.Resolve(other);
                var distance = Math.Abs(candidate.Value - data.Value);
                if (candidate.Color == data.Color)
                    value += distance == 0 ? _weights.Duplicate : distance == 1 ? _weights.Adjacent : distance == 2 ? _weights.Gap : 0;
                else if (distance == 0) value += _weights.Set;
            }
            return value;
        }

        public int ChooseDiscard(PlayerObservation observation)
        {
            var plan = PlanOpening(observation, out _);
            var protectedIds = plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).ToHashSet();
            return observation.Hand.OrderBy(stone => observation.MeldRules.IsWild(stone) ||
                    observation.Melds.Any(meld => observation.MeldRules.TryExtend(meld, stone, out _)))
                .ThenBy(stone => observation.MeldRules.IsWild(stone))
                .ThenBy(stone => protectedIds.Contains(stone.Id))
                .ThenBy(stone => RetentionValue(stone, observation, protectedIds))
                .ThenBy(stone => observation.NextPlayerDiscards.Count == 0 ? 0 :
                    observation.NextPlayerDiscards.Reverse().Take(3).Any(discard =>
                        observation.MeldRules.Resolve(discard).Value == observation.MeldRules.Resolve(stone).Value ||
                        observation.MeldRules.Resolve(discard).Color == observation.MeldRules.Resolve(stone).Color) ? 0 : 1)
                .ThenBy(stone => observation.MeldRules.Resolve(stone).Value).First().Id;
        }
    }
}
