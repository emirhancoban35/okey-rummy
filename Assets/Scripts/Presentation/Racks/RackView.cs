using System;
using System.Collections.Generic;
using Okey.Presentation.Stones;
using UnityEngine;

namespace Okey.Presentation.Racks
{
    [DisallowMultipleComponent]
    public sealed class RackView : MonoBehaviour
    {
        [SerializeField] private Transform _stoneRoot;
        [SerializeField] private Transform _dealOrigin;
        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();
        [SerializeField] private Vector3 _stoneScale;
        [SerializeField, Min(0)] private float _dealInterval;

        public Transform StoneRoot => _stoneRoot;
        public int Capacity => _slots.Length;

        public void ValidateCapacity(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_stoneRoot == null || _dealOrigin == null)
                throw new InvalidOperationException("Stone root and deal origin must be assigned to the rack.");

            if (count > Capacity)
                throw new InvalidOperationException("The rack does not have enough slots for the hand.");

            var slots = new HashSet<Transform>();
            foreach (var slot in _slots)
            {
                if (slot == null || !slots.Add(slot))
                    throw new InvalidOperationException("Rack slots must be assigned and unique.");
            }

            if (_stoneScale.x <= 0 || _stoneScale.y <= 0 || _stoneScale.z <= 0 || _dealInterval < 0)
                throw new InvalidOperationException("Stone scale must be positive and deal interval cannot be negative.");
        }

        public Vector3 GetSlotLocalPosition(int index)
        {
            if (index < 0 || index >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _stoneRoot.InverseTransformPoint(_slots[index].position);
        }

        public void Place(Stone stone, int index, bool animated)
        {
            if (stone == null)
                throw new ArgumentNullException(nameof(stone));

            var destination = GetSlotLocalPosition(index);
            stone.transform.SetParent(_stoneRoot, false);
            stone.transform.rotation = _slots[index].rotation;
            stone.transform.localScale = _stoneScale;
            stone.SetFace(true);

            if (animated)
            {
                stone.Motion.SnapTo(_stoneRoot.InverseTransformPoint(_dealOrigin.position));
                stone.Motion.MoveTo(destination, index * _dealInterval);
            }
            else
            {
                stone.Motion.SnapTo(destination);
            }
        }
    }
}
