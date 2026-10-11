using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Okey.Presentation.Menus
{
    public sealed class NewGameMenuView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _partnershipButton;
        [SerializeField] private Button _botModeButton;
        [SerializeField] private Button _escalatingButton;
        [SerializeField] private TMP_Text _partnershipText;
        [SerializeField] private TMP_Text _botModeText;
        [SerializeField] private TMP_Text _escalatingText;
        [SerializeField] private string _individualLabel = "INDIVIDUAL";
        [SerializeField] private string _partnershipLabel = "PARTNERS";
        [SerializeField] private string _humanlikeLabel = "HUMANLIKE";
        [SerializeField] private string _fastLabel = "FAST";
        [SerializeField] private string _fixedLabel = "FIXED";
        [SerializeField] private string _risingLabel = "RISING";
        [SerializeField] private CanvasGroup _card;
        [SerializeField, Min(0)] private float _fadeDuration = .25f;
        private Tween _fade;
        public event Action PlayRequested;
        public event Action PartnershipRequested;
        public event Action BotModeRequested;
        public event Action EscalatingRequested;
        public void Render(bool partnerships, bool humanlike, bool escalating)
        {
            _partnershipText.text = partnerships ? _partnershipLabel : _individualLabel;
            _botModeText.text = humanlike ? _humanlikeLabel : _fastLabel;
            _escalatingText.text = escalating ? _risingLabel : _fixedLabel;
        }
        private void Play() => PlayRequested?.Invoke();
        private void TogglePartnership() => PartnershipRequested?.Invoke();
        private void ToggleBots() => BotModeRequested?.Invoke();
        private void ToggleEscalating() => EscalatingRequested?.Invoke();
        private void OnEnable()
        {
            _playButton.onClick.AddListener(Play);
            _partnershipButton.onClick.AddListener(TogglePartnership);
            _botModeButton.onClick.AddListener(ToggleBots);
            _escalatingButton.onClick.AddListener(ToggleEscalating);
            _card.alpha = 0;
            _fade = DOTween.To(() => _card.alpha, value => _card.alpha = value, 1, _fadeDuration).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        private void OnDisable()
        {
            _fade?.Kill();
            _playButton.onClick.RemoveListener(Play);
            _partnershipButton.onClick.RemoveListener(TogglePartnership);
            _botModeButton.onClick.RemoveListener(ToggleBots);
            _escalatingButton.onClick.RemoveListener(ToggleEscalating);
        }
    }
}
