using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Okey.Presentation.Racks
{
    public sealed class DrawSourceView : MonoBehaviour, IPointerClickHandler
    {
        public bool CanDraw { get; set; }
        public event Action DrawRequested;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (CanDraw && eventData.button == PointerEventData.InputButton.Left)
                DrawRequested?.Invoke();
        }
    }
}
