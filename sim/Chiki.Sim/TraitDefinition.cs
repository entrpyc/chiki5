using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>Where a Trait definition enters the pool from (PRD 4.11).</summary>
    public enum TraitSource
    {
        Pool,

        /// <summary>Unlocked by an NPC relationship level (PRD 3.10.6).</summary>
        Relationship,
    }

    /// <summary>
    /// A Trait definition (PRD 4.11, 3.4.19): the modifier a card carries for the rest of the run,
    /// at most one per card. Its one effect is a framework entry (P8.1) registered on the card
    /// that holds the Trait, so it fires on a battle event or stands as a passive modifier; it is
    /// never played on its own, so an on-play trigger is refused. Applying Traits at the Forge
    /// (PRD 3.4.19) and Disarmed switching them off (PRD 3.3.7.8) are later plans'.
    /// </summary>
    public sealed record TraitDefinition
    {
        public string Id { get; }

        public string Name { get; }

        public EffectDefinition Effect { get; }

        public TraitSource Source { get; }

        public TraitDefinition(string id, string name, EffectDefinition effect, TraitSource source = TraitSource.Pool)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Trait id is required.", nameof(id));
            }

            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect), "A Trait has exactly one effect.");
            }

            if (effect.Trigger == EffectTrigger.OnPlay)
            {
                throw new ArgumentException("A Trait is never played; its effect fires on a battle event or stands as a passive modifier.", nameof(effect));
            }

            Id = id;
            Name = name ?? id;
            Effect = effect;
            Source = source;
        }
    }

    /// <summary>One data set of Trait definitions: one JSON file under data/traits.</summary>
    public sealed record TraitSet
    {
        public string Id { get; }

        public IReadOnlyList<TraitDefinition> Traits { get; }

        public TraitSet(string id, IReadOnlyList<TraitDefinition> traits)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Set id is required.", nameof(id));
            }

            Traits = traits ?? throw new ArgumentNullException(nameof(traits));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trait in traits)
            {
                if (trait is null)
                {
                    throw new ArgumentException("A Trait must not be null.", nameof(traits));
                }

                if (!ids.Add(trait.Id))
                {
                    throw new ArgumentException($"Trait id '{trait.Id}' appears twice.", nameof(traits));
                }
            }

            Id = id;
        }
    }
}
