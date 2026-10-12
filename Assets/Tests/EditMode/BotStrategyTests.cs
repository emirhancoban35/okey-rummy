using System;
using NUnit.Framework;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
using Okey.Configuration;

namespace Okey.Tests.EditMode
{
    public sealed class BotStrategyTests
    {
        private static StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
        private static OkeyRules Settings => new OkeyRules(4, 21, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2);

        [Test]
        public void DiscardKeepsAnAdjacentHighRunAndPrefersTheLowestIsolatedTile()
        {
            var rules = new MeldRules(Settings, Tile(0, 12, StoneColor.Black));
            var hand = new[] { Tile(1, 1), Tile(2, 11, StoneColor.Blue), Tile(3, 12, StoneColor.Blue), Tile(4, 7, StoneColor.Yellow) };
            var observation = new PlayerObservation(hand, Array.Empty<Meld>(), OpeningMode.Closed, Settings, rules);
            Assert.That(new MeldPlayerStrategy(new MeldPlanner()).ChooseDiscard(observation), Is.EqualTo(1));
        }

        [Test]
        public void BotKeepsAJokerInsteadOfSpendingItOnAnUnrelatedPublicRun()
        {
            var rules = new MeldRules(Settings, Tile(0, 12, StoneColor.Black));
            rules.TryCreate(new[] { Tile(10, 5), Tile(11, 6), Tile(12, 7) }, MeldKind.Run, out var run);
            var hand = new[] { Tile(1, 13, StoneColor.Black), Tile(2, 2, StoneColor.Blue), Tile(3, 10, StoneColor.Yellow), Tile(4, 1) };
            var observation = new PlayerObservation(hand, new[] { run }, OpeningMode.Series, Settings, rules);
            var turn = new PlayerTurnObservation(observation, TurnPhase.AwaitingDiscard, new Okey101Game(),
                new TableOptions(allowJokerReclaim: false), null, 20, 4);
            var policy = new BotDecisionPolicy(new MeldPlayerStrategy(new MeldPlanner()), new SeededRandomSource(42));
            var action = policy.Decide(turn, new BotPreferences(false, 0, 0, 0, 0, 0, 0), false);
            Assert.That(action.Kind, Is.EqualTo(PlayerActionKind.Discard));
            Assert.That(action.StoneId, Is.Not.EqualTo(1));
        }

        [Test]
        public void BotAssignsAJokerToTheMissingMiddleOfItsOwnRun()
        {
            var rules = new MeldRules(Settings, Tile(0, 12, StoneColor.Black));
            var hand = new[] { Tile(1, 13, StoneColor.Black), Tile(2, 10, StoneColor.Blue), Tile(3, 12, StoneColor.Blue), Tile(4, 1) };
            var observation = new PlayerObservation(hand, Array.Empty<Meld>(), OpeningMode.Series, Settings, rules);
            var plan = new MeldPlayerStrategy(new MeldPlanner()).PlanOpening(observation, out _);
            Assert.That(plan.Melds[0].Tiles[1].Stone.Id, Is.EqualTo(1));
            Assert.That(plan.Melds[0].Tiles[1].Value, Is.EqualTo(11));
        }
    }
}
