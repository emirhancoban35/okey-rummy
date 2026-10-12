using System;
using NUnit.Framework;
using Okey.Core.Rooms;
using Okey.Core.Players;
using System.Linq;
namespace Okey.Tests.EditMode
{
    public sealed class LocalRoomTests
    {
        private static PlayerIdentity Player(string name) => new PlayerIdentity(name, name);
        private static RoomOptions Options(int hands = 3) => new RoomOptions(0, hands, 4, false, true, false, true);
        [Test]
        public void CreatedRoomsKeepAnIndependentSettingsSnapshotAndUniqueNumber()
        {
            var directory = new LocalRoomDirectory(Array.Empty<LocalRoom>(), 100001);
            var first = directory.Create("Coffee", "private", Options(), Player("You"));
            var second = directory.Create("Friends", "", Options(7), Player("You"));
            Assert.That(second.Number, Is.Not.EqualTo(first.Number));
            Assert.That(first.Options.Hands, Is.EqualTo(3));
            Assert.That(second.Options.Hands, Is.EqualTo(7));
            Assert.That(first.Joined, Is.False);
            Assert.That(first.Members, Is.Empty);
            Assert.That(second.Members.Select(player => player.Id), Is.EqualTo(new[] { "You" }));
        }
        [TestCase(999999, "", RoomJoinResult.NotFound)]
        [TestCase(102301, "incorrect", RoomJoinResult.WrongPassword)]
        public void FailedJoinPreservesTheCurrentRoom(int number, string password, RoomJoinResult result)
        {
            var target = new LocalRoom(102301, "Private", "coffee", Options(), new[] { Player("Player123") });
            var directory = new LocalRoomDirectory(new[] { target }, 100001);
            var current = directory.Create("Mine", "", Options(), Player("You"));
            Assert.That(directory.Join(number, password, Player("You")), Is.EqualTo(result));
            Assert.That(directory.Current, Is.SameAs(current));
            Assert.That(target.Members.Count, Is.EqualTo(1));
        }
        [Test]
        public void JoiningAPrivateRoomPlacesTheLocalPlayerFirstWithoutLosingOtherMembers()
        {
            var target = new LocalRoom(102301, "Private", "coffee", Options(), new[] { Player("Player123"), Player("Player456") });
            var directory = new LocalRoomDirectory(new[] { target }, 100001);
            Assert.That(directory.Join(102301, "coffee", Player("You")), Is.EqualTo(RoomJoinResult.Joined));
            Assert.That(target.Members.Select(player => player.Id), Is.EqualTo(new[] { "You", "Player123", "Player456" }));
            Assert.That(directory.Join(102301, "coffee", Player("You")), Is.EqualTo(RoomJoinResult.Joined));
            Assert.That(target.Members.Count, Is.EqualTo(3));
            Assert.That(target.AddBot(Player("Player789")), Is.True);
            Assert.That(target.Full, Is.True);
            Assert.That(target.AddBot(Player("Overflow")), Is.False);
            directory.Leave();
            Assert.That(target.Members.Select(player => player.Id), Is.EqualTo(new[] { "Player123", "Player456", "Player789" }));
            Assert.That(target.AddBot(Player("Not joined")), Is.False);
        }
        [Test]
        public void RemovingAPlayerFreesTheSeatButCannotRemoveTheLocalPlayerOrDuplicateAnIdentity()
        {
            var directory = new LocalRoomDirectory(Array.Empty<LocalRoom>(), 100001);
            var room = directory.Create("Mine", "", Options(), Player("You"));
            Assert.That(room.AddBot(Player("Bot")), Is.True);
            Assert.That(room.AddBot(Player("Bot")), Is.False);
            Assert.That(room.RemovePlayer("You"), Is.False);
            Assert.That(room.RemovePlayer("Missing"), Is.False);
            Assert.That(room.RemovePlayer("Bot"), Is.True);
            Assert.That(room.Members.Count, Is.EqualTo(1));
            Assert.That(room.AddBot(Player("Bot")), Is.True);
            directory.Leave();
            Assert.That(room.RemovePlayer("Bot"), Is.False);
        }
        [Test]
        public void FullRoomsRejectJoinAndNumbersSkipCatalogEntries()
        {
            var full = new LocalRoom(100001, "Full", "", Options(), new[] { Player("One"), Player("Two"), Player("Three"), Player("Four") });
            var directory = new LocalRoomDirectory(new[] { full }, 100001);
            Assert.That(directory.Join(100001, "", Player("You")), Is.EqualTo(RoomJoinResult.Full));
            Assert.That(directory.Current, Is.Null);
            Assert.That(directory.Create("Mine", "", Options(), Player("You")).Number, Is.EqualTo(100002));
        }
    }
}
