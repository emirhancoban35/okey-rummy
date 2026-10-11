using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Configuration
{
    public abstract class TableRuleProfile : ScriptableObject
    {
        [SerializeField] private string _label;
        [SerializeField, Min(1)] private int _handSize;
        [SerializeField, Min(1)] private int _minimumValue = 1;
        [SerializeField, Min(1)] private int _maximumValue = 13;
        [SerializeField, Min(1)] private int _seriesThreshold;
        [SerializeField, Min(1)] private int _pairThreshold;
        [SerializeField, Min(0)] private int _invalidOpeningPenalty;
        [SerializeField, Min(0)] private int _unopenedScore;
        [SerializeField, Min(0)] private int _jokerScore;
        [SerializeField, Min(0)] private int _discardPenalty;
        [SerializeField] private int _winnerScore;
        [SerializeField, Min(1)] private int _pairMultiplier = 2;
        [SerializeField, Min(1)] private int _jokerFinishMultiplier = 2;
        public string Label => _label;
        public abstract bool AllowsPublicMelds { get; }
        protected abstract bool AllowsHighOne { get; }
        public abstract ITableGame CreateGame(MeldPlanner planner);
        public abstract IPlayerStrategy CreateStrategy(MeldPlanner planner);
        public virtual IBotDecisionPolicy CreateBotDecisions(IPlayerStrategy strategy, IRandomSource random) =>
            new BotDecisionPolicy(strategy, random);
        public OkeyRules CreateRules(int playerCount) => new OkeyRules(playerCount, _handSize, _minimumValue, _maximumValue,
            _seriesThreshold, _pairThreshold, _invalidOpeningPenalty, _unopenedScore, _jokerScore, _winnerScore,
            _discardPenalty, _pairMultiplier, _jokerFinishMultiplier, AllowsHighOne);
    }
}
