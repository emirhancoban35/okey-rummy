using System.Linq;
using NUnit.Framework;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
namespace Okey.Tests.EditMode
{
    public sealed class RemainingHandValueTests
    {
        [Test]
        public void RemainingValueUsesTheHeldJokerPenaltyAndShrinksWhenTilesAreOpened()
        {
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Blue) => new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            var deck = Enumerable.Range(0, 30).Select(id => Tile(id, 8)).ToArray();
            deck[0] = Tile(0, 9, StoneColor.Red); deck[1] = Tile(1, 10, StoneColor.Red);
            deck[2] = Tile(2, 5); deck[3] = Tile(3, 6); deck[4] = Tile(4, 7);
            var table = new TableSession(new OkeyRules(2, 3, 1, 13, 1, 1, 101, 202, 101, -101, 101, 2, 2), deck, 8, new Okey101Game());
            Assert.That(table.RemainingHandValue(0), Is.EqualTo(119));
            table.MeldRules.TryCreate(new[] { deck[2], deck[3], deck[4] }, MeldKind.Run, out var run);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { run }), Is.True);
            Assert.That(table.RemainingHandValue(0), Is.EqualTo(101));
        }
        [Test]
        public void PairOpeningUsesTheSameMultiplierAsRoundScoring()
        {
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Blue) => new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            var deck = Enumerable.Range(0, 30).Select(id => Tile(id, 8)).ToArray();
            deck[0] = Tile(0, 9, StoneColor.Red); deck[5] = Tile(5, 3); deck[6] = Tile(6, 3); deck[7] = Tile(7, 10, StoneColor.Red); deck[8] = Tile(8, 12);
            var table = new TableSession(new OkeyRules(2, 3, 1, 13, 1, 1, 101, 202, 101, -101, 101, 2, 2), deck, 8, new Okey101Game());
            table.TryDiscard(0, 4); table.TryDraw(1);
            table.MeldRules.TryCreate(new[] { deck[5], deck[6] }, MeldKind.Pair, out var pair);
            Assert.That(table.TryOpen(1, OpeningMode.Pairs, new[] { pair }), Is.True);
            Assert.That(table.RemainingHandValue(1), Is.EqualTo(226));
            Assert.That(new Okey101Scoring(table.Rules, table.MeldRules).Calculate(table.Players, 0, false)[1], Is.EqualTo(table.RemainingHandValue(1)));
        }
    }
}
