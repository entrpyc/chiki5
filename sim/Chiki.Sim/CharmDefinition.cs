using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>A Charm's rarity (PRD 4.9): a content-table band, stronger than any Imprint tier (PRD 3.9.7).</summary>
    public enum CharmRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    /// <summary>
    /// When a Charm fires (PRD 3.9.8): a framework trigger event with the condition that must
    /// hold on it. <see cref="PerfectDefense"/> is the chief one: the battle ended with zero
    /// damage taken (PRD 3.3.9.4).
    /// </summary>
    public sealed record CharmTrigger(EffectTrigger Event, EffectCondition Condition)
    {
        public static readonly CharmTrigger PerfectDefense = new CharmTrigger(EffectTrigger.BattleEnded, EffectCondition.IfPerfectDefense);
    }

    /// <summary>The profile fact a Charm's unlock condition reads (PRD 4.9, 3.9.5, 3.9.10).</summary>
    public enum UnlockFact
    {
        /// <summary>Bosses defeated across all runs reached the amount (PRD 3.9.10).</summary>
        BossesDefeated,

        /// <summary>The named milestone was reached (PRD 3.9.5).</summary>
        Milestone,

        /// <summary>The named challenge was completed (PRD 3.9.5).</summary>
        Challenge,

        /// <summary>The named NPC's relationship reached the level (PRD 3.9.9, 3.10.6).</summary>
        RelationshipLevel,
    }

    /// <summary>The profile facts unlock conditions are evaluated against (PRD 3.1.2, 3.9.2).</summary>
    public sealed record ProfileFacts
    {
        public int BossesDefeated { get; init; }

        public IReadOnlyCollection<string> Milestones { get; init; } = Array.Empty<string>();

        public IReadOnlyCollection<string> Challenges { get; init; } = Array.Empty<string>();

        /// <summary>Relationship level by NPC id.</summary>
        public IReadOnlyDictionary<string, int> RelationshipLevels { get; init; } = new Dictionary<string, int>();
    }

    /// <summary>A predicate over profile facts (PRD 4.9): the fact, the amount it must reach and, for named facts, the subject.</summary>
    public sealed record UnlockCondition
    {
        public UnlockFact Fact { get; }

        /// <summary>The count or level to reach; 1 for a named milestone or challenge.</summary>
        public int Amount { get; }

        /// <summary>The milestone, challenge or NPC id; null for <see cref="UnlockFact.BossesDefeated"/>.</summary>
        public string? Subject { get; }

        public UnlockCondition(UnlockFact fact, int amount = 1, string? subject = null)
        {
            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "An unlock amount is at least 1.");
            }

            bool named = fact != UnlockFact.BossesDefeated;
            if (named ? string.IsNullOrWhiteSpace(subject) : subject != null)
            {
                throw new ArgumentException("Exactly a milestone, challenge or relationship unlock names its subject.", nameof(subject));
            }

            Fact = fact;
            Amount = amount;
            Subject = subject;
        }

        public bool IsMet(ProfileFacts facts)
        {
            if (facts is null)
            {
                throw new ArgumentNullException(nameof(facts));
            }

            switch (Fact)
            {
                case UnlockFact.BossesDefeated:
                    return facts.BossesDefeated >= Amount;
                case UnlockFact.Milestone:
                    return Contains(facts.Milestones, Subject!);
                case UnlockFact.Challenge:
                    return Contains(facts.Challenges, Subject!);
                case UnlockFact.RelationshipLevel:
                    return facts.RelationshipLevels.TryGetValue(Subject!, out int level) && level >= Amount;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Fact), Fact, "Unknown unlock fact.");
            }
        }

        private static bool Contains(IReadOnlyCollection<string> ids, string id)
        {
            foreach (var candidate in ids)
            {
                if (string.Equals(candidate, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// A Charm definition (PRD 4.9, 3.9.5–3.9.9): the permanent, chosen, build-defining layer of
    /// progression. It fires its effects the moment its <see cref="Trigger"/> is satisfied
    /// (PRD 3.9.8); every effect entry carries the Charm's trigger and condition. The run
    /// registers equipped Charms with the framework at battle start (P18.4).
    /// </summary>
    public sealed record CharmDefinition
    {
        public string Id { get; }

        public string Name { get; }

        public CharmRarity Rarity { get; }

        public CharmTrigger Trigger { get; }

        public IReadOnlyList<EffectDefinition> Effects { get; }

        public UnlockCondition Unlock { get; }

        /// <summary>True for the three level-10 Charms that alter the ending (PRD 3.9.9).</summary>
        public bool EndingAltering { get; }

        /// <summary>The most the Charm's stat changes may total in one run; null when uncapped. Enforced by the run-level registry (P18.4).</summary>
        public int? RunCap { get; }

        public CharmDefinition(
            string id,
            string name,
            CharmRarity rarity,
            CharmTrigger trigger,
            IReadOnlyList<EffectDefinition> effects,
            UnlockCondition unlock,
            bool endingAltering = false,
            int? runCap = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Charm id is required.", nameof(id));
            }

            if (trigger is null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            if (effects is null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (effects.Count == 0)
            {
                throw new ArgumentException("A Charm has at least one effect.", nameof(effects));
            }

            foreach (var effect in effects)
            {
                if (effect.Trigger != trigger.Event || effect.Condition != trigger.Condition)
                {
                    throw new ArgumentException("Every effect of a Charm fires on the Charm's own trigger and condition.", nameof(effects));
                }
            }

            if (runCap < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(runCap), "A run cap is at least 1.");
            }

            Id = id;
            Name = name ?? id;
            Rarity = rarity;
            Trigger = trigger;
            Effects = effects;
            Unlock = unlock ?? throw new ArgumentNullException(nameof(unlock));
            EndingAltering = endingAltering;
            RunCap = runCap;
        }
    }

    /// <summary>One data set of Charm definitions: one JSON file under data/charms.</summary>
    public sealed record CharmSet
    {
        public string Id { get; }

        public IReadOnlyList<CharmDefinition> Charms { get; }

        public CharmSet(string id, IReadOnlyList<CharmDefinition> charms)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Set id is required.", nameof(id));
            }

            Id = id;
            Charms = charms ?? throw new ArgumentNullException(nameof(charms));
        }
    }
}
