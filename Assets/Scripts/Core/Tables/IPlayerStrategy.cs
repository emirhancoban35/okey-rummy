using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public interface IPlayerStrategy
    {
        MeldPlan PlanOpening(PlayerObservation observation, out OpeningMode mode);
        int ChooseDiscard(PlayerObservation observation);
    }
}
