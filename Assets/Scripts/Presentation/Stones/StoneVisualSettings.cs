using System;
using Okey.Core.Stones;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [CreateAssetMenu(menuName = "Okey/Stone Visual Settings")]
    public sealed class StoneVisualSettings : ScriptableObject
    {
        [Serializable]
        private struct ColorEntry
        {
            public StoneColor StoneColor;
            public Color DisplayColor;
        }

        [SerializeField] private ColorEntry[] _colors;
        [SerializeField] private string _fakeJokerText;

        public string FakeJokerText => _fakeJokerText;

        public Color GetColor(StoneColor color)
        {
            foreach (var entry in _colors)
            {
                if (entry.StoneColor == color)
                    return entry.DisplayColor;
            }

            throw new InvalidOperationException($"No display color is configured for {color} in {name}.");
        }
    }
}
