using TMPro;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class TableHudView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image _identity;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _handText;
        [SerializeField] private TMP_Text _pileText;
        [SerializeField] private TMP_Text _penaltyText;
        [SerializeField] private string _yourTurn = "YOUR TURN";
        [SerializeField] private string _discardTrial = "USE BORROWED TILE";
        [SerializeField] private string _emptyStock = "Take discard or end.";
        [SerializeField] private string _classicDiscard = "Arrange, then discard.";
        [SerializeField] private string _drawnRound = "Stock empty. Round drawn.";
        [SerializeField] private string _winnerFormat = "{0} wins";
        private string _handFormat;
        private string _pileFormat;
        private string _penaltyFormat;
        public TMP_Text StatusText => _statusText;

        private void Awake()
        {
            _handFormat = _handText.text;
            _pileFormat = _pileText.text;
            _penaltyFormat = _penaltyText.text;
        }

        public void SetIdentity(Color color) => _identity.color = color;
        public void ShowHandValue(bool visible) => _handText.gameObject.SetActive(visible);
        public void ShowTurn() => _statusText.text = _yourTurn;
        public void ShowBorrowedTile() => _statusText.text = _discardTrial;
        public void ShowEmptyStock() => _statusText.text = _emptyStock;
        public void ShowClassicDiscard() => _statusText.text = _classicDiscard;
        public void ShowDrawnRound() => _statusText.text = _drawnRound;
        public void ShowWinner(string name) => _statusText.text = string.Format(_winnerFormat, name);
        public void ShowWaiting(string name) => _statusText.text = name;

        public void RenderValues(int hand, int pile, int penalty)
        {
            _handText.text = string.Format(_handFormat, hand);
            _pileText.text = string.Format(_pileFormat, pile);
            _penaltyText.text = string.Format(_penaltyFormat, penalty);
            _penaltyText.gameObject.SetActive(penalty != 0);
        }
    }
}
