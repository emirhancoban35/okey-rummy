using System;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Decks;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
namespace Okey.Tests.EditMode
{
    public sealed class TableOptionsTests
    {
        private static TableSession Table(int start = 0, bool partners = false)
        {
            var deck = new DeckFactory().Create(new DeckDefinition(Enumerable.Range(0, 4).Select(color => new StoneSetDefinition(StoneType.Regular, (StoneColor)color, 1, 13, 2)).Append(new StoneSetDefinition(StoneType.FakeJoker, StoneColor.None, 0, 0, 2))));
            return new TableSession(new OkeyRules(4, 21, 1, 13, 1, 5, 101, 202, 101, -101, 101, 2, 2), deck, 28,
                startingSeat: start, options: new TableOptions(partners, true, 10), game: new Okey.Core.Games.Okey101Game());
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void EverySeatCanStartAndReceivesTheExtraTile(int seat)
        {
            var table = Table(seat);
            Assert.That(table.ActiveSeat, Is.EqualTo(seat));
            Assert.That(table.Players[seat].Hand.Count, Is.EqualTo(22));
            Assert.That(table.Players.Where((player, index) => index != seat).All(player => player.Hand.Count == 21), Is.True);
            Assert.That(table.RemainingCount, Is.EqualTo(20));
        }
        [Test]
        public void PartnershipsUseOppositeSeats()
        {
            var table = Table(partners: true);
            Assert.That(table.TeamOf(0), Is.EqualTo(table.TeamOf(2)));
            Assert.That(table.TeamOf(1), Is.EqualTo(table.TeamOf(3)));
            Assert.That(table.TeamOf(0), Is.Not.EqualTo(table.TeamOf(1)));
        }
        [Test]
        public void ClosedPlayerCanInspectADiscardButInvalidImmediateLayoffDoesNotChangeTable()
        {
            var table = Table();
            table.TryDiscard(0, table.Players[0].Hand.Slots.First(stone => stone != null).Id);
            var stock = table.RemainingCount;
            Assert.That(table.CanTakeDiscard(1), Is.True);
            Assert.That(table.TryTakeDiscard(1, 0), Is.False);
            Assert.That(table.Players[0].Discards.Count, Is.EqualTo(1));
            Assert.That(table.RemainingCount, Is.EqualTo(stock));
            Assert.That(table.Players[1].Hand.Count, Is.EqualTo(21));
        }
        [Test]
        public void TimerExpiresOnceAndResetsForNextSeat()
        {
            var table = Table();
            var clock = new TurnClock(table);
            Assert.That(clock.Advance(9), Is.False);
            Assert.That(clock.Advance(1), Is.True);
            Assert.That(clock.Advance(1), Is.False);
            table.TryDiscard(0, table.Players[0].Hand.Slots.First(stone => stone != null).Id);
            Assert.That(clock.Advance(0), Is.False);
            Assert.That(clock.Remaining, Is.EqualTo(10));
            table.TryDraw(1);
            clock.Advance(3);
            Assert.That(clock.Remaining, Is.EqualTo(7));
        }
        [Test]
        public void SuccessfulOpeningRefreshesTimerButInvalidOpeningDoesNot()
        {
            var table = Table();
            var clock = new TurnClock(table);
            clock.Advance(7);
            table.TryOpen(0, OpeningMode.Series, Array.Empty<Meld>());
            clock.Advance(0);
            Assert.That(clock.Remaining, Is.EqualTo(3));
            var stones = table.Players[0].Hand.Slots.Where(stone => stone != null && stone.Data.Value >= 3 && stone.Data.Value <= 5).GroupBy(stone => stone.Data.Value).Select(group => group.First()).ToArray();
            Assert.That(table.MeldRules.TryCreate(stones, MeldKind.Run, out var meld), Is.True);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { meld }), Is.True);
            clock.Advance(0);
            Assert.That(clock.Remaining, Is.EqualTo(10));
        }
        [Test]
        public void OpenPlayerTakesOnlyPreviousDiscardAndImmediatelyExtendsAMeld()
        {
            StoneInstance Tile(int id, int value, StoneColor color) => new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            var deck = Enumerable.Range(0, 40).Select(id => Tile(id, id % 13 + 1, StoneColor.Black)).ToArray();
            deck[0] = Tile(0, 13, StoneColor.Black);
            var values = new[] { 3,4,5,9,6,3,4,5,9 };
            for (var index = 0; index < values.Length; index++) deck[index + 1] = Tile(index + 1, values[index], index < 5 ? StoneColor.Red : StoneColor.Blue);
            deck[21] = Tile(21, 6, StoneColor.Red);
            var rules = new OkeyRules(4,4,1,13,1,5,101,202,101,-101,101,2,2);
            var table = new TableSession(rules, deck, 8, game: new Okey.Core.Games.Okey101Game());
            table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var red);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { red }), Is.True);
            table.TryDiscard(0, 5);
            Assert.That(table.TryTakeDiscard(1, 0), Is.False);
            table.TryDraw(1);
            table.MeldRules.TryCreate(deck.Skip(6).Take(3), MeldKind.Run, out var blue);
            Assert.That(table.TryOpen(1, OpeningMode.Series, new[] { blue }), Is.True);
            table.TryDiscard(1, 9);
            for (var seat = 2; seat < 4; seat++)
            {
                table.TryDraw(seat);
                table.TryDiscard(seat, table.Players[seat].Hand.Slots.First(stone => stone != null).Id);
            }
            table.TryDraw(0, out var drawn);
            Assert.That(drawn.Id, Is.EqualTo(21));
            table.TryDiscard(0, drawn.Id);
            var before = table.RemainingCount;
            Assert.That(table.TryTakeDiscard(1, 999), Is.False);
            Assert.That(table.TryTakeDiscard(1, 0), Is.True);
            Assert.That(table.Players[0].Discards.Last().Id, Is.EqualTo(5));
            Assert.That(table.Melds[0].Tiles.Last().Stone.Id, Is.EqualTo(21));
            Assert.That(table.Players[1].Hand.Count, Is.EqualTo(1));
            Assert.That(table.RemainingCount, Is.EqualTo(before));
        }

        [Test]
        public void DenseDuplicateRunsReserveTheIsolatedDiscardAndKeepPhysicalIdsUnique()
        {
            var hand = Enumerable.Range(0,20).Select(id => new StoneInstance(id, new StoneData(StoneType.Regular, StoneColor.Red, 3 + id / 2))).Append(new StoneInstance(20,new StoneData(StoneType.Regular,StoneColor.Black,1))).ToArray();
            var rules = new OkeyRules(4,21,1,13,101,5,101,202,101,-101,101,2,2);
            var meldRules = new MeldRules(rules,new StoneInstance(99,new StoneData(StoneType.Regular,StoneColor.Blue,13)));
            var plan = new MeldPlanner().Find(hand,meldRules,rules,OpeningMode.Series);
            Assert.That(plan.Points, Is.EqualTo(150));
            Assert.That(plan.TileCount, Is.EqualTo(20));
            Assert.That(plan.Melds.SelectMany(meld => meld.Tiles).Select(tile => tile.Stone.Id).Distinct().Count(),Is.EqualTo(20));
        }

        private sealed class FixedScoring : IRoundScoring
        {
            public int RemainingValue(TablePlayer player) => 0;
            public System.Collections.Generic.IReadOnlyList<int> Calculate(System.Collections.Generic.IReadOnlyList<TablePlayer> players, int winner, bool jokerFinish) => new[] { 10,20,30,40 };
        }
        [Test]
        public void WinningPartnerDoesNotKeepTheUnopenedHandScore()
        {
            var table = Table(partners: true);
            var scores = new PartnershipScoring(new FixedScoring()).Calculate(table.Players, 1, false);
            Assert.That(scores, Is.EqualTo(new[] {10,20,30,0}));
            Assert.That(new PartnershipScoring(new FixedScoring()).Calculate(table.Players, -1, false), Is.EqualTo(new[] {10,20,30,40}));
        }

        [TestCase(-1)] [TestCase(4)]
        public void InvalidStartingSeatIsRejected(int seat) => Assert.Throws<ArgumentOutOfRangeException>(() => Table(seat));
        [Test]
        public void InvalidTimerDurationIsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new TableOptions(turnSeconds: double.NaN));
    }
}
