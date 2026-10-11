using Okey.Core.Rules;
using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Table Settings")]
    public sealed class TableSettings : ScriptableObject
    {
        [SerializeField, Range(2, 4)] private int _playerCount = 4;
        [SerializeField] private TableRuleProfile[] _ruleProfiles;
        [SerializeField] private int _ruleIndex;
        public TableRuleProfile ActiveProfile => _ruleProfiles[_ruleIndex];
        public int RuleIndex => _ruleIndex;
        public void NextRules() => _ruleIndex = (_ruleIndex + 1) % _ruleProfiles.Length;
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
        public TableOptions CreateOptions() => new TableOptions(_partnerships, _allowDiscardPickup, _turnSeconds, _escalatingOpenings && ActiveProfile.AllowsPublicMelds, _openingIncrement);
        public float BotActionDelay => _botActionDelay;
        public OkeyRules CreateRules() => ActiveProfile.CreateRules(_playerCount);
    }
}
