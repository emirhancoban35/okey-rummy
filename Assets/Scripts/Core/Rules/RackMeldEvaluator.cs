using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public sealed class RackMeldEvaluator
    {
        public MeldPlan Find(IReadOnlyList<StoneInstance> slots, int rowCapacity, IMeldRules rules, OpeningMode mode)
        {
            return Evaluate(slots, rowCapacity, rules, mode, true);
        }

        public MeldPlan FindGroups(IReadOnlyList<StoneInstance> slots, int rowCapacity, IMeldRules rules)
        {
            return Evaluate(slots, rowCapacity, rules, null, false);
        }

        private MeldPlan Evaluate(IReadOnlyList<StoneInstance> slots, int rowCapacity, IMeldRules rules,
            OpeningMode? mode, bool reserveDiscard)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (rowCapacity < 1) throw new ArgumentOutOfRangeException(nameof(rowCapacity));
            var budget = Math.Max(0, slots.Count(stone => stone != null) - (reserveDiscard ? 1 : 0));
            var cache = new Dictionary<(int, int), MeldPlan>();
            MeldPlan Search(int index, int remaining)
            {
                if (index >= slots.Count || remaining < 2) return new MeldPlan(Array.Empty<Meld>());
                if (cache.TryGetValue((index, remaining), out var cached)) return cached;
                var best = Search(index + 1, remaining);
                var tiles = new List<StoneInstance>();
                var end = Math.Min(slots.Count, (index / rowCapacity + 1) * rowCapacity);
                for (var next = index; next < end && slots[next] != null && tiles.Count < remaining; next++)
                {
                    tiles.Add(slots[next]);
                    Meld meld = null;
                    var valid = mode == OpeningMode.Pairs
                        ? tiles.Count == 2 && rules.TryCreate(tiles, MeldKind.Pair, out meld)
                        : rules.TryCreate(tiles, MeldKind.Run, out meld) || rules.TryCreate(tiles, MeldKind.Set, out meld) ||
                            !mode.HasValue && rules.TryCreate(tiles, MeldKind.Pair, out meld);
                    if (!valid) continue;
                    var tail = Search(next + 1, remaining - tiles.Count);
                    var candidate = new MeldPlan(new[] {meld}.Concat(tail.Melds));
                    var score = !mode.HasValue ? candidate.TileCount : mode == OpeningMode.Pairs ? candidate.Melds.Count : candidate.Points;
                    var bestScore = !mode.HasValue ? best.TileCount : mode == OpeningMode.Pairs ? best.Melds.Count : best.Points;
                    if (score > bestScore || score == bestScore && candidate.TileCount > best.TileCount) best = candidate;
                }
                cache[(index, remaining)] = best;
                return best;
            }
            return Search(0, budget);
        }
    }
}
