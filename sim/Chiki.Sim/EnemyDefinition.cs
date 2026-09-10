using System;

namespace Chiki.Sim
{
    /// <summary>
    /// An enemy definition (PRD 4.7). Phase 2 carries the fields a battle needs to run its chart;
    /// P9.1 adds tier, role, rhythm profile, abilities, traits, statuses, portrait and quote.
    /// </summary>
    public sealed record EnemyDefinition
    {
        public string Id { get; }
        public string Name { get; }

        /// <summary>The enemy's one chart, on the enemy's own track (PRD 3.6.28).</summary>
        public Chart Chart { get; }

        public Track Track => Chart.Track;

        /// <summary>Damage per hit (PRD 4.7, 3.7.16).</summary>
        public int DamagePerHit { get; }

        public EnemyDefinition(string id, string name, Chart chart, int damagePerHit)
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

            Id = id;
            Name = name ?? id;
            DamagePerHit = damagePerHit;
        }
    }
}
