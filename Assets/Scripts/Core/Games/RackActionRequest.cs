using System.Collections.Generic;
using Okey.Core.Rules;
using Okey.Core.Stones;

namespace Okey.Core.Games
{
    public sealed class RackActionRequest
    {
        public IReadOnlyList<StoneInstance> Slots { get; }
        public int RowCapacity { get; }
        public IMeldRules MeldRules { get; }
        public OkeyRules Rules { get; }
        public OpeningMode Opening { get; }
        public bool PairBoardOpen { get; }
        public int SeriesThreshold { get; }
        public int PairThreshold { get; }

        public RackActionRequest(IReadOnlyList<StoneInstance> slots, int rowCapacity, IMeldRules meldRules,
            OkeyRules rules, OpeningMode opening, int seriesThreshold, int pairThreshold, bool pairBoardOpen = false)
        {
            PairBoardOpen = pairBoardOpen;
            Slots = slots;
            RowCapacity = rowCapacity;
            MeldRules = meldRules;
            Rules = rules;
            Opening = opening;
            SeriesThreshold = seriesThreshold;
            PairThreshold = pairThreshold;
        }
    }
}
