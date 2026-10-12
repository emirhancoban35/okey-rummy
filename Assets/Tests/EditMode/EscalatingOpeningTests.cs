using System;
using System.Linq;
using NUnit.Framework;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
namespace Okey.Tests.EditMode
{
    public sealed class EscalatingOpeningTests
    {
        private static TableSession Table(bool escalating = true)
        {
            var deck = Enumerable.Range(0, 80).Select(id => Tile(id, 7, StoneColor.Black)).ToArray();
            deck[0] = Tile(0, 13, StoneColor.Blue);
            var first = new[] { 10,11,12,3,4,5,8,8,9,9,7 };
            var second = new[] { 10,11,12,11,12,13,8,8,9,9 };
            for (var i = 0; i < first.Length; i++) deck[i + 1] = Tile(i + 1, first[i], StoneColor.Red);
            for (var i = 0; i < second.Length; i++) deck[i + 12] = Tile(i + 12, second[i], StoneColor.Black);
            return new TableSession(new OkeyRules(4,10,1,13,1,2,101,202,101,-101,101,2,2), deck,14,
                options: new TableOptions(escalatingOpenings: escalating), game: new Okey.Core.Games.Okey101Game());
        }
        private static StoneInstance Tile(int id, int value, StoneColor color) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static Meld Meld(TableSession table, int seat, MeldKind kind, params int[] ids)
        {
            var hand = table.Players[seat].Hand.Slots;
            Assert.That(table.MeldRules.TryCreate(ids.Select(id => hand.Single(tile => tile?.Id == id)), kind, out var meld), Is.True);
            return meld;
        }
        [Test]
        public void InitialOpeningRaisesOnlyItsOwnThresholdAndAdditionalMeldsDoNotRaiseIt()
        {
            var table = Table();
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { Meld(table,0,MeldKind.Run,1,2,3) }), Is.True);
            Assert.That(table.SeriesThreshold, Is.EqualTo(34));
            Assert.That(table.PairThreshold, Is.EqualTo(2));
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { Meld(table,0,MeldKind.Run,4,5,6) }), Is.True);
            Assert.That(table.SeriesThreshold, Is.EqualTo(34));
            table.TryDiscard(0,11);
            table.TryDraw(1);
            Assert.That(table.TryOpen(1, OpeningMode.Series, new[] { Meld(table,1,MeldKind.Run,12,13,14) }), Is.False);
            Assert.That(table.SeriesThreshold, Is.EqualTo(34));
            Assert.That(table.Players[1].Penalty, Is.EqualTo(101));
            Assert.That(table.TryOpen(1, OpeningMode.Series, new[] { Meld(table,1,MeldKind.Run,15,16,17) }), Is.True);
            Assert.That(table.SeriesThreshold, Is.EqualTo(37));
        }
        [Test]
        public void PairOpeningRaisesThePairThresholdIndependently()
        {
            var table = Table();
            Assert.That(table.TryOpen(0, OpeningMode.Pairs, new[] { Meld(table,0,MeldKind.Pair,7,8), Meld(table,0,MeldKind.Pair,9,10) }), Is.True);
            Assert.That(table.PairThreshold, Is.EqualTo(3));
            Assert.That(table.SeriesThreshold, Is.EqualTo(1));
        }
        [Test]
        public void FixedTablesKeepTheirConfiguredThresholds()
        {
            var table = Table(false);
            table.TryOpen(0,OpeningMode.Series,new[] { Meld(table,0,MeldKind.Run,1,2,3) });
            Assert.That(table.SeriesThreshold, Is.EqualTo(1));
        }
        [Test]
        public void BotUsesTheCurrentPairThresholdInsteadOfTheInitialThreshold()
        {
            var table = Table();
            var player = table.Players[0];
            var strategy = new MeldPlayerStrategy(new MeldPlanner());
            var observation = new PlayerObservation(player.Hand.Slots.Where(tile => tile != null),table.Melds,
                OpeningMode.Closed,table.Rules,table.MeldRules,pairThreshold: 3);
            strategy.PlanOpening(observation,out var mode);
            Assert.That(mode, Is.EqualTo(OpeningMode.Series));
        }
        [Test]
        public void OpeningIncrementMustBePositive() => Assert.Throws<ArgumentOutOfRangeException>(() => new TableOptions(openingIncrement:0));
        [TestCase(false)]
        [TestCase(true)]
        public void PairThresholdControlsTheNextPlayersOpening(bool escalating)
        {
            var table = Table(escalating);
            table.TryOpen(0, OpeningMode.Pairs, new[] { Meld(table, 0, MeldKind.Pair, 7, 8), Meld(table, 0, MeldKind.Pair, 9, 10) });
            table.TryDiscard(0, 11); table.TryDraw(1);
            var accepted = table.TryOpen(1, OpeningMode.Pairs, new[] { Meld(table, 1, MeldKind.Pair, 18, 19), Meld(table, 1, MeldKind.Pair, 20, 21) });
            Assert.That(accepted, Is.EqualTo(!escalating));
            Assert.That(table.PairThreshold, Is.EqualTo(escalating ? 3 : 2));
            Assert.That(table.Players[1].Penalty, Is.EqualTo(escalating ? 101 : 0));
        }
        [Test]
        public void EmptyAdditionalOpeningDoesNotChargeAnAlreadyOpenedPlayer()
        {
            var table = Table();
            table.TryOpen(0, OpeningMode.Series, new[] { Meld(table, 0, MeldKind.Run, 1, 2, 3) });
            Assert.That(table.TryOpen(0, OpeningMode.Series, Array.Empty<Meld>()), Is.False);
            Assert.That(table.Players[0].Penalty, Is.Zero);
        }

    }
}
