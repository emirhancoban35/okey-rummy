using System.Linq;
using NUnit.Framework;
using Okey.Core.Games;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
namespace Okey.Tests.EditMode
{
    public sealed class DiscardTrialTests
    {
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static StoneInstance[] Deck()
        {
            var deck = Enumerable.Range(0, 50).Select(id => Tile(id, 8, StoneColor.Black)).ToArray();
            deck[0] = Tile(0, 13, StoneColor.Blue);
            deck[1] = Tile(1, 3); deck[2] = Tile(2, 4); deck[3] = Tile(3, 5); deck[4] = Tile(4, 6);
            deck[9] = Tile(9, 4); deck[10] = Tile(10, 5);
            return deck;
        }
        private static TableSession Table(StoneInstance[] deck, int threshold = 1, int start = 0, bool reclaim = true, int penalty = 101) =>
            new TableSession(new OkeyRules(2, 7, 1, 13, threshold, 1, 101, 202, 101, -101, 101, 2, 2,
                jokerReclaimPenalty: penalty), deck, 16, new Okey101Game(), startingSeat: start,
                options: new TableOptions(allowJokerReclaim: reclaim));
        private static Meld Meld(TableSession table, MeldKind kind, params StoneInstance[] tiles)
        {
            Assert.That(table.MeldRules.TryCreate(tiles, kind, out var meld), Is.True);
            return meld;
        }
        [Test]
        public void ClosedPlayerCanTryADiscardButMustUseItToReachTheOpeningThreshold()
        {
            var deck = Deck(); var table = Table(deck, 15);
            table.TryDiscard(0, 4); var stock = table.RemainingCount;
            Assert.That(table.TryTakeDiscard(1), Is.True);
            Assert.That(table.PendingDiscard.Id, Is.EqualTo(4));
            Assert.That(table.TryDiscard(1, 9), Is.False);
            Assert.That(table.TryDraw(1), Is.False);
            var run = Meld(table, MeldKind.Run, deck[9], deck[10], deck[4]);
            Assert.That(table.TryOpen(1, OpeningMode.Series, new[] { run }), Is.True);
            Assert.That(table.PendingDiscard, Is.Null);
            Assert.That(table.Players[1].OpeningValue, Is.EqualTo(15));
            Assert.That(table.TryDiscard(1, 11), Is.True);
            Assert.That(table.RemainingCount, Is.EqualTo(stock));
        }
        [Test]
        public void ReturningATrialRestoresTheSameDiscardAndDoesNotConsumeStock()
        {
            var table = Table(Deck(), 101); table.TryDiscard(0, 4);
            var before = table.Players[1].Hand.Slots.Where(tile => tile != null).Select(tile => tile.Id).ToArray();
            var stock = table.RemainingCount;
            table.TryTakeDiscard(1);
            Assert.That(table.TryReturnDiscard(0), Is.False);
            Assert.That(table.TryReturnDiscard(1), Is.True);
            Assert.That(table.TryReturnDiscard(1), Is.False);
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
            Assert.That(table.Players[0].Discards.Last().Id, Is.EqualTo(4));
            Assert.That(table.Players[1].Hand.Slots.Where(tile => tile != null).Select(tile => tile.Id), Is.EquivalentTo(before));
            Assert.That(table.RemainingCount, Is.EqualTo(stock));
            Assert.That(table.TryDraw(1), Is.True);
            Assert.That(table.RemainingCount, Is.EqualTo(stock - 1));
        }
        [Test]
        public void AFailedOpeningKeepsTheTrialAndCannotLoseOrDiscardTheBorrowedTile()
        {
            var deck = Deck(); var table = Table(deck, 101); table.TryDiscard(0, 4); table.TryTakeDiscard(1);
            Assert.That(table.TryOpen(1, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[9], deck[10], deck[4]) }), Is.False);
            Assert.That(table.Players[1].Penalty, Is.EqualTo(101));
            Assert.That(table.PendingDiscard.Id, Is.EqualTo(4));
            Assert.That(table.TryDiscard(1, 4), Is.False);
            Assert.That(table.TryReturnDiscard(1), Is.True);
        }
        [Test]
        public void TrialCannotBeCommittedByOpeningAnUnrelatedMeld()
        {
            var deck = Deck(); deck[11] = Tile(11, 6);
            var table = Table(deck); table.TryDiscard(0, 4); table.TryTakeDiscard(1);
            Assert.That(table.TryOpen(1, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[9], deck[10], deck[11]) }), Is.False);
            Assert.That(table.Melds, Is.Empty);
            Assert.That(table.PendingDiscard.Id, Is.EqualTo(4));
        }
        [Test]
        public void OpenPlayerCanBorrowAndExtendAnExistingRun()
        {
            var deck = Deck();
            deck[8] = Tile(8, 3, StoneColor.Blue); deck[9] = Tile(9, 4, StoneColor.Blue); deck[10] = Tile(10, 5, StoneColor.Blue);
            var table = Table(deck, start: 1);
            table.TryOpen(1, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[8], deck[9], deck[10]) });
            table.TryDiscard(1, 11); table.TryDraw(0);
            table.TryOpen(0, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[1], deck[2], deck[3]) });
            table.TryDiscard(0, 4); table.TryTakeDiscard(1);
            Assert.That(table.TryLayOff(1, 12, 1), Is.False);
            Assert.That(table.TryLayOff(1, 4, 1), Is.True);
            Assert.That(table.PendingDiscard, Is.Null);
            Assert.That(table.Melds[1].Tiles.Last().Stone.Id, Is.EqualTo(4));
        }
        [Test]
        public void SeriesPlayerCanAddABorrowedPairOnlyAfterAPairOpeningAndKeepsSeriesScoring()
        {
            var deck = Deck(); deck[1] = Tile(1, 9, StoneColor.Black); deck[2] = Tile(2, 9, StoneColor.Black);
            deck[8] = Tile(8, 3, StoneColor.Blue); deck[9] = Tile(9, 4, StoneColor.Blue); deck[10] = Tile(10, 5, StoneColor.Blue);
            deck[12] = Tile(12, 6);
            var table = Table(deck, start: 1);
            table.TryOpen(1, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[8], deck[9], deck[10]) });
            table.TryDiscard(1, 11); table.TryDraw(0);
            table.TryOpen(0, OpeningMode.Pairs, new[] { Meld(table, MeldKind.Pair, deck[1], deck[2]) });
            table.TryDiscard(0, 4); table.TryTakeDiscard(1);
            Assert.That(table.TryOpen(1, OpeningMode.Pairs, new[] { Meld(table, MeldKind.Pair, deck[4], deck[12]) }), Is.True);
            Assert.That(table.Players[1].Opening, Is.EqualTo(OpeningMode.Series));
            Assert.That(table.Players[1].OpeningValue, Is.EqualTo(12));
            Assert.That(table.PendingDiscard, Is.Null);
        }
        [TestCase(true, 101)]
        [TestCase(true, 0)]
        [TestCase(false, 101)]
        public void JokerReclaimPreservesOwnershipAndChargesItsOriginalOwnerWhenEnabled(bool allowed, int penalty)
        {
            var deck = Deck(); deck[2] = Tile(2, 1, StoneColor.Blue);
            deck[9] = Tile(9, 4); deck[10] = Tile(10, 5); deck[11] = Tile(11, 6); deck[12] = Tile(12, 4);
            var table = Table(deck, reclaim: allowed, penalty: penalty);
            table.TryOpen(0, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[1], deck[2], deck[3]) });
            table.TryDiscard(0, 5); table.TryDraw(1);
            Assert.That(table.TryReclaimJoker(1, 12, 0), Is.False);
            table.TryOpen(1, OpeningMode.Series, new[] { Meld(table, MeldKind.Run, deck[9], deck[10], deck[11]) });
            var slot = table.Players[1].Hand.FindSlot(12); TableActionNotice notice = null;
            table.ActionCompleted += value => notice = value;
            Assert.That(table.TryReclaimJoker(1, 12, 0), Is.EqualTo(allowed));
            Assert.That(table.Players[0].Penalty, Is.EqualTo(allowed ? penalty : 0));
            if (!allowed) return;
            Assert.That(table.Melds[0].Tiles[1].Stone.Id, Is.EqualTo(12));
            Assert.That(table.Players[1].Hand.FindSlot(2), Is.EqualTo(slot));
            Assert.That(table.Players[1].Hand.FindSlot(12), Is.EqualTo(-1));
            Assert.That(notice.PenaltySeat, Is.Zero);
            Assert.That(notice.Seat, Is.EqualTo(1));
            Assert.That(notice.PenaltyKind, Is.EqualTo(TablePenaltyKind.JokerReclaimed));
            Assert.That(table.TryReclaimJoker(1, 12, 0), Is.False);
        }
        [TestCase(MeldKind.Pair)]
        [TestCase(MeldKind.Set)]
        [TestCase(MeldKind.Run)]
        public void JokerExchangeUsesTheAssignedValueInEveryMeldKind(MeldKind kind)
        {
            var table = Table(Deck()); var joker = Tile(100, 1, StoneColor.Blue);
            StoneInstance[] tiles = kind == MeldKind.Pair ? new[] { Tile(101, 4), joker } :
                kind == MeldKind.Run ? new[] { Tile(101, 3), joker, Tile(102, 5) } :
                new[] { Tile(101, 4, StoneColor.Yellow), Tile(102, 4, StoneColor.Black), joker };
            var meld = Meld(table, kind, tiles); var assignment = meld.Tiles.Single(tile => tile.Stone.Id == joker.Id);
            Assert.That(table.MeldRules.TryReplaceJoker(meld, Tile(103, assignment.Value, assignment.Color), out var replaced, out var reclaimed), Is.True);
            Assert.That(reclaimed.Id, Is.EqualTo(joker.Id));
            Assert.That(replaced.Tiles.Any(tile => tile.Stone.Id == joker.Id), Is.False);
            Assert.That(table.MeldRules.TryReplaceJoker(meld, Tile(104, 12, StoneColor.Blue), out _, out _), Is.False);
        }
        [TestCase(15, PlayerActionKind.TakeDiscard)]
        [TestCase(101, PlayerActionKind.Draw)]
        public void BotTakesAClosedDiscardOnlyWhenItCanOpenUsingThatTile(int threshold, PlayerActionKind expected)
        {
            var deck = Deck();
            deck[11] = Tile(11, 11, StoneColor.Black); deck[12] = Tile(12, 2, StoneColor.Yellow);
            deck[13] = Tile(13, 9, StoneColor.Blue); deck[14] = Tile(14, 11, StoneColor.Yellow);
            var table = Table(deck, threshold); table.TryDiscard(0, 4);
            var policy = new BotDecisionPolicy(new MeldPlayerStrategy(new MeldPlanner()), new SeededRandomSource(42));
            var preferences = new BotPreferences(false, 0, 0, 1, 2, .1, 2);
            var action = policy.Decide(table.ObserveTurn(1), preferences, true);
            Assert.That(action.Kind, Is.EqualTo(expected));
            Assert.That(table.TryApply(1, action), Is.True);
            if (expected != PlayerActionKind.TakeDiscard) return;
            action = policy.Decide(table.ObserveTurn(1), preferences, true);
            Assert.That(action.Kind, Is.EqualTo(PlayerActionKind.Open));
            Assert.That(table.TryApply(1, action), Is.True);
            Assert.That(table.PendingDiscard, Is.Null);
        }

    }
}
