using System.Linq;
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
                player.Hand.Count, true, player.SeriesThreshold, player.PairThreshold, player.Rules, turn.Options, turn.PairBoardOpen));
            var shouldOpen = player.Opening != OpeningMode.Closed || !holdOpening ||
                plan.TileCount >= player.Hand.Count - preferences.LateHandMargin || turn.RemainingCount <= turn.PlayerCount;
            var usesBorrowed = turn.PendingDiscardId < 0 || plan.Melds.SelectMany(meld => meld.Tiles)
                .Any(tile => tile.Stone.Id == turn.PendingDiscardId);
            if (opening.Accepted && usesBorrowed && (shouldOpen || turn.PendingDiscardId >= 0)) return PlayerAction.Open(mode, plan.Melds);
            if (turn.Options.AllowJokerReclaim && player.Opening != OpeningMode.Closed)
                foreach (var stone in player.Hand.Where(stone => turn.PendingDiscardId < 0 || stone.Id == turn.PendingDiscardId))
                    for (var index = 0; index < player.Melds.Count; index++)
                        if (player.MeldRules.TryReplaceJoker(player.Melds[index], stone, out _, out _))
                            return PlayerAction.ReclaimJoker(stone.Id, index);
            if (turn.PendingDiscardId >= 0)
            {
                for (var index = 0; index < player.Melds.Count; index++)
                    if (player.Opening != OpeningMode.Closed && player.MeldRules.TryExtend(player.Melds[index],
                        player.Hand.Single(stone => stone.Id == turn.PendingDiscardId), out _))
                        return PlayerAction.LayOff(turn.PendingDiscardId, index);
                return PlayerAction.ReturnDiscard();
            }
            if (turn.Game.TryFindFinish(player.Hand, player.MeldRules, player.Rules, out var finish))
                return PlayerAction.Discard(finish);
            if (_random.NextDouble() < preferences.MistakeChance)
                return PlayerAction.Discard(player.Hand[_random.Next(player.Hand.Count)].Id);
            if (turn.Game.OpeningPolicy.CanLayOff(player.Opening, player.Hand.Count))
                foreach (var stone in player.Hand.Where(stone => !player.MeldRules.IsWild(stone) || player.Hand.Count == 2)
                    .OrderByDescending(stone => player.MeldRules.Resolve(stone).Value))
                    for (var index = 0; index < player.Melds.Count; index++)
                        if (player.MeldRules.TryExtend(player.Melds[index], stone, out _))
                            return PlayerAction.LayOff(stone.Id, index);
            return PlayerAction.Discard(_strategy.ChooseDiscard(player));
        }

        private PlayerAction ChooseDraw(PlayerTurnObservation turn)
        {
            if (turn.PreviousDiscard == null) return PlayerAction.Draw();
            var player = turn.Player;
            var extended = new PlayerObservation(player.Hand.Append(turn.PreviousDiscard), player.Melds,
                player.Opening, player.Rules, player.MeldRules, player.NextPlayerDiscards,
                player.SeriesThreshold, player.PairThreshold);
            var before = _strategy.PlanOpening(player, out _);
            var after = _strategy.PlanOpening(extended, out var mode);
            var usesDiscard = after.Melds.SelectMany(meld => meld.Tiles).Any(tile => tile.Stone.Id == turn.PreviousDiscard.Id);
            if (turn.Game.RequiresDiscardUse)
            {
                if (player.Opening != OpeningMode.Closed)
                    for (var index = 0; index < player.Melds.Count; index++)
                    {
                        if (player.MeldRules.TryExtend(player.Melds[index], turn.PreviousDiscard, out _))
                            return PlayerAction.TakeDiscard(index);
                        if (turn.Options.AllowJokerReclaim && player.MeldRules.TryReplaceJoker(player.Melds[index], turn.PreviousDiscard, out _, out _))
                            return PlayerAction.TakeDiscard();
                    }
                var opening = turn.Game.OpeningPolicy.Evaluate(new OpeningRequest(mode, player.Opening, after.Melds,
                    extended.Hand.Count, true, player.SeriesThreshold, player.PairThreshold, player.Rules, turn.Options, turn.PairBoardOpen));
                if (usesDiscard && opening.Accepted) return PlayerAction.TakeDiscard();
            }
            else if (usesDiscard && after.TileCount > before.TileCount) return PlayerAction.TakeDiscard();
            return PlayerAction.Draw();
        }
    }
}
