using System;
using NUnit.Framework;
using Okey.Core.Stones;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Okey.Tests.EditMode
{
    public sealed class RackViewTests
    {
        private GameObject _rackObject;
        private GameObject _stoneObject;
        private RackView _rack;

        [SetUp]
        public void SetUp()
        {
            _rackObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Istaka.prefab"));
            _stoneObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stone.prefab"));
            _rack = _rackObject.GetComponent<RackView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stoneObject);
            Object.DestroyImmediate(_rackObject);
        }

        [Test]
        public void PrefabProvidesConfiguredOpeningCapacity()
        {
            Assert.That(_rack, Is.Not.Null);
            Assert.That(_rack.Capacity, Is.EqualTo(24));
            Assert.DoesNotThrow(() => _rack.ValidateCapacity(21));
            Assert.Throws<InvalidOperationException>(() => _rack.ValidateCapacity(25));
        }

        [Test]
        public void PlacementUsesEditedSlotTransformAndSerializedScale()
        {
            var serialized = new SerializedObject(_rack);
            var slot = (Transform)serialized.FindProperty("_slots").GetArrayElementAtIndex(0).objectReferenceValue;
            slot.localPosition = new Vector3(1, 2, 0);
            slot.localRotation = Quaternion.Euler(0, 0, 17);
            _rackObject.transform.rotation = Quaternion.Euler(0, 0, 10);
            var scale = new Vector3(0.4f, 0.6f, 1);
            serialized.FindProperty("_stoneScale").vector3Value = scale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var stone = _stoneObject.GetComponent<Stone>();
            stone.Initialize(new StoneInstance(0, new StoneData(StoneType.Regular, StoneColor.Blue, 7)));
            stone.SetFace(false);

            _rack.Place(stone, 0, false);

            Assert.That(stone.transform.parent, Is.EqualTo(_rack.StoneRoot));
            Assert.That(Vector3.Distance(stone.transform.position, slot.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(stone.transform.rotation, slot.rotation), Is.LessThan(0.001f));
            Assert.That(stone.transform.localScale, Is.EqualTo(scale));
            Assert.That(stone.IsFaceUp, Is.True);
            Assert.That(stone.Motion, Is.SameAs(stone.GetComponent<StoneMotion>()));
        }

        [Test]
        public void MissingAndDuplicateSlotsFailValidation()
        {
            var serialized = new SerializedObject(_rack);
            var slots = serialized.FindProperty("_slots");
            var first = slots.GetArrayElementAtIndex(0).objectReferenceValue;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = first;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => _rack.ValidateCapacity(21));

            slots.GetArrayElementAtIndex(1).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => _rack.ValidateCapacity(21));
        }

        [Test]
        public void DropSelectionUsesEditedSlotsAndLocalRadiusAfterRackTransformChanges()
        {
            _rackObject.transform.rotation = Quaternion.Euler(0, 0, 25);
            _rackObject.transform.localScale = new Vector3(2, 3, 1);
            var serialized = new SerializedObject(_rack);
            var slot = (Transform)serialized.FindProperty("_slots").GetArrayElementAtIndex(23).objectReferenceValue;
            slot.localPosition = new Vector3(2, -1, 0);
            serialized.FindProperty("_dropRadius").floatValue = 0.1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var localPosition = _rack.GetSlotLocalPosition(23);

            Assert.That(_rack.TryFindSlot(_rack.StoneRoot.TransformPoint(localPosition + Vector3.right * 0.05f), out var index), Is.True);
            Assert.That(index, Is.EqualTo(23));
            Assert.That(_rack.TryFindSlot(_rack.StoneRoot.TransformPoint(localPosition + Vector3.down * 0.2f), out index), Is.False);
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void InvalidSlotDoesNotChangeStoneParentOrPosition()
        {
            var stone = _stoneObject.GetComponent<Stone>();
            var position = stone.transform.position;
            var parent = stone.transform.parent;

            Assert.Throws<ArgumentOutOfRangeException>(() => _rack.Place(stone, _rack.Capacity, false));
            Assert.That(stone.transform.position, Is.EqualTo(position));
            Assert.That(stone.transform.parent, Is.EqualTo(parent));
            Assert.Throws<ArgumentNullException>(() => _rack.Place(null, 0, false));
        }
    }
}
