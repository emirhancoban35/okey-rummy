using System;
using System.Collections.Generic;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class ClassicOkeyScoring : IRoundScoring
    {
        private readonly OkeyRules _rules;
        public ClassicOkeyScoring(OkeyRules rules) => _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        public int RemainingValue(TablePlayer player) => 0;
        public IReadOnlyList<int> Calculate(IReadOnlyList<TablePlayer> players, int winner, bool jokerFinish)
        {
            var scores = new int[players.Count];
            if (winner < 0) return Array.AsReadOnly(scores);
            var multiplier = (jokerFinish ? _rules.JokerFinishMultiplier : 1) *
                (players[winner].Opening == OpeningMode.Pairs ? _rules.PairMultiplier : 1);
            for (var seat = 0; seat < players.Count; seat++)
                scores[seat] = seat == winner ? _rules.WinnerScore : _rules.UnopenedScore * multiplier;
            return Array.AsReadOnly(scores);
        }
    }
}
