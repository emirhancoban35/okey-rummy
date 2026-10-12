using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Menus
{
    public sealed class RoomListItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        private int _number;
        public event Action<int> Selected;

        public void Render(int number, string label, bool available)
        {
            _number = number;
            _label.text = label;
            _button.interactable = available;
        }

        private void Select() => Selected?.Invoke(_number);
        private void OnEnable() => _button.onClick.AddListener(Select);
        private void OnDisable() => _button.onClick.RemoveListener(Select);
    }
}
