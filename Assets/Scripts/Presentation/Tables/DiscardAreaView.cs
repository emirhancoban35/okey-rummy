using UnityEngine;
using System;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Tables
{
    public sealed class DiscardAreaView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private BoxCollider2D _dropArea;
        [SerializeField] private Transform _stoneRoot;
        [SerializeField] private GameObject _availableHighlight;
        private readonly TileSourceDragGesture _gesture = new TileSourceDragGesture();
        private bool _available;
        public event Action<DiscardAreaView> Clicked;
        public event Func<DiscardAreaView, Vector3, bool> DragStarted;
        public event Action<Vector3> Dragged { add => _gesture.Moved += value; remove => _gesture.Moved -= value; }
        public event Action<Vector3> DragEnded { add => _gesture.Ended += value; remove => _gesture.Ended -= value; }
        public event Action DragCancelled { add => _gesture.Cancelled += value; remove => _gesture.Cancelled -= value; }
        private void Awake() => _gesture.Started += position => DragStarted?.Invoke(this, position) == true;
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) _gesture.ResetPress();
        }
        public void OnBeginDrag(PointerEventData data)
        {
            if (_available) _gesture.Begin(data, transform);
        }
        public void OnDrag(PointerEventData data) => _gesture.Move(data);
        public void OnEndDrag(PointerEventData data) => _gesture.End(data);
        private void OnDisable() => _gesture.Cancel();
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) _gesture.Cancel();
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && !data.dragging && !_gesture.SuppressClick) Clicked?.Invoke(this);
        }
        public Transform StoneRoot => _stoneRoot;
        public void SetAvailable(bool available)
        {
            _available = available;
            _availableHighlight.SetActive(available);
        }
        public bool Contains(Vector3 worldPosition)
        {
            var local = (Vector2)_dropArea.transform.InverseTransformPoint(worldPosition) - _dropArea.offset;
            var half = _dropArea.size * .5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
        }
    }
}
