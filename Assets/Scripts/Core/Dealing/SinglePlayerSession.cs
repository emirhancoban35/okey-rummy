using System;
using System.Collections.Generic;
using Okey.Core.Decks;
using Okey.Core.Stones;

namespace Okey.Core.Dealing
{
    public sealed class SinglePlayerSession
    {
        private readonly DrawPile _drawPile;
        private readonly List<StoneInstance> _discards = new List<StoneInstance>();

        public RackArrangement Hand { get; }
        public TurnPhase Phase { get; private set; }
        public IReadOnlyList<StoneInstance> Discards { get; }
        public int RemainingCount => _drawPile.RemainingCount;
        public bool CanDraw => Phase == TurnPhase.AwaitingDraw && Hand.Count < Hand.Capacity && RemainingCount > 0;

        public SinglePlayerSession(RackArrangement hand, DrawPile drawPile, TurnPhase initialPhase)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            _drawPile = drawPile ?? throw new ArgumentNullException(nameof(drawPile));
            if (initialPhase != TurnPhase.AwaitingDraw && initialPhase != TurnPhase.AwaitingDiscard)
                throw new ArgumentOutOfRangeException(nameof(initialPhase));
            if (initialPhase == TurnPhase.AwaitingDiscard && hand.Count == 0)
                throw new ArgumentException("A discard phase requires a stone in hand.", nameof(hand));
            foreach (var stone in hand.Slots)
                if (stone != null && drawPile.Contains(stone.Id))
                    throw new ArgumentException("Hand and draw pile must have distinct physical IDs.", nameof(drawPile));

            Phase = initialPhase == TurnPhase.AwaitingDraw && RemainingCount == 0 ? TurnPhase.Finished : initialPhase;
            Discards = _discards.AsReadOnly();
        }

        public bool TryDraw(out StoneInstance stone)
        {
            stone = null;
            if (!CanDraw)
                return false;

            _drawPile.TryDraw(out stone);
            Hand.Add(stone);
            Phase = TurnPhase.AwaitingDiscard;
            return true;
        }

        public bool TryDiscard(int stoneId, out StoneInstance stone)
        {
            stone = null;
            if (Phase != TurnPhase.AwaitingDiscard || Hand.FindSlot(stoneId) < 0)
                return false;

            stone = Hand.Remove(stoneId);
            _discards.Add(stone);
            Phase = RemainingCount == 0 ? TurnPhase.Finished : TurnPhase.AwaitingDraw;
            return true;
        }
    }
}
