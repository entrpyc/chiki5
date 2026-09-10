using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>
    /// A card definition (PRD 4.4, 3.4.7): name, Category, Rarity, class, CardValue (damage for
    /// attacks, Block for Defense; PRD 3.3.4.3), cooldown in beats (PRD 3.3.5.1), typed effect
    /// entries with their reaction conditions (PRD 3.4.9), special rules as designer text,
    /// upgrade step (PRD 3.4.18), Unstable lifespan (PRD 3.4.16), flavor text (PRD 3.14.6) and
    /// unlock source. A card defines only its CardValue and modifiers, never the formula
    /// (PRD 3.4.10). The constructor checks shape only; <see cref="CardValidator"/> holds the
    /// rules of PRD 3.4 and every shipped set passes it (PRD 3.4.21).
    /// </summary>
    public sealed record CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardCategory Category { get; }
        public CardRarity Rarity { get; }
        public CardClass Class { get; }

        /// <summary>CardValue (PRD 3.3.4.3): damage for an attack card, Block gained for a Defense card.</summary>
        public int Value { get; }

        /// <summary>Beats the slot cools down for after any accepted press of this card, 2–6 (PRD 3.3.5.1, 3.4.7).</summary>
        public int CooldownBeats { get; }

        /// <summary>The card's effects beyond its CardValue, each with its trigger, condition and modifier (PRD 3.4.9).</summary>
        public IReadOnlyList<EffectDefinition> Effects { get; }

        /// <summary>How the value grows with a player stat, Block or buff stacks, or CRP (PRD 3.4.9, 3.8.8); null when it does not.</summary>
        public ValueScaling? Scaling { get; }

        /// <summary>Special rules as the designer wrote them; text for the card face, no runtime meaning.</summary>
        public string? SpecialRules { get; }

        /// <summary>How much an Upgrade improves the value (PRD 3.4.18).</summary>
        public int UpgradeStep { get; }

        /// <summary>Battles an Unstable card lives (PRD 3.4.16); null for the other classes.</summary>
        public int? Lifespan { get; }

        public string? FlavorText { get; }

        public UnlockSource UnlockSource { get; }

        /// <summary>The slots this card may sit in: its Category's two keys on both lines (PRD 3.4.1).</summary>
        public IReadOnlyList<Slot> LegalSlots => CardCategories.SlotsFor(Category);

        public CardDefinition(
            string id,
            string name,
            CardCategory category,
            int value,
            int cooldownBeats = Tuning.CooldownMinBeats,
            CardRarity rarity = CardRarity.Common,
            CardClass cardClass = CardClass.Normal,
            IReadOnlyList<EffectDefinition>? effects = null,
            string? specialRules = null,
            int upgradeStep = 0,
            int? lifespan = null,
            string? flavorText = null,
            UnlockSource unlockSource = UnlockSource.Pool,
            ValueScaling? scaling = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id is required.", nameof(id));
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "CardValue must not be negative.");
            }

            if (cooldownBeats < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownBeats), "Cooldown must not be negative.");
            }

            if (upgradeStep < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeStep), "Upgrade step must not be negative.");
            }

            if (lifespan < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lifespan), "Lifespan must not be negative.");
            }

            Id = id;
            Name = name ?? id;
            Category = category;
            Rarity = rarity;
            Class = cardClass;
            Value = value;
            CooldownBeats = cooldownBeats;
            Effects = effects ?? Array.Empty<EffectDefinition>();
            SpecialRules = specialRules;
            UpgradeStep = upgradeStep;
            Lifespan = lifespan;
            FlavorText = flavorText;
            UnlockSource = unlockSource;
            Scaling = scaling;
        }
    }
}
