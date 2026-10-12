using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Okey.Presentation.Tables
{
    public sealed class TileSourceDragGesture
    {
        private Camera _camera;
        private Plane _plane;
        private int _pointerId;
        private bool _dragging;
        public bool SuppressClick { get; private set; }
        public event Func<Vector3, bool> Started;
        public event Action<Vector3> Moved;
        public event Action<Vector3> Ended;
        public event Action Cancelled;
        public void ResetPress() => SuppressClick = false;
        public void Begin(PointerEventData data, Transform surface)
        {
            if (_dragging || data.button != PointerEventData.InputButton.Left) return;
            _camera = data.pressEventCamera;
            if (_camera == null) return;
            _plane = new Plane(surface.forward, surface.position);
            if (!TryPosition(data.position, out var position)) return;
            _pointerId = data.pointerId;
            SuppressClick = true;
            _dragging = Started?.Invoke(position) == true;
        }
        public void Move(PointerEventData data)
        {
            if (_dragging && data.pointerId == _pointerId && TryPosition(data.position, out var position)) Moved?.Invoke(position);
        }
        public void End(PointerEventData data)
        {
            if (!_dragging || data.pointerId != _pointerId) return;
            _dragging = false;
            if (TryPosition(data.position, out var position)) Ended?.Invoke(position);
            else Cancelled?.Invoke();
        }
        private bool TryPosition(Vector2 screenPosition, out Vector3 position)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            if (_plane.Raycast(ray, out var distance)) { position = ray.GetPoint(distance); return true; }
            position = default;
            return false;
        }
        public void Cancel()
        {
            if (!_dragging) return;
            _dragging = false;
            Cancelled?.Invoke();
        }
    }
}
