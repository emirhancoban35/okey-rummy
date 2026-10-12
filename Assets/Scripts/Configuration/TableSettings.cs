using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Table Settings")]
    public sealed class TableSettings : ScriptableObject
    {
        [SerializeField, Range(2, 4)] private int _playerCount = 4;
        [SerializeField] private TableRuleProfile[] _ruleProfiles;
        [SerializeField] private int _ruleIndex;
        [SerializeField] private bool _partnerships;
        [SerializeField] private bool _escalatingOpenings;
        [SerializeField, Min(1)] private int _openingIncrement = 1;
        [SerializeField] private bool _randomStartingSeat = true;
        [SerializeField] private bool _allowDiscardPickup = true;
        [SerializeField, Min(5)] private float _turnSeconds = 45;
        [SerializeField] private bool _humanlikeBots = true;
        [SerializeField, Range(0, 1)] private float _botMistakeChance = .01f;
        [SerializeField, Range(0, 1)] private float _openingPatience = .35f;
        [SerializeField] private Vector2 _thinkingSeconds = new Vector2(1.2f, 3.8f);
        [SerializeField, Min(0.1f)] private float _botActionDelay = 0.8f;
        [SerializeField, Min(0)] private int _botLateHandMargin = 2;

        [SerializeField, Min(1)] private int _matchRounds = 5;
        [SerializeField] private bool _allowJokerReclaim = true;
        public bool AllowJokerReclaim => _allowJokerReclaim;
        public int MatchRounds => _matchRounds;
        public int PlayerCount => _playerCount;
        public TableRuleProfile[] RuleProfiles => (TableRuleProfile[])_ruleProfiles.Clone();
        public int RuleIndex => _ruleIndex;
        public bool Partnerships => _partnerships;
        public bool EscalatingOpenings => _escalatingOpenings;
        public int OpeningIncrement => _openingIncrement;
        public bool RandomStartingSeat => _randomStartingSeat;
        public bool AllowDiscardPickup => _allowDiscardPickup;
        public double TurnSeconds => _turnSeconds;
        public bool HumanlikeBots => _humanlikeBots;
        public float MistakeChance => _botMistakeChance;
        public float OpeningPatience => _openingPatience;
        public Vector2 ThinkingSeconds => _thinkingSeconds;
        public float BotActionDelay => _botActionDelay;
        public int BotLateHandMargin => _botLateHandMargin;

        public GameSetup CreateSetup() => new GameSetup(this);
    }
}
