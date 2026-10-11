using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class TableFeedbackView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private Color _successColor;
        [SerializeField] private Color _failureColor;
        [SerializeField, Min(0)] private float _feedbackHold;
        [SerializeField, Min(0)] private float _feedbackFade;
        private Tween _feedback;
        public void Show(string text, bool success)
        {
            _feedback?.Kill();
            _feedbackText.text = text;
            _feedbackText.color = success ? _successColor : _failureColor;
            _feedbackText.alpha = 1;
            _feedback = DOTween.To(() => _feedbackText.alpha, value => _feedbackText.alpha = value, 0, _feedbackFade)
                .SetDelay(_feedbackHold).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        private void OnDisable() => _feedback?.Kill();
    }
}
