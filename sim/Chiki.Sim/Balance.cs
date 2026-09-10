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
    }
}
