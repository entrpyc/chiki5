using System;

namespace Chiki.Sim
{
    /// <summary>
    /// A card definition (PRD 4.4). Phase 3 carries the fields resolution needs: id, name, Category
    /// and CardValue (damage for attacks, Block for Defense). A card defines only its CardValue and
    /// modifiers, never the formula (PRD 3.4.10); P7.1 adds rarity, class, cooldown, effects and
    /// the rest of the anatomy (PRD 3.4.7).
    /// </summary>
    public sealed record CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardCategory Category { get; }

        /// <summary>CardValue (PRD 3.3.4.3): damage for an attack card, Block gained for a Defense card.</summary>
        public int Value { get; }

        public CardDefinition(string id, string name, CardCategory category, int value)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id is required.", nameof(id));
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "CardValue must not be negative.");
            }

            Id = id;
            Name = name ?? id;
            Category = category;
            Value = value;
        }
    }
}
