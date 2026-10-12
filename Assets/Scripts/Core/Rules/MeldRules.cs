using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public sealed class MeldRules : IMeldRules
    {
        private readonly OkeyRules _rules;
        public StoneData Joker { get; }
        private readonly StoneData _indicator;

        public MeldRules(OkeyRules rules, StoneInstance indicator)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (indicator == null || indicator.Data.Type != StoneType.Regular ||
                indicator.Data.Color == StoneColor.None || indicator.Data.Value < rules.MinimumValue || indicator.Data.Value > rules.MaximumValue)
                throw new ArgumentException("A regular indicator within the configured range is required.", nameof(indicator));
            _indicator = indicator.Data;
            Joker = new StoneData(StoneType.Regular, indicator.Data.Color,
                indicator.Data.Value == rules.MaximumValue ? rules.MinimumValue : indicator.Data.Value + 1);
        }

        public bool IsWild(StoneInstance stone) => stone.Data.Type == StoneType.Regular &&
            stone.Data.Color == Joker.Color && stone.Data.Value == Joker.Value;

        public StoneData Resolve(StoneInstance stone) => stone.Data.Type == StoneType.FakeJoker ? Joker : stone.Data;

        public bool TryCreate(IEnumerable<StoneInstance> stones, MeldKind kind, out Meld meld)
        {
            meld = null;
            if (stones == null) return false;
            var hand = stones.ToArray();
            if (hand.Any(stone => stone == null) || hand.Select(stone => stone.Id).Distinct().Count() != hand.Length)
                return false;
            var indicatorPair = kind == MeldKind.Pair && _rules.AllowsIndicatorPairs;
            bool IsPairIndicator(StoneInstance stone) => indicatorPair && stone.Data.Type == StoneType.Regular &&
                stone.Data.Color == _indicator.Color && stone.Data.Value == _indicator.Value;
            var fixedTiles = hand.Where(stone => !IsWild(stone) && !IsPairIndicator(stone)).ToArray();
            if (fixedTiles.Any(stone => Resolve(stone).Color == StoneColor.None || Resolve(stone).Value < _rules.MinimumValue ||
                Resolve(stone).Value > _rules.MaximumValue)) return false;
            var wild = new Queue<StoneInstance>(hand.Where(IsWild));
            if (fixedTiles.Length == 0 && (kind != MeldKind.Pair || hand.Length != 2))
                return false;
            var first = fixedTiles.Length == 0 ? (hand.Any(IsPairIndicator) ? _indicator : Joker) : Resolve(fixedTiles[0]);
            var placed = new List<MeldTile>();
            if (kind == MeldKind.Pair)
            {
                if (hand.Length != 2 || fixedTiles.Any(stone => Resolve(stone).Color != first.Color || Resolve(stone).Value != first.Value))
                    return false;
                placed.AddRange(hand.Select(stone => new MeldTile(stone, first.Color, first.Value)));
            }
            else if (kind == MeldKind.Set)
            {
                if (hand.Length < 3 || hand.Length > 4 || fixedTiles.Any(stone => Resolve(stone).Value != first.Value) ||
                    fixedTiles.Select(stone => Resolve(stone).Color).Distinct().Count() != fixedTiles.Length)
                    return false;
                placed.AddRange(fixedTiles.Select(stone => new MeldTile(stone, Resolve(stone).Color, first.Value)));
                foreach (StoneColor color in Enum.GetValues(typeof(StoneColor)))
                    if (color != StoneColor.None && wild.Count > 0 && placed.All(tile => tile.Color != color))
                        placed.Add(new MeldTile(wild.Dequeue(), color, first.Value));
            }
            else if (kind == MeldKind.Run)
            {
                if (hand.Length < 3 || hand.Length > _rules.MaximumValue - _rules.MinimumValue + 1 ||
                    fixedTiles.Any(stone => Resolve(stone).Color != first.Color) ||
                    fixedTiles.Select(stone => Resolve(stone).Value).Distinct().Count() != fixedTiles.Length)
                    return false;
                var low = fixedTiles.Min(stone => Resolve(stone).Value);
                var high = fixedTiles.Max(stone => Resolve(stone).Value);
                var start = Math.Min(low, _rules.MaximumValue - hand.Length + 1);
                var values = Enumerable.Range(Math.Max(_rules.MinimumValue, start), hand.Length).ToArray();
                var fits = start >= _rules.MinimumValue && values.Last() <= _rules.MaximumValue && values.Last() >= high;
                if (!fits && _rules.AllowsHighOne)
                {
                    start = _rules.MaximumValue - hand.Length + 2;
                    values = Enumerable.Range(start, hand.Length - 1).Append(_rules.MinimumValue).ToArray();
                    fits = start > _rules.MinimumValue && fixedTiles.All(stone => values.Contains(Resolve(stone).Value));
                }
                if (!fits) return false;
                foreach (var value in values)
                {
                    var stone = fixedTiles.FirstOrDefault(tile => Resolve(tile).Value == value);
                    if (stone == null && wild.Count == 0) return false;
                    placed.Add(new MeldTile(stone ?? wild.Dequeue(), first.Color, value));
                }
            }
            else return false;
            meld = new Meld(kind, placed);
            return true;
        }

        public bool TryReplaceJoker(Meld meld, StoneInstance replacement, out Meld replaced, out StoneInstance joker)
        {
            replaced = null;
            joker = null;
            if (meld == null || replacement == null || IsWild(replacement) ||
                meld.Tiles.Any(tile => tile.Stone.Id == replacement.Id)) return false;
            var data = Resolve(replacement);
            var target = meld.Tiles.FirstOrDefault(tile => IsWild(tile.Stone) && tile.Color == data.Color && tile.Value == data.Value);
            if (target == null) return false;
            joker = target.Stone;
            replaced = new Meld(meld.Kind, meld.Tiles.Select(tile => tile == target
                ? new MeldTile(replacement, tile.Color, tile.Value) : tile));
            return true;
        }

        public bool TryExtend(Meld meld, StoneInstance stone, out Meld extended)
        {
            extended = null;
            if (meld == null || stone == null || Resolve(stone).Color == StoneColor.None ||
                Resolve(stone).Value < _rules.MinimumValue || Resolve(stone).Value > _rules.MaximumValue ||
                meld.Kind == MeldKind.Pair || meld.Tiles.Any(tile => tile.Stone.Id == stone.Id))
                return false;
            var data = Resolve(stone);
            var tiles = meld.Tiles.ToList();
            if (meld.Kind == MeldKind.Set)
            {
                if (tiles.Count == 4)
                    return false;
                var color = IsWild(stone) ? Enumerable.Range(0, 4).Select(value => (StoneColor)value)
                    .First(value => tiles.All(tile => tile.Color != value)) : data.Color;
                if ((!IsWild(stone) && data.Value != tiles[0].Value) || tiles.Any(tile => tile.Color == color))
                    return false;
                tiles.Add(new MeldTile(stone, color, tiles[0].Value));
            }
            else
            {
                var low = tiles[0].Value;
                var high = tiles[tiles.Count - 1].Value;
                var value = IsWild(stone) ? (high < _rules.MaximumValue ? high + 1 : low - 1) : data.Value;
                if ((!IsWild(stone) && data.Color != tiles[0].Color) || value < _rules.MinimumValue ||
                    value > _rules.MaximumValue || (value != low - 1 && value != high + 1))
                    return false;
                var tile = new MeldTile(stone, tiles[0].Color, value);
                if (value < low) tiles.Insert(0, tile); else tiles.Add(tile);
            }
            extended = new Meld(meld.Kind, tiles);
            return true;
        }
    }
}
