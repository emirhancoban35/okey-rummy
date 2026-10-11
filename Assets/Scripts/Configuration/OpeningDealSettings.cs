using System;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Opening Deal Settings")]
    public sealed class OpeningDealSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int _initialHandSize;
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed;

        public int InitialHandSize => _initialHandSize;

        public int CreateSeed()
        {
            return _useFixedSeed ? _seed : Guid.NewGuid().GetHashCode();
        }
    }
}
