using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>One data set of card definitions (PRD 4.4): one JSON file under data/sets.</summary>
    public sealed record CardSet
    {
        public string Id { get; }

        public IReadOnlyList<CardDefinition> Cards { get; }

        public CardSet(string id, IReadOnlyList<CardDefinition> cards)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Set id is required.", nameof(id));
            }

            Id = id;
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }
    }
}
