using NUnit.Framework;
using Okey.Core.Stones;
using Okey.Presentation.Stones;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Okey.Tests.EditMode
{
    public sealed class StonePresentationTests
    {
        private GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stone.prefab");
            Assert.That(prefab, Is.Not.Null);
            _instance = Object.Instantiate(prefab);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_instance);
        }

        [Test]
        public void PrefabDisplaysNumberAndConfiguredColor()
        {
            var stone = _instance.GetComponent<Stone>();
            var data = new StoneData(StoneType.Regular, StoneColor.Blue, 7);
            var settings = AssetDatabase.LoadAssetAtPath<StoneVisualSettings>(
                "Assets/Settings/Stones/DefaultStoneVisualSettings.asset");

            var instance = new StoneInstance(0, data);
            stone.Initialize(instance);

            var number = _instance.GetComponentInChildren<TMP_Text>();
            Assert.That(stone.Data, Is.SameAs(data));
            Assert.That(stone.Instance, Is.SameAs(instance));
            Assert.That(number.text, Is.EqualTo("7"));
            Assert.That(number.color, Is.EqualTo(settings.GetColor(data.Color)));
        }

        [Test]
        public void FaceVisibilityBelongsToEachView()
        {
            var data = new StoneData(StoneType.Regular, StoneColor.Red, 5);
            var first = _instance.GetComponent<Stone>();
            var secondInstance = Object.Instantiate(_instance);

            try
            {
                var second = secondInstance.GetComponent<Stone>();
                first.Initialize(new StoneInstance(0, data));
                second.Initialize(new StoneInstance(0, data));

                first.SetFace(false);
                first.Initialize(new StoneInstance(0, data));

                Assert.That(first.IsFaceUp, Is.False);
                Assert.That(second.IsFaceUp, Is.True);
                Assert.That(_instance.GetComponentInChildren<TMP_Text>(), Is.Null);
                Assert.That(secondInstance.GetComponentInChildren<TMP_Text>(), Is.Not.Null);
                Assert.That(_instance.activeSelf, Is.True);
                Assert.That(_instance.GetComponent<SpriteRenderer>().enabled, Is.True);

                first.SetFace(true);
                Assert.That(_instance.GetComponentInChildren<TMP_Text>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(secondInstance);
            }
        }

        [Test]
        public void FakeJokerUsesConfiguredText()
        {
            var stone = _instance.GetComponent<Stone>();
            var settings = AssetDatabase.LoadAssetAtPath<StoneVisualSettings>(
                "Assets/Settings/Stones/DefaultStoneVisualSettings.asset");

            stone.Initialize(new StoneInstance(0,
                new StoneData(StoneType.FakeJoker, StoneColor.None, 0)));

            Assert.That(_instance.GetComponentInChildren<TMP_Text>().text,
                Is.EqualTo(settings.FakeJokerText));
        }

        [Test]
        public void MissingPaletteEntryReportsConfigurationError()
        {
            var settings = AssetDatabase.LoadAssetAtPath<StoneVisualSettings>(
                "Assets/Settings/Stones/DefaultStoneVisualSettings.asset");

            Assert.Throws<System.InvalidOperationException>(() => settings.GetColor((StoneColor)99));
        }
    }
}
