using System.Collections.Generic;

namespace Okey.Core.Stones
{
    public interface IReadOnlyRack
    {
        IReadOnlyList<StoneInstance> Slots { get; }
        int Count { get; }
        int Capacity { get; }
        int FindSlot(int stoneId);
    }
}
