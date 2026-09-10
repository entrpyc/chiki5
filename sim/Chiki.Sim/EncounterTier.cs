using System;

namespace Chiki.Sim
{
    /// <summary>Encounter tier of a track and of the enemy fought on it (PRD 3.3.9.1, 4.14).</summary>
    public enum EncounterTier
    {
        Normal,
        Elite,
        Boss,
    }

    /// <summary>
    /// What a tier may carry (PRD 3.6.4): inclusive counts of abilities, traits and status
    /// effects used.
    /// </summary>
    public sealed record TierBudget(ValueBand Abilities, ValueBand Traits, ValueBand Statuses);

    public static class EncounterTiers
    {
        /// <summary>The intended battle duration band of a tier in seconds, inclusive (PRD 3.3.9.1).</summary>
        public static ValueBand DurationBand(EncounterTier tier)
        {
            switch (tier)
            {
                case EncounterTier.Normal: return new ValueBand(Tuning.NormalMinSeconds, Tuning.NormalMaxSeconds);
                case EncounterTier.Elite: return new ValueBand(Tuning.EliteMinSeconds, Tuning.EliteMaxSeconds);
                case EncounterTier.Boss: return new ValueBand(Tuning.BossMinSeconds, Tuning.BossMaxSeconds);
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown tier.");
            }
        }

        /// <summary>The capability budget of a tier (PRD 3.6.4).</summary>
        public static TierBudget Budget(EncounterTier tier)
        {
            switch (tier)
            {
                case EncounterTier.Normal:
                    return new TierBudget(
                        new ValueBand(Tuning.NormalAbilitiesMin, Tuning.NormalAbilitiesMax),
                        new ValueBand(Tuning.NormalTraitsMin, Tuning.NormalTraitsMax),
                        new ValueBand(Tuning.NormalStatusesMin, Tuning.NormalStatusesMax));
                case EncounterTier.Elite:
                    return new TierBudget(
                        new ValueBand(Tuning.EliteAbilitiesMin, Tuning.EliteAbilitiesMax),
                        new ValueBand(Tuning.EliteTraitsMin, Tuning.EliteTraitsMax),
                        new ValueBand(Tuning.EliteStatusesMin, Tuning.EliteStatusesMax));
                case EncounterTier.Boss:
                    return new TierBudget(
                        new ValueBand(Tuning.BossAbilitiesMin, Tuning.BossAbilitiesMax),
                        new ValueBand(Tuning.BossTraitsMin, Tuning.BossTraitsMax),
                        new ValueBand(Tuning.BossStatusesMin, Tuning.BossStatusesMax));
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown tier.");
            }
        }
    }
}
