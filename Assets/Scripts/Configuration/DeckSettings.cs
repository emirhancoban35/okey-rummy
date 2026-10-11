using System;
using Okey.Core.Decks;
using Okey.Core.Stones;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Deck Settings")]
    public sealed class DeckSettings : ScriptableObject
    {
        [Serializable]
        private struct StoneSetSettings
        {
            public StoneType Type;
            public StoneColor Color;
            [Min(0)] public int MinimumValue;
            [Min(0)] public int MaximumValue;
            [Min(1)] public int CopiesPerValue;

            public StoneSetDefinition CreateDefinition()
            {
                return new StoneSetDefinition(Type, Color, MinimumValue, MaximumValue, CopiesPerValue);
            }
        }

        [SerializeField] private StoneSetSettings[] _sets = Array.Empty<StoneSetSettings>();

        public DeckDefinition CreateDefinition()
        {
            var sets = new StoneSetDefinition[_sets.Length];
            for (var index = 0; index < _sets.Length; index++)
                sets[index] = _sets[index].CreateDefinition();

            return new DeckDefinition(sets);
        }
    }
}
