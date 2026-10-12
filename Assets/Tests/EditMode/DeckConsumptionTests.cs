using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Decks;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class DeckConsumptionTests
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

    }
}
