using Okey.Core.Dealing;
using Okey.Core.Games;
using Okey.Core.Stones;

namespace Okey.Core.Tables
{
    public sealed class PlayerTurnObservation
    {
        public PlayerObservation Player { get; }
        public TurnPhase Phase { get; }
        public ITableGame Game { get; }
        public TableOptions Options { get; }
        public StoneInstance PreviousDiscard { get; }
        public int RemainingCount { get; }
        public int PlayerCount { get; }
        public int PendingDiscardId { get; }
        public bool PairBoardOpen { get; }

        public PlayerTurnObservation(PlayerObservation player, TurnPhase phase, ITableGame game,
            TableOptions options, StoneInstance previousDiscard, int remainingCount, int playerCount, int pendingDiscardId = -1, bool pairBoardOpen = false)
        {
            PendingDiscardId = pendingDiscardId;
            PairBoardOpen = pairBoardOpen;
            Player = player;
            Phase = phase;
            Game = game;
            Options = options;
            PreviousDiscard = previousDiscard;
            RemainingCount = remainingCount;
            PlayerCount = playerCount;
        }
    }
}
