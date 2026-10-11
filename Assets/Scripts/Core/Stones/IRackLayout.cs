using System.Collections.Generic;

namespace Okey.Core.Stones
{
    public interface IRackLayout : IReadOnlyRack
    {
        IReadOnlyList<StoneInstance> Move(int stoneId, int destination);
    }
}
