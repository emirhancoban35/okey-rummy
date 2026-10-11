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
        public void InsertionIntoFullRackShiftsNeighborsIntoLiftedSlot()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 3);

            var moved = rack.Move(0, 2);

            Assert.That(rack.Slots, Is.EqualTo(new[] { stones[1], stones[2], stones[0] }));
            Assert.That(rack.FindSlot(0), Is.EqualTo(2));
            Assert.That(rack.FindSlot(1), Is.Zero);
            Assert.That(rack.FindSlot(2), Is.EqualTo(1));
            Assert.That(moved, Is.EquivalentTo(stones));
            Assert.That(rack.Move(0, 2), Is.Empty);
        }

        [Test]
        public void InsertionFromRightPreservesNeighborOrder()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 3);

            rack.Move(2, 0);

            Assert.That(rack.Slots, Is.EqualTo(new[] { stones[2], stones[0], stones[1] }));
        }

        [Test]
        public void NearestGapLimitsShiftingAndPreservesOtherGroups()
        {
            var stones = Enumerable.Range(0, 6).Select(id =>
                new StoneInstance(id, new StoneData(StoneType.Regular, StoneColor.Blue, id + 1))).ToArray();
            var rack = new RackArrangement(stones, 10);
            rack.Move(2, 8);

            var moved = rack.Move(5, 1);

            Assert.That(rack.Slots, Is.EqualTo(new[]
                { stones[0], stones[5], stones[1], stones[3], stones[4], null, null, null, stones[2], null }));
            Assert.That(moved, Is.EquivalentTo(new[] { stones[1], stones[5] }));
            Assert.That(rack.FindSlot(1), Is.EqualTo(2));
            Assert.That(rack.FindSlot(5), Is.EqualTo(1));
        }

        [Test]
        public void InsertionUsesEmptySlotOnLeftWhenItIsCloser()
        {
            var stones = Enumerable.Range(0, 7).Select(id =>
                new StoneInstance(id, new StoneData(StoneType.Regular, StoneColor.Red, id + 1))).ToArray();
            var rack = new RackArrangement(stones, 10);
            rack.Move(1, 9);

            var moved = rack.Move(6, 2);

            Assert.That(rack.Slots, Is.EqualTo(new[]
                { stones[0], stones[2], stones[6], stones[3], stones[4], stones[5], null, null, null, stones[1] }));
            Assert.That(moved, Is.EquivalentTo(new[] { stones[2], stones[6] }));
        }

        [Test]
        public void EmptySlotMoveReportsOnlyDraggedStoneAndKeepsOtherGaps()
        {
            var stones = CreateStones();
            var rack = new RackArrangement(stones, 7);
            rack.Move(0, 5);

            var moved = rack.Move(1, 6);

            Assert.That(rack.Slots, Is.EqualTo(new[] { null, null, stones[2], null, null, stones[0], stones[1] }));
            Assert.That(moved, Is.EqualTo(new[] { stones[1] }));
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
