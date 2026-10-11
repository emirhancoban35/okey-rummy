using System.Collections.Generic;
using Okey.Core.Stones;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class PlayerObservation
    {
        public IReadOnlyList<StoneInstance> Hand { get; }
        public IReadOnlyList<Meld> Melds { get; }
        public IReadOnlyList<StoneInstance> NextPlayerDiscards { get; }
        public OpeningMode Opening { get; }
        public OkeyRules Rules { get; }
        public IMeldRules MeldRules { get; }
        public PlayerObservation(IEnumerable<StoneInstance> hand, IEnumerable<Meld> melds,
            OpeningMode opening, OkeyRules rules, IMeldRules meldRules, IEnumerable<StoneInstance> nextPlayerDiscards = null)
        {
            Hand = new List<StoneInstance>(hand).AsReadOnly();
            Melds = new List<Meld>(melds).AsReadOnly();
            NextPlayerDiscards = new List<StoneInstance>(nextPlayerDiscards ?? System.Array.Empty<StoneInstance>()).AsReadOnly();
            Opening = opening;
            Rules = rules;
            MeldRules = meldRules;
        }
    }

    public interface IPlayerStrategy
    {
        MeldPlan PlanOpening(PlayerObservation observation, out OpeningMode mode);
        int ChooseDiscard(PlayerObservation observation);
    }
}
