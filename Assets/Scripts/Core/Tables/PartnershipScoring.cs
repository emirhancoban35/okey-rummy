using System;
using System.Collections.Generic;
using System.Linq;
namespace Okey.Core.Tables
{
    public sealed class PartnershipScoring : IRoundScoring
    {
        private readonly IRoundScoring _individual;
        public PartnershipScoring(IRoundScoring individual) => _individual = individual ?? throw new ArgumentNullException(nameof(individual));
        public IReadOnlyList<int> Calculate(IReadOnlyList<TablePlayer> players, int winner, bool jokerFinish)
        {
            if (players.Count != 4) throw new ArgumentException("Partnerships require four seats.", nameof(players));
            var scores = _individual.Calculate(players, winner, jokerFinish).ToArray();
            if (winner >= 0)
            {
                var partner = (winner + 2) % players.Count;
                scores[partner] = players[partner].Penalty;
            }
            return Array.AsReadOnly(scores);
        }
    }
}
