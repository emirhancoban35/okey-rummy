using System;

namespace Okey.Core.Tables
{
    public sealed class BotPreferences
    {
        public event Action Changed;
        public bool Humanlike { get; private set; }
        public double MistakeChance => Humanlike ? _mistakeChance : 0;
        public double OpeningPatience { get; }
        public double MinimumThinkingSeconds { get; }
        public double MaximumThinkingSeconds { get; }
        public double FastActionSeconds { get; }
        public int LateHandMargin { get; }
        private readonly double _mistakeChance;

        public BotPreferences(bool humanlike, double mistakeChance, double openingPatience,
            double minimumThinkingSeconds, double maximumThinkingSeconds, double fastActionSeconds, int lateHandMargin)
        {
            if (!IsProbability(mistakeChance) || !IsProbability(openingPatience) ||
                !IsDuration(minimumThinkingSeconds) || !IsDuration(maximumThinkingSeconds) ||
                !IsDuration(fastActionSeconds) || maximumThinkingSeconds < minimumThinkingSeconds || lateHandMargin < 0)
                throw new ArgumentOutOfRangeException(nameof(mistakeChance), "Invalid bot preferences.");
            Humanlike = humanlike;
            _mistakeChance = mistakeChance;
            OpeningPatience = openingPatience;
            MinimumThinkingSeconds = minimumThinkingSeconds;
            MaximumThinkingSeconds = maximumThinkingSeconds;
            FastActionSeconds = fastActionSeconds;
            LateHandMargin = lateHandMargin;
        }

        private static bool IsProbability(double value) => value >= 0 && value <= 1;
        private static bool IsDuration(double value) => value >= 0 && !double.IsInfinity(value);

        public void ToggleMode()
        {
            Humanlike = !Humanlike;
            Changed?.Invoke();
        }
    }
}
