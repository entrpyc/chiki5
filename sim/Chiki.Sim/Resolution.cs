using System;

namespace Chiki.Sim
{
    /// <summary>
    /// What an enemy attack did to the player (PRD 3.3.4.2): Block depletes first and the
    /// remainder hits ARD. Neither part is ever negative.
    /// </summary>
    public readonly struct IncomingDamage
    {
        public int BlockAbsorbed { get; }
        public int ArdLoss { get; }

        public IncomingDamage(int blockAbsorbed, int ardLoss)
        {
            BlockAbsorbed = blockAbsorbed;
            ArdLoss = ardLoss;
        }
    }

    /// <summary>
    /// The resolution formulas of PRD 3.3.4, as pure functions. Everything is computed in
    /// thousandths and rounded exactly once (PRD 3.3.4.5). Timing governs incoming damage and the
    /// player card's output; category governs only what the card does (PRD 3.3.4.1).
    /// </summary>
    public static class Resolution
    {
        /// <summary>IncomingMult in thousandths (PRD 3.3.4.2); null is no input.</summary>
        public static int IncomingMultiplier(Judgment? grade)
        {
            switch (grade)
            {
                case Judgment.Perfect:
                    return Tuning.IncomingMultPerfectThousandths;
                case Judgment.Good:
                    return Tuning.IncomingMultGoodThousandths;
                case Judgment.Miss:
                    return Tuning.IncomingMultMissThousandths;
                default:
                    return Tuning.IncomingMultNoInputThousandths;
            }
        }

        /// <summary>JudgmentMult in thousandths (PRD 3.3.4.3).</summary>
        public static int JudgmentMultiplier(Judgment grade)
        {
            switch (grade)
            {
                case Judgment.Perfect:
                    return Tuning.JudgmentMultPerfectThousandths;
                case Judgment.Good:
                    return Tuning.JudgmentMultGoodThousandths;
                case Judgment.Miss:
                    return Tuning.JudgmentMultMissThousandths;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, "Unknown judgment.");
            }
        }

        /// <summary>
        /// Incoming damage on a beat the enemy attacks (PRD 3.3.4.2):
        /// <c>EnemyDMG x IncomingMult x StatusMults - Block</c>. The product is rounded once; Block
        /// absorbs first and the remainder is the ARD loss. Statuses (P5) pass their multiplier in
        /// thousandths; until then it is <see cref="Fixed.One"/>.
        /// </summary>
        public static IncomingDamage Incoming(int enemyDmg, Judgment? grade, int block, int statusMultThousandths = Fixed.One)
        {
            if (enemyDmg < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyDmg), "Enemy damage must not be negative.");
            }

            if (block < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(block), "Block must not be negative.");
            }

            long product = (long)enemyDmg * IncomingMultiplier(grade) * statusMultThousandths;
            int damage = Fixed.Round(product, (long)Fixed.One * Fixed.One);
            int absorbed = Math.Min(block, damage);
            return new IncomingDamage(absorbed, damage - absorbed);
        }

        /// <summary>
        /// The player's card effect (PRD 3.3.4.3):
        /// <c>CardValue x JudgmentMult x (1 + BaseDMG%) x StatusMults</c>, times the efficacy of
        /// the card against the charted action (PRD 3.3.4.4), rounded once. The Base DMG term is
        /// realised as <paramref name="baseDmgBonus"/> added to CardValue before the multipliers,
        /// so each +1 Base DMG adds +1 to an attack card's Perfect output (PRD 3.2.3); callers
        /// pass 0 for Defense and Ability cards.
        /// </summary>
        public static int PlayerEffect(int cardValue, Judgment grade, int baseDmgBonus, int efficacyThousandths = Fixed.One, int statusMultThousandths = Fixed.One)
        {
            if (cardValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardValue), "CardValue must not be negative.");
            }

            if (baseDmgBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDmgBonus), "Base DMG must not be negative.");
            }

            if (efficacyThousandths < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(efficacyThousandths), "Efficacy must not be negative.");
            }

            long product = (long)(cardValue + baseDmgBonus)
                * JudgmentMultiplier(grade)
                * efficacyThousandths
                * statusMultThousandths;
            return Fixed.Round(product, (long)Fixed.One * Fixed.One * Fixed.One);
        }

        /// <summary>
        /// The efficacy matrix for an attack card (PRD 3.3.4.4), in thousandths of full damage:
        /// against a Left or Right attack the correct side deals full damage and the wrong side 0;
        /// against a Defend the damage is reduced by the enemy's defense level; against a Buff or
        /// a Charge wind-up (PRD 3.6.16) full damage, since no wrong side exists.
        /// </summary>
        public static int AttackEfficacy(EnemyAction enemyAction, CardCategory attack)
        {
            if (enemyAction is null)
            {
                throw new ArgumentNullException(nameof(enemyAction));
            }

            if (!attack.IsAttack())
            {
                throw new ArgumentException("Efficacy applies to attack cards only.", nameof(attack));
            }

            switch (enemyAction.Kind)
            {
                case EnemyActionKind.AttackLeft:
                    return attack == CardCategory.LeftAttack ? Fixed.One : 0;
                case EnemyActionKind.AttackRight:
                    return attack == CardCategory.RightAttack ? Fixed.One : 0;
                case EnemyActionKind.Defend:
                    return Math.Max(0, Fixed.One - enemyAction.DefenseLevel);
                case EnemyActionKind.Buff:
                case EnemyActionKind.Charge:
                    return Fixed.One;
                default:
                    throw new ArgumentOutOfRangeException(nameof(enemyAction), enemyAction.Kind, "Unknown enemy action kind.");
            }
        }
    }
}
