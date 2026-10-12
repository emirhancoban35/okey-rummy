using System.Linq;
namespace Okey.Core.Tables
{
    public sealed class TimeoutDiscardPolicy
    {
        public int ChooseDiscard(PlayerObservation observation) => observation.Hand
            .OrderBy(stone => observation.MeldRules.IsWild(stone) ? 2 :
                observation.Melds.Any(meld => observation.MeldRules.TryExtend(meld, stone, out _)) ? 1 : 0)
            .ThenBy(stone => observation.MeldRules.Resolve(stone).Value)
            .ThenBy(stone => stone.Id).First().Id;
    }
}
