using Okey.Configuration;
using Okey.Core.Rooms;
using Okey.Core.Tables;

namespace Okey.Composition
{
    public sealed class GameFlowState
    {
        public LocalRoomDirectory Rooms { get; }
        public MatchHistory Matches { get; } = new MatchHistory();
        public GameSetup Setup { get; }
        public StartingSeatSequence StartingSeats { get; }

        public GameFlowState(TableSettings defaults, ShuffleSettings shuffle)
        {
            Setup = defaults.CreateSetup();
            Rooms = defaults.Rooms.CreateDirectory(Setup.PlayerCount);
            StartingSeats = new StartingSeatSequence(new SeededRandomSource(shuffle.CreateSeed()));
        }
    }
}
