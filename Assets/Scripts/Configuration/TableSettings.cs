using Okey.Core.Rules;
using Okey.Core.Tables;
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
        [SerializeField] private bool _partnerships;
        [SerializeField] private bool _escalatingOpenings;
        [SerializeField, Min(1)] private int _openingIncrement = 1;
        public bool EscalatingOpenings => _escalatingOpenings;
        public void ToggleEscalatingOpenings() => _escalatingOpenings = !_escalatingOpenings;
        [SerializeField] private bool _randomStartingSeat = true;
        [SerializeField] private bool _allowDiscardPickup = true;
        [SerializeField, Min(5)] private float _turnSeconds = 45;
        [SerializeField] private bool _humanlikeBots = true;
        [SerializeField, Range(0, 1)] private float _botMistakeChance = .01f;
        [SerializeField, Range(0, 1)] private float _openingPatience = .35f;
        [SerializeField] private Vector2 _thinkingSeconds = new Vector2(1.2f, 3.8f);
        private int _lastStartingSeat = -1;
        public int NextStartingSeat()
        {
            if (!_randomStartingSeat) return 0;
            _lastStartingSeat = _lastStartingSeat < 0 ? Random.Range(0, _playerCount) : (_lastStartingSeat + 1) % _playerCount;
            return _lastStartingSeat;
        }
        public bool Partnerships => _partnerships;
        public void ToggleBotMode() => _humanlikeBots = !_humanlikeBots;
        public void TogglePartnerships() { if (_playerCount == 4) _partnerships = !_partnerships; }
        public bool HumanlikeBots => _humanlikeBots;
        public float MistakeChance => _humanlikeBots ? _botMistakeChance : 0;
        public float OpeningPatience => _openingPatience;
        public Vector2 ThinkingSeconds => _thinkingSeconds;
        public bool RandomStartingSeat => _randomStartingSeat;
        public TableOptions CreateOptions() => new TableOptions(_partnerships, _allowDiscardPickup, _turnSeconds, _escalatingOpenings, _openingIncrement);
        public float BotActionDelay => _botActionDelay;
        public OkeyRules CreateRules() => new OkeyRules(_playerCount, _handSize, _minimumValue, _maximumValue,
            _seriesThreshold, _pairThreshold, _invalidOpeningPenalty, _unopenedScore, _jokerScore, _winnerScore, _discardPenalty, _pairMultiplier, _jokerFinishMultiplier);
    }
}
