using Okey.Core.Rules;

namespace Okey.Core.Games
{
    public sealed class RackActionPlan
    {
        public RackActionKind Kind { get; }
        public OpeningMode Mode { get; }
        public MeldPlan Plan { get; }
        public int Progress { get; }
        public int Required { get; }
        public bool CanExecute { get; }
        public bool AdditionalGroups { get; }

        public RackActionPlan(RackActionKind kind, OpeningMode mode, MeldPlan melds, int progress,
            int required, bool canExecute, bool additionalGroups = false)
        {
            Kind = kind;
            Mode = mode;
            Plan = melds;
            Progress = progress;
            Required = required;
            CanExecute = canExecute;
            AdditionalGroups = additionalGroups;
        }
    }
}
