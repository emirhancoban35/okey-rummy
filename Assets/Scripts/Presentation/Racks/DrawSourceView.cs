using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Racks
{
    public sealed class DrawSourceView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private SpriteRenderer _highlight;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private string _readyText;
        [SerializeField] private string _waitingText;
        [SerializeField] private Color _readyColor;
        [SerializeField] private Color _waitingColor;
        [SerializeField] private Color _hoverColor;
        private bool _canDraw;
        private bool _dragging;
        private bool _suppressClick;
        private int _pointerId;
        private Camera _camera;
        private Plane _plane;
        public bool CanDraw
        {
            get => _canDraw;
            set
            {
                _canDraw = value;
                if (_highlight != null) _highlight.color = value ? _readyColor : _waitingColor;
                if (_hint != null) _hint.text = value ? _readyText : _waitingText;
            }
        }
        public event Action DrawRequested;
        public event Func<Vector3, bool> DragStarted;
        public event Action<Vector3> Dragged;
        public event Action<Vector3> DragEnded;
        public event Action DragCancelled;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _suppressClick = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CanDraw && !_suppressClick && !eventData.dragging && eventData.button == PointerEventData.InputButton.Left)
                DrawRequested?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDraw || _dragging || eventData.button != PointerEventData.InputButton.Left) return;
            _camera = eventData.pressEventCamera;
            if (_camera == null) return;
            _plane = new Plane(transform.forward, transform.position);
            if (!TryGetPosition(eventData.position, out var position)) return;
            _pointerId = eventData.pointerId;
            _suppressClick = true;
            _dragging = DragStarted?.Invoke(position) == true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging && eventData.pointerId == _pointerId && TryGetPosition(eventData.position, out var position))
                Dragged?.Invoke(position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging || eventData.pointerId != _pointerId) return;
            _dragging = false;
            if (TryGetPosition(eventData.position, out var position)) DragEnded?.Invoke(position);
            else DragCancelled?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanDraw && _highlight != null) _highlight.color = _hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_highlight != null) _highlight.color = CanDraw ? _readyColor : _waitingColor;
        }

        private bool TryGetPosition(Vector2 screenPosition, out Vector3 position)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            if (_plane.Raycast(ray, out var distance)) { position = ray.GetPoint(distance); return true; }
            position = default;
            return false;
        }

        private void Cancel()
        {
            if (!_dragging) return;
            _dragging = false;
            DragCancelled?.Invoke();
        }

        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Cancel();
        }
    }
}
