using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Menus
{
    public sealed class RoomSeatView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _remove;
        public event Action<RoomSeatView> RemoveRequested;

        public void Render(string label, bool removable)
        {
            _label.text = label;
            _remove.gameObject.SetActive(removable);
        }

        private void Remove() => RemoveRequested?.Invoke(this);
        private void OnEnable() => _remove.onClick.AddListener(Remove);
        private void OnDisable() => _remove.onClick.RemoveListener(Remove);
    }
}
