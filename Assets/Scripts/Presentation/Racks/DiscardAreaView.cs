using UnityEngine;
using System;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Racks
{
    public sealed class DiscardAreaView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private BoxCollider2D _dropArea;
        [SerializeField] private Transform _stoneRoot;
        [SerializeField] private SpriteRenderer _highlight;
        [SerializeField] private Color _availableColor;
        [SerializeField] private Color _waitingColor;
        public event Action<DiscardAreaView> Clicked;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && !data.dragging) Clicked?.Invoke(this);
        }
        public Transform StoneRoot => _stoneRoot;
        public void SetAvailable(bool available)
        {
            if (_highlight != null) _highlight.color = available ? _availableColor : _waitingColor;
        }
        public bool Contains(Vector3 worldPosition)
        {
            var local = (Vector2)_dropArea.transform.InverseTransformPoint(worldPosition) - _dropArea.offset;
            var half = _dropArea.size * .5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
        }
    }
}
