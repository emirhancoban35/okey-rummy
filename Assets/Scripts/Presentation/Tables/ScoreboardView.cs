using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace Okey.Presentation.Tables
{
    public sealed class ScoreboardView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _open;
        [SerializeField] private Button _close;
        [SerializeField] private Button _continue;
        [SerializeField] private TMP_Text _continueLabel;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private TMP_Text _rounds;
        [SerializeField] private ScoreboardRowView[] _rows;
        [SerializeField] private string _roundFormat = "Hand {0} / {1}";
        [SerializeField] private string _roundEnded = "Hand complete";
        [SerializeField] private string _matchEnded = "Match complete";
        [SerializeField] private string _playing = "Current match";
        [SerializeField] private string _nextHand = "Next hand";
        [SerializeField] private string _newMatch = "New match";
        [SerializeField] private string _historyFormat = "Hand {0}: {1}";
        [SerializeField] private float _fadeSeconds = .18f;
        private Tween _fade;
        public event Action ContinueRequested;
        public void RenderHeader(int hands, int limit, bool finished, bool complete, string winner, string history)
        {
            _subtitle.text = string.Format(_roundFormat, Mathf.Min(hands + (finished ? 0 : 1), limit), limit) + "  ·  " +
                (complete ? _matchEnded : finished ? _roundEnded : _playing) + (winner.Length == 0 ? string.Empty : "\n" + winner);
            _rounds.text = history;
            _continue.gameObject.SetActive(finished);
            _continueLabel.text = complete ? _newMatch : _nextHand;
        }
        public string HistoryLine(int hand, string scores) => string.Format(_historyFormat, hand, scores);
        public void RenderRow(int row, string name, string opening, string penalty, string score, string total, Color color)
        {
            _rows[row].gameObject.SetActive(true);
            _rows[row].Render(name, opening, penalty, score, total, color);
        }
        public void SetPlayerCount(int count)
        {
            for (var index = 0; index < _rows.Length; index++) _rows[index].gameObject.SetActive(index < count);
        }
        public void Show()
        {
            _fade?.Kill();
            _panel.SetActive(true);
            _group.alpha = 0;
            _fade = DOTween.To(() => _group.alpha, value => _group.alpha = value, 1, _fadeSeconds).SetUpdate(true).SetLink(gameObject);
        }
        private void Hide() { _fade?.Kill(); _panel.SetActive(false); }
        private void Continue() => ContinueRequested?.Invoke();
        private void OnEnable()
        {
            _open.onClick.AddListener(Show);
            _close.onClick.AddListener(Hide);
            _continue.onClick.AddListener(Continue);
        }
        private void OnDisable()
        {
            _fade?.Kill();
            _open.onClick.RemoveListener(Show);
            _close.onClick.RemoveListener(Hide);
            _continue.onClick.RemoveListener(Continue);
        }
    }
}
