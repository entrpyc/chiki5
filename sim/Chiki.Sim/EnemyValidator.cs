using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>One broken rule on one enemy: the enemy, the PRD number of the rule and what is wrong.</summary>
    public sealed record EnemyViolation(string EnemyId, string Rule, string Message)
    {
        public override string ToString()
        {
            return $"{EnemyId}: {Message} ({Rule})";
        }
    }

    /// <summary>
    /// The rules every enemy definition must obey, each a build-time failure on the shipped
    /// sets: the intended duration inside its tier's band (PRD 3.3.9.1), exactly one rhythm
    /// profile (PRD 3.6.2), the tier's capability budget (PRD 3.6.4), a chart written for
    /// the enemy's own track (PRD 3.6.28) and damage per hit inside the role's World 1 band
    /// (PRD 3.7.16). Every violation is returned, not just the first.
    /// </summary>
    public static class EnemyValidator
    {
        public const string RuleDuration = "3.3.9.1";
        public const string RuleProfile = "3.6.2";
        public const string RuleBudget = "3.6.4";
        public const string RuleTrack = "3.6.28";
        public const string RuleDamageBand = "3.7.16";
        public const string RuleUniqueId = "4.7";

        /// <summary>Validates a whole set: every enemy's rules, unique ids, and no track shared by two enemies (PRD 3.6.28).</summary>
        public static IReadOnlyList<EnemyViolation> Validate(EnemySet set)
        {
            if (set is null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            var violations = new List<EnemyViolation>();
            var seenIds = new HashSet<string>();
            var trackOwners = new Dictionary<string, string>();
            foreach (var enemy in set.Enemies)
            {
                if (!seenIds.Add(enemy.Id))
                {
                    violations.Add(new EnemyViolation(enemy.Id, RuleUniqueId, "duplicate enemy id"));
                }

                if (trackOwners.TryGetValue(enemy.TrackId, out var owner))
                {
                    violations.Add(new EnemyViolation(enemy.Id, RuleTrack, $"track '{enemy.TrackId}' already belongs to '{owner}'"));
                }
                else
                {
                    trackOwners[enemy.TrackId] = enemy.Id;
                }

                violations.AddRange(Validate(enemy));
            }

            return violations;
        }

        public static IReadOnlyList<EnemyViolation> Validate(EnemyDefinition enemy)
        {
            if (enemy is null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            var violations = new List<EnemyViolation>();
            ValidateDuration(enemy, violations);
            ValidateProfile(enemy, violations);
            ValidateBudget(enemy, violations);
            ValidateTrack(enemy, violations);
            ValidateDamageBand(enemy, violations);
            return violations;
        }

        /// <summary>PRD 3.7.16: the definition's damage per hit is its World 1 base and sits in the role's band for the tier; the battle raises it per World.</summary>
        private static void ValidateDamageBand(EnemyDefinition enemy, List<EnemyViolation> violations)
        {
            var band = Balance.DamageBand(enemy.Role, enemy.Tier);
            if (!band.Contains(enemy.DamagePerHit))
            {
                violations.Add(new EnemyViolation(enemy.Id, RuleDamageBand, $"{enemy.Tier} {enemy.Role} damage per hit {enemy.DamagePerHit} is outside the World 1 band {band}"));
            }
        }

        /// <summary>PRD 3.3.9.1: the intended duration sits in the tier's band.</summary>
        private static void ValidateDuration(EnemyDefinition enemy, List<EnemyViolation> violations)
        {
            var band = EncounterTiers.DurationBand(enemy.Tier);
            if (!band.Contains(enemy.IntendedSeconds))
            {
                violations.Add(new EnemyViolation(enemy.Id, RuleDuration, $"{enemy.Tier} intended duration {enemy.IntendedSeconds} s is outside {band} s"));
            }
        }

        /// <summary>PRD 3.6.2: exactly one rhythm profile.</summary>
        private static void ValidateProfile(EnemyDefinition enemy, List<EnemyViolation> violations)
        {
            if (enemy.Profile is null)
            {
                violations.Add(new EnemyViolation(enemy.Id, RuleProfile, "a rhythm profile (Fast or Slow) is required"));
            }
        }

        /// <summary>PRD 3.6.4: no more abilities, traits and statuses than the tier's budget, and no fewer.</summary>
        private static void ValidateBudget(EnemyDefinition enemy, List<EnemyViolation> violations)
        {
            var budget = EncounterTiers.Budget(enemy.Tier);
            CheckCount(enemy, "abilities", enemy.Abilities.Count, budget.Abilities, violations);
            CheckCount(enemy, "traits", enemy.Traits.Count, budget.Traits, violations);
            CheckCount(enemy, "statuses used", enemy.StatusesUsed.Count, budget.Statuses, violations);
        }

        private static void CheckCount(EnemyDefinition enemy, string what, int count, ValueBand band, List<EnemyViolation> violations)
        {
            if (!band.Contains(count))
            {
                violations.Add(new EnemyViolation(enemy.Id, RuleBudget, $"a {enemy.Tier} enemy carries {band} {what}, not {count}"));
            }
        }

        /// <summary>PRD 3.6.28: the chart is the enemy's own and written for the enemy's track.</summary>
        private static void ValidateTrack(EnemyDefinition enemy, List<EnemyViolation> violations)
        {
            if (enemy.Chart.TrackId != enemy.TrackId)
            {
                violations.Add(new EnemyViolation(enemy.Id, RuleTrack, $"chart '{enemy.ChartId}' is written for track '{enemy.Chart.TrackId}', not the enemy's track '{enemy.TrackId}'"));
            }

            if (enemy.Chart.EnemyId != enemy.Id)
            {
                violations.Add(new EnemyViolation(enemy.Id, RuleTrack, $"chart '{enemy.ChartId}' belongs to enemy '{enemy.Chart.EnemyId}'"));
            }
        }
    }
}
