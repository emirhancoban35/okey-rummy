using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Dealing;
using Okey.Core.Decks;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class DealingTests
    {
        private static DeckDefinition CreateDefinition()
        {
            return new DeckDefinition(new[]
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Red, 1, 6, 2)
            });
        }

        [Test]
        public void ShufflePreservesInventoryWithoutChangingSource()
        {
            var source = new DeckFactory().Create(CreateDefinition());
            var originalIds = source.Select(stone => stone.Id).ToArray();

            var shuffled = new SeededStoneShuffler().Shuffle(source, 42);

            Assert.That(shuffled.Select(stone => stone.Id), Is.EquivalentTo(originalIds));
            Assert.That(source.Select(stone => stone.Id), Is.EqualTo(originalIds));
            Assert.That(shuffled.Select(stone => stone.Id), Is.Not.EqualTo(originalIds));
            foreach (var stone in shuffled)
                Assert.That(stone, Is.SameAs(source[stone.Id]));
            Assert.Throws<NotSupportedException>(() => ((IList<StoneInstance>)shuffled).Clear());
        }

        [Test]
        public void ShuffleUsesIndependentSeededRandomState()
        {
            var source = new DeckFactory().Create(CreateDefinition());
            var shuffler = new SeededStoneShuffler();
            var first = shuffler.Shuffle(source, 42);
            var other = shuffler.Shuffle(source, 43);
            var repeated = shuffler.Shuffle(source, 42);

            Assert.That(repeated.Select(stone => stone.Id), Is.EqualTo(first.Select(stone => stone.Id)));
            Assert.That(other.Select(stone => stone.Id), Is.Not.EqualTo(first.Select(stone => stone.Id)));
        }

        [Test]
        public void ShuffleAcceptsEmptyAndSingleStoneCollections()
        {
            var shuffler = new SeededStoneShuffler();
            var stone = new StoneInstance(0, new StoneData(StoneType.Regular, StoneColor.Red, 1));

            Assert.That(shuffler.Shuffle(Array.Empty<StoneInstance>(), 0), Is.Empty);
            Assert.That(shuffler.Shuffle(new[] { stone }, 0).Single(), Is.SameAs(stone));
            Assert.Throws<ArgumentNullException>(() => shuffler.Shuffle(null, 0));
        }

        [Test]
        public void SingleAndBatchDrawsConsumeStonesInOrder()
        {
            var source = new DeckFactory().Create(CreateDefinition());
            var pile = new DrawPile(source);

            Assert.That(pile.TryDraw(out var first), Is.True);
            Assert.That(first, Is.SameAs(source[0]));
            var batch = pile.Draw(3);

            Assert.That(batch, Is.EqualTo(source.Skip(1).Take(3)));
            Assert.That(pile.RemainingCount, Is.EqualTo(source.Count - 4));
            Assert.That(pile.TryDraw(out var next), Is.True);
            Assert.That(next, Is.SameAs(source[4]));
            Assert.Throws<NotSupportedException>(() => ((IList<StoneInstance>)batch).Clear());
        }

        [Test]
        public void FailedBatchDrawLeavesPileUnchanged()
        {
            var source = new DeckFactory().Create(CreateDefinition());
            var pile = new DrawPile(source);

            Assert.Throws<InvalidOperationException>(() => pile.Draw(source.Count + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pile.Draw(-1));
            Assert.That(pile.RemainingCount, Is.EqualTo(source.Count));
            Assert.That(pile.TryDraw(out var first), Is.True);
            Assert.That(first, Is.SameAs(source[0]));
        }

        [Test]
        public void ExhaustedPileReturnsNoStoneAndAllowsZeroDraw()
        {
            var pile = new DrawPile(new DeckFactory().Create(CreateDefinition()));
            pile.Draw(pile.RemainingCount);

            Assert.That(pile.TryDraw(out var stone), Is.False);
            Assert.That(stone, Is.Null);
            Assert.That(pile.Draw(0), Is.Empty);
            Assert.That(pile.RemainingCount, Is.Zero);
        }

        [Test]
        public void PileSnapshotsSourceAndRejectsAmbiguousInventory()
        {
            var source = new List<StoneInstance>(new DeckFactory().Create(CreateDefinition()));
            var first = source[0];
            var pile = new DrawPile(source);
            source.Clear();

            Assert.That(pile.RemainingCount, Is.EqualTo(12));
            Assert.That(pile.TryDraw(out var drawn), Is.True);
            Assert.That(drawn, Is.SameAs(first));
            Assert.Throws<ArgumentNullException>(() => new DrawPile(null));
            Assert.Throws<ArgumentException>(() => new DrawPile(new StoneInstance[] { null }));
            Assert.Throws<ArgumentException>(() => new DrawPile(new[]
            {
                first, new StoneInstance(first.Id, first.Data)
            }));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(12)]
        public void OpeningDealPreservesEveryPhysicalStone(int handSize)
        {
            var factory = new OpeningDealFactory(new DeckFactory(), new SeededStoneShuffler());
            var deal = factory.Create(CreateDefinition(), handSize, 42);

            Assert.That(deal.Seed, Is.EqualTo(42));
            Assert.That(deal.InitialHand.Count, Is.EqualTo(handSize));
            Assert.That(deal.DrawPile.RemainingCount, Is.EqualTo(12 - handSize));
            var remaining = deal.DrawPile.Draw(deal.DrawPile.RemainingCount);
            var allIds = deal.InitialHand.Concat(remaining).Select(stone => stone.Id).ToArray();
            Assert.That(allIds, Is.EquivalentTo(Enumerable.Range(0, 12)));
            Assert.That(allIds.Distinct().Count(), Is.EqualTo(12));
        }

        [Test]
        public void OpeningDealsHaveIndependentStateAndRepeatableOrder()
        {
            var factory = new OpeningDealFactory(new DeckFactory(), new SeededStoneShuffler());
            var first = factory.Create(CreateDefinition(), 3, 42);
            var second = factory.Create(CreateDefinition(), 3, 42);

            first.DrawPile.Draw(2);

            Assert.That(second.DrawPile.RemainingCount, Is.EqualTo(9));
            Assert.That(first.InitialHand.Select(stone => stone.Id),
                Is.EqualTo(second.InitialHand.Select(stone => stone.Id)));
            Assert.That(first.InitialHand[0], Is.Not.SameAs(second.InitialHand[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<StoneInstance>)first.InitialHand).Clear());
        }

        [Test]
        public void OpeningDealUsesInjectedShuffleStrategy()
        {
            var shuffler = new ReverseShuffler();
            var factory = new OpeningDealFactory(new DeckFactory(), shuffler);

            var deal = factory.Create(CreateDefinition(), 2, 17);

            Assert.That(shuffler.ReceivedSeed, Is.EqualTo(17));
            Assert.That(deal.InitialHand.Select(stone => stone.Id), Is.EqualTo(new[] { 11, 10 }));
            Assert.That(deal.DrawPile.TryDraw(out var next), Is.True);
            Assert.That(next.Id, Is.EqualTo(9));
        }

        [TestCase(-1)]
        [TestCase(13)]
        public void InvalidOpeningHandSizeFailsBeforeShuffle(int handSize)
        {
            var shuffler = new ReverseShuffler();
            var factory = new OpeningDealFactory(new DeckFactory(), shuffler);

            Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(CreateDefinition(), handSize, 17));
            Assert.That(shuffler.ReceivedSeed, Is.Null);
        }

        [Test]
        public void OpeningDealRejectsMissingDependenciesAndDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => new OpeningDealFactory(null, new SeededStoneShuffler()));
            Assert.Throws<ArgumentNullException>(() => new OpeningDealFactory(new DeckFactory(), null));
            var factory = new OpeningDealFactory(new DeckFactory(), new SeededStoneShuffler());
            Assert.Throws<ArgumentNullException>(() => factory.Create(null, 0, 0));
        }

        private sealed class ReverseShuffler : IStoneShuffler
        {
            public int? ReceivedSeed { get; private set; }

            public IReadOnlyList<StoneInstance> Shuffle(IReadOnlyList<StoneInstance> stones, int seed)
            {
                ReceivedSeed = seed;
                var reversed = new List<StoneInstance>(stones);
                reversed.Reverse();
                return reversed.AsReadOnly();
            }
        }
    }
}
