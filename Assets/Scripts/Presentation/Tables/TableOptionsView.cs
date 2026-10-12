using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Tables
{
    public sealed class TableOptionsView : MonoBehaviour
    {
        [SerializeField] private Button _botModeButton;
        [SerializeField] private Button _partnershipButton;
        [SerializeField] private Button _newHandButton;
        [SerializeField] private Button _escalatingButton;
        [SerializeField] private TMP_Text _botModeLabel;
        [SerializeField] private TMP_Text _partnershipLabel;
        [SerializeField] private TMP_Text _escalatingLabel;
        [SerializeField] private string _humanlike = "BOTS: HUMANLIKE";
        [SerializeField] private string _fast = "BOTS: FAST";
        [SerializeField] private string _partners = "NEXT: PARTNERS";
        [SerializeField] private string _individual = "NEXT: INDIVIDUAL";
        [SerializeField] private string _rising = "NEXT: RISING OPENINGS";
        [SerializeField] private string _fixed = "NEXT: FIXED OPENINGS";
        public event Action BotModeRequested;
        public event Action PartnershipRequested;
        public event Action NewHandRequested;
        public event Action EscalatingRequested;
        public void Render(bool humanlike, bool partners, bool rising, bool showOpenings)
        {
            _botModeLabel.text = humanlike ? _humanlike : _fast;
            _partnershipLabel.text = partners ? _partners : _individual;
            _escalatingLabel.text = rising ? _rising : _fixed;
            _escalatingButton.gameObject.SetActive(showOpenings);
        }
        private void RequestBots() => BotModeRequested?.Invoke();
        private void RequestPartners() => PartnershipRequested?.Invoke();
        private void RequestNewHand() => NewHandRequested?.Invoke();
        private void RequestOpenings() => EscalatingRequested?.Invoke();
        private void OnEnable()
        {
            _botModeButton.onClick.AddListener(RequestBots);
            _partnershipButton.onClick.AddListener(RequestPartners);
            _newHandButton.onClick.AddListener(RequestNewHand);
            _escalatingButton.onClick.AddListener(RequestOpenings);
        }
        private void OnDisable()
        {
            _botModeButton.onClick.RemoveListener(RequestBots);
            _partnershipButton.onClick.RemoveListener(RequestPartners);
            _newHandButton.onClick.RemoveListener(RequestNewHand);
            _escalatingButton.onClick.RemoveListener(RequestOpenings);
        }
    }
}
