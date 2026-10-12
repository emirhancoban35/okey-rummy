using System.Collections.Generic;

namespace Okey.Core.Stones
{
    public interface IRackLayout : IReadOnlyRack
    {
        IReadOnlyList<StoneInstance> Move(int stoneId, int destination);
        IReadOnlyList<StoneInstance> MoveGroup(IReadOnlyList<int> stoneIds, int destination, int rowCapacity);
    }
}
