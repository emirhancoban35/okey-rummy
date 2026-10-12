using Okey.Presentation.Text;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Okey.Presentation.Menus
{
    public sealed class NewGameMenuView : MonoBehaviour
    {
        [SerializeField] private TableTextCatalog _texts;
        public TableTextCatalog Texts => _texts;
        [SerializeField] private Button _ruleButton;
        [SerializeField] private TMP_Text _ruleText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _partnershipButton;
        [SerializeField] private Button _botModeButton;
        [SerializeField] private Button _escalatingButton;
        [SerializeField] private TMP_Text _partnershipText;
        [SerializeField] private TMP_Text _botModeText;
        [SerializeField] private TMP_Text _escalatingText;
        [SerializeField] private CanvasGroup _card;
        [SerializeField, Min(0)] private float _fadeDuration = .25f;
        [SerializeField] private Button _jokerReclaimButton;
        [SerializeField] private TMP_Text _jokerReclaimText;
        public event Action JokerReclaimRequested;
        private Tween _fade;
        public event Action RulesRequested;
        public event Action PlayRequested;
        public event Action PartnershipRequested;
        public event Action BotModeRequested;
        public event Action EscalatingRequested;
        public void Render(string rules, string partnerships, string bots, string openings, bool canChooseOpenings)
        {
            _ruleText.text = rules;
            _partnershipText.text = partnerships;
            _botModeText.text = bots;
            _escalatingText.text = openings;
            _escalatingButton.interactable = canChooseOpenings;
        }
        public void RenderJokerReclaim(string label, bool available)
        {
            _jokerReclaimText.text = label;
            _jokerReclaimButton.interactable = available;
        }
        private void ToggleJokerReclaim() => JokerReclaimRequested?.Invoke();
        private void ToggleRules() => RulesRequested?.Invoke();
        private void Play() => PlayRequested?.Invoke();
        private void TogglePartnership() => PartnershipRequested?.Invoke();
        private void ToggleBots() => BotModeRequested?.Invoke();
        private void ToggleEscalating() => EscalatingRequested?.Invoke();
        private void OnEnable()
        {
            _jokerReclaimButton.onClick.AddListener(ToggleJokerReclaim);
            _ruleButton.onClick.AddListener(ToggleRules);
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
            _jokerReclaimButton.onClick.RemoveListener(ToggleJokerReclaim);
            _ruleButton.onClick.RemoveListener(ToggleRules);
            _playButton.onClick.RemoveListener(Play);
            _partnershipButton.onClick.RemoveListener(TogglePartnership);
            _botModeButton.onClick.RemoveListener(ToggleBots);
            _escalatingButton.onClick.RemoveListener(ToggleEscalating);
        }
    }
}
