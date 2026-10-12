using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Rules;
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
        private readonly Dictionary<int, int[]> _groups = new Dictionary<int, int[]>();
        private readonly Dictionary<int, Vector3> _groupOffsets = new Dictionary<int, Vector3>();
        private int[] _draggedGroup;
        private Stone _dragged;
        private Vector3 _dragScale;
        public Stone Selected { get; private set; }
        public bool IsDragging => _dragged != null;
        public event Action Changed;
        public event Action<Stone> DiscardRequested;
        public event Action<Stone, Vector3> DragMoved;
        public event Func<Stone, Vector3, bool> DropRequested;
        public event Func<IReadOnlyList<int>, Vector3, bool> GroupDropRequested;

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

        public void SetGroups(IReadOnlyList<Meld> groups)
        {
            if (IsDragging) return;
            _groups.Clear();
            foreach (var view in _views.Values) view.Selection.SetGroupHandle(false);
            foreach (var group in groups)
            {
                var ids = group.Tiles.Select(tile => tile.Stone.Id).OrderBy(_arrangement.FindSlot).ToArray();
                var last = ids[ids.Length - 1];
                _groups[last] = ids;
                _views[last].Selection.SetGroupHandle(true);
            }
        }

        public bool TryBeginDrag(Stone stone) => TryBeginDrag(stone, false);
        public bool TryBeginDrag(Stone stone, bool group)
        {
            if (_dragged != null || stone == null || stone.Instance == null ||
                !_views.TryGetValue(stone.Instance.Id, out var registered) || registered != stone)
                return false;

            _dragged = stone;
            _draggedGroup = group && _groups.TryGetValue(stone.Instance.Id, out var ids) ? ids : null;
            _groupOffsets.Clear();
            if (_draggedGroup != null)
                foreach (var id in _draggedGroup)
                {
                    var member = _views[id];
                    member.Selection.CancelPress();
                    member.Motion.SnapTo(member.transform.localPosition);
                    member.DragHandler.SetLifted(true);
                    _groupOffsets.Add(id, member.transform.localPosition - stone.transform.localPosition);
                }
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

            var local = _rack.StoneRoot.InverseTransformPoint(worldPosition);
            if (_draggedGroup != null)
            {
                foreach (var id in _draggedGroup) _views[id].Motion.SnapTo(local + _groupOffsets[id]);
                return;
            }
            stone.Motion.SnapTo(local);
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

            if (_draggedGroup != null)
            {
                if (!_rack.TryFindSlot(worldPosition, out var groupDestination))
                {
                    var group = _draggedGroup;
                    FinishGroupDrag(false);
                    GroupDropRequested?.Invoke(group, worldPosition);
                    return;
                }
                var rowStart = groupDestination / _rack.RowCapacity * _rack.RowCapacity;
                var start = Mathf.Clamp(groupDestination - _draggedGroup.Length + 1, rowStart,
                    Mathf.Min(rowStart + _rack.RowCapacity, _arrangement.Capacity) - _draggedGroup.Length);
                _arrangement.MoveGroup(_draggedGroup, start, _rack.RowCapacity);
                FinishGroupDrag(true);
                return;
            }
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

        private void FinishGroupDrag(bool animated)
        {
            foreach (var id in _draggedGroup) _views[id].DragHandler.SetLifted(false);
            _draggedGroup = null;
            _groupOffsets.Clear();
            _dragged = null;
            foreach (var view in _views.Values) _rack.MoveToSlot(view, _arrangement.FindSlot(view.Instance.Id), animated);
            Changed?.Invoke();
        }

        public void CancelDrag(Stone stone)
        {
            if (_dragged == null || _dragged != stone)
                return;
            if (_draggedGroup != null) { FinishGroupDrag(false); return; }

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
            _groups.Clear();
        }
    }
}
