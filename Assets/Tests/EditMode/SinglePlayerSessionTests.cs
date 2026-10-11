using System;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Dealing;
using Okey.Core.Decks;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class SinglePlayerSessionTests
    {
        private static StoneInstance Stone(int id)
        {
            return new StoneInstance(id, new StoneData(StoneType.Regular, StoneColor.Red, id + 1));
        }

        private static SinglePlayerSession Session(int capacity = 4, TurnPhase phase = TurnPhase.AwaitingDraw)
        {
            return new SinglePlayerSession(new RackArrangement(new[] { Stone(0), Stone(1) }, capacity),
                new DrawPile(new[] { Stone(2), Stone(3), Stone(4) }), phase);
        }

        [Test]
        public void DrawAddsPhysicalStoneAndRequiresDiscardBeforeNextDraw()
        {
            var session = Session();
            Assert.That(session.TryDiscard(0, out _), Is.False);
            Assert.That(session.TryDraw(out var drawn), Is.True);
            Assert.That(drawn.Id, Is.EqualTo(2));
            Assert.That(session.Hand.Slots[2], Is.SameAs(drawn));
            Assert.That(session.Hand.Count, Is.EqualTo(3));
            Assert.That(session.RemainingCount, Is.EqualTo(2));
            Assert.That(session.Phase, Is.EqualTo(TurnPhase.AwaitingDiscard));
            Assert.That(session.TryDraw(out _), Is.False);
            Assert.That(session.RemainingCount, Is.EqualTo(2));
        }

        [Test]
        public void DiscardRemovesOnlyChosenStoneAndLeavesItsSlotEmpty()
        {
            var session = Session();
            session.TryDraw(out _);
            Assert.That(session.TryDiscard(99, out _), Is.False);
            Assert.That(session.TryDiscard(0, out var discarded), Is.True);
            Assert.That(discarded.Id, Is.Zero);
            Assert.That(session.Hand.Slots[0], Is.Null);
            Assert.That(session.Hand.Count, Is.EqualTo(2));
            Assert.That(session.Discards.Single(), Is.SameAs(discarded));
            Assert.That(session.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
            Assert.That(session.TryDiscard(1, out _), Is.False);
            session.TryDraw(out var next);
            Assert.That(session.Hand.Slots[0], Is.SameAs(next));
        }

        [Test]
        public void FullRackRejectsDrawWithoutConsumingPile()
        {
            var session = Session(2);
            Assert.That(session.CanDraw, Is.False);
            Assert.That(session.TryDraw(out _), Is.False);
            Assert.That(session.RemainingCount, Is.EqualTo(3));
            Assert.That(session.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
        }

        [Test]
        public void EmptyPileFinishesAfterLastDiscardAndConservesAllStones()
        {
            var session = Session();
            for (var turn = 0; turn < 3; turn++)
            {
                Assert.That(session.TryDraw(out var drawn), Is.True);
                Assert.That(session.Phase, Is.EqualTo(TurnPhase.AwaitingDiscard));
                Assert.That(session.TryDiscard(drawn.Id, out _), Is.True);
            }
            Assert.That(session.Phase, Is.EqualTo(TurnPhase.Finished));
            Assert.That(session.TryDraw(out _), Is.False);
            Assert.That(session.TryDiscard(0, out _), Is.False);
            Assert.That(session.Hand.Slots.Where(stone => stone != null).Concat(session.Discards).Select(stone => stone.Id),
                Is.EquivalentTo(Enumerable.Range(0, 5)));
        }

        [Test]
        public void ConfiguredDiscardOpeningSkipsInitialDraw()
        {
            var session = Session(4, TurnPhase.AwaitingDiscard);
            Assert.That(session.TryDraw(out _), Is.False);
            Assert.That(session.TryDiscard(1, out _), Is.True);
            Assert.That(session.Hand.Count, Is.EqualTo(1));
            Assert.That(session.RemainingCount, Is.EqualTo(3));
            Assert.That(session.CanDraw, Is.True);
        }

        [Test]
        public void EmptyDrawOpeningFinishesImmediately()
        {
            var session = new SinglePlayerSession(new RackArrangement(new[] { Stone(0) }, 2),
                new DrawPile(Array.Empty<StoneInstance>()), TurnPhase.AwaitingDraw);
            Assert.That(session.Phase, Is.EqualTo(TurnPhase.Finished));
            Assert.That(session.CanDraw, Is.False);
        }

        [Test]
        public void InvalidOpeningAndOverlappingInventoriesAreRejected()
        {
            var hand = new RackArrangement(new[] { Stone(0) }, 2);
            Assert.Throws<ArgumentException>(() => new SinglePlayerSession(hand, new DrawPile(new[] { Stone(0) }), TurnPhase.AwaitingDraw));
            Assert.Throws<ArgumentException>(() => new SinglePlayerSession(new RackArrangement(Array.Empty<StoneInstance>(), 2),
                new DrawPile(new[] { Stone(0) }), TurnPhase.AwaitingDiscard));
            Assert.Throws<ArgumentOutOfRangeException>(() => Session(4, TurnPhase.Finished));
            Assert.Throws<ArgumentNullException>(() => new SinglePlayerSession(null, new DrawPile(new[] { Stone(0) }), TurnPhase.AwaitingDraw));
            Assert.Throws<ArgumentNullException>(() => new SinglePlayerSession(hand, null, TurnPhase.AwaitingDraw));
        }

        [Test]
        public void RackMembershipChangesPreserveGapsAndRejectInvalidAdds()
        {
            var rack = new RackArrangement(new[] { Stone(0), Stone(1) }, 3);
            rack.Move(0, 2);
            var removed = rack.Remove(1);
            Assert.That(rack.Count, Is.EqualTo(1));
            Assert.That(rack.Add(removed), Is.Zero);
            Assert.That(rack.Slots[1], Is.Null);
            Assert.Throws<ArgumentException>(() => rack.Add(removed));
            Assert.Throws<ArgumentNullException>(() => rack.Add(null));
            Assert.Throws<ArgumentException>(() => rack.Remove(99));
            rack.Add(Stone(2));
            Assert.Throws<InvalidOperationException>(() => rack.Add(Stone(3)));
        }
    }
}
