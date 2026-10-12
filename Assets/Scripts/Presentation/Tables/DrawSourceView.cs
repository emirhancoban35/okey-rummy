using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Tables
{
    public sealed class DrawSourceView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject _readyHighlight;
        [SerializeField] private GameObject _hoverHighlight;
        private bool _hovered;
        private bool _canDraw;
        private readonly TileSourceDragGesture _gesture = new TileSourceDragGesture();
        public bool CanDraw
        {
            get => _canDraw;
            set
            {
                _canDraw = value;
                RenderHighlight();
            }
        }
        public event Action DrawRequested;
        public event Func<Vector3, bool> DragStarted { add => _gesture.Started += value; remove => _gesture.Started -= value; }
        public event Action<Vector3> Dragged { add => _gesture.Moved += value; remove => _gesture.Moved -= value; }
        public event Action<Vector3> DragEnded { add => _gesture.Ended += value; remove => _gesture.Ended -= value; }
        public event Action DragCancelled { add => _gesture.Cancelled += value; remove => _gesture.Cancelled -= value; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _gesture.ResetPress();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CanDraw && !_gesture.SuppressClick && !eventData.dragging && eventData.button == PointerEventData.InputButton.Left)
                DrawRequested?.Invoke();
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (CanDraw) _gesture.Begin(data, transform);
        }
        public void OnDrag(PointerEventData data) => _gesture.Move(data);
        public void OnEndDrag(PointerEventData data) => _gesture.End(data);

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RenderHighlight();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            RenderHighlight();
        }

        private void RenderHighlight()
        {
            _readyHighlight.SetActive(CanDraw && !_hovered);
            _hoverHighlight.SetActive(CanDraw && _hovered);
        }

        private void OnDisable()
        {
            _hovered = false;
            RenderHighlight();
            _gesture.Cancel();
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) _gesture.Cancel();
        }
    }
}
