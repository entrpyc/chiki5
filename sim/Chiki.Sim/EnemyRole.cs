using System;

namespace Chiki.Sim
{
    /// <summary>The one role every enemy has (PRD 3.6.1); it fixes the HP multiplier.</summary>
    public enum EnemyRole
    {
        /// <summary>High damage, 0.8x HP, weak to disruption.</summary>
        Aggressor,

        /// <summary>1.2x HP, low damage, Block and stances.</summary>
        Tank,

        /// <summary>1.0x HP; statuses, control, deceptive actions, damage over time.</summary>
        Mentalist,
    }

    public static class EnemyRoles
    {
        /// <summary>The multiplier a role applies to the formula HP, in thousandths (PRD 3.6.1, 3.7.15).</summary>
        public static int HpMultiplierThousandths(this EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Aggressor: return Tuning.AggressorHpMultiplierThousandths;
                case EnemyRole.Tank: return Tuning.TankHpMultiplierThousandths;
                case EnemyRole.Mentalist: return Tuning.MentalistHpMultiplierThousandths;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.");
            }
        }
    }

    /// <summary>The one rhythm profile every enemy has (PRD 3.6.2).</summary>
    public enum RhythmProfile
    {
        /// <summary>Frequent weak attacks.</summary>
        Fast,

        /// <summary>Rare heavy attacks.</summary>
        Slow,
    }
}
