using System.Collections.Generic;

namespace Okey.Core.Tables
{
    public interface IRoundScoring
    {
        int RemainingValue(TablePlayer player);
        IReadOnlyList<int> Calculate(IReadOnlyList<TablePlayer> players, int winner, bool jokerFinish);
    }
}
