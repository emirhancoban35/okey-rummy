using Okey.Configuration;
using Okey.Core.Tables;
using UnityEngine;

namespace Okey.Composition
{
    public sealed class GameFlowState : MonoBehaviour
    {
        public GameSetup Setup { get; private set; }
        public StartingSeatSequence StartingSeats { get; private set; }

        public static GameFlowState Resolve(TableSettings defaults)
        {
            var state = Object.FindFirstObjectByType<GameFlowState>();
            if (state != null) return state;
            state = new GameObject(nameof(GameFlowState)).AddComponent<GameFlowState>();
            state.Setup = defaults.CreateSetup();
            state.StartingSeats = new StartingSeatSequence(new SeededRandomSource(System.Guid.NewGuid().GetHashCode()));
            DontDestroyOnLoad(state.gameObject);
            return state;
        }
    }
}
