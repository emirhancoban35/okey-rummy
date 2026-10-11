using System;
using System.Collections.Generic;

namespace Okey.Core.Decks
{
    public sealed class DeckDefinition
    {
        public IReadOnlyList<StoneSetDefinition> Sets { get; }
        public int Count { get; }

        public DeckDefinition(IEnumerable<StoneSetDefinition> sets)
        {
            if (sets == null)
                throw new ArgumentNullException(nameof(sets));

            var entries = new List<StoneSetDefinition>(sets);
            if (entries.Count == 0)
                throw new ArgumentException("A deck must contain at least one stone set.", nameof(sets));

            var count = 0;
            foreach (var entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("A deck cannot contain a null stone set.", nameof(sets));

                count = checked(count + entry.Count);
            }

            Sets = entries.AsReadOnly();
            Count = count;
        }
    }
}
