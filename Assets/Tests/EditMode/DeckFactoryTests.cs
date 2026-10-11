using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Decks;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class DeckFactoryTests
    {
        [Test]
        public void FactoryPreservesConfiguredRangesAndCopies()
        {
            var definition = new DeckDefinition(new[]
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Blue, 18, 20, 3),
                new StoneSetDefinition(StoneType.FakeJoker, StoneColor.None, 0, 0, 1)
            });

            var stones = new DeckFactory().Create(definition);

            Assert.That(stones.Count, Is.EqualTo(10));
            Assert.That(stones.Take(9).All(stone => stone.Data.Type == StoneType.Regular
                && stone.Data.Color == StoneColor.Blue), Is.True);
            foreach (var value in new[] { 18, 19, 20 })
                Assert.That(stones.Count(stone => stone.Data.Value == value), Is.EqualTo(3));

            Assert.That(stones[9].Data.Type, Is.EqualTo(StoneType.FakeJoker));
            Assert.That(stones[9].Data.Color, Is.EqualTo(StoneColor.None));
            Assert.That(stones[9].Data.Value, Is.Zero);
        }

        [Test]
        public void CopiesHaveDistinctIdentityAndShareImmutableDefinition()
        {
            var definition = new DeckDefinition(new[]
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Black, 4, 4, 2)
            });

            var stones = new DeckFactory().Create(definition);

            Assert.That(stones[0], Is.Not.SameAs(stones[1]));
            Assert.That(stones[0].Id, Is.Not.EqualTo(stones[1].Id));
            Assert.That(stones[0].Data, Is.SameAs(stones[1].Data));
        }

        [Test]
        public void RepeatedGenerationProducesIndependentDecksWithStableIds()
        {
            var definition = new DeckDefinition(new[]
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Red, 1, 3, 2)
            });
            var factory = new DeckFactory();

            var first = factory.Create(definition);
            var second = factory.Create(definition);

            Assert.That(first.Select(stone => stone.Id), Is.EqualTo(Enumerable.Range(0, first.Count)));
            Assert.That(second.Select(stone => stone.Id), Is.EqualTo(first.Select(stone => stone.Id)));
            for (var index = 0; index < first.Count; index++)
                Assert.That(first[index], Is.Not.SameAs(second[index]));
        }

        [Test]
        public void DeckDefinitionTakesSnapshotOfSourceCollection()
        {
            var source = new List<StoneSetDefinition>
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Red, 1, 1, 2)
            };
            var definition = new DeckDefinition(source);

            source.Clear();

            Assert.That(new DeckFactory().Create(definition).Count, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<StoneSetDefinition>)definition.Sets).Clear());
        }

        [Test]
        public void GeneratedCollectionCannotBeModified()
        {
            var definition = new DeckDefinition(new[]
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Red, 1, 1, 1)
            });
            var stones = new DeckFactory().Create(definition);

            Assert.Throws<NotSupportedException>(() => ((IList<StoneInstance>)stones).Clear());
        }

        [Test]
        public void MaximumIntegerValueDoesNotOverflowGenerationLoop()
        {
            var definition = new DeckDefinition(new[]
            {
                new StoneSetDefinition(StoneType.Regular, StoneColor.Blue, int.MaxValue, int.MaxValue, 1)
            });

            var stones = new DeckFactory().Create(definition);

            Assert.That(stones.Count, Is.EqualTo(1));
            Assert.That(stones[0].Data.Value, Is.EqualTo(int.MaxValue));
        }

        [TestCase(-1, 1, 1)]
        [TestCase(3, 2, 1)]
        [TestCase(1, 2, 0)]
        [TestCase(1, 2, -1)]
        public void InvalidRangesAndCopyCountsAreRejected(int minimum, int maximum, int copies)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StoneSetDefinition(StoneType.Regular, StoneColor.Blue, minimum, maximum, copies));
        }

        [Test]
        public void UndefinedStoneTypeIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StoneSetDefinition((StoneType)99, StoneColor.Blue, 1, 1, 1));
        }

        [Test]
        public void UndefinedStoneColorIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StoneSetDefinition(StoneType.Regular, (StoneColor)99, 1, 1, 1));
        }

        [Test]
        public void SetCountOverflowIsRejectedBeforeAllocation()
        {
            Assert.Throws<OverflowException>(() =>
                new StoneSetDefinition(StoneType.Regular, StoneColor.Blue, 0, int.MaxValue, 1));
        }

        [Test]
        public void CombinedDeckCountOverflowIsRejectedBeforeAllocation()
        {
            var set = new StoneSetDefinition(StoneType.Regular, StoneColor.Blue, 1, int.MaxValue, 1);

            Assert.Throws<OverflowException>(() => new DeckDefinition(new[] { set, set }));
        }

        [Test]
        public void MissingOrEmptyDeckDefinitionsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new DeckDefinition(null));
            Assert.Throws<ArgumentException>(() => new DeckDefinition(Array.Empty<StoneSetDefinition>()));
            Assert.Throws<ArgumentException>(() => new DeckDefinition(new StoneSetDefinition[] { null }));
            Assert.Throws<ArgumentNullException>(() => new DeckFactory().Create(null));
        }

        [Test]
        public void InvalidPhysicalStoneIsRejected()
        {
            var data = new StoneData(StoneType.Regular, StoneColor.Red, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => new StoneInstance(-1, data));
            Assert.Throws<ArgumentNullException>(() => new StoneInstance(0, null));
        }
    }
}
