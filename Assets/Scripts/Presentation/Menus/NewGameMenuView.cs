using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Okey.Presentation.Menus
{
    public sealed class NewGameMenuView : MonoBehaviour
    {
        [SerializeField] private Button _ruleButton;
        [SerializeField] private TMP_Text _ruleText;
        [SerializeField] private Button _createRoomButton;
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
        [SerializeField] private Button _handsButton;
        [SerializeField] private TMP_Text _handsText;
        [SerializeField] private string _unavailable = "NOT USED";
        [SerializeField] private string _partners = "PARTNERS";
        [SerializeField] private string _individual = "INDIVIDUAL";
        [SerializeField] private string _humanlike = "HUMANLIKE";
        [SerializeField] private string _fast = "FAST";
        [SerializeField] private string _rising = "RISING";
        [SerializeField] private string _fixed = "FIXED";
        [SerializeField] private string _reclaimOn = "RECLAIM ON";
        [SerializeField] private string _reclaimOff = "RECLAIM OFF";
        private string _handsFormat;
        public event Action HandsRequested;
        public event Action JokerReclaimRequested;
        public event Action RulesRequested;
        public event Action CreateRoomRequested;
        public event Action PartnershipRequested;
        public event Action BotModeRequested;
        public event Action EscalatingRequested;
        private Tween _fade;

        private void Awake() => _handsFormat = _handsText.text;

        public void RenderHands(int hands) => _handsText.text = string.Format(_handsFormat, hands);
        public void Render(string rules, bool partnerships, bool humanlike, bool rising, bool canChooseOpenings)
        {
            _ruleText.text = rules;
            _partnershipText.text = partnerships ? _partners : _individual;
            _botModeText.text = humanlike ? _humanlike : _fast;
            _escalatingText.text = !canChooseOpenings ? _unavailable : rising ? _rising : _fixed;
            _escalatingButton.interactable = canChooseOpenings;
        }
        public void RenderJokerReclaim(bool enabled, bool available)
        {
            _jokerReclaimText.text = !available ? _unavailable : enabled ? _reclaimOn : _reclaimOff;
            _jokerReclaimButton.interactable = available;
        }
        private void NextHands() => HandsRequested?.Invoke();
        private void ToggleJokerReclaim() => JokerReclaimRequested?.Invoke();
        private void ToggleRules() => RulesRequested?.Invoke();
        private void CreateRoom() => CreateRoomRequested?.Invoke();
        private void TogglePartnership() => PartnershipRequested?.Invoke();
        private void ToggleBots() => BotModeRequested?.Invoke();
        private void ToggleEscalating() => EscalatingRequested?.Invoke();
        private void OnEnable()
        {
            _handsButton.onClick.AddListener(NextHands);
            _jokerReclaimButton.onClick.AddListener(ToggleJokerReclaim);
            _ruleButton.onClick.AddListener(ToggleRules);
            _createRoomButton.onClick.AddListener(CreateRoom);
            _partnershipButton.onClick.AddListener(TogglePartnership);
            _botModeButton.onClick.AddListener(ToggleBots);
            _escalatingButton.onClick.AddListener(ToggleEscalating);
            _card.alpha = 0;
            _fade = DOTween.To(() => _card.alpha, value => _card.alpha = value, 1, _fadeDuration).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        private void OnDisable()
        {
            _fade?.Kill();
            _handsButton.onClick.RemoveListener(NextHands);
            _jokerReclaimButton.onClick.RemoveListener(ToggleJokerReclaim);
            _ruleButton.onClick.RemoveListener(ToggleRules);
            _createRoomButton.onClick.RemoveListener(CreateRoom);
            _partnershipButton.onClick.RemoveListener(TogglePartnership);
            _botModeButton.onClick.RemoveListener(ToggleBots);
            _escalatingButton.onClick.RemoveListener(ToggleEscalating);
        }
    }
}
