using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;

namespace Okey.Tests.EditMode
{
    public sealed class TableBoundaryTests
    {
        private static StoneInstance Tile(int id, int value) =>
            new StoneInstance(id, new StoneData(StoneType.Regular, StoneColor.Red, value));

        [Test]
        public void PublishedHandAllowsReorderingWithoutExposingOwnershipMutation()
        {
            var deck = Enumerable.Range(0, 30).Select(id => Tile(id, id % 13 + 1)).ToArray();
            var rules = new OkeyRules(2, 3, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2);
            var table = new TableSession(rules, deck, 8, game: new Okey.Core.Games.Okey101Game());
            var hand = table.Players[0].Hand;
            var ids = hand.Slots.Where(tile => tile != null).Select(tile => tile.Id).ToArray();
            Assert.That(hand, Is.Not.InstanceOf<RackArrangement>());
            table.Players[0].Layout.Move(ids[0], 7);
            Assert.That(hand.FindSlot(ids[0]), Is.EqualTo(7));
            Assert.That(hand.Slots.Where(tile => tile != null).Select(tile => tile.Id), Is.EquivalentTo(ids));
            Assert.That(table.RemainingCount, Is.EqualTo(22));
            Assert.That(table.Phase, Is.EqualTo(Okey.Core.Dealing.TurnPhase.AwaitingDiscard));
        }

        [Test]
        public void OpeningPoliciesApplyModeSpecificPenalties()
        {
            var rules = new OkeyRules(2, 3, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2);
            var meldRules = new MeldRules(rules, Tile(99, 12));
            meldRules.TryCreate(new[] { Tile(1, 1), Tile(2, 2), Tile(3, 3) }, MeldKind.Run, out var run);
            var request = new OpeningRequest(OpeningMode.Series, OpeningMode.Closed, new[] { run }, 4,
                true, 101, 5, rules, new TableOptions());
            var rejected = new Okey101OpeningPolicy().Evaluate(request);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Penalty, Is.EqualTo(101));
            var closed = new ClosedHandOpeningPolicy().Evaluate(request);
            Assert.That(closed.Accepted, Is.False);
            Assert.That(closed.Penalty, Is.Zero);
        }
        [Test]
        public void AVariantPolicyCanAcceptBelow101AndReplaceOpeningThresholds()
        {
            var rules = new OkeyRules(2, 3, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2);
            var deck = new[] { Tile(99, 12), Tile(1, 1), Tile(2, 2), Tile(3, 3), Tile(4, 9) }
                .Concat(Enumerable.Range(5, 20).Select(id => Tile(id, id % 13 + 1))).ToArray();
            var table = new TableSession(rules, deck, 8, game: new VariantGame());
            table.MeldRules.TryCreate(table.Players[0].Hand.Slots.Take(3).ToArray(), MeldKind.Run, out var meld);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { meld }), Is.True);
            Assert.That(table.SeriesThreshold, Is.EqualTo(7));
            Assert.That(table.PairThreshold, Is.EqualTo(2));
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(1));
        }

        [Test]
        public void APermissiveVariantCannotRemoveTheSamePhysicalTileTwice()
        {
            var rules = new OkeyRules(2, 3, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2);
            var deck = new[] { Tile(99, 12), Tile(1, 1), Tile(2, 2), Tile(3, 3), Tile(4, 9) }
                .Concat(Enumerable.Range(5, 20).Select(id => Tile(id, id % 13 + 1))).ToArray();
            var table = new TableSession(rules, deck, 8, game: new VariantGame());
            table.MeldRules.TryCreate(table.Players[0].Hand.Slots.Take(3).ToArray(), MeldKind.Run, out var meld);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { meld, meld }), Is.False);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(4));
            Assert.That(table.Melds, Is.Empty);
        }

        [Test]
        public void BotDecisionsAreReadOnlyUntilSubmittedToTheSession()
        {
            var rules = new OkeyRules(2, 3, 1, 13, 101, 5, 101, 202, 101, -101, 101, 2, 2);
            var deck = Enumerable.Range(0, 30).Select(id => Tile(id, id % 13 + 1)).ToArray();
            var table = new TableSession(rules, deck, 8, game: new Okey.Core.Games.Okey101Game());
            table.TryDiscard(0, table.Players[0].Hand.Slots[0].Id);
            var before = table.RemainingCount;
            var observation = table.ObserveTurn(1);
            var policy = new BotDecisionPolicy(new MeldPlayerStrategy(new MeldPlanner()), new SeededRandomSource(42));
            var action = policy.Decide(observation, new BotPreferences(false, .01, .35, 1, 2, .1, 2), false);
            Assert.That(action.Kind, Is.EqualTo(PlayerActionKind.Draw));
            Assert.That(table.RemainingCount, Is.EqualTo(before));
            Assert.That(table.Players[1].Hand.Count, Is.EqualTo(3));
            Assert.That(table.TryApply(1, action), Is.True);
            Assert.That(table.Players[1].Hand.Count, Is.EqualTo(4));
            Assert.That(observation.Player.Hand.Count, Is.EqualTo(3));
        }

        [Test]
        public void ExternalRulesCanBuildImmutableMeldsAndPlans()
        {
            var tiles = new[]
            {
                new MeldTile(Tile(1, 1), StoneColor.Red, 1),
                new MeldTile(Tile(2, 2), StoneColor.Red, 2),
                new MeldTile(Tile(3, 3), StoneColor.Red, 3)
            };
            var meld = new Meld(MeldKind.Run, tiles);
            var groups = new[] { meld };
            var plan = new MeldPlan(groups);
            tiles[0] = new MeldTile(Tile(4, 4), StoneColor.Red, 4);
            groups[0] = new Meld(MeldKind.Run, tiles);
            Assert.That(plan.Points, Is.EqualTo(6));
            Assert.That(plan.Melds[0].Tiles[0].Stone.Id, Is.EqualTo(1));
            Assert.That(plan.TileCount, Is.EqualTo(3));
        }

        private sealed class VariantOpeningPolicy : IOpeningPolicy
        {
            public OpeningDecision Evaluate(OpeningRequest request) => new OpeningDecision(true, 0, 7, 2);
            public bool CanLayOff(OpeningMode opening, int handCount) => true;
        }

        private sealed class VariantGame : ITableGame
        {
            private readonly Okey101Game _game = new Okey101Game();
            public IOpeningPolicy OpeningPolicy { get; } = new VariantOpeningPolicy();
            public bool AllowsPublicMelds => true;
            public bool RequiresDiscardUse => true;
            public bool AllowsEmptyStockPlay => false;
            public bool PenalizesPlayableDiscards => false;
            public RackActionState PlanRackActions(RackActionRequest request) => _game.PlanRackActions(request);
            public bool TryFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules,
                out MeldPlan plan, out OpeningMode mode) => _game.TryFinish(hand, meldRules, rules, out plan, out mode);
            public bool TryFindFinish(IReadOnlyList<StoneInstance> hand, IMeldRules meldRules, OkeyRules rules,
                out int discardId) => _game.TryFindFinish(hand, meldRules, rules, out discardId);
            public IRoundScoring CreateScoring(OkeyRules rules, IMeldRules meldRules) => _game.CreateScoring(rules, meldRules);
        }
    }
}
