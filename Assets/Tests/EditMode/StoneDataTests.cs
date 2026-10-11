using System;
using NUnit.Framework;
using Okey.Core.Stones;

namespace Okey.Tests.EditMode
{
    public sealed class StoneDataTests
    {
        [Test]
        public void ConstructorPreservesStoneDefinition()
        {
            var stone = new StoneData(StoneType.Regular, StoneColor.Blue, 13);

            Assert.That(stone.Type, Is.EqualTo(StoneType.Regular));
            Assert.That(stone.Color, Is.EqualTo(StoneColor.Blue));
            Assert.That(stone.Value, Is.EqualTo(13));
        }

        [Test]
        public void ConstructorAllowsValuesOutsideStandardOkeyRange()
        {
            var stone = new StoneData(StoneType.Regular, StoneColor.Red, 20);

            Assert.That(stone.Value, Is.EqualTo(20));
        }

        [Test]
        public void FakeJokerCanHaveNoColorOrNumber()
        {
            var stone = new StoneData(StoneType.FakeJoker, StoneColor.None, 0);

            Assert.That(stone.Type, Is.EqualTo(StoneType.FakeJoker));
            Assert.That(stone.Color, Is.EqualTo(StoneColor.None));
            Assert.That(stone.Value, Is.Zero);
        }

        [Test]
        public void ConstructorRejectsUndefinedType()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StoneData((StoneType)99, StoneColor.Red, 1));
        }

        [Test]
        public void ConstructorRejectsUndefinedColor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StoneData(StoneType.Regular, (StoneColor)99, 1));
        }

        [Test]
        public void ConstructorRejectsNegativeValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StoneData(StoneType.Regular, StoneColor.Red, -1));
        }
    }
}
