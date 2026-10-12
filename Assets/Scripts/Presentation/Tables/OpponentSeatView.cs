using TMPro;
using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class OpponentSeatView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private SpriteRenderer _highlight;
        [SerializeField] private Color _activeColor;
        [SerializeField] private Color _idleColor;
        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }
        public void Render(string text, bool active)
        {
            _text.text = text;
            _highlight.color = active ? _activeColor : _idleColor;
        }
    }
}
