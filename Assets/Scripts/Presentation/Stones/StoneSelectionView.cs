using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class StoneSelectionView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Stone _stone;
        [SerializeField] private GameObject _highlight;

        public event Action<Stone> Clicked;

        public void SetSelected(bool selected)
        {
            _highlight.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
                Clicked?.Invoke(_stone);
        }
    }
}
