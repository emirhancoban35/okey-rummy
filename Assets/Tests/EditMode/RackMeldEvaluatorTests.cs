using NUnit.Framework;
using Okey.Core.Rules;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class RackMeldEvaluatorTests
    {
        private readonly RackMeldEvaluator _evaluator = new RackMeldEvaluator();
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static IMeldRules Rules => new MeldRules(new OkeyRules(4,21,1,13,101,5,101,202,101,-101,101,2,2), Tile(100, 13, StoneColor.Blue));

        [Test]
        public void SeparatedMatchingTilesDoNotCountUntilMovedTogether()
        {
            var rack = new RackArrangement(new[] {Tile(0,10), Tile(1,11), Tile(2,12), Tile(3,2)}, 8);
            rack.Move(1,5);
            Assert.That(_evaluator.Find(rack.Slots, 8, Rules, OpeningMode.Series).Points, Is.Zero);
            rack.Move(1,1);
            Assert.That(_evaluator.Find(rack.Slots, 8, Rules, OpeningMode.Series).Points, Is.EqualTo(33));
        }
        [Test]
        public void ARunCannotCrossBetweenRackRows()
        {
            var slots = new[] {Tile(0,2), null, Tile(1,10), Tile(2,11), Tile(3,12), null};
            Assert.That(_evaluator.Find(slots, 3, Rules, OpeningMode.Series).Points, Is.Zero);
        }
        [Test]
        public void AdjacentRunsCanBeSeparatedIntoNonoverlappingGroups()
        {
            var slots = new[] {Tile(0,10),Tile(1,11),Tile(2,12),Tile(3,10,StoneColor.Black),Tile(4,11,StoneColor.Black),Tile(5,12,StoneColor.Black),Tile(6,2)};
            var plan = _evaluator.Find(slots,14,Rules,OpeningMode.Series);
            Assert.That(plan.Points, Is.EqualTo(66));
            Assert.That(plan.Melds.Count, Is.EqualTo(2));
        }
        [Test]
        public void PairsMustBeAdjacentAndNeverReuseATile()
        {
            var slots = new[] {Tile(0,5),Tile(1,5),Tile(2,5),null,Tile(3,7),Tile(4,7),Tile(5,1)};
            var plan = _evaluator.Find(slots,14,Rules,OpeningMode.Pairs);
            Assert.That(plan.Melds.Count, Is.EqualTo(2));
            Assert.That(plan.TileCount, Is.EqualTo(4));
        }
        [Test]
        public void AnOpeningAlwaysLeavesATileToDiscard()
        {
            var slots = new[] {Tile(0,10),Tile(1,11),Tile(2,12)};
            Assert.That(_evaluator.Find(slots,14,Rules,OpeningMode.Series).TileCount, Is.Zero);
        }
        [Test]
        public void WildTilesFollowTheSameContiguousGroupRules()
        {
            var slots = new[] {Tile(0,10),Tile(1,1,StoneColor.Blue),Tile(2,12),Tile(3,2)};
            Assert.That(_evaluator.Find(slots,14,Rules,OpeningMode.Series).Points, Is.EqualTo(33));
        }
    }
}
