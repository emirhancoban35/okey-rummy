using System;
using Okey.Core.Stones;
using TMPro;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class StoneVisual : MonoBehaviour
    {
        [SerializeField] private TMP_Text _numberText;
        [SerializeField] private SpriteRenderer _symbolRenderer;
        [SerializeField] private GameObject[] _faceUpVisuals;
        [SerializeField] private StoneVisualSettings _settings;
        [SerializeField] private GameObject[] _faceDownVisuals = Array.Empty<GameObject>();
        [SerializeField] private GameObject _inspectedJokerVisual;
        private bool _isWild;
        private bool _faceUp = true;

        public void ConfigureInspection(bool isWild)
        {
            _isWild = isWild;
            SetFace(_faceUp);
        }

        public void SetVisual(StoneData stone)
        {
            if (stone == null)
                throw new ArgumentNullException(nameof(stone));

            var color = _settings.GetColor(stone.Color);
            _numberText.text = stone.Type == StoneType.FakeJoker
                ? _settings.FakeJokerText
                : stone.Value.ToString();
            _numberText.color = color;
            _symbolRenderer.color = color;
        }

        public void SetFace(bool isFaceUp)
        {
            _faceUp = isFaceUp;
            foreach (var visual in _faceDownVisuals)
                visual.SetActive(!isFaceUp);
            if (_inspectedJokerVisual != null) _inspectedJokerVisual.SetActive(!isFaceUp && _isWild);
            foreach (var visual in _faceUpVisuals)
                visual.SetActive(isFaceUp);
        }
    }
}
