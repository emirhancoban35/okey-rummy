using System;
using Okey.Core.Games;
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
        [SerializeField] private string _addLabel = "ADD";
        [SerializeField] private string _finishLabel = "FINISH";
        [SerializeField] private string _pairsLabel = "PAIRS";
        [SerializeField] private string _additionalFormat = "+ {0}";
        private string _openLabel;
        private string _progressFormat;
        public Button Button => _button;
        public TMP_Text Label => _label;
        public TMP_Text ProgressText => _progressText;
        public event Action Requested;

        private void Awake()
        {
            _openLabel = _label.text;
            _progressFormat = _progressText.text;
        }

        public void Render(RackActionKind kind, bool additional, int progress, int required, bool available)
        {
            _label.text = kind == RackActionKind.OpenMelds ? (additional ? _addLabel : _openLabel) :
                kind == RackActionKind.Finish ? _finishLabel : _pairsLabel;
            _progressText.text = additional ? string.Format(_additionalFormat, progress) :
                string.Format(_progressFormat, progress, required);
            _button.interactable = available;
        }

        private void Request() => Requested?.Invoke();
        private void OnEnable() => _button.onClick.AddListener(Request);
        private void OnDisable() => _button.onClick.RemoveListener(Request);
    }
}
