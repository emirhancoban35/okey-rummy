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
        public MeldPlan WinningHand { get; private set; }
        public int ActiveSeat { get; private set; }
        public int Winner { get; private set; } = -1;
        public TurnPhase Phase { get; private set; } = TurnPhase.AwaitingDiscard;
        public int RemainingCount => _pile.RemainingCount;
        public event Action Changed;

        public TableSession(OkeyRules rules, IReadOnlyList<StoneInstance> deck, int capacity, IMeldRules meldRules = null, IRoundScoring scoring = null, int startingSeat = 0, TableOptions options = null, ITableGame game = null)
        {
            Game = game ?? new Okey101Game();
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
            var individualScoring = Game.CreateScoring(rules, MeldRules);
            _scoring = scoring ?? (Options.Partnerships ? new PartnershipScoring(individualScoring) : individualScoring);
            _pile = new DrawPile(remaining);
            var players = new TablePlayer[rules.PlayerCount];
            for (var seat = 0; seat < players.Length; seat++)
                players[seat] = new TablePlayer(new RackArrangement(_pile.Draw(rules.HandSize + (seat == startingSeat ? 1 : 0)), capacity));
            Players = Array.AsReadOnly(players);
            Melds = _melds.AsReadOnly();
        }

        private bool CanReceiveTile(int seat) => seat == ActiveSeat && Phase == TurnPhase.AwaitingDraw &&
            Players[seat].Hand.Count < Players[seat].Hand.Capacity;
        public bool CanDraw(int seat) => CanReceiveTile(seat) && RemainingCount > 0;
        public bool CanEndDrawnRound(int seat) => CanReceiveTile(seat) && RemainingCount == 0 && Game.AllowsEmptyStockPlay;

        public bool CanTakeDiscard(int seat) => Options.AllowDiscardPickup && CanReceiveTile(seat) &&
            (!Game.RequiresOpenDiscardPickup || Players[seat].Opening != OpeningMode.Closed) && Players[(seat + Players.Count - 1) % Players.Count].Discards.Count > 0;

        public bool TryTakeDiscard(int seat, int meldIndex = -1)
        {
            if (!CanTakeDiscard(seat)) return false;
            var previous = Players[(seat + Players.Count - 1) % Players.Count];
            var stone = previous.Discards[previous.Discards.Count - 1];
            if (Game.RequiresOpenDiscardPickup)
            {
                if (meldIndex < 0 || meldIndex >= _melds.Count || !MeldRules.TryExtend(_melds[meldIndex], stone, out var extended)) return false;
                _melds[meldIndex] = extended;
            }
            else Players[seat].Hand.Add(stone);
            previous.TakeDiscard();
            Phase = TurnPhase.AwaitingDiscard;
            Changed?.Invoke();
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
            Players[seat].Hand.Add(stone);
            Phase = TurnPhase.AwaitingDiscard;
            Changed?.Invoke();
            return true;
        }

        public bool TryOpen(int seat, OpeningMode mode, IReadOnlyList<Meld> melds)
        {
            if (!Game.AllowsPublicMelds || seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || mode == OpeningMode.Closed || !Enum.IsDefined(typeof(OpeningMode), mode))
                return false;
            var player = Players[seat];
            if (melds == null || melds.Any(meld => meld == null)) return false;
            var validated = new List<Meld>();
            foreach (var meld in melds)
            {
                var stones = meld.Tiles.Select(tile => player.Hand.FindSlot(tile.Stone.Id))
                    .Select(slot => slot < 0 ? null : player.Hand.Slots[slot]).ToArray();
                if (!MeldRules.TryCreate(stones, meld.Kind, out var canonical))
                {
                    player.Penalty += Rules.InvalidOpeningPenalty;
                    Changed?.Invoke();
                    return false;
                }
                validated.Add(canonical);
            }
            var tiles = validated.SelectMany(meld => meld.Tiles).ToArray();
            var ids = new HashSet<int>();
            var valid = melds.Count > 0 && tiles.Length < player.Hand.Count &&
                tiles.All(tile => player.Hand.FindSlot(tile.Stone.Id) >= 0 && ids.Add(tile.Stone.Id)) &&
                melds.All(meld => mode == OpeningMode.Pairs ? meld.Kind == MeldKind.Pair : meld.Kind != MeldKind.Pair) &&
                (player.Opening == OpeningMode.Closed || player.Opening == mode);
            if (player.Opening == OpeningMode.Closed)
                valid &= mode == OpeningMode.Pairs ? melds.Count >= PairThreshold : validated.Sum(meld => meld.Points) >= SeriesThreshold;
            if (!valid)
            {
                player.Penalty += Rules.InvalidOpeningPenalty;
                Changed?.Invoke();
                return false;
            }
            if (Options.EscalatingOpenings && player.Opening == OpeningMode.Closed)
            {
                if (mode == OpeningMode.Pairs) PairThreshold = validated.Count + Options.OpeningIncrement;
                else SeriesThreshold = validated.Sum(meld => meld.Points) + Options.OpeningIncrement;
            }
            foreach (var tile in tiles) player.Hand.Remove(tile.Stone.Id);
            _melds.AddRange(validated);
            player.Opening = mode;
            OpeningRevision++;
            Changed?.Invoke();
            return true;
        }

        public bool TryLayOff(int seat, int stoneId, int meldIndex)
        {
            if (!Game.AllowsPublicMelds || seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || meldIndex < 0 || meldIndex >= _melds.Count)
                return false;
            var player = Players[seat];
            var slot = player.Hand.FindSlot(stoneId);
            if (player.Opening == OpeningMode.Closed || player.Hand.Count <= 1 || slot < 0 ||
                !MeldRules.TryExtend(_melds[meldIndex], player.Hand.Slots[slot], out var extended)) return false;
            player.Hand.Remove(stoneId);
            _melds[meldIndex] = extended;
            Changed?.Invoke();
            return true;
        }

        public bool TryFindFinish(int seat, out int discardId)
        {
            discardId = -1;
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || Game.AllowsPublicMelds) return false;
            var hand = Players[seat].Hand.Slots.Where(tile => tile != null).ToArray();
            return Game.TryFindFinish(hand, MeldRules, Rules, out discardId);
        }

        public bool TryDiscard(int seat, int stoneId)
        {
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard) return false;
            var player = Players[seat];
            if (player.Hand.FindSlot(stoneId) < 0) return false;
            var stone = player.Hand.Remove(stoneId);
            player.Discard(stone);
            if (Game.PenalizesPlayableDiscards && player.Hand.Count > 0 && (MeldRules.IsWild(stone) || _melds.Any(meld => MeldRules.TryExtend(meld, stone, out _))))
                player.Penalty += Rules.DiscardPenalty;
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
            return true;
        }

        private void ApplyScores(bool jokerFinish)
        {
            var scores = _scoring.Calculate(Players, Winner, jokerFinish);
            for (var index = 0; index < Players.Count; index++) Players[index].Score = scores[index];
        }
    }
}
