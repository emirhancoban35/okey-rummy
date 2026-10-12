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
        private readonly IRackLayout _arrangement;
        private readonly RackView _rack;
        private readonly Dictionary<int, Stone> _views = new Dictionary<int, Stone>();
        private Stone _dragged;
        private Vector3 _dragScale;
        public Stone Selected { get; private set; }
        public bool IsDragging => _dragged != null;
        public event Action Changed;
        public event Action<Stone> DiscardRequested;
        public event Action<Stone, Vector3> DragMoved;
        public event Func<Stone, Vector3, bool> DropRequested;

        public RackInteractionController(IRackLayout arrangement, RackView rack)
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
            stone.Selection.Clicked += Select;
            stone.Selection.DoubleClicked += RequestDiscard;
        }

        private void RequestDiscard(Stone stone)
        {
            if (!IsDragging && stone != null && stone.Instance != null &&
                _views.TryGetValue(stone.Instance.Id, out var registered) && registered == stone)
                DiscardRequested?.Invoke(stone);
        }

        public void Select(Stone stone)
        {
            if (IsDragging || stone == null || stone.Instance == null ||
                !_views.TryGetValue(stone.Instance.Id, out var registered) || registered != stone)
                return;

            if (Selected != null)
                Selected.Selection.SetSelected(false);
            Selected = Selected == stone ? null : stone;
            if (Selected != null)
                Selected.Selection.SetSelected(true);
            Changed?.Invoke();
        }

        public void Unregister(Stone stone)
        {
            stone.Selection.Clicked -= Select;
            stone.Selection.DoubleClicked -= RequestDiscard;
            _views.Remove(stone.Instance.Id);
            if (Selected == stone)
            {
                stone.Selection.SetSelected(false);
                Selected = null;
            }
        }

        public bool TryBeginDrag(Stone stone)
        {
            if (_dragged != null || stone == null || stone.Instance == null ||
                !_views.TryGetValue(stone.Instance.Id, out var registered) || registered != stone)
                return false;

            _dragged = stone;
            _dragScale = stone.transform.localScale;
            stone.DragHandler.SetLifted(true);
            stone.Motion.SnapTo(stone.transform.localPosition);
            Changed?.Invoke();
            return true;
        }

        public void DragTo(Stone stone, Vector3 worldPosition)
        {
            if (_dragged == null || _dragged != stone)
                return;

            stone.Motion.SnapTo(_rack.StoneRoot.InverseTransformPoint(worldPosition));
            DragMoved?.Invoke(stone, worldPosition);
        }

        public void SetDragScale(Stone stone, Vector3? worldScale)
        {
            if (_dragged == null || _dragged != stone) return;
            var parentScale = stone.transform.parent.lossyScale;
            stone.transform.localScale = worldScale.HasValue ? new Vector3(
                worldScale.Value.x / parentScale.x, worldScale.Value.y / parentScale.y,
                worldScale.Value.z / parentScale.z) : _dragScale;
        }

        public void EndDrag(Stone stone, Vector3 worldPosition)
        {
            if (_dragged == null || _dragged != stone)
                return;

            stone.transform.localScale = _dragScale;
            stone.DragHandler.SetLifted(false);
            _dragged = null;
            if (DropRequested?.Invoke(stone, worldPosition) == true)
            {
                Changed?.Invoke();
                return;
            }
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
            Changed?.Invoke();
        }

        public void CancelActiveDrag()
        {
            if (_dragged != null) CancelDrag(_dragged);
        }

        public void CancelDrag(Stone stone)
        {
            if (_dragged == null || _dragged != stone)
                return;

            stone.transform.localScale = _dragScale;
            stone.DragHandler.SetLifted(false);
            _dragged = null;
            _rack.MoveToSlot(stone, _arrangement.FindSlot(stone.Instance.Id), false);
            Changed?.Invoke();
        }

        public void Clear()
        {
            if (_dragged != null)
                CancelDrag(_dragged);

            foreach (var view in _views.Values)
            {
                if (view == null)
                    continue;
                view.Selection.Clicked -= Select;
                view.Selection.DoubleClicked -= RequestDiscard;
                view.Selection.SetSelected(false);
            }
            Selected = null;
            _views.Clear();
        }
    }
}
