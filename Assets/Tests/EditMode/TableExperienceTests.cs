using System.Linq;
using NUnit.Framework;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
namespace Okey.Tests.EditMode
{
    public sealed class TableExperienceTests
    {
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static OkeyRules Rules(int hand = 7, DiscardPenalties penalties = null) =>
            new OkeyRules(4, hand, 1, 13, 1, 1, 101, 202, 101, -101, 101, 2, 2, discardPenalties: penalties);
        private static TableSession OpenTable(DiscardPenalties penalties = null, System.Action<TableActionNotice> onAction = null)
        {
            var deck = Enumerable.Range(0, 50).Select(id => Tile(id, 8, StoneColor.Black)).ToArray();
            deck[0] = Tile(0, 13, StoneColor.Blue);
            for (var value = 1; value <= 3; value++) deck[value] = Tile(value, value);
            deck[4] = Tile(4, 1, StoneColor.Blue);
            deck[5] = Tile(5, 4);
            var table = new TableSession(Rules(penalties: penalties), deck, 16, new Okey101Game());
            table.ActionCompleted += onAction;
            table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var meld);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { meld }), Is.True);
            return table;
        }
        [TestCase(4, 17)]
        [TestCase(5, 29)]
        public void JokerAndPlayableDiscardPenaltiesAreConfiguredIndependently(int tile, int expected)
        {
            var table = OpenTable(new DiscardPenalties(17, 29));
            Assert.That(table.TryDiscard(0, tile), Is.True);
            Assert.That(table.Players[0].Penalty, Is.EqualTo(expected));
        }
        [Test]
        public void ZeroPenaltiesDisableBothDiscardCharges()
        {
            var table = OpenTable(new DiscardPenalties(0, 0));
            table.TryDiscard(0, 4);
            Assert.That(table.Players[0].Penalty, Is.Zero);
        }
        [Test]
        public void TimeoutProtectsWildAndPlayableTilesBeforeChoosingTheLowestSafeValue()
        {
            var table = OpenTable();
            var observation = new PlayerObservation(new[] { Tile(80, 1, StoneColor.Blue), Tile(81, 4),
                Tile(82, 12, StoneColor.Black), Tile(83, 2, StoneColor.Black) }, table.Melds,
                OpeningMode.Series, table.Rules, table.MeldRules);
            Assert.That(new TimeoutDiscardPolicy().ChooseDiscard(observation), Is.EqualTo(83));
        }
        [Test]
        public void TimeoutUsesAPlayableTileBeforeSacrificingAJokerWhenEveryTileIsRisky()
        {
            var table = OpenTable();
            var observation = new PlayerObservation(new[] { Tile(80, 1, StoneColor.Blue), Tile(81, 4) },
                table.Melds, OpeningMode.Series, table.Rules, table.MeldRules);
            Assert.That(new TimeoutDiscardPolicy().ChooseDiscard(observation), Is.EqualTo(81));
        }
        [Test]
        public void OpeningAnnouncementsContainThePublicValueAndPreserveTheFirstOpening()
        {
            TableActionNotice notice = null;
            var table = OpenTable(onAction: value => notice = value);
            Assert.That(notice.Kind, Is.EqualTo(PlayerActionKind.Open));
            Assert.That(notice.InitialOpening, Is.True);
            Assert.That(notice.Value, Is.EqualTo(6));
            Assert.That(table.Players[0].OpeningValue, Is.EqualTo(6));
            table.ActionCompleted += value => notice = value;
            Assert.That(table.TryDiscard(0, 6), Is.True);
            Assert.That(notice.Kind, Is.EqualTo(PlayerActionKind.Discard));
            Assert.That(notice.Seat, Is.Zero);
            Assert.That(table.Players[0].OpeningValue, Is.EqualTo(6));
        }
        private static TableSession FinishedTable()
        {
            var deck = Enumerable.Range(0, 30).Select(id => Tile(id, 8, StoneColor.Black)).ToArray();
            deck[0] = Tile(0, 13, StoneColor.Blue);
            for (var value = 1; value <= 3; value++) deck[value] = Tile(value, value);
            var table = new TableSession(Rules(3), deck, 8, new Okey101Game());
            table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var meld);
            table.TryOpen(0, OpeningMode.Series, new[] { meld });
            table.TryDiscard(0, 4);
            return table;
        }
        [Test]
        public void MatchRecordsEachFinishedHandOnceAndUsesTheConfiguredWinnerScore()
        {
            var ledger = new MatchScoreLedger(4, 2);
            var table = FinishedTable();
            Assert.That(ledger.Record(table), Is.True);
            Assert.That(ledger.Record(table), Is.False);
            Assert.That(ledger.Totals[0], Is.EqualTo(-101));
            Assert.That(ledger.Rounds[0].Openings[0], Is.EqualTo(6));
            Assert.That(ledger.Record(FinishedTable()), Is.True);
            Assert.That(ledger.Totals[0], Is.EqualTo(-202));
            Assert.That(ledger.Complete, Is.True);
            Assert.That(ledger.Record(FinishedTable()), Is.False);
        }
        [Test]
        public void UnfinishedHandsDoNotEnterTheScoreSheet()
        {
            var ledger = new MatchScoreLedger(4, 5);
            Assert.That(ledger.Record(OpenTable()), Is.False);
            Assert.That(ledger.Rounds, Is.Empty);
        }
        [Test]
        public void GroupInsertionShiftsNeighborsAndPreservesEveryPhysicalTile()
        {
            var tiles = Enumerable.Range(0, 8).Select(id => Tile(id, id + 1)).ToArray();
            var rack = new RackArrangement(tiles, 16);
            rack.MoveGroup(new[] { 1, 2, 3 }, 4, 8);
            Assert.That(rack.Slots.Take(8).Select(tile => tile.Id), Is.EqualTo(new[] { 0, 4, 5, 6, 1, 2, 3, 7 }));
            Assert.That(rack.Slots.Where(tile => tile != null).Select(tile => tile.Id), Is.EquivalentTo(tiles.Select(tile => tile.Id)));
        }
        [Test]
        public void GroupMovesAcrossRowsAsAContiguousBlock()
        {
            var rack = new RackArrangement(Enumerable.Range(0, 10).Select(id => Tile(id, id % 13 + 1)), 16);
            rack.MoveGroup(new[] { 1, 2, 3 }, 10, 8);
            Assert.That(new[] { rack.FindSlot(1), rack.FindSlot(2), rack.FindSlot(3) }, Is.EqualTo(new[] { 10, 11, 12 }));
            Assert.That(rack.Count, Is.EqualTo(10));
        }
        [Test]
        public void GroupDropIntoAFullRowOrAcrossARowBoundaryIsAtomic()
        {
            var rack = new RackArrangement(Enumerable.Range(0, 16).Select(id => Tile(id, id % 13 + 1)), 16);
            var before = rack.Slots.ToArray();
            Assert.That(rack.MoveGroup(new[] { 1, 2, 3 }, 10, 8), Is.Empty);
            Assert.That(rack.MoveGroup(new[] { 1, 2, 3 }, 7, 8), Is.Empty);
            Assert.That(rack.Slots, Is.EqualTo(before));
        }
        [Test]
        public void RackGroupsIncludeIncompleteOpeningRunsAndPairsWithoutReservingATile()
        {
            var table = OpenTable();
            var slots = new[] { Tile(100, 3), Tile(101, 4), Tile(102, 5), null,
                Tile(103, 9, StoneColor.Black), Tile(104, 9, StoneColor.Black) };
            var groups = new RackMeldEvaluator().FindGroups(slots, 8, table.MeldRules);
            Assert.That(groups.Melds.Select(meld => meld.Kind), Is.EquivalentTo(new[] { MeldKind.Run, MeldKind.Pair }));
            Assert.That(groups.TileCount, Is.EqualTo(5));
        }

    }
}
