using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class StoneDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Stone _stone;
        [SerializeField] private SortingGroup _sortingGroup;
        [SerializeField] private int _dragSortingOrder;

        private IStoneDragTarget _target;
        private Camera _camera;
        private Plane _plane;
        private Vector3 _offset;
        private int _pointerId;
        private int _originalOrder;
        private bool _dragging;

        public void Bind(IStoneDragTarget target)
        {
            Cancel();
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragging || _target == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            _stone.Selection.CancelPress();
            _camera = eventData.pressEventCamera;
            if (_camera == null)
                return;

            _plane = new Plane(_stone.transform.parent.forward, _stone.transform.position);
            if (!TryGetPosition(eventData.position, out var position) || !_target.TryBeginDrag(_stone))
                return;

            _offset = _stone.transform.position - position;
            _pointerId = eventData.pointerId;
            _originalOrder = _sortingGroup.sortingOrder;
            _sortingGroup.sortingOrder = _dragSortingOrder;
            _dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging && eventData.pointerId == _pointerId && TryGetPosition(eventData.position, out var position))
                _target.DragTo(_stone, position + _offset);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging || eventData.pointerId != _pointerId)
                return;

            RestoreOrder();
            if (TryGetPosition(eventData.position, out var position))
                _target.EndDrag(_stone, position + _offset);
            else
                _target.CancelDrag(_stone);
        }

        private bool TryGetPosition(Vector2 screenPosition, out Vector3 position)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            if (_plane.Raycast(ray, out var distance))
            {
                position = ray.GetPoint(distance);
                return true;
            }

            position = default;
            return false;
        }

        private void RestoreOrder()
        {
            _dragging = false;
            _sortingGroup.sortingOrder = _originalOrder;
        }

        private void Cancel()
        {
            if (!_dragging)
                return;

            RestoreOrder();
            _target.CancelDrag(_stone);
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Cancel();
        }
    }
}
