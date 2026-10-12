using System;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Decks;
using Okey.Core.Stones;
using Okey.Core.Tables;
using Okey.Core.Rules;

namespace Okey.Tests.EditMode
{
    public sealed class TableSessionTests
    {
        private static OkeyRules Rules(int hand = 21, int threshold = 101) => new OkeyRules(4, hand, 1, 13, threshold, 5, 101, 202, 101, -101, 101, 2, 2);
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static StoneInstance[] Deck() => Enumerable.Range(0, 106).Select(id => id >= 104 ?
            new StoneInstance(id, new StoneData(StoneType.FakeJoker, StoneColor.None, 0)) :
            Tile(id, id % 13 + 1, (StoneColor)(id / 26))).ToArray();

        [Test]
        public void DealConservesAllPhysicalStonesAndStartsWithAnExtraTile()
        {
            var table = new TableSession(Rules(), Deck(), 28, game: new Okey.Core.Games.Okey101Game());
            Assert.That(table.Players.Select(player => player.Hand.Count), Is.EqualTo(new[] {22, 21, 21, 21}));
            Assert.That(table.RemainingCount, Is.EqualTo(20));
            Assert.That(table.Players.SelectMany(player => player.Hand.Slots).Where(tile => tile != null)
                .Select(tile => tile.Id).Append(table.Indicator.Id).Distinct().Count(), Is.EqualTo(86));
            Assert.That(table.TryDraw(0), Is.False);
            Assert.That(table.TryDiscard(1, table.Players[1].Hand.Slots[0].Id), Is.False);
            Assert.That(table.TryDiscard(0, table.Players[0].Hand.Slots[0].Id), Is.True);
            Assert.That(table.ActiveSeat, Is.EqualTo(1));
            Assert.That(table.TryDraw(1), Is.True);
            Assert.That(table.TryDraw(1), Is.False);
        }

        [Test]
        public void IndicatorWrapsAndFakeJokerIsFixed()
        {
            var rules = new MeldRules(Rules(), Tile(99, 13));
            Assert.That(rules.IsWild(Tile(1, 1)), Is.True);
            var fake = new StoneInstance(2, new StoneData(StoneType.FakeJoker, StoneColor.None, 0));
            Assert.That(rules.IsWild(fake), Is.False);
            Assert.That(rules.Resolve(fake).Value, Is.EqualTo(1));
            Assert.That(rules.TryCreate(new[] { Tile(3, 12), Tile(4, 13), fake }, MeldKind.Run, out _), Is.False);
        }

        [Test]
        public void RunsSetsAndPairsRejectDuplicatePhysicalTilesAndInvalidGroups()
        {
            var rules = new MeldRules(Rules(), Tile(99, 12, StoneColor.Black));
            Assert.That(rules.TryCreate(new[] {Tile(1, 7), Tile(2, 8), Tile(3, 9)}, MeldKind.Run, out var run), Is.True);
            Assert.That(run.Points, Is.EqualTo(24));
            Assert.That(rules.TryCreate(new[] {Tile(1, 7), Tile(2, 7), Tile(3, 7, StoneColor.Blue)}, MeldKind.Set, out _), Is.False);
            Assert.That(rules.TryCreate(new[] {Tile(1, 7), Tile(1, 7)}, MeldKind.Pair, out _), Is.False);
            Assert.That(rules.TryCreate(new[] {Tile(1, 7), Tile(2, 7)}, MeldKind.Pair, out _), Is.True);
        }

        [Test]
        public void TwoRealJokersFormAPairAndOutOfRangeTilesCannotMeld()
        {
            var rules = new MeldRules(Rules(), Tile(99, 12, StoneColor.Black));
            Assert.That(rules.TryCreate(new[] {Tile(1, 13, StoneColor.Black), Tile(2, 13, StoneColor.Black)},
                MeldKind.Pair, out _), Is.True);
            Assert.That(rules.TryCreate(new[] {Tile(1, 0), Tile(2, 1), Tile(3, 2)}, MeldKind.Run, out _), Is.False);
        }

        [Test]
        public void WildAssignmentsStayFixedWhenExtending()
        {
            var rules = new MeldRules(Rules(), Tile(99, 12, StoneColor.Black));
            rules.TryCreate(new[] {Tile(1, 7), Tile(2, 9), Tile(3, 13, StoneColor.Black)}, MeldKind.Run, out var run);
            Assert.That(run.Tiles[1].Value, Is.EqualTo(8));
            Assert.That(rules.TryExtend(run, Tile(4, 8), out _), Is.False);
            Assert.That(rules.TryExtend(run, Tile(4, 10), out var extended), Is.True);
            Assert.That(extended.Tiles[1], Is.SameAs(run.Tiles[1]));
        }

        [Test]
        public void PlannerFindsDisjointMeldsAndAlwaysKeepsADiscard()
        {
            var rules = Rules();
            var meldRules = new MeldRules(rules, Tile(99, 12, StoneColor.Black));
            var hand = new[] {Tile(1, 10), Tile(2, 11), Tile(3, 12), Tile(4, 10, StoneColor.Blue),
                Tile(5, 11, StoneColor.Blue), Tile(6, 12, StoneColor.Blue), Tile(7, 10)};
            var plan = new MeldPlanner().Find(hand, meldRules, rules, OpeningMode.Series);
            Assert.That(plan.Points, Is.EqualTo(66));
            Assert.That(plan.TileCount, Is.EqualTo(6));
            Assert.That(plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).Distinct().Count(), Is.EqualTo(6));
            var noDiscard = new MeldPlanner().Find(hand.Take(3), meldRules, rules, OpeningMode.Series);
            Assert.That(noDiscard.Melds, Is.Empty);
        }

        [Test]
        public void InvalidOpeningPenalizesWithoutChangingHandOrPublicMelds()
        {
            var table = new TableSession(Rules(), Deck(), 28, game: new Okey.Core.Games.Okey101Game());
            var ids = table.Players[0].Hand.Slots.Where(tile => tile != null).Select(tile => tile.Id).ToArray();
            Assert.That(table.TryOpen(0, OpeningMode.Series, Array.Empty<Meld>()), Is.False);
            Assert.That(table.Players[0].Penalty, Is.EqualTo(101));
            Assert.That(table.Players[0].Hand.Slots.Where(tile => tile != null).Select(tile => tile.Id), Is.EqualTo(ids));
            Assert.That(table.Melds, Is.Empty);
        }
        [Test]
        public void OpeningLayoffAndFinalDiscardScoreTheRound()
        {
            var deck = Deck();
            deck[0] = Tile(0, 13);
            deck[1] = Tile(1, 10);
            deck[2] = Tile(2, 11);
            deck[3] = Tile(3, 12);
            deck[4] = Tile(4, 10, StoneColor.Blue);
            deck[5] = Tile(5, 11, StoneColor.Blue);
            deck[6] = Tile(6, 12, StoneColor.Blue);
            deck[7] = Tile(7, 13);
            deck[8] = Tile(8, 2, StoneColor.Black);
            var table = new TableSession(Rules(7, 66), deck, 28, game: new Okey.Core.Games.Okey101Game());
            Assert.That(table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var first), Is.True);
            Assert.That(table.MeldRules.TryCreate(deck.Skip(4).Take(3), MeldKind.Run, out var second), Is.True);
            Assert.That(table.TryLayOff(0, 7, 0), Is.False);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] {first, second}), Is.True);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(2));
            Assert.That(table.TryLayOff(0, 7, 0), Is.True);
            Assert.That(table.TryLayOff(0, 8, 0), Is.False);
            Assert.That(table.TryDiscard(0, 8), Is.True);
            Assert.That(table.Winner, Is.Zero);
            Assert.That(table.Phase, Is.EqualTo(Okey.Core.Tables.TurnPhase.Finished));
            Assert.That(table.Players.Select(player => player.Score), Is.EqualTo(new[] {-101, 202, 202, 202}));
            Assert.That(table.TryDraw(0), Is.False);
            Assert.That(table.TryDiscard(0, 8), Is.False);
        }

        [Test]
        public void OpeningRevalidatesTileDataInsteadOfTrustingAnotherRulesInstance()
        {
            var table = new TableSession(Rules(21, 20), Deck(), 28, game: new Okey.Core.Games.Okey101Game());
            var forged = new[] {Tile(1, 10), Tile(2, 11), Tile(3, 12)};
            var foreignRules = new MeldRules(Rules(), Tile(999, 12, StoneColor.Black));
            Assert.That(foreignRules.TryCreate(forged, MeldKind.Run, out var meld), Is.True);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] {meld}), Is.False);
            Assert.That(table.Players[0].Penalty, Is.EqualTo(101));
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(22));
        }

        [Test]
        public void DiscardingAWildTileAppliesPenaltyWhileHandRemains()
        {
            var table = new TableSession(Rules(), Deck(), 28, game: new Okey.Core.Games.Okey101Game());
            Assert.That(table.Indicator.Data.Value, Is.EqualTo(1));
            Assert.That(table.MeldRules.IsWild(table.Players[0].Hand.Slots[0]), Is.True);
            Assert.That(table.TryDiscard(0, table.Players[0].Hand.Slots[0].Id), Is.True);
            Assert.That(table.Players[0].Penalty, Is.EqualTo(101));
        }

        [TestCase(7)]
        [TestCase(101)]
        [TestCase(2026)]
        public void StrategiesPlayCompleteRoundsWithoutDuplicatingOrLeakingTiles(int seed)
        {
            var table = new TableSession(Rules(), new SeededStoneShuffler().Shuffle(Deck(), seed), 28, game: new Okey.Core.Games.Okey101Game());
            var strategy = new MeldPlayerStrategy(new MeldPlanner());
            var steps = 0;
            PlayerObservation Observe(int seat) => new PlayerObservation(table.Players[seat].Hand.Slots.Where(tile => tile != null),
                table.Melds, table.Players[seat].Opening, table.Rules, table.MeldRules);
            while (table.Phase != Okey.Core.Tables.TurnPhase.Finished && steps++ < 100)
            {
                var seat = table.ActiveSeat;
                if (table.Phase == Okey.Core.Tables.TurnPhase.AwaitingDraw) Assert.That(table.TryDraw(seat), Is.True);
                var plan = strategy.PlanOpening(Observe(seat), out var mode);
                if (plan.Melds.Count > 0 && (table.Players[seat].Opening != OpeningMode.Closed ||
                    (mode == OpeningMode.Pairs ? plan.Melds.Count >= table.Rules.PairThreshold : plan.Points >= table.Rules.SeriesThreshold)))
                    Assert.That(table.TryOpen(seat, mode, plan.Melds), Is.True);
                foreach (var tile in table.Players[seat].Hand.Slots.Where(tile => tile != null).ToArray())
                    for (var meld = 0; meld < table.Melds.Count; meld++)
                        if (table.TryLayOff(seat, tile.Id, meld)) break;
                Assert.That(table.TryDiscard(seat, strategy.ChooseDiscard(Observe(seat))), Is.True);
                var publicAndHands = table.Players.SelectMany(player => player.Hand.Slots.Where(tile => tile != null)
                    .Concat(player.Discards)).Concat(table.Melds.SelectMany(meld => meld.Tiles.Select(tile => tile.Stone)))
                    .Append(table.Indicator).Select(tile => tile.Id).ToArray();
                Assert.That(publicAndHands.Distinct().Count(), Is.EqualTo(publicAndHands.Length));
                Assert.That(publicAndHands.Length + table.RemainingCount, Is.EqualTo(106));
            }
            Assert.That(steps, Is.LessThan(100));
            Assert.That(table.Phase, Is.EqualTo(Okey.Core.Tables.TurnPhase.Finished));
        }

    }
}
