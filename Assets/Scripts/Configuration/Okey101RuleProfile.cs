using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Rules/101 Okey")]
    public sealed class Okey101RuleProfile : TableRuleProfile
    {
        public override bool AllowsPublicMelds => true;
        protected override bool AllowsHighOne => false;
        public override ITableGame CreateGame(MeldPlanner planner) => new Okey101Game();
        public override IPlayerStrategy CreateStrategy(MeldPlanner planner) => new MeldPlayerStrategy(planner, DiscardWeights);
    }
}
