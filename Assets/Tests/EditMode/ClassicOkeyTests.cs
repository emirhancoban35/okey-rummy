using System;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;

namespace Okey.Tests.EditMode
{
    public sealed class ClassicOkeyTests
    {
        private static OkeyRules Rules() => new OkeyRules(4, 14, 1, 13, 14, 7, 0, 2, 0, 0, 0, 2, 2, true);
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static StoneInstance Indicator => Tile(1000, 12, StoneColor.Black);
        private static StoneInstance[] Series() => new[]
        {
            Tile(1, 1), Tile(2, 2), Tile(3, 3), Tile(4, 4),
            Tile(5, 5, StoneColor.Blue), Tile(6, 6, StoneColor.Blue), Tile(7, 7, StoneColor.Blue), Tile(8, 8, StoneColor.Blue),
            Tile(9, 9, StoneColor.Yellow), Tile(10, 10, StoneColor.Yellow), Tile(11, 11, StoneColor.Yellow),
            Tile(12, 4, StoneColor.Black), Tile(13, 5, StoneColor.Black), Tile(14, 6, StoneColor.Black)
        };
        private static TableSession Table(StoneInstance[] hand = null, bool partners = false) => new TableSession(Rules(),
            new[] { Indicator }.Concat(hand ?? Series().Append(Tile(15, 9)).ToArray())
                .Concat(Enumerable.Range(100, 90).Select(id => Tile(id, id % 13 + 1, (StoneColor)(id % 4)))).ToArray(),
            28, game: new ClassicOkeyGame(new MeldPlanner()), options: new TableOptions(partnerships: partners));
        [Test]
        public void ClassicDealUsesFifteenAndFourteenTiles()
        {
            var table = Table();
            Assert.That(table.Players.Select(player => player.Hand.Count), Is.EqualTo(new[] {15, 14, 14, 14}));
            Assert.That(table.RemainingCount, Is.EqualTo(48));
            Assert.That(table.TryDraw(0), Is.False);
        }
        [Test]
        public void ClosedPlayersCanPickUpOnlyThePreviousDiscardIntoTheirRack()
        {
            var table = Table();
            table.TryDiscard(0, 1);
            var pile = table.RemainingCount;
            Assert.That(table.CanTakeDiscard(0), Is.False);
            Assert.That(table.CanTakeDiscard(1), Is.True);
            Assert.That(table.TryTakeDiscard(1), Is.True);
            Assert.That(table.Players[1].Hand.FindSlot(1), Is.GreaterThanOrEqualTo(0));
            Assert.That(table.Players[0].Discards, Is.Empty);
            Assert.That(table.Players[1].Hand.Count, Is.EqualTo(15));
            Assert.That(table.RemainingCount, Is.EqualTo(pile));
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.AwaitingDiscard));
            Assert.That(table.TryTakeDiscard(1), Is.False);
            Assert.That(table.TryDraw(1), Is.False);
        }
        [Test]
        public void ClassicOpeningAndLayoffCannotChangeTheClosedHandOrApply101Penalties()
        {
            var table = Table();
            table.MeldRules.TryCreate(Series().Take(3), MeldKind.Run, out var meld);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] {meld}), Is.False);
            Assert.That(table.TryOpen(0, OpeningMode.Pairs, Array.Empty<Meld>()), Is.False);
            Assert.That(table.TryLayOff(0, 4, 0), Is.False);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(15));
            Assert.That(table.Players[0].Opening, Is.EqualTo(OpeningMode.Closed));
            Assert.That(table.Players[0].Penalty, Is.Zero);
            Assert.That(table.Melds, Is.Empty);
        }
        [Test]
        public void DiscardingTheSpareTileWinsWithAllFourteenTilesCoveredAndRetained()
        {
            var table = Table();
            Assert.That(table.TryFindFinish(0, out var spare), Is.True);
            Assert.That(spare, Is.EqualTo(15));
            Assert.That(table.TryDiscard(0, spare), Is.True);
            Assert.That(table.Winner, Is.Zero);
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.Finished));
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(14));
            Assert.That(table.Players.Select(player => player.Score), Is.EqualTo(new[] {0, 2, 2, 2}));
            Assert.That(table.TryDraw(0), Is.False);
            Assert.That(table.TryDiscard(0, 1), Is.False);
        }
        [Test]
        public void DiscardingARequiredTileKeepsTheRoundGoing()
        {
            var table = Table();
            Assert.That(table.TryDiscard(0, 1), Is.True);
            Assert.That(table.Winner, Is.EqualTo(-1));
            Assert.That(table.ActiveSeat, Is.EqualTo(1));
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
        }
        [Test]
        public void SevenPairsAndJokerDiscardMultiplyRoundScore()
        {
            var pairs = Enumerable.Range(1, 14).Select(id => Tile(id, (id + 1) / 2)).ToArray();
            var table = Table(pairs.Append(Tile(15, 13, StoneColor.Black)).ToArray());
            Assert.That(table.TryDiscard(0, 15), Is.True);
            Assert.That(table.Winner, Is.Zero);
            Assert.That(table.Players[0].Opening, Is.EqualTo(OpeningMode.Pairs));
            Assert.That(table.Players.Select(player => player.Score), Is.EqualTo(new[] {0, 8, 8, 8}));
        }
        [Test]
        public void HighOneCanEndARunButCannotWrapIntoTwo()
        {
            var meldRules = new MeldRules(Rules(), Indicator);
            Assert.That(meldRules.TryCreate(new[] {Tile(1, 12), Tile(2, 13), Tile(3, 1)}, MeldKind.Run, out var high), Is.True);
            Assert.That(high.Tiles.Select(tile => tile.Value), Is.EqualTo(new[] {12, 13, 1}));
            Assert.That(meldRules.TryCreate(new[] {Tile(1, 13), Tile(2, 1), Tile(3, 2)}, MeldKind.Run, out _), Is.False);
            Assert.That(meldRules.TryCreate(new[] {Tile(1, 1), Tile(2, 2), Tile(3, 3)}, MeldKind.Run, out _), Is.True);
        }
        [Test]
        public void FinishPlannerFindsHighOneAndWildcardsWithoutReusingTiles()
        {
            var hand = Series();
            hand[0] = Tile(1, 11); hand[1] = Tile(2, 12); hand[2] = Tile(3, 13); hand[3] = Tile(4, 1);
            hand[5] = Tile(6, 13, StoneColor.Black);
            var game = new ClassicOkeyGame(new MeldPlanner());
            var rules = new MeldRules(Rules(), Indicator);
            Assert.That(game.TryFinish(hand, rules, Rules(), out var plan, out var mode), Is.True);
            Assert.That(mode, Is.EqualTo(OpeningMode.Series));
            Assert.That(plan.TileCount, Is.EqualTo(14));
            Assert.That(plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).Distinct().Count(), Is.EqualTo(14));
            Assert.That(game.TryFinish(hand.Take(13).ToArray(), rules, Rules(), out _, out _), Is.False);
        }
        [Test]
        public void FakeJokerRemainsAFixedTileAndCannotRepairAnUnrelatedRun()
        {
            var hand = Series();
            hand[1] = new StoneInstance(2, new StoneData(StoneType.FakeJoker, StoneColor.None, 0));
            var game = new ClassicOkeyGame(new MeldPlanner());
            Assert.That(game.TryFinish(hand, new MeldRules(Rules(), Indicator), Rules(), out _, out _), Is.False);
        }
        [Test]
        public void PartnershipFinishDoesNotChargeTheWinningPartner()
        {
            var table = Table(partners: true);
            table.TryDiscard(0, 15);
            Assert.That(table.Players.Select(player => player.Score), Is.EqualTo(new[] {0, 2, 0, 2}));
            Assert.That(table.TeamScore(0), Is.Zero);
            Assert.That(table.TeamScore(1), Is.EqualTo(4));
        }
        [Test]
        public void EmptyStockStillAllowsTheLastDiscardAndDecliningItDrawsTheRound()
        {
            var table = Table();
            while (table.RemainingCount > 0)
            {
                var seat = table.ActiveSeat;
                if (table.Phase == TurnPhase.AwaitingDraw) table.TryDraw(seat);
                var stones = table.Players[seat].Hand.Slots.Where(tile => tile != null).ToArray();
                var hasFinish = table.TryFindFinish(seat, out var finishing);
                var discard = hasFinish ? stones.First(tile => tile.Id != finishing) : stones[0];
                table.TryDiscard(seat, discard.Id);
                Assert.That(table.Winner, Is.EqualTo(-1));
            }
            var next = table.ActiveSeat;
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
            Assert.That(table.CanDraw(next), Is.False);
            Assert.That(table.CanTakeDiscard(next), Is.True);
            Assert.That(table.TryTakeDiscard(next), Is.True);
            var tiles = table.Players[next].Hand.Slots.Where(tile => tile != null).ToArray();
            var complete = table.TryFindFinish(next, out var spare);
            table.TryDiscard(next, complete ? tiles.First(tile => tile.Id != spare).Id : tiles[0].Id);
            Assert.That(table.CanEndDrawnRound(table.ActiveSeat), Is.True);
            Assert.That(table.TryDraw(table.ActiveSeat), Is.False);
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.Finished));
            Assert.That(table.Players.Select(player => player.Score), Is.EqualTo(new[] {0, 0, 0, 0}));
        }
        [Test]
        public void ClassicBotKeepsTheCompleteHandAndChoosesTheWinningDiscard()
        {
            var table = Table();
            var observation = new PlayerObservation(table.Players[0].Hand.Slots.Where(tile => tile != null),
                table.Melds, OpeningMode.Closed, table.Rules, table.MeldRules);
            var strategy = new ClassicOkeyStrategy(new MeldPlanner());
            Assert.That(strategy.ChooseDiscard(observation), Is.EqualTo(15));
        }
    }
}
