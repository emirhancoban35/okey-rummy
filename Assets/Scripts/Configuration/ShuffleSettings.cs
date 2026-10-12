using System;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Shuffle Settings")]
    public sealed class ShuffleSettings : ScriptableObject
    {
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed;


        public int CreateSeed()
        {
            return _useFixedSeed ? _seed : Guid.NewGuid().GetHashCode();
        }
    }
}
