using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class RackArrangementTests
    {
        private static StoneInstance[] CreateStones()
        {
            return Enumerable.Range(0, 3).Select(id =>
                new StoneInstance(id, new StoneData(StoneType.Regular, StoneColor.Blue, id + 1))).ToArray();
        }

        [Test]
        public void ArrangementSnapshotsMembershipAndExposesReadOnlySlots()
        {
            var stones = CreateStones();
            var first = stones[0];
            var rack = new RackArrangement(stones, 5);
            stones[0] = null;

            Assert.That(rack.Count, Is.EqualTo(3));
            Assert.That(rack.Capacity, Is.EqualTo(5));
            Assert.That(rack.Slots[0], Is.SameAs(first));
            Assert.That(rack.Slots[3], Is.Null);
            Assert.Throws<NotSupportedException>(() => ((IList<StoneInstance>)rack.Slots)[0] = null);
        }

        [Test]
        public void MovingToEmptySlotPreservesIdentityAndLeavesGap()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 5);

            rack.Move(stones[0].Id, 4);

            Assert.That(rack.Slots[0], Is.Null);
            Assert.That(rack.Slots[4], Is.SameAs(stones[0]));
            Assert.That(rack.FindSlot(stones[0].Id), Is.EqualTo(4));
            Assert.That(rack.Count, Is.EqualTo(3));
        }

        [Test]
        public void OccupiedSlotSwapsBothStonesAndTheirPositions()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 3);

            rack.Move(0, 2);
            rack.Move(0, 2);

            Assert.That(rack.Slots, Is.EqualTo(new[] { stones[2], stones[1], stones[0] }));
            Assert.That(rack.FindSlot(0), Is.EqualTo(2));
            Assert.That(rack.FindSlot(2), Is.Zero);
        }

        [Test]
        public void InvalidMovesDoNotChangeArrangement()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => rack.Move(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rack.Move(0, 3));
            Assert.Throws<ArgumentException>(() => rack.Move(99, 0));
            Assert.That(rack.Slots, Is.EqualTo(stones));
            Assert.That(rack.FindSlot(99), Is.EqualTo(-1));
        }

        [Test]
        public void ConstructorRejectsAmbiguousInventoryAndInsufficientCapacity()
        {
            var stones = CreateStones();
            Assert.Throws<ArgumentNullException>(() => new RackArrangement(null, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RackArrangement(stones, -1));
            Assert.Throws<ArgumentException>(() => new RackArrangement(stones, 2));
            Assert.Throws<ArgumentException>(() => new RackArrangement(new[] { stones[0], stones[0] }, 2));
            Assert.Throws<ArgumentException>(() => new RackArrangement(new StoneInstance[] { null }, 1));
            Assert.That(new RackArrangement(Array.Empty<StoneInstance>(), 0).Slots, Is.Empty);
        }

        [Test]
        public void RepeatedMovesPreserveEveryPhysicalStone()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 5);
            var random = new Random(42);
            for (var move = 0; move < 100; move++)
            {
                rack.Move(random.Next(stones.Length), random.Next(rack.Capacity));
                var occupied = rack.Slots.Where(stone => stone != null).ToArray();
                Assert.That(occupied, Is.EquivalentTo(stones));
                foreach (var stone in occupied)
                    Assert.That(rack.Slots[rack.FindSlot(stone.Id)], Is.SameAs(stone));
            }
        }
    }
}
