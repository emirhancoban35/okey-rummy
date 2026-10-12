using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class TableFeedbackView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private UnityEngine.UI.Image _accent;
        [SerializeField] private Color _successColor;
        [SerializeField] private Color _failureColor;
        [SerializeField, Min(0)] private float _feedbackHold;
        [SerializeField, Min(0)] private float _feedbackFade;
        private Tween _feedback;
        private readonly Queue<(string Text, bool Success)> _pending = new Queue<(string, bool)>();
        public void Show(string text, bool success)
        {
            _pending.Enqueue((text, success));
            if (_feedback == null) ShowNext();
        }
        private void ShowNext()
        {
            if (_pending.Count == 0) return;
            var notice = _pending.Dequeue();
            _feedbackText.text = notice.Text;
            _accent.color = notice.Success ? _successColor : _failureColor;
            _panel.alpha = 0;
            _feedback = DOTween.Sequence().SetUpdate(true)
                .Append(DOTween.To(() => _panel.alpha, value => _panel.alpha = value, 1, _feedbackFade))
                .AppendInterval(_feedbackHold)
                .Append(DOTween.To(() => _panel.alpha, value => _panel.alpha = value, 0, _feedbackFade))
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => { _feedback = null; ShowNext(); });
        }
        private void OnDisable()
        {
            _feedback?.Kill();
            _feedback = null;
            _pending.Clear();
            _panel.alpha = 0;
        }
    }
}
