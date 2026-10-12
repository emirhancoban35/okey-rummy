using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Tables
{
    public sealed class MeldActionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _progressText;
        public Button Button => _button;
        public TMP_Text Label => _label;
        public TMP_Text ProgressText => _progressText;
        public event Action Requested;
        public void Render(string label, string progress, bool available)
        {
            _label.text = label;
            _progressText.text = progress;
            _button.interactable = available;
        }
        private void Request() => Requested?.Invoke();
        private void OnEnable() => _button.onClick.AddListener(Request);
        private void OnDisable() => _button.onClick.RemoveListener(Request);
    }
}
