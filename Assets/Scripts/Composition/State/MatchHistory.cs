using System.Collections.Generic;
using Okey.Configuration;
using Okey.Core.Tables;
namespace Okey.Composition
{
    public sealed class MatchHistory
    {
        private readonly Dictionary<(TableRuleProfile Profile, bool Partners), MatchScoreLedger> _matches =
            new Dictionary<(TableRuleProfile, bool), MatchScoreLedger>();
        public MatchScoreLedger For(GameSetup setup)
        {
            var key = (setup.ActiveProfile, setup.Partnerships);
            if (!_matches.TryGetValue(key, out var ledger))
            {
                ledger = new MatchScoreLedger(setup.PlayerCount, setup.MatchRounds);
                _matches.Add(key, ledger);
            }
            return ledger;
        }
        public void Reset(GameSetup setup) => _matches.Remove((setup.ActiveProfile, setup.Partnerships));
    }
}
