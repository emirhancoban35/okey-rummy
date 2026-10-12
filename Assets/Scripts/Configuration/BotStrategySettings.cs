using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Bot Strategy")]
    public sealed class BotStrategySettings : ScriptableObject
    {
        [SerializeField, Min(0)] private float _adjacent = 4;
        [SerializeField, Min(0)] private float _gap = 1;
        [SerializeField, Min(0)] private float _set = 2;
        [SerializeField, Min(0)] private float _duplicate = 1;
        [SerializeField, Min(0)] private float _lowTile = .2f;

        public DiscardWeights CreateWeights() => new DiscardWeights(_adjacent, _gap, _set, _duplicate, _lowTile);
    }
}
