using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>The tier table an Imprint is rolled from (PRD 3.9.3).</summary>
    public enum ImprintTier
    {
        Common,
        Uncommon,
        Rare,
    }

    /// <summary>Where an Imprint definition enters the pool from (PRD 4.10).</summary>
    public enum ImprintSource
    {
        Pool,

        /// <summary>Unlocked by an NPC relationship level (PRD 3.10.6).</summary>
        Relationship,
    }

    /// <summary>
    /// An Imprint definition (PRD 4.10, 3.9.3): the run-scoped, rolled layer of progression. Its
    /// effects are framework entries (P8.1) the run registers when the Imprint is acquired
    /// (P18.3): an <see cref="EffectTrigger.Acquired"/> entry fires once on acquisition, the rest
    /// fire on their battle events. A stackable Imprint acquired twice registers its effects
    /// twice (PRD 3.9.3). Imprints are lost at run end (PRD 3.9.1).
    /// </summary>
    public sealed record ImprintDefinition
    {
        public string Id { get; }

        public string Name { get; }

        public ImprintTier Tier { get; }

        public IReadOnlyList<EffectDefinition> Effects { get; }

        /// <summary>Whether a second copy stacks its effects on the first (PRD 3.9.3).</summary>
        public bool Stackable { get; }

        public ImprintSource Source { get; }

        public ImprintDefinition(
            string id,
            string name,
            ImprintTier tier,
            IReadOnlyList<EffectDefinition> effects,
            bool stackable = false,
            ImprintSource source = ImprintSource.Pool)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Imprint id is required.", nameof(id));
            }

            if (effects is null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (effects.Count == 0)
            {
                throw new ArgumentException("An Imprint has at least one effect.", nameof(effects));
            }

            foreach (var effect in effects)
            {
                if (effect.Trigger == EffectTrigger.OnPlay)
                {
                    throw new ArgumentException("An Imprint is never played; its effects fire on acquisition or on battle events.", nameof(effects));
                }
            }

            Id = id;
            Name = name ?? id;
            Tier = tier;
            Effects = effects;
            Stackable = stackable;
            Source = source;
        }
    }

    /// <summary>One data set of Imprint definitions: one JSON file under data/imprints.</summary>
    public sealed record ImprintSet
    {
        public string Id { get; }

        public IReadOnlyList<ImprintDefinition> Imprints { get; }

        public ImprintSet(string id, IReadOnlyList<ImprintDefinition> imprints)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Set id is required.", nameof(id));
            }

            Id = id;
            Imprints = imprints ?? throw new ArgumentNullException(nameof(imprints));
        }

        /// <summary>The Imprints of one tier, in set order: the pool a roll of that tier draws from (PRD 3.9.3).</summary>
        public IReadOnlyList<ImprintDefinition> OfTier(ImprintTier tier)
        {
            var ofTier = new List<ImprintDefinition>();
            foreach (var imprint in Imprints)
            {
                if (imprint.Tier == tier)
                {
                    ofTier.Add(imprint);
                }
            }

            return ofTier;
        }
    }
}
