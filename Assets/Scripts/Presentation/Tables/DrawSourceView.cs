using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Tables
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
        private readonly TileSourceDragGesture _gesture = new TileSourceDragGesture();
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
            if (CanDraw && _highlight != null) _highlight.color = _hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_highlight != null) _highlight.color = CanDraw ? _readyColor : _waitingColor;
        }

        private void OnDisable() => _gesture.Cancel();
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) _gesture.Cancel();
        }
    }
}
