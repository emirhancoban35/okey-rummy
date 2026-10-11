using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Rules/Classic Okey")]
    public sealed class ClassicOkeyRuleProfile : TableRuleProfile
    {
        public override bool AllowsPublicMelds => false;
        protected override bool AllowsHighOne => true;
        public override ITableGame CreateGame(MeldPlanner planner) => new ClassicOkeyGame(planner);
        public override IPlayerStrategy CreateStrategy(MeldPlanner planner) => new ClassicOkeyStrategy(planner);
    }
}
