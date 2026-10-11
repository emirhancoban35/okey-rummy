using System.Collections.Generic;
using Okey.Core.Stones;

namespace Okey.Core.Rules
{
    public interface IMeldRules
    {
        StoneData Joker { get; }
        bool IsWild(StoneInstance stone);
        StoneData Resolve(StoneInstance stone);
        bool TryCreate(IEnumerable<StoneInstance> stones, MeldKind kind, out Meld meld);
        bool TryExtend(Meld meld, StoneInstance stone, out Meld extended);
    }
}
