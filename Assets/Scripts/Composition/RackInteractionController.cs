using System;
using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Presentation.Racks;
using Okey.Presentation.Stones;
using UnityEngine;

namespace Okey.Composition
{
    public sealed class RackInteractionController : IStoneDragTarget
    {
        private readonly RackArrangement _arrangement;
        private readonly RackView _rack;
        private readonly Dictionary<int, Stone> _views = new Dictionary<int, Stone>();
        private Stone _dragged;

        public RackInteractionController(RackArrangement arrangement, RackView rack)
        {
            _arrangement = arrangement ?? throw new ArgumentNullException(nameof(arrangement));
            _rack = rack != null ? rack : throw new ArgumentNullException(nameof(rack));
        }

        public void Register(Stone stone)
        {
            if (stone == null || stone.Instance == null || _arrangement.FindSlot(stone.Instance.Id) < 0)
                throw new ArgumentException("The view must represent a stone in the rack.", nameof(stone));

            _views.Add(stone.Instance.Id, stone);
            stone.DragHandler.Bind(this);
        }

        public bool TryBeginDrag(Stone stone)
        {
            if (_dragged != null || stone == null || stone.Instance == null ||
                !_views.TryGetValue(stone.Instance.Id, out var registered) || registered != stone)
                return false;

            _dragged = stone;
            stone.Motion.SnapTo(stone.transform.localPosition);
            return true;
        }

        public void DragTo(Stone stone, Vector3 worldPosition)
        {
            if (_dragged == null || _dragged != stone)
                return;

            stone.Motion.SnapTo(_rack.StoneRoot.InverseTransformPoint(worldPosition));
        }

        public void EndDrag(Stone stone, Vector3 worldPosition)
        {
            if (_dragged == null || _dragged != stone)
                return;

            _dragged = null;
            if (_rack.TryFindSlot(worldPosition, out var destination))
            {
                var moved = _arrangement.Move(stone.Instance.Id, destination);
                foreach (var shifted in moved)
                {
                    if (shifted.Id != stone.Instance.Id)
                        _rack.MoveToSlot(_views[shifted.Id], _arrangement.FindSlot(shifted.Id), true);
                }
            }

            _rack.MoveToSlot(stone, _arrangement.FindSlot(stone.Instance.Id), true);
        }

        public void CancelDrag(Stone stone)
        {
            if (_dragged == null || _dragged != stone)
                return;

            _dragged = null;
            _rack.MoveToSlot(stone, _arrangement.FindSlot(stone.Instance.Id), false);
        }

        public void Clear()
        {
            if (_dragged != null)
                CancelDrag(_dragged);

            _views.Clear();
        }
    }
}
