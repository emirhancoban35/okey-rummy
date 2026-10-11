using System.Collections.Generic;
using System.Linq;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class PlayerAction
    {
        public PlayerActionKind Kind { get; }
        public int StoneId { get; }
        public int MeldIndex { get; }
        public OpeningMode Opening { get; }
        public IReadOnlyList<Meld> Melds { get; }

        private PlayerAction(PlayerActionKind kind, int stoneId = -1, int meldIndex = -1,
            OpeningMode opening = OpeningMode.Closed, IReadOnlyList<Meld> melds = null)
        {
            Kind = kind;
            StoneId = stoneId;
            MeldIndex = meldIndex;
            Opening = opening;
            Melds = System.Array.AsReadOnly((melds ?? System.Array.Empty<Meld>()).ToArray());
        }

        public static PlayerAction Draw() => new PlayerAction(PlayerActionKind.Draw);
        public static PlayerAction TakeDiscard(int meldIndex = -1) => new PlayerAction(PlayerActionKind.TakeDiscard, meldIndex: meldIndex);
        public static PlayerAction Open(OpeningMode mode, IReadOnlyList<Meld> melds) => new PlayerAction(PlayerActionKind.Open, opening: mode, melds: melds);
        public static PlayerAction LayOff(int stoneId, int meldIndex) => new PlayerAction(PlayerActionKind.LayOff, stoneId, meldIndex);
        public static PlayerAction Discard(int stoneId) => new PlayerAction(PlayerActionKind.Discard, stoneId);
    }
}
