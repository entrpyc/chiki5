using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>
    /// An enemy definition (PRD 4.7): name, tier, role, rhythm profile, its own track and chart
    /// (PRD 3.6.28), damage per hit, abilities, traits, statuses used, portrait and quote line,
    /// and for a Boss its phases (PRD 3.6.30; a list of ids only in this plan). HP is not a
    /// field: it is derived at battle start (PRD 3.7.15, 3.6.29). The rules of PRD 3.6 are
    /// <see cref="EnemyValidator"/>'s; construction checks shape only.
    /// </summary>
    public sealed record EnemyDefinition
    {
        public string Id { get; }
        public string Name { get; }

        public EncounterTier Tier { get; }

        public EnemyRole Role { get; }

        /// <summary>The rhythm profile (PRD 3.6.2); null when the definition names none, which the validator rejects.</summary>
        public RhythmProfile? Profile { get; }

        /// <summary>The intended battle duration in seconds, inside the tier's band (PRD 3.3.9.1); feeds the HP formula (PRD 3.7.15).</summary>
        public int IntendedSeconds { get; }

        /// <summary>The id of the enemy's own track (PRD 3.6.28); the chart must be written for it.</summary>
        public string TrackId { get; }

        /// <summary>The enemy's one chart, on the enemy's own track (PRD 3.6.28, 4.16).</summary>
        public Chart Chart { get; }

        public string ChartId => Chart.Id;

        /// <summary>The track the chart was written for; the battle's beat map comes from it (PRD 3.6.28).</summary>
        public Track Track => Chart.Track;

        /// <summary>Damage per hit (PRD 4.7, 3.7.16).</summary>
        public int DamagePerHit { get; }

        public IReadOnlyList<EnemyAbility> Abilities { get; }

        public IReadOnlyList<EnemyTrait> Traits { get; }

        /// <summary>The status effects this enemy uses (PRD 3.6.4).</summary>
        public IReadOnlyList<StatusKind> StatusesUsed { get; }

        /// <summary>Visual lookup id of the portrait (PRD 3.6.26); definitions never reference art directly.</summary>
        public string? PortraitId { get; }

        /// <summary>The quote line on the enemy card (PRD 3.6.26).</summary>
        public string? QuoteLine { get; }

        /// <summary>Phase ids of a Boss (PRD 3.6.30); empty for the other tiers.</summary>
        public IReadOnlyList<string> Phases { get; }

        public EnemyDefinition(
            string id,
            string name,
            EncounterTier tier,
            EnemyRole role,
            RhythmProfile? profile,
            int intendedSeconds,
            Chart chart,
            int damagePerHit,
            IReadOnlyList<EnemyAbility>? abilities = null,
            IReadOnlyList<EnemyTrait>? traits = null,
            IReadOnlyList<StatusKind>? statusesUsed = null,
            string? portraitId = null,
            string? quoteLine = null,
            IReadOnlyList<string>? phases = null,
            string? trackId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Enemy id is required.", nameof(id));
            }

            Chart = chart ?? throw new ArgumentNullException(nameof(chart));
            if (damagePerHit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePerHit), "Damage per hit must not be negative.");
            }

            if (intendedSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intendedSeconds), "Intended duration must not be negative.");
            }

            Id = id;
            Name = name ?? id;
            Tier = tier;
            Role = role;
            Profile = profile;
            IntendedSeconds = intendedSeconds;
            TrackId = string.IsNullOrWhiteSpace(trackId) ? chart.TrackId : trackId!;
            DamagePerHit = damagePerHit;
            Abilities = abilities ?? Array.Empty<EnemyAbility>();
            Traits = traits ?? Array.Empty<EnemyTrait>();
            StatusesUsed = statusesUsed ?? Array.Empty<StatusKind>();
            PortraitId = portraitId;
            QuoteLine = quoteLine;
            Phases = phases ?? Array.Empty<string>();
        }
    }

    /// <summary>A file of enemy definitions (PRD 4.7), one per enemy.</summary>
    public sealed record EnemySet
    {
        public string Id { get; }

        public IReadOnlyList<EnemyDefinition> Enemies { get; }

        public EnemySet(string id, IReadOnlyList<EnemyDefinition> enemies)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Set id is required.", nameof(id));
            }

            Id = id;
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        }
    }
}
