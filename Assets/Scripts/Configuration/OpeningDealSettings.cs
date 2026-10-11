using System;
using Okey.Core.Dealing;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Opening Deal Settings")]
    public sealed class OpeningDealSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int _initialHandSize;
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed;
        [SerializeField] private TurnPhase _initialPhase;

        public int InitialHandSize => _initialHandSize;
        public TurnPhase InitialPhase => _initialPhase;

        public int CreateSeed()
        {
            return _useFixedSeed ? _seed : Guid.NewGuid().GetHashCode();
        }
    }
}
