using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public sealed class MeldPlanner
    {
        private sealed class Candidate
        {
            public ulong Mask;
            public Meld Meld;
        }

        private sealed class Choice
        {
            public int Score;
            public Candidate Candidate;
            public ulong Next;
        }

        public MeldPlan Find(IEnumerable<StoneInstance> stones, IMeldRules rules, OkeyRules settings, OpeningMode mode, bool reserveDiscard = true, bool maximizeTiles = false)
        {
            var hand = stones.Where(stone => stone != null).ToArray();
            if (hand.Length > 63) throw new ArgumentException("The meld planner supports up to 63 stones.", nameof(stones));
            if (hand.Length < 2) return new MeldPlan(Array.Empty<Meld>());
            var candidates = new Dictionary<ulong, Candidate>();
            var wild = Enumerable.Range(0, hand.Length).Where(index => rules.IsWild(hand[index])).ToArray();
            var fixedIndices = Enumerable.Range(0, hand.Length).Except(wild).ToArray();
            void Add(List<int> indices, MeldKind kind)
            {
                ulong mask = 0;
                foreach (var index in indices) mask |= 1UL << index;
                if (!candidates.ContainsKey(mask) && rules.TryCreate(indices.Select(index => hand[index]), kind, out var meld))
                    candidates.Add(mask, new Candidate { Mask = mask, Meld = meld });
            }
            void Choose(List<int[]> buckets, int bucket, List<int> indices, MeldKind kind)
            {
                if (bucket == buckets.Count) { Add(indices, kind); return; }
                foreach (var index in buckets[bucket].Concat(wild))
                {
                    if (indices.Contains(index)) continue;
                    indices.Add(index);
                    Choose(buckets, bucket + 1, indices, kind);
                    indices.RemoveAt(indices.Count - 1);
                }
            }
            if (mode == OpeningMode.Pairs)
            {
                for (var left = 0; left < hand.Length; left++)
                    for (var right = left + 1; right < hand.Length; right++)
                        Add(new List<int> { left, right }, MeldKind.Pair);
            }
            else
            {
                for (var value = settings.MinimumValue; value <= settings.MaximumValue; value++)
                {
                    for (var colors = 1; colors < 16; colors++)
                    {
                        var buckets = new List<int[]>();
                        for (var color = 0; color < 4; color++)
                            if ((colors & (1 << color)) != 0)
                                buckets.Add(fixedIndices.Where(index => rules.Resolve(hand[index]).Value == value &&
                                    (int)rules.Resolve(hand[index]).Color == color).ToArray());
                        if (buckets.Count >= 3)
                            Choose(buckets, 0, new List<int>(), MeldKind.Set);
                    }
                }
                for (var color = 0; color < 4; color++)
                    for (var start = settings.MinimumValue; start <= settings.MaximumValue - 2; start++)
                    {
                        var buckets = new List<int[]>();
                        var missing = 0;
                        for (var value = start; value <= settings.MaximumValue; value++)
                        {
                            var bucket = fixedIndices.Where(index => rules.Resolve(hand[index]).Value == value &&
                                (int)rules.Resolve(hand[index]).Color == color).ToArray();
                            buckets.Add(bucket);
                            if (bucket.Length == 0) missing++;
                            if (missing > wild.Length) break;
                            if (buckets.Count >= 3) Choose(buckets, 0, new List<int>(), MeldKind.Run);
                        }
                    }
            }
            if (settings.AllowsHighOne && mode != OpeningMode.Pairs)
                for (var color = 0; color < 4; color++)
                    for (var start = settings.MinimumValue + 1; start < settings.MaximumValue; start++)
                    {
                        var buckets = Enumerable.Range(start, settings.MaximumValue - start + 1).Append(settings.MinimumValue)
                            .Select(value => fixedIndices.Where(index => rules.Resolve(hand[index]).Value == value &&
                                (int)rules.Resolve(hand[index]).Color == color).ToArray()).ToList();
                        if (buckets.Count(bucket => bucket.Length == 0) <= wild.Length)
                            Choose(buckets, 0, new List<int>(), MeldKind.Run);
                    }
            var byTile = new List<Candidate>[hand.Length];
            for (var index = 0; index < hand.Length; index++)
                byTile[index] = candidates.Values.Where(candidate => (candidate.Mask & (1UL << index)) != 0).OrderByDescending(candidate => mode == OpeningMode.Pairs ? candidate.Meld.Tiles.Count : candidate.Meld.Points).ToList();
            var cache = new Dictionary<ulong, Choice>();
            var values = hand.Select(stone => rules.IsWild(stone) ? settings.MaximumValue : rules.Resolve(stone).Value).ToArray();
            int UpperBound(ulong mask)
            {
                if (mode == OpeningMode.Pairs || maximizeTiles)
                {
                    var count = 0;
                    for (var index = 0; index < hand.Length; index++) if ((mask & (1UL << index)) != 0) count++;
                    return mode == OpeningMode.Pairs ? count / 2 : count;
                }
                var points = 0;
                for (var index = 0; index < hand.Length; index++) if ((mask & (1UL << index)) != 0) points += values[index];
                return points;
            }
            Choice Solve(ulong mask)
            {
                if (mask == 0) return new Choice();
                if (cache.TryGetValue(mask, out var saved)) return saved;
                var first = 0;
                while ((mask & (1UL << first)) == 0) first++;
                var next = mask & ~(1UL << first);
                var best = new Choice { Next = next };
                var upper = UpperBound(mask);
                foreach (var candidate in byTile[first])
                {
                    if (best.Score >= upper) break;
                    if ((candidate.Mask & mask) != candidate.Mask) continue;
                    next = mask ^ candidate.Mask;
                    var value = mode == OpeningMode.Pairs ? 1 : maximizeTiles ? candidate.Meld.Tiles.Count : candidate.Meld.Points;
                    if (value + UpperBound(next) <= best.Score) continue;
                    var score = Solve(next).Score + value;
                    if (score > best.Score)
                        best = new Choice { Score = score, Candidate = candidate, Next = next };
                }
                next = mask & ~(1UL << first);
                if (best.Score < upper && UpperBound(next) > best.Score)
                {
                    var score = Solve(next).Score;
                    if (score > best.Score) best = new Choice { Score = score, Next = next };
                }
                cache[mask] = best;
                return best;
            }
            var all = (1UL << hand.Length) - 1;
            ulong selected = 0;
            var maximum = -1;
            var globalUpper = mode == OpeningMode.Pairs ? (hand.Length - (reserveDiscard ? 1 : 0)) / 2 : maximizeTiles ? hand.Length - (reserveDiscard ? 1 : 0) : values.Sum() - (reserveDiscard ? values.Min() : 0);
            foreach (var index in reserveDiscard ? Enumerable.Range(0, hand.Length).OrderBy(index => values[index]) : Enumerable.Repeat(-1, 1))
            {
                if (maximum >= globalUpper) break;
                var mask = index < 0 ? all : all ^ (1UL << index);
                var score = Solve(mask).Score;
                if (score > maximum) { maximum = score; selected = mask; }
            }
            var result = new List<Meld>();
            while (selected != 0)
            {
                var choice = Solve(selected);
                if (choice.Candidate != null) result.Add(choice.Candidate.Meld);
                selected = choice.Next;
            }
            return new MeldPlan(result);
        }
    }
}
