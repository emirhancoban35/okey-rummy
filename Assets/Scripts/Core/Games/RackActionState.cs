using System.Collections.Generic;
using System.Linq;
using Okey.Core.Rules;

namespace Okey.Core.Games
{
    public sealed class RackActionState
    {
        public IReadOnlyList<RackActionPlan> Actions { get; }
        public MeldPlan GroupedTiles { get; }

        public RackActionState(IEnumerable<RackActionPlan> actions, MeldPlan groupedTiles)
        {
            Actions = System.Array.AsReadOnly(actions.ToArray());
            GroupedTiles = groupedTiles;
        }
    }
}
