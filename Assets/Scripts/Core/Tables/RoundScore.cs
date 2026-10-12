using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Rules;
namespace Okey.Core.Tables
{
    public sealed class RoundScore
    {
        public IReadOnlyList<int> Scores { get; }
        public IReadOnlyList<int> Penalties { get; }
        public IReadOnlyList<int> Openings { get; }
        public IReadOnlyList<OpeningMode> OpeningModes { get; }
        public int Winner { get; }
        internal RoundScore(TableSession table)
        {
            Scores = Array.AsReadOnly(table.Players.Select(player => player.Score).ToArray());
            Penalties = Array.AsReadOnly(table.Players.Select(player => player.Penalty).ToArray());
            Openings = Array.AsReadOnly(table.Players.Select(player => player.OpeningValue).ToArray());
            OpeningModes = Array.AsReadOnly(table.Players.Select(player => player.Opening).ToArray());
            Winner = table.Winner;
        }
    }
}
