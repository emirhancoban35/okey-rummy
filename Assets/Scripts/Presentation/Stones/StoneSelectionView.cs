using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class StoneSelectionView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Stone _stone;
        [SerializeField] private GameObject _highlight;
        [SerializeField] private GameObject _groupHighlight;
        public void SetGrouped(bool grouped)
        {
            if (_groupHighlight != null) _groupHighlight.SetActive(grouped);
        }
        [SerializeField, Min(.1f)] private float _holdDuration = .5f;
        [SerializeField, Min(.01f)] private float _flipDuration = .12f;
        private PointerEventData _press;
        private float _pressedAt;
        private bool _consumed;
        private Sequence _flip;
        private Vector3 _scale;
        public event Action<Stone> Clicked;

        public void SetSelected(bool selected) => _highlight.SetActive(selected);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_press != null || eventData.button != PointerEventData.InputButton.Left || _stone.Instance == null) return;
            CancelPress();
            _consumed = false;
            _press = eventData;
            _pressedAt = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_press != null && _press.pointerId == eventData.pointerId) _press = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_press != null && _press.pointerId == eventData.pointerId) _press = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_consumed && eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
                Clicked?.Invoke(_stone);
        }

        private void Update()
        {
            if (_press == null || _press.dragging || Time.unscaledTime - _pressedAt < _holdDuration) return;
            _press = null;
            _consumed = true;
            _scale = _stone.transform.localScale;
            var faceUp = !_stone.IsFaceUp;
            _flip = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Append(_stone.transform.DOScaleX(0, _flipDuration))
                .AppendCallback(() => _stone.SetFace(faceUp))
                .Append(_stone.transform.DOScaleX(_scale.x, _flipDuration))
                .OnComplete(() => _flip = null);
        }

        public void CancelPress()
        {
            _press = null;
            if (_flip == null) return;
            _flip.Kill();
            _flip = null;
            _stone.transform.localScale = _scale;
        }

        private void OnDisable() => CancelPress();
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) CancelPress();
        }
    }
}
