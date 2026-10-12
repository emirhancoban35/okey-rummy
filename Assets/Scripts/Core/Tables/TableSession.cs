using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Dealing;
using Okey.Core.Games;
using Okey.Core.Decks;
using Okey.Core.Stones;
using Okey.Core.Rules;

namespace Okey.Core.Tables
{
    public sealed class TableSession
    {
        private readonly DrawPile _pile;
        private readonly IRoundScoring _scoring;
        private readonly List<Meld> _melds = new List<Meld>();
        public ITableGame Game { get; }
        public TableOptions Options { get; }
        public int StartingSeat { get; }
        public int TurnRevision { get; private set; }
        public int OpeningRevision { get; private set; }
        public int TeamOf(int seat) => Options.Partnerships ? seat % 2 : seat;
        public int TeamScore(int seat) => Players.Where((player, index) => TeamOf(index) == TeamOf(seat)).Sum(player => player.Score);
        public int SeriesThreshold { get; private set; }
        public int PairThreshold { get; private set; }
        public OkeyRules Rules { get; }
        public IMeldRules MeldRules { get; }
        public StoneInstance Indicator { get; }
        public IReadOnlyList<TablePlayer> Players { get; }
        public IReadOnlyList<Meld> Melds { get; }
        private readonly List<int> _meldOwners = new List<int>();
        public IReadOnlyList<int> MeldOwners { get; }
        public StoneInstance PendingDiscard { get; private set; }
        public bool PairBoardOpen => Players.Any(player => player.Opening == OpeningMode.Pairs);
        public MeldPlan WinningHand { get; private set; }
        public int ActiveSeat { get; private set; }
        public int Winner { get; private set; } = -1;
        public TurnPhase Phase { get; private set; } = TurnPhase.AwaitingDiscard;
        public int RemainingCount => _pile.RemainingCount;
        public event Action Changed;
        public event Action<TableActionNotice> ActionCompleted;

        public TableSession(OkeyRules rules, IReadOnlyList<StoneInstance> deck, int capacity, ITableGame game, IMeldRules meldRules = null, IRoundScoring scoring = null, int startingSeat = 0, TableOptions options = null)
        {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Options = options ?? new TableOptions();
            SeriesThreshold = rules.SeriesThreshold;
            PairThreshold = rules.PairThreshold;
            if (startingSeat < 0 || startingSeat >= rules.PlayerCount) throw new ArgumentOutOfRangeException(nameof(startingSeat));
            if (Options.Partnerships && rules.PlayerCount != 4) throw new ArgumentException("Partnerships require four seats.", nameof(options));
            StartingSeat = ActiveSeat = startingSeat;
            if (deck == null || capacity <= rules.HandSize || deck.Count <= rules.PlayerCount * rules.HandSize + 1 ||
                deck.Any(stone => stone == null) || deck.Select(stone => stone.Id).Distinct().Count() != deck.Count)
                throw new ArgumentException("The table needs a unique deck, an indicator and space to draw.", nameof(deck));
            var remaining = deck.ToList();
            var indicatorIndex = remaining.FindIndex(stone => stone.Data.Type == StoneType.Regular);
            if (indicatorIndex < 0) throw new ArgumentException("The deck needs a regular indicator.", nameof(deck));
            Indicator = remaining[indicatorIndex];
            remaining.RemoveAt(indicatorIndex);
            MeldRules = meldRules ?? new MeldRules(rules, Indicator);
            if (scoring != null) _scoring = scoring;
            else
            {
                var individualScoring = Game.CreateScoring(rules, MeldRules);
                _scoring = Options.Partnerships ? new PartnershipScoring(individualScoring) : individualScoring;
            }
            _pile = new DrawPile(remaining);
            var players = new TablePlayer[rules.PlayerCount];
            for (var seat = 0; seat < players.Length; seat++)
                players[seat] = new TablePlayer(new RackArrangement(_pile.Draw(rules.HandSize + (seat == startingSeat ? 1 : 0)), capacity));
            Players = Array.AsReadOnly(players);
            Melds = _melds.AsReadOnly();
            MeldOwners = _meldOwners.AsReadOnly();
        }

        private bool CanReceiveTile(int seat) => seat == ActiveSeat && Phase == TurnPhase.AwaitingDraw &&
            Players[seat].Hand.Count < Players[seat].Hand.Capacity;
        public bool CanDraw(int seat) => CanReceiveTile(seat) && RemainingCount > 0;
        public bool CanEndDrawnRound(int seat) => CanReceiveTile(seat) && RemainingCount == 0 && Game.AllowsEmptyStockPlay;

        public bool CanTakeDiscard(int seat) => Options.AllowDiscardPickup && CanReceiveTile(seat) &&
            Players[(seat + Players.Count - 1) % Players.Count].Discards.Count > 0;

        public bool TryTakeDiscard(int seat, int meldIndex = -1)
        {
            if (!CanTakeDiscard(seat)) return false;
            var previous = Players[(seat + Players.Count - 1) % Players.Count];
            var stone = previous.Discards[previous.Discards.Count - 1];
            if (meldIndex >= 0)
            {
                if (!Game.AllowsPublicMelds || Players[seat].Opening == OpeningMode.Closed || meldIndex >= _melds.Count ||
                    !MeldRules.TryExtend(_melds[meldIndex], stone, out var extended)) return false;
                _melds[meldIndex] = extended;
            }
            else
            {
                Players[seat].OwnedHand.Add(stone);
                if (Game.RequiresDiscardUse) PendingDiscard = stone;
            }
            previous.TakeDiscard();
            Phase = TurnPhase.AwaitingDiscard;
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.TakeDiscard, seat));
            return true;
        }

        public bool TryReturnDiscard(int seat)
        {
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || PendingDiscard == null) return false;
            var stone = Players[seat].OwnedHand.Remove(PendingDiscard.Id);
            Players[(seat + Players.Count - 1) % Players.Count].Discard(stone);
            PendingDiscard = null;
            Phase = TurnPhase.AwaitingDraw;
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.ReturnDiscard, seat));
            return true;
        }

        public bool CanReclaimJoker(int seat, int stoneId, int meldIndex, out Meld replaced, out StoneInstance joker)
        {
            replaced = null;
            joker = null;
            if (!Options.AllowJokerReclaim || !Game.AllowsPublicMelds || seat != ActiveSeat ||
                Phase != TurnPhase.AwaitingDiscard || Players[seat].Opening == OpeningMode.Closed ||
                meldIndex < 0 || meldIndex >= _melds.Count || PendingDiscard != null && PendingDiscard.Id != stoneId) return false;
            var slot = Players[seat].Hand.FindSlot(stoneId);
            return slot >= 0 && MeldRules.TryReplaceJoker(_melds[meldIndex], Players[seat].Hand.Slots[slot], out replaced, out joker);
        }

        public bool TryReclaimJoker(int seat, int stoneId, int meldIndex)
        {
            if (!CanReclaimJoker(seat, stoneId, meldIndex, out var replaced, out var joker)) return false;
            var slot = Players[seat].Hand.FindSlot(stoneId);
            Players[seat].OwnedHand.Remove(stoneId);
            Players[seat].OwnedHand.Add(joker);
            Players[seat].Layout.Move(joker.Id, slot);
            _melds[meldIndex] = replaced;
            PendingDiscard = null;
            var owner = _meldOwners[meldIndex];
            Players[owner].Penalty += Rules.JokerReclaimPenalty;
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.ReclaimJoker, seat,
                penalty: Rules.JokerReclaimPenalty, penaltyKind: TablePenaltyKind.JokerReclaimed, penaltySeat: owner));
            return true;
        }

        public bool TryDraw(int seat) => TryDraw(seat, out _);

        public bool TryDraw(int seat, out StoneInstance stone)
        {
            stone = null;
            if (CanEndDrawnRound(seat))
            {
                Phase = TurnPhase.Finished;
                ApplyScores(false);
                Changed?.Invoke();
                return false;
            }
            if (!CanDraw(seat) || !_pile.TryDraw(out stone)) return false;
            Players[seat].OwnedHand.Add(stone);
            Phase = TurnPhase.AwaitingDiscard;
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.Draw, seat));
            return true;
        }

        public bool TryOpen(int seat, OpeningMode mode, IReadOnlyList<Meld> melds)
        {
            if (!Game.AllowsPublicMelds || seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || mode == OpeningMode.Closed || !Enum.IsDefined(typeof(OpeningMode), mode))
                return false;
            var player = Players[seat];
            if (melds == null || melds.Any(meld => meld == null)) return false;
            var validated = new List<Meld>();
            var ids = new HashSet<int>();
            var validTiles = true;
            foreach (var meld in melds)
            {
                var stones = meld.Tiles.Select(tile => player.Hand.FindSlot(tile.Stone.Id))
                    .Select(slot => slot < 0 ? null : player.Hand.Slots[slot]).ToArray();
                if (!MeldRules.TryCreate(stones, meld.Kind, out var canonical))
                {
                    validTiles = false;
                    break;
                }
                validTiles &= canonical.Tiles.All(tile => ids.Add(tile.Stone.Id));
                validated.Add(canonical);
            }
            if (PendingDiscard != null && !ids.Contains(PendingDiscard.Id)) return false;
            var decision = Game.OpeningPolicy.Evaluate(new OpeningRequest(mode, player.Opening, validated,
                player.Hand.Count, validTiles, SeriesThreshold, PairThreshold, Rules, Options, PairBoardOpen));
            if (!validTiles || !decision.Accepted)
            {
                player.Penalty += decision.Penalty;
                Changed?.Invoke();
                ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.Open, seat, penalty: decision.Penalty, penaltyKind: TablePenaltyKind.InvalidOpening));
                return false;
            }
            SeriesThreshold = decision.SeriesThreshold;
            PairThreshold = decision.PairThreshold;
            var tiles = validated.SelectMany(meld => meld.Tiles).ToArray();
            foreach (var tile in tiles) player.OwnedHand.Remove(tile.Stone.Id);
            _melds.AddRange(validated);
            _meldOwners.AddRange(Enumerable.Repeat(seat, validated.Count));
            PendingDiscard = null;
            var initialOpening = player.Opening == OpeningMode.Closed;
            var openingValue = mode == OpeningMode.Pairs ? validated.Count : validated.Sum(meld => meld.Points);
            if (initialOpening) player.OpeningValue = openingValue;
            if (initialOpening) player.Opening = mode;
            OpeningRevision++;
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.Open, seat, openingValue, initialOpening: initialOpening));
            return true;
        }

        public bool CanLayOff(int seat, int stoneId, int meldIndex, out Meld extended)
        {
            extended = null;
            if (!Game.AllowsPublicMelds || seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard ||
                meldIndex < 0 || meldIndex >= _melds.Count || PendingDiscard != null && PendingDiscard.Id != stoneId) return false;
            var player = Players[seat];
            var slot = player.Hand.FindSlot(stoneId);
            return Game.OpeningPolicy.CanLayOff(player.Opening, player.Hand.Count) && slot >= 0 &&
                MeldRules.TryExtend(_melds[meldIndex], player.Hand.Slots[slot], out extended);
        }

        public bool TryLayOff(int seat, int stoneId, int meldIndex)
        {
            if (!CanLayOff(seat, stoneId, meldIndex, out var extended)) return false;
            Players[seat].OwnedHand.Remove(stoneId);
            _melds[meldIndex] = extended;
            PendingDiscard = null;
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.LayOff, seat));
            return true;
        }

        public bool TryFindFinish(int seat, out int discardId)
        {
            discardId = -1;
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard) return false;
            var hand = Players[seat].Hand.Slots.Where(tile => tile != null).ToArray();
            return Game.TryFindFinish(hand, MeldRules, Rules, out discardId);
        }

        private int DiscardPenaltyFor(TablePlayer player, StoneInstance stone)
        {
            if (!Game.PenalizesPlayableDiscards || player.Hand.Count == 0) return 0;
            if (MeldRules.IsWild(stone)) return Rules.DiscardPenalties.Joker;
            return _melds.Any(meld => MeldRules.TryExtend(meld, stone, out _)) ? Rules.DiscardPenalties.Playable : 0;
        }

        public bool TryDiscard(int seat, int stoneId)
        {
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard) return false;
            var player = Players[seat];
            if (PendingDiscard != null || player.Hand.FindSlot(stoneId) < 0) return false;
            var stone = player.OwnedHand.Remove(stoneId);
            player.Discard(stone);
            var discardPenalty = DiscardPenaltyFor(player, stone);
            player.Penalty += discardPenalty;
            if (Game.TryFinish(player.Hand.Slots.Where(tile => tile != null).ToArray(), MeldRules, Rules, out var finish, out var mode))
            {
                WinningHand = finish;
                if (mode != OpeningMode.Closed) player.Opening = mode;
                Winner = seat;
                Phase = TurnPhase.Finished;
            }
            else
            {
                ActiveSeat = (ActiveSeat + 1) % Players.Count;
                TurnRevision++;
                Phase = RemainingCount > 0 || Game.AllowsEmptyStockPlay ? TurnPhase.AwaitingDraw : TurnPhase.Finished;
            }
            if (Phase == TurnPhase.Finished) ApplyScores(Winner >= 0 && MeldRules.IsWild(stone));
            Changed?.Invoke();
            ActionCompleted?.Invoke(new TableActionNotice(PlayerActionKind.Discard, seat, penalty: discardPenalty,
                penaltyKind: discardPenalty == 0 ? TablePenaltyKind.None : MeldRules.IsWild(stone) ? TablePenaltyKind.JokerDiscard : TablePenaltyKind.PlayableDiscard));
            return true;
        }

        public PlayerTurnObservation ObserveTurn(int seat)
        {
            var player = Players[seat];
            var next = Players[(seat + 1) % Players.Count];
            var previous = Players[(seat + Players.Count - 1) % Players.Count];
            var observation = new PlayerObservation(player.Hand.Slots.Where(stone => stone != null), _melds,
                player.Opening, Rules, MeldRules, next.Discards, SeriesThreshold, PairThreshold);
            return new PlayerTurnObservation(observation, Phase, Game, Options,
                CanTakeDiscard(seat) ? previous.Discards[previous.Discards.Count - 1] : null, RemainingCount, Players.Count, PendingDiscard?.Id ?? -1, PairBoardOpen);
        }

        public bool TryApply(int seat, PlayerAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            switch (action.Kind)
            {
                case PlayerActionKind.ReturnDiscard: return TryReturnDiscard(seat);
                case PlayerActionKind.ReclaimJoker: return TryReclaimJoker(seat, action.StoneId, action.MeldIndex);
                case PlayerActionKind.Draw: return TryDraw(seat);
                case PlayerActionKind.TakeDiscard: return TryTakeDiscard(seat, action.MeldIndex);
                case PlayerActionKind.Open: return TryOpen(seat, action.Opening, action.Melds);
                case PlayerActionKind.LayOff: return TryLayOff(seat, action.StoneId, action.MeldIndex);
                case PlayerActionKind.Discard: return TryDiscard(seat, action.StoneId);
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private void ApplyScores(bool jokerFinish)
        {
            var scores = _scoring.Calculate(Players, Winner, jokerFinish);
            for (var index = 0; index < Players.Count; index++) Players[index].Score = scores[index];
        }
    }
}
