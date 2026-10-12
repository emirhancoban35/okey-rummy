using System.Linq;
using NUnit.Framework;
using Okey.Core.Rules;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class IndicatorPairTests
    {
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static OkeyRules Settings(bool allowed) => new OkeyRules(4, 21, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2,
            allowsIndicatorPairs: allowed);

        [TestCase(true)]
        [TestCase(false)]
        public void AnIndicatorCopyCanCompleteAnUnrelatedPairOnlyWhenEnabled(bool allowed)
        {
            var rules = new MeldRules(Settings(allowed), Tile(0, 5));
            var indicator = Tile(1, 5);
            var other = Tile(2, 11, StoneColor.Blue);
            Assert.That(rules.TryCreate(new[] { indicator, other }, MeldKind.Pair, out var pair), Is.EqualTo(allowed));
            Assert.That(rules.IsWild(indicator), Is.False);
            if (allowed)
            {
                Assert.That(pair.Tiles.Select(tile => tile.Value), Is.All.EqualTo(11));
                Assert.That(pair.Tiles.Select(tile => tile.Color), Is.All.EqualTo(StoneColor.Blue));
                Assert.That(rules.TryReplaceJoker(pair, Tile(3, 11, StoneColor.Blue), out _, out _), Is.False);
            }
            Assert.That(rules.TryCreate(new[] { indicator, Tile(4, 3), Tile(5, 4) }, MeldKind.Run, out _), Is.True);
            Assert.That(rules.TryCreate(new[] { indicator, Tile(6, 8), Tile(7, 10) }, MeldKind.Run, out _), Is.False);
        }

        [Test]
        public void PlannerAndAdjacentRackEvaluationUseTheSameIndicatorPairRule()
        {
            var settings = Settings(true);
            var rules = new MeldRules(settings, Tile(0, 5));
            var tiles = new[] { Tile(1, 5), Tile(2, 11, StoneColor.Blue), Tile(3, 2, StoneColor.Black) };
            var plan = new MeldPlanner().Find(tiles, rules, settings, OpeningMode.Pairs);
            Assert.That(plan.Melds.Count, Is.EqualTo(1));
            Assert.That(plan.Melds[0].Tiles.Select(tile => tile.Stone.Id), Does.Contain(1));
            Assert.That(plan.TileCount, Is.EqualTo(2));
            var evaluator = new RackMeldEvaluator();
            Assert.That(evaluator.Find(tiles, 16, rules, OpeningMode.Pairs).Melds.Count, Is.EqualTo(1));
            Assert.That(evaluator.Find(new[] { tiles[0], null, tiles[1], tiles[2] }, 16, rules, OpeningMode.Pairs).Melds.Count, Is.Zero);
        }
    }
}
