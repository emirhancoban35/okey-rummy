using System;
using System.Collections.Generic;
namespace Okey.Core.Tables
{
    public sealed class MatchScoreLedger
    {
        private readonly List<RoundScore> _rounds = new List<RoundScore>();
        private readonly HashSet<TableSession> _recorded = new HashSet<TableSession>();
        private readonly int[] _totals;
        public IReadOnlyList<RoundScore> Rounds { get; }
        public IReadOnlyList<int> Totals { get; }
        public int RoundLimit { get; }
        public bool Complete => _rounds.Count >= RoundLimit;
        public MatchScoreLedger(int playerCount, int roundLimit)
        {
            if (playerCount < 2 || roundLimit < 1) throw new ArgumentOutOfRangeException(nameof(roundLimit));
            _totals = new int[playerCount];
            Totals = Array.AsReadOnly(_totals);
            Rounds = _rounds.AsReadOnly();
            RoundLimit = roundLimit;
        }
        public bool Record(TableSession table)
        {
            if (table.Phase != TurnPhase.Finished || Complete || _recorded.Contains(table)) return false;
            if (table.Players.Count != _totals.Length) throw new ArgumentException("Player count must match the score sheet.", nameof(table));
            var round = new RoundScore(table);
            _recorded.Add(table);
            _rounds.Add(round);
            for (var seat = 0; seat < _totals.Length; seat++) _totals[seat] += round.Scores[seat];
            return true;
        }
        public int TeamTotal(int seat, bool partnerships)
        {
            var total = 0;
            for (var index = 0; index < _totals.Length; index++)
                if (partnerships ? index % 2 == seat % 2 : index == seat) total += _totals[index];
            return total;
        }
    }
}
