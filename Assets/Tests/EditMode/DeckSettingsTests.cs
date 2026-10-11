using System.Linq;
using NUnit.Framework;
using Okey.Configuration;
using Okey.Core.Decks;
using Okey.Core.Stones;
using UnityEditor;
using UnityEngine;

namespace Okey.Tests.EditMode
{
    public sealed class DeckSettingsTests
    {
        [Test]
        public void StandardAssetCreatesCompleteOkeyDeck()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DeckSettings>(
                "Assets/Settings/Decks/StandardOkeyDeck.asset");
            Assert.That(settings, Is.Not.Null);

            var stones = new DeckFactory().Create(settings.CreateDefinition());

            Assert.That(stones.Count, Is.EqualTo(106));
            Assert.That(stones.Select(stone => stone.Id).Distinct().Count(), Is.EqualTo(106));
            var regular = stones.Where(stone => stone.Data.Type == StoneType.Regular).ToArray();
            Assert.That(regular.Length, Is.EqualTo(104));
            foreach (var color in new[] { StoneColor.Red, StoneColor.Blue, StoneColor.Yellow, StoneColor.Black })
            {
                foreach (var value in Enumerable.Range(1, 13))
                    Assert.That(regular.Count(stone => stone.Data.Color == color && stone.Data.Value == value),
                        Is.EqualTo(2));
            }

            Assert.That(stones.Count(stone => stone.Data.Type == StoneType.FakeJoker
                && stone.Data.Color == StoneColor.None && stone.Data.Value == 0), Is.EqualTo(2));
        }

        [Test]
        public void InspectorChangesControlGeneratedDeckWithoutChangingExistingDefinition()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DeckSettings>(
                "Assets/Settings/Decks/StandardOkeyDeck.asset");
            var settings = Object.Instantiate(asset);

            try
            {
                var original = settings.CreateDefinition();
                var serialized = new SerializedObject(settings);
                var sets = serialized.FindProperty("_sets");
                sets.arraySize = 1;
                var entry = sets.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("Type").intValue = (int)StoneType.Regular;
                entry.FindPropertyRelative("Color").intValue = (int)StoneColor.Yellow;
                entry.FindPropertyRelative("MinimumValue").intValue = 20;
                entry.FindPropertyRelative("MaximumValue").intValue = 22;
                entry.FindPropertyRelative("CopiesPerValue").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var stones = new DeckFactory().Create(settings.CreateDefinition());

                Assert.That(original.Count, Is.EqualTo(106));
                Assert.That(stones.Count, Is.EqualTo(9));
                Assert.That(stones.All(stone => stone.Data.Color == StoneColor.Yellow), Is.True);
                foreach (var value in new[] { 20, 21, 22 })
                    Assert.That(stones.Count(stone => stone.Data.Value == value), Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
