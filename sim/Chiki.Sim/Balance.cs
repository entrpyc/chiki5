using System;

namespace Chiki.Sim
{
    /// <summary>
    /// The expected share of player answers by grade (PRD 3.7.15): Perfect and Good in
    /// thousandths, the remainder Misses, which contribute nothing.
    /// </summary>
    public sealed record JudgmentMix
    {
        public int PerfectThousandths { get; }

        public int GoodThousandths { get; }

        public JudgmentMix(int perfectThousandths, int goodThousandths)
        {
            if (perfectThousandths < 0 || goodThousandths < 0 || perfectThousandths + goodThousandths > Fixed.One)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectThousandths), "A judgment mix is non-negative shares summing to at most 1000 thousandths.");
            }

            PerfectThousandths = perfectThousandths;
            GoodThousandths = goodThousandths;
        }

        /// <summary>Every answer Perfect, the worked check's mix (PRD 3.7.15).</summary>
        public static readonly JudgmentMix AllPerfect = new JudgmentMix(Fixed.One, 0);
    }

    /// <summary>
    /// The inputs that turn an enemy definition into its battle numbers (PRD 3.6.29): the World
    /// fought in, which scales damage (PRD 3.7.16) and sets AvgCardDMG (PRD 3.2.17), and the
    /// AttackRatio and judgment mix of the HP formula (PRD 3.7.15).
    /// </summary>
    public sealed record EncounterBalance
    {
        public int World { get; }

        public int AttackRatioThousandths { get; }

        public int AvgCardDmg { get; }

        public JudgmentMix JudgmentMix { get; }

        public EncounterBalance(int world, int attackRatioThousandths, int avgCardDmg, JudgmentMix judgmentMix)
        {
            if (world < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(world), "Worlds count from 1.");
            }

            if (attackRatioThousandths < 0 || attackRatioThousandths > Fixed.One)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRatioThousandths), "AttackRatio is 0–1000 thousandths.");
            }

            if (avgCardDmg < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(avgCardDmg), "AvgCardDMG must not be negative.");
            }

            World = world;
            AttackRatioThousandths = attackRatioThousandths;
            AvgCardDmg = avgCardDmg;
            JudgmentMix = judgmentMix ?? throw new ArgumentNullException(nameof(judgmentMix));
        }

        /// <summary>The standard inputs for a World: its AvgCardDMG (PRD 3.2.17), the typical AttackRatio and an all-Perfect mix (PRD 3.7.15).</summary>
        public static EncounterBalance ForWorld(int world)
        {
            return new EncounterBalance(world, Tuning.DefaultAttackRatioThousandths, Balance.AvgCardDmgForWorld(world), JudgmentMix.AllPerfect);
        }
    }

    /// <summary>The balancing formulas that turn intended durations into enemy numbers (PRD 3.7).</summary>
    public static class Balance
    {
        /// <summary>
        /// Base enemy HP before the role multiplier (PRD 3.7.15): AAPM = ActionsPerMinute x
        /// AttackRatio; AvgAttackDMG = AvgCardDMG x (P% x 1 + G% x 0.5); DPM = AAPM x AvgAttackDMG;
        /// DPS = DPM / 60; HP = DPS x IntendedBattleDuration. Computed exactly in thousandths and
        /// rounded once. <paramref name="actionsPerMinuteThousandths"/> is the chart's
        /// (<see cref="Chart.ActionsPerMinuteThousandths"/>).
        /// </summary>
        public static int EnemyHp(int actionsPerMinuteThousandths, int attackRatioThousandths, int avgCardDmg, JudgmentMix judgmentMix, int intendedSeconds)
        {
            if (actionsPerMinuteThousandths < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionsPerMinuteThousandths), "Actions per minute must not be negative.");
            }

            if (attackRatioThousandths < 0 || attackRatioThousandths > Fixed.One)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRatioThousandths), "AttackRatio is 0–1000 thousandths.");
            }

            if (avgCardDmg < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(avgCardDmg), "AvgCardDMG must not be negative.");
            }

            if (judgmentMix is null)
            {
                throw new ArgumentNullException(nameof(judgmentMix));
            }

            if (intendedSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intendedSeconds), "Intended duration must not be negative.");
            }

            // P + G/2 kept exact as (2P + G) in two-thousandths.
            long mixTwoThousandths = 2L * judgmentMix.PerfectThousandths + judgmentMix.GoodThousandths;
            long numerator = checked((long)actionsPerMinuteThousandths * attackRatioThousandths * avgCardDmg * mixTwoThousandths * intendedSeconds);
            const long scale = (long)Fixed.One * Fixed.One * (2 * Fixed.One) * 60;
            return Fixed.Round(numerator, scale);
        }

        /// <summary>The base HP of an enemy from its own chart and intended duration (PRD 3.7.15), before the role multiplier.</summary>
        public static int EnemyHp(EnemyDefinition enemy, int attackRatioThousandths, int avgCardDmg, JudgmentMix judgmentMix)
        {
            if (enemy is null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            return EnemyHp(enemy.Chart.ActionsPerMinuteThousandths, attackRatioThousandths, avgCardDmg, judgmentMix, enemy.IntendedSeconds);
        }

        /// <summary>An enemy's HP at battle start (PRD 3.6.29): the formula value for the balance inputs, times the role multiplier (PRD 3.6.1).</summary>
        public static int EnemyHp(EnemyDefinition enemy, EncounterBalance balance)
        {
            if (balance is null)
            {
                throw new ArgumentNullException(nameof(balance));
            }

            int baseHp = EnemyHp(enemy, balance.AttackRatioThousandths, balance.AvgCardDmg, balance.JudgmentMix);
            return Fixed.Mul(baseHp, enemy.Role.HpMultiplierThousandths());
        }

        /// <summary>The AvgCardDMG the HP formula assumes in a World (PRD 3.2.17); Worlds past the third keep the third's.</summary>
        public static int AvgCardDmgForWorld(int world)
        {
            if (world < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(world), "Worlds count from 1.");
            }

            switch (world)
            {
                case 1: return Tuning.AvgCardDmgWorld1;
                case 2: return Tuning.AvgCardDmgWorld2;
                default: return Tuning.AvgCardDmgWorld3;
            }
        }

        /// <summary>The World 1 damage-per-hit band of a role in a tier (PRD 3.7.16).</summary>
        public static ValueBand DamageBand(EnemyRole role, EncounterTier tier)
        {
            switch (role)
            {
                case EnemyRole.Aggressor:
                    return Band(tier,
                        Tuning.AggressorNormalDamageMin, Tuning.AggressorNormalDamageMax,
                        Tuning.AggressorEliteDamageMin, Tuning.AggressorEliteDamageMax,
                        Tuning.AggressorBossDamageMin, Tuning.AggressorBossDamageMax);
                case EnemyRole.Mentalist:
                    return Band(tier,
                        Tuning.MentalistNormalDamageMin, Tuning.MentalistNormalDamageMax,
                        Tuning.MentalistEliteDamageMin, Tuning.MentalistEliteDamageMax,
                        Tuning.MentalistBossDamageMin, Tuning.MentalistBossDamageMax);
                case EnemyRole.Tank:
                    return Band(tier,
                        Tuning.TankNormalDamageMin, Tuning.TankNormalDamageMax,
                        Tuning.TankEliteDamageMin, Tuning.TankEliteDamageMax,
                        Tuning.TankBossDamageMin, Tuning.TankBossDamageMax);
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.");
            }
        }

        private static ValueBand Band(EncounterTier tier, int normalMin, int normalMax, int eliteMin, int eliteMax, int bossMin, int bossMax)
        {
            switch (tier)
            {
                case EncounterTier.Normal: return new ValueBand(normalMin, normalMax);
                case EncounterTier.Elite: return new ValueBand(eliteMin, eliteMax);
                case EncounterTier.Boss: return new ValueBand(bossMin, bossMax);
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown tier.");
            }
        }

        /// <summary>
        /// A definition's World 1 damage per hit raised 15% per World above World 1 (PRD 3.7.16),
        /// compounded and rounded once: 10 in World 3 is 13.
        /// </summary>
        public static int DamageForWorld(int baseDamage, int world)
        {
            if (baseDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDamage), "Damage must not be negative.");
            }

            if (world < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(world), "Worlds count from 1.");
            }

            int rises = world - 1;
            if (rises == 0)
            {
                return baseDamage;
            }

            long value = baseDamage;
            long scale = 1;
            for (int i = 0; i < rises; i++)
            {
                value = checked(value * (Fixed.One + Tuning.DamageRisePerWorldThousandths));
                scale = checked(scale * Fixed.One);
            }

            return Fixed.Round(value, scale);
        }
    }
}
