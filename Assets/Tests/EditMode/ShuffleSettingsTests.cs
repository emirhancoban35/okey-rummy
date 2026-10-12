using NUnit.Framework;
using Okey.Configuration;
using UnityEditor;
using UnityEngine;

namespace Okey.Tests.EditMode
{
    public sealed class ShuffleSettingsTests
    {
        [Test]
        public void FixedSeedCanBeEditedWithoutChangingTheAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ShuffleSettings>("Assets/Settings/Tables/StandardShuffle.asset");
            Assert.That(asset, Is.Not.Null);
            var settings = Object.Instantiate(asset);
            try
            {
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_useFixedSeed").boolValue = true;
                serialized.FindProperty("_seed").intValue = -123;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(settings.CreateSeed(), Is.EqualTo(-123));
                Assert.That(settings.CreateSeed(), Is.EqualTo(-123));
                Assert.That(new SerializedObject(asset).FindProperty("_useFixedSeed").boolValue, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
