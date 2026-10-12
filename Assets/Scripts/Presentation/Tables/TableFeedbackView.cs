using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class TableFeedbackView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private GameObject _failureAccent;
        [SerializeField, Min(0)] private float _feedbackHold;
        [SerializeField, Min(0)] private float _feedbackFade;
        [SerializeField] private string _yourTurn = "YOUR TURN";
        [SerializeField] private string _openingFormat = "{0} opened with {1}";
        [SerializeField] private string _penaltyFormat = "{0} +{1} penalty";
        [SerializeField] private string _playableDiscardFormat = "{0}\nPlayable discard · +{1}";
        [SerializeField] private string _jokerDiscardFormat = "{0}\nJoker discard · +{1}";
        [SerializeField] private string _jokerReclaimedFormat = "{2} reclaimed a joker\n{0} · +{1}";
        [SerializeField, TextArea] private string _discardTrialHelp = "Use this tile to open or meld.\nClick the pile to return and draw.";
        [SerializeField] private string _previousDiscardHelp = "Take from the highlighted tray before drawing.";

        public void ShowTurn() => Show(_yourTurn, true);
        public void ShowOpening(string name, int value) => Show(string.Format(_openingFormat, name, value), true);
        public void ShowDiscardTrialHelp(bool success = true) => Show(_discardTrialHelp, success);
        public void ShowPreviousDiscardHelp() => Show(_previousDiscardHelp, false);
        public void ShowPenalty(TablePenaltyKind kind, string player, int penalty, string actor)
        {
            var format = kind == TablePenaltyKind.PlayableDiscard ? _playableDiscardFormat :
                kind == TablePenaltyKind.JokerDiscard ? _jokerDiscardFormat :
                kind == TablePenaltyKind.JokerReclaimed ? _jokerReclaimedFormat : _penaltyFormat;
            Show(string.Format(format, player, penalty, actor), false);
        }

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
            _failureAccent.SetActive(!notice.Success);
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
