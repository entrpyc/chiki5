using System;

namespace Chiki.Sim
{
    /// <summary>
    /// A card definition (PRD 4.4). Phases 3 and 4 carry the fields resolution needs: id, name,
    /// Category, CardValue (damage for attacks, Block for Defense) and the cooldown in beats. A
    /// card defines only its CardValue and modifiers, never the formula (PRD 3.4.10); P7.1 adds
    /// rarity, class, effects and the rest of the anatomy (PRD 3.4.7).
    /// </summary>
    public sealed record CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardCategory Category { get; }

        /// <summary>CardValue (PRD 3.3.4.3): damage for an attack card, Block gained for a Defense card.</summary>
        public int Value { get; }

        /// <summary>
        /// Beats the slot cools down for after any accepted press of this card, 2–6 (PRD 3.3.5.1, 3.4.7).
        /// Until P7.1 loads it from data, callers that omit it get the minimum.
        /// </summary>
        public int CooldownBeats { get; }

        public CardDefinition(string id, string name, CardCategory category, int value, int cooldownBeats = Tuning.CooldownMinBeats)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id is required.", nameof(id));
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "CardValue must not be negative.");
            }

            if (cooldownBeats < Tuning.CooldownMinBeats || cooldownBeats > Tuning.CooldownMaxBeats)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cooldownBeats),
                    $"Cooldown must be {Tuning.CooldownMinBeats}–{Tuning.CooldownMaxBeats} beats.");
            }

            Id = id;
            Name = name ?? id;
            Category = category;
            Value = value;
            CooldownBeats = cooldownBeats;
        }
    }
}
