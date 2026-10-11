using System;
using Okey.Core.Decks;

namespace Okey.Core.Dealing
{
    public sealed class OpeningDealFactory
    {
        private readonly DeckFactory _deckFactory;
        private readonly IStoneShuffler _shuffler;

        public OpeningDealFactory(DeckFactory deckFactory, IStoneShuffler shuffler)
        {
            _deckFactory = deckFactory ?? throw new ArgumentNullException(nameof(deckFactory));
            _shuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
        }

        public OpeningDeal Create(DeckDefinition definition, int initialHandSize, int seed)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (initialHandSize < 0 || initialHandSize > definition.Count)
                throw new ArgumentOutOfRangeException(nameof(initialHandSize));

            var deck = _deckFactory.Create(definition);
            var drawPile = new DrawPile(_shuffler.Shuffle(deck, seed));
            var initialHand = drawPile.Draw(initialHandSize);
            return new OpeningDeal(seed, initialHand, drawPile);
        }
    }
}
