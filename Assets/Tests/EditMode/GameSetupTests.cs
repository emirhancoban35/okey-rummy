using NUnit.Framework;
using Okey.Configuration;
using Okey.Core.Tables;
using UnityEditor;

namespace Okey.Tests.EditMode
{
    public sealed class GameSetupTests
    {
        [Test]
        public void IndependentSetupsAndTablesDoNotMutateDefaultsOrEachOther()
        {
            var defaults = AssetDatabase.LoadAssetAtPath<TableSettings>("Assets/Settings/Dealing/Standard101Table.asset");
            var original = EditorJsonUtility.ToJson(defaults);
            var first = defaults.CreateSetup();
            var second = defaults.CreateSetup();
            var options = first.CreateOptions();
            var bots = first.CreateBotPreferences();
            var otherBots = first.CreateBotPreferences();
            first.NextRules();
            first.TogglePartnerships();
            first.ToggleEscalatingOpenings();
            first.ToggleBotMode();
            bots.ToggleMode();
            Assert.That(second.RuleIndex, Is.EqualTo(defaults.RuleIndex));
            Assert.That(second.Partnerships, Is.EqualTo(defaults.Partnerships));
            Assert.That(second.HumanlikeBots, Is.EqualTo(defaults.HumanlikeBots));
            Assert.That(options.Partnerships, Is.EqualTo(defaults.Partnerships));
            Assert.That(otherBots.Humanlike, Is.EqualTo(defaults.HumanlikeBots));
            Assert.That(bots.Humanlike, Is.Not.EqualTo(otherBots.Humanlike));
            Assert.That(EditorJsonUtility.ToJson(defaults), Is.EqualTo(original));
        }

        [Test]
        public void StartingSeatHistoryBelongsToItsOwnGameFlow()
        {
            var first = new StartingSeatSequence(new SeededRandomSource(42));
            var second = new StartingSeatSequence(new SeededRandomSource(42));
            var initial = first.Next(4, true);
            Assert.That(first.Next(4, true), Is.EqualTo((initial + 1) % 4));
            Assert.That(second.Next(4, true), Is.EqualTo(initial));
            Assert.That(first.Next(4, false), Is.Zero);
        }
    }
}
