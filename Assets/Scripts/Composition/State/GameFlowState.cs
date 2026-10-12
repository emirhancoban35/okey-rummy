using Okey.Configuration;
using Okey.Core.Tables;
using Okey.Core.Rooms;
using UnityEngine;

namespace Okey.Composition
{
    public sealed class GameFlowState : MonoBehaviour
    {
        public LocalRoomDirectory Rooms { get; private set; }
        public MatchHistory Matches { get; } = new MatchHistory();
        public GameSetup Setup { get; private set; }
        public StartingSeatSequence StartingSeats { get; private set; }

        public static GameFlowState Resolve(TableSettings defaults)
        {
            var state = Object.FindFirstObjectByType<GameFlowState>();
            if (state != null) return state;
            state = new GameObject(nameof(GameFlowState)).AddComponent<GameFlowState>();
            state.Setup = defaults.CreateSetup();
            state.Rooms = defaults.Rooms.CreateDirectory(state.Setup.PlayerCount);
            state.StartingSeats = new StartingSeatSequence(new SeededRandomSource(System.Guid.NewGuid().GetHashCode()));
            DontDestroyOnLoad(state.gameObject);
            return state;
        }
    }
}
