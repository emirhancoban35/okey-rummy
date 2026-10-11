using TMPro;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class TableHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _handText;
        [SerializeField] private TMP_Text _pileText;
        [SerializeField] private TMP_Text _penaltyText;
        [SerializeField] private TMP_Text _modeText;
        public TMP_Text StatusText => _statusText;
        public void Render(string status, string hand, string pile, string penalty, bool showPenalty)
        {
            _statusText.text = status;
            _handText.text = hand;
            _pileText.text = pile;
            _penaltyText.text = penalty;
            _penaltyText.gameObject.SetActive(showPenalty);
        }
        public void RenderMode(string text) => _modeText.text = text;
    }
}
