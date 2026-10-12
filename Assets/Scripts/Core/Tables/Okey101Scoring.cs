using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class Okey101Scoring : IRoundScoring
    {
        private readonly OkeyRules _rules;
        private readonly IMeldRules _meldRules;
        public Okey101Scoring(OkeyRules rules, IMeldRules meldRules)
        {
            _rules = rules;
            _meldRules = meldRules;
        }

        public int RemainingValue(TablePlayer player) => player.Hand.Slots.Where(stone => stone != null)
            .Sum(stone => _meldRules.IsWild(stone) ? _rules.JokerScore : _meldRules.Resolve(stone).Value) *
            (player.Opening == OpeningMode.Pairs ? _rules.PairMultiplier : 1);

        public IReadOnlyList<int> Calculate(IReadOnlyList<TablePlayer> players, int winner, bool jokerFinish)
        {
            var scores = new int[players.Count];
            var multiplier = winner < 0 ? 1 : (jokerFinish ? _rules.JokerFinishMultiplier : 1) * (players[winner].Opening == OpeningMode.Pairs ? _rules.PairMultiplier : 1);
            for (var seat = 0; seat < players.Count; seat++)
            {
                var player = players[seat];
                var hand = player.Hand.Slots.Where(stone => stone != null).ToArray();
                var score = winner < 0 ? hand.Count(_meldRules.IsWild) * _rules.JokerScore :
                    seat == winner ? _rules.WinnerScore : player.Opening == OpeningMode.Closed ? _rules.UnopenedScore :
                    RemainingValue(player);
                scores[seat] = score * multiplier + player.Penalty;
            }
            return Array.AsReadOnly(scores);
        }
    }
}
