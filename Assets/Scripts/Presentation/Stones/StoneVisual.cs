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
            foreach (var visual in _faceUpVisuals)
                visual.SetActive(isFaceUp);
        }
    }
}
