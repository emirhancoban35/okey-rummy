using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Games;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class BotDecisionPolicy : IBotDecisionPolicy
    {
        private readonly IPlayerStrategy _strategy;
        private readonly IRandomSource _random;

        public BotDecisionPolicy(IPlayerStrategy strategy, IRandomSource random)
        {
            _strategy = strategy;
            _random = random;
        }

        public bool ShouldHoldOpening(BotPreferences preferences) =>
            preferences.Humanlike && _random.NextDouble() < preferences.OpeningPatience;

        public PlayerAction Decide(PlayerTurnObservation turn, BotPreferences preferences, bool holdOpening)
        {
            var player = turn.Player;
            if (turn.Phase == TurnPhase.AwaitingDraw) return ChooseDraw(turn);
            var plan = _strategy.PlanOpening(player, out var mode);
            var opening = turn.Game.OpeningPolicy.Evaluate(new OpeningRequest(mode, player.Opening, plan.Melds,
                player.Hand.Count, true, player.SeriesThreshold, player.PairThreshold, player.Rules, turn.Options));
            var shouldOpen = player.Opening != OpeningMode.Closed || !holdOpening ||
                plan.TileCount >= player.Hand.Count - preferences.LateHandMargin || turn.RemainingCount <= turn.PlayerCount;
            if (opening.Accepted && shouldOpen) return PlayerAction.Open(mode, plan.Melds);
            if (turn.Game.TryFindFinish(player.Hand, player.MeldRules, player.Rules, out var finish))
                return PlayerAction.Discard(finish);
            if (_random.NextDouble() < preferences.MistakeChance)
                return PlayerAction.Discard(player.Hand[_random.Next(player.Hand.Count)].Id);
            if (turn.Game.OpeningPolicy.CanLayOff(player.Opening, player.Hand.Count))
                foreach (var stone in player.Hand)
                    for (var index = 0; index < player.Melds.Count; index++)
                        if (player.MeldRules.TryExtend(player.Melds[index], stone, out _))
                            return PlayerAction.LayOff(stone.Id, index);
            return PlayerAction.Discard(_strategy.ChooseDiscard(player));
        }

        private PlayerAction ChooseDraw(PlayerTurnObservation turn)
        {
            if (turn.PreviousDiscard == null) return PlayerAction.Draw();
            var player = turn.Player;
            if (turn.Game.RequiresOpenDiscardPickup)
            {
                for (var index = 0; index < player.Melds.Count; index++)
                    if (player.MeldRules.TryExtend(player.Melds[index], turn.PreviousDiscard, out _))
                        return PlayerAction.TakeDiscard(index);
            }
            else
            {
                var extended = new PlayerObservation(player.Hand.Append(turn.PreviousDiscard), player.Melds,
                    player.Opening, player.Rules, player.MeldRules, player.NextPlayerDiscards,
                    player.SeriesThreshold, player.PairThreshold);
                var before = _strategy.PlanOpening(player, out _);
                var after = _strategy.PlanOpening(extended, out _);
                if (after.TileCount > before.TileCount && after.Melds.SelectMany(meld => meld.Tiles)
                    .Any(tile => tile.Stone.Id == turn.PreviousDiscard.Id)) return PlayerAction.TakeDiscard();
            }
            return PlayerAction.Draw();
        }
    }
}
