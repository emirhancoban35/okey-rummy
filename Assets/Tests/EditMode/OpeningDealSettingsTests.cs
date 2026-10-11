using NUnit.Framework;
using Okey.Configuration;
using Okey.Core.Dealing;
using Okey.Core.Decks;
using UnityEditor;
using UnityEngine;

namespace Okey.Tests.EditMode
{
    public sealed class OpeningDealSettingsTests
    {
        [Test]
        public void InspectorControlsInitialHandSizeAndRepeatableSeed()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OpeningDealSettings>(
                "Assets/Settings/Dealing/StandardOpeningDeal.asset");
            Assert.That(asset, Is.Not.Null);
            var settings = Object.Instantiate(asset);

            try
            {
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_initialHandSize").intValue = 7;
                serialized.FindProperty("_useFixedSeed").boolValue = true;
                serialized.FindProperty("_seed").intValue = -123;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var deckSettings = AssetDatabase.LoadAssetAtPath<DeckSettings>(
                    "Assets/Settings/Decks/StandardOkeyDeck.asset");
                var factory = new OpeningDealFactory(new DeckFactory(), new SeededStoneShuffler());

                var deal = factory.Create(deckSettings.CreateDefinition(),
                    settings.InitialHandSize, settings.CreateSeed());

                Assert.That(deal.InitialHand.Count, Is.EqualTo(7));
                Assert.That(deal.DrawPile.RemainingCount, Is.EqualTo(99));
                Assert.That(deal.Seed, Is.EqualTo(-123));
                Assert.That(settings.CreateSeed(), Is.EqualTo(-123));
                Assert.That(asset.InitialHandSize, Is.EqualTo(21));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
