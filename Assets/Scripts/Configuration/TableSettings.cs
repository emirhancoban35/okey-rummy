using Okey.Core.Rules;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Table Settings")]
    public sealed class TableSettings : ScriptableObject
    {
        [SerializeField, Range(2, 4)] private int _playerCount = 4;
        [SerializeField, Min(1)] private int _handSize = 21;
        [SerializeField, Min(1)] private int _minimumValue = 1;
        [SerializeField, Min(1)] private int _maximumValue = 13;
        [SerializeField, Min(1)] private int _seriesThreshold = 101;
        [SerializeField, Min(1)] private int _pairThreshold = 5;
        [SerializeField, Min(0)] private int _invalidOpeningPenalty = 101;
        [SerializeField, Min(0)] private int _unopenedScore = 202;
        [SerializeField, Min(0)] private int _jokerScore = 101;
        [SerializeField, Min(0)] private int _discardPenalty = 101;
        [SerializeField] private int _winnerScore = -101;
        [SerializeField, Min(1)] private int _pairMultiplier = 2;
        [SerializeField, Min(1)] private int _jokerFinishMultiplier = 2;
        [SerializeField, Min(0.1f)] private float _botActionDelay = 0.8f;
        public float BotActionDelay => _botActionDelay;
        public OkeyRules CreateRules() => new OkeyRules(_playerCount, _handSize, _minimumValue, _maximumValue,
            _seriesThreshold, _pairThreshold, _invalidOpeningPenalty, _unopenedScore, _jokerScore, _winnerScore, _discardPenalty, _pairMultiplier, _jokerFinishMultiplier);
    }
}
