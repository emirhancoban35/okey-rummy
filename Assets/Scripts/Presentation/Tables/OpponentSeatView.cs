using TMPro;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class OpponentSeatView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _identity;
        public void SetIdentity(Color color) => _identity.color = color;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private GameObject _activeHighlight;
        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }
        public void Render(string text, bool active)
        {
            _text.text = text;
            _activeHighlight.SetActive(active);
        }
    }
}
