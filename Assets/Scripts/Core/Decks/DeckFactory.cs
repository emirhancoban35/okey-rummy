using System;
using System.Collections.Generic;
using Okey.Core.Stones;

namespace Okey.Core.Decks
{
    public sealed class DeckFactory
    {
        public IReadOnlyList<StoneInstance> Create(DeckDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var stones = new StoneInstance[definition.Count];
            var index = 0;

            foreach (var set in definition.Sets)
            {
                var valueCount = set.MaximumValue - set.MinimumValue + 1;
                for (var offset = 0; offset < valueCount; offset++)
                {
                    var data = new StoneData(set.Type, set.Color, set.MinimumValue + offset);
                    for (var copy = 0; copy < set.CopiesPerValue; copy++)
                    {
                        stones[index] = new StoneInstance(index, data);
                        index++;
                    }
                }
            }

            return Array.AsReadOnly(stones);
        }
    }
}
