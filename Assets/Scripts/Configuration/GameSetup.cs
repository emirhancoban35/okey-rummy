using System;
using Okey.Core.Rules;
using Okey.Core.Rooms;
using Okey.Core.Tables;

namespace Okey.Configuration
{
    public sealed class GameSetup
    {
        private readonly TableRuleProfile[] _profiles;
        private readonly double _mistakeChance;
        private readonly double _openingPatience;
        private readonly double _thinkingMinimum;
        private readonly double _thinkingMaximum;
        private readonly double _fastDelay;
        private readonly int _lateHandMargin;
        private readonly bool _allowDiscardPickup;
        private readonly double _turnSeconds;
        private readonly int _openingIncrement;
        public event Action Changed;
        public bool AllowJokerReclaim { get; private set; }
        public int MatchRounds { get; private set; }
        public int PlayerCount { get; }
        public int RuleIndex { get; private set; }
        public TableRuleProfile ActiveProfile => _profiles[RuleIndex];
        public bool Partnerships { get; private set; }
        public bool HumanlikeBots { get; private set; }
        public bool EscalatingOpenings { get; private set; }
        public bool RandomStartingSeat { get; }

        public GameSetup(TableSettings defaults)
        {
            _profiles = defaults.RuleProfiles;
            if (_profiles.Length == 0 || Array.Exists(_profiles, profile => profile == null))
                throw new ArgumentException("At least one assigned rule profile is required.", nameof(defaults));
            RuleIndex = defaults.RuleIndex;
            if (RuleIndex < 0 || RuleIndex >= _profiles.Length)
                throw new ArgumentOutOfRangeException(nameof(defaults), "The selected rule profile does not exist.");
            PlayerCount = defaults.PlayerCount;
            MatchRounds = defaults.MatchRounds;
            AllowJokerReclaim = defaults.AllowJokerReclaim;
            Partnerships = defaults.Partnerships;
            HumanlikeBots = defaults.HumanlikeBots;
            EscalatingOpenings = defaults.EscalatingOpenings;
            RandomStartingSeat = defaults.RandomStartingSeat;
            _mistakeChance = defaults.MistakeChance;
            _openingPatience = defaults.OpeningPatience;
            _thinkingMinimum = defaults.ThinkingSeconds.x;
            _thinkingMaximum = defaults.ThinkingSeconds.y;
            _fastDelay = defaults.BotActionDelay;
            _lateHandMargin = defaults.BotLateHandMargin;
            _allowDiscardPickup = defaults.AllowDiscardPickup;
            _turnSeconds = defaults.TurnSeconds;
            _openingIncrement = defaults.OpeningIncrement;
        }

        public void SetHands(int hands)
        {
            if (hands < 1) throw new ArgumentOutOfRangeException(nameof(hands));
            MatchRounds = hands;
            Changed?.Invoke();
        }

        public RoomOptions CreateRoomOptions() => new RoomOptions(RuleIndex, MatchRounds, PlayerCount, Partnerships,
            HumanlikeBots, EscalatingOpenings, AllowJokerReclaim);
        public void ApplyRoom(RoomOptions options)
        {
            if (options.RuleIndex >= _profiles.Length || options.Seats != PlayerCount)
                throw new ArgumentException("The room is not compatible with this setup.", nameof(options));
            RuleIndex = options.RuleIndex;
            MatchRounds = options.Hands;
            Partnerships = options.Partnerships;
            HumanlikeBots = options.Humanlike;
            EscalatingOpenings = options.Rising;
            AllowJokerReclaim = options.Reclaim;
            Changed?.Invoke();
        }

        public void ToggleJokerReclaim()
        {
            AllowJokerReclaim = !AllowJokerReclaim;
            Changed?.Invoke();
        }

        public void NextRules()
        {
            RuleIndex = (RuleIndex + 1) % _profiles.Length;
            Changed?.Invoke();
        }

        public void TogglePartnerships()
        {
            if (PlayerCount != 4) return;
            Partnerships = !Partnerships;
            Changed?.Invoke();
        }

        public void ToggleBotMode() => SetBotMode(!HumanlikeBots);
        public void SetBotMode(bool humanlike)
        {
            HumanlikeBots = humanlike;
            Changed?.Invoke();
        }

        public void ToggleEscalatingOpenings()
        {
            EscalatingOpenings = !EscalatingOpenings;
            Changed?.Invoke();
        }

        public OkeyRules CreateRules() => ActiveProfile.CreateRules(PlayerCount);
        public TableOptions CreateOptions() => new TableOptions(Partnerships, _allowDiscardPickup, _turnSeconds,
            EscalatingOpenings && ActiveProfile.AllowsPublicMelds, _openingIncrement,
            AllowJokerReclaim && ActiveProfile.AllowsPublicMelds);
        public BotPreferences CreateBotPreferences() => new BotPreferences(HumanlikeBots, _mistakeChance,
            _openingPatience, _thinkingMinimum, _thinkingMaximum, _fastDelay, _lateHandMargin);
    }
}
