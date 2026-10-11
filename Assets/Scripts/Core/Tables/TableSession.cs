using System;
using System.Collections.Generic;
using System.Linq;
using Okey.Core.Dealing;
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
        public OkeyRules Rules { get; }
        public IMeldRules MeldRules { get; }
        public StoneInstance Indicator { get; }
        public IReadOnlyList<TablePlayer> Players { get; }
        public IReadOnlyList<Meld> Melds { get; }
        public int ActiveSeat { get; private set; }
        public int Winner { get; private set; } = -1;
        public TurnPhase Phase { get; private set; } = TurnPhase.AwaitingDiscard;
        public int RemainingCount => _pile.RemainingCount;
        public event Action Changed;

        public TableSession(OkeyRules rules, IReadOnlyList<StoneInstance> deck, int capacity, IMeldRules meldRules = null, IRoundScoring scoring = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (deck == null || capacity <= rules.HandSize || deck.Count <= rules.PlayerCount * rules.HandSize + 1 ||
                deck.Any(stone => stone == null) || deck.Select(stone => stone.Id).Distinct().Count() != deck.Count)
                throw new ArgumentException("The table needs a unique deck, an indicator and space to draw.", nameof(deck));
            var remaining = deck.ToList();
            var indicatorIndex = remaining.FindIndex(stone => stone.Data.Type == StoneType.Regular);
            if (indicatorIndex < 0) throw new ArgumentException("The deck needs a regular indicator.", nameof(deck));
            Indicator = remaining[indicatorIndex];
            remaining.RemoveAt(indicatorIndex);
            MeldRules = meldRules ?? new MeldRules(rules, Indicator);
            _scoring = scoring ?? new Okey101Scoring(rules, MeldRules);
            _pile = new DrawPile(remaining);
            var players = new TablePlayer[rules.PlayerCount];
            for (var seat = 0; seat < players.Length; seat++)
                players[seat] = new TablePlayer(new RackArrangement(_pile.Draw(rules.HandSize + (seat == 0 ? 1 : 0)), capacity));
            Players = Array.AsReadOnly(players);
            Melds = _melds.AsReadOnly();
        }

        public bool CanDraw(int seat) => seat == ActiveSeat && Phase == TurnPhase.AwaitingDraw &&
            RemainingCount > 0 && Players[seat].Hand.Count < Players[seat].Hand.Capacity;

        public bool TryDraw(int seat) => TryDraw(seat, out _);

        public bool TryDraw(int seat, out StoneInstance stone)
        {
            stone = null;
            if (!CanDraw(seat) || !_pile.TryDraw(out stone)) return false;
            Players[seat].Hand.Add(stone);
            Phase = TurnPhase.AwaitingDiscard;
            Changed?.Invoke();
            return true;
        }

        public bool TryOpen(int seat, OpeningMode mode, IReadOnlyList<Meld> melds)
        {
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || mode == OpeningMode.Closed || !Enum.IsDefined(typeof(OpeningMode), mode))
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
                valid &= mode == OpeningMode.Pairs ? melds.Count >= Rules.PairThreshold : validated.Sum(meld => meld.Points) >= Rules.SeriesThreshold;
            if (!valid)
            {
                player.Penalty += Rules.InvalidOpeningPenalty;
                Changed?.Invoke();
                return false;
            }
            foreach (var tile in tiles) player.Hand.Remove(tile.Stone.Id);
            _melds.AddRange(validated);
            player.Opening = mode;
            Changed?.Invoke();
            return true;
        }

        public bool TryLayOff(int seat, int stoneId, int meldIndex)
        {
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard || meldIndex < 0 || meldIndex >= _melds.Count)
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

        public bool TryDiscard(int seat, int stoneId)
        {
            if (seat != ActiveSeat || Phase != TurnPhase.AwaitingDiscard) return false;
            var player = Players[seat];
            if (player.Hand.FindSlot(stoneId) < 0) return false;
            var stone = player.Hand.Remove(stoneId);
            player.Discard(stone);
            if (player.Hand.Count > 0 && (MeldRules.IsWild(stone) || _melds.Any(meld => MeldRules.TryExtend(meld, stone, out _))))
                player.Penalty += Rules.DiscardPenalty;
            if (player.Hand.Count == 0)
            {
                Winner = seat;
                Phase = TurnPhase.Finished;
            }
            else
            {
                ActiveSeat = (ActiveSeat + 1) % Players.Count;
                Phase = RemainingCount > 0 ? TurnPhase.AwaitingDraw : TurnPhase.Finished;
            }
            if (Phase == TurnPhase.Finished)
            {
                var scores = _scoring.Calculate(Players, Winner, Winner >= 0 && MeldRules.IsWild(stone));
                for (var index = 0; index < Players.Count; index++) Players[index].Score = scores[index];
            }
            Changed?.Invoke();
            return true;
        }
    }
}
