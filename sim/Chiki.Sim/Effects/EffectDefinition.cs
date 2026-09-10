using System;

namespace Chiki.Sim.Effects
{
    /// <summary>
    /// When an effect fires: on the play of its card, continuously while it lives, or when a
    /// battle event of the named type is appended to the stream (PRD 4.8).
    /// </summary>
    public enum EffectTrigger
    {
        /// <summary>The card resolves (PRD 3.3.4.3, 3.3.4.4).</summary>
        OnPlay,

        /// <summary>A standing modifier that applies for as long as its lifetime runs.</summary>
        Passive,

        BeatStarted,
        InputJudged,
        DamageDealt,
        DamageTaken,
        BlockGained,
        StatusApplied,
        BattleEnded,
    }

    /// <summary>The reaction conditions of PRD 3.4.9, evaluated against battle state when the trigger fires.</summary>
    public enum EffectCondition
    {
        None,

        /// <summary>"on Perfect": the press that answered the action was Perfect.</summary>
        OnPerfect,

        /// <summary>"if this kills": the card's damage took the enemy to 0 HP.</summary>
        IfKills,

        /// <summary>"if the enemy is attacking this beat": the action being answered is an attack.</summary>
        IfEnemyAttacking,
    }

    /// <summary>What an effect does when it fires.</summary>
    public enum EffectModifier
    {
        /// <summary>Damage to the target by the player effect formula (PRD 3.3.4.3).</summary>
        DealDamage,

        /// <summary>True DMG to the target (PRD 3.3.4.7).</summary>
        DealTrueDamage,

        /// <summary>Block to the target (PRD 3.4.8 Block-on-attack).</summary>
        GainBlock,

        /// <summary>A status on the target (PRD 3.3.7.1, 3.4.8).</summary>
        ApplyStatus,

        /// <summary>A run stat changes by the amount (PRD 3.2.3); Ard upward is Repair (PRD 3.4.8).</summary>
        ChangeStat,

        /// <summary>The card's value gains the amount before the multipliers (PRD 3.4.9 reaction bonus).</summary>
        AddValue,

        /// <summary>A value is multiplied by the amount in thousandths.</summary>
        MultiplyValue,
    }

    /// <summary>The run-wide stats an effect may change (PRD 3.2.3).</summary>
    public enum RunStat
    {
        Ard,
        BaseDmg,
        Essence,
        Crp,
    }

    /// <summary>What a <see cref="EffectModifier.MultiplyValue"/> modifier multiplies.</summary>
    public enum EffectValue
    {
        /// <summary>The playing card's value (PRD 3.3.4.3).</summary>
        CardValue,

        /// <summary>Damage the player deals.</summary>
        DamageDealt,

        /// <summary>Damage the player takes.</summary>
        DamageTaken,
    }

    /// <summary>How long a fired effect's modifier lives (P8.1): once, some beats, the battle or the run.</summary>
    public enum EffectLifetime
    {
        Instant,
        Beats,
        Battle,
        Run,
    }

    /// <summary>
    /// One typed effect entry (PRD 4.4, 3.4.9): trigger, optional condition, modifier and
    /// lifetime. Cards, Imprints, Charms, enemy abilities and traits act through nothing else.
    /// <see cref="Amount"/> is the modifier's number: damage, Block, a stat delta, a value bonus
    /// or a multiplier in thousandths. Validated on construction so content that breaks a rule
    /// fails at load.
    /// </summary>
    public sealed record EffectDefinition
    {
        public EffectTrigger Trigger { get; }

        public EffectCondition Condition { get; }

        public EffectModifier Modifier { get; }

        /// <summary>The side the modifier acts on.</summary>
        public StatusTarget Target { get; }

        public int Amount { get; }

        /// <summary>The status an <see cref="EffectModifier.ApplyStatus"/> modifier applies, with its stacks and per-source value.</summary>
        public StatusApplication? Status { get; }

        /// <summary>The stat a <see cref="EffectModifier.ChangeStat"/> modifier changes.</summary>
        public RunStat? Stat { get; }

        /// <summary>The value a <see cref="EffectModifier.MultiplyValue"/> modifier multiplies.</summary>
        public EffectValue? Value { get; }

        public EffectLifetime Lifetime { get; }

        /// <summary>Beats the modifier lives when <see cref="Lifetime"/> is <see cref="EffectLifetime.Beats"/>; otherwise 0.</summary>
        public int LifetimeBeats { get; }

        public EffectDefinition(
            EffectTrigger trigger,
            EffectModifier modifier,
            int amount = 0,
            EffectCondition condition = EffectCondition.None,
            StatusTarget target = StatusTarget.Enemy,
            StatusApplication? status = null,
            RunStat? stat = null,
            EffectValue? value = null,
            EffectLifetime lifetime = EffectLifetime.Instant,
            int lifetimeBeats = 0)
        {
            if (amount < 0 && modifier != EffectModifier.ChangeStat)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Only a change-stat effect may have a negative amount.");
            }

            switch (modifier)
            {
                case EffectModifier.ApplyStatus:
                    if (status is null)
                    {
                        throw new ArgumentException("An apply-status effect names its status.", nameof(status));
                    }

                    break;
                case EffectModifier.ChangeStat:
                    if (stat is null)
                    {
                        throw new ArgumentException("A change-stat effect names its stat.", nameof(stat));
                    }

                    break;
                case EffectModifier.MultiplyValue:
                    if (value is null)
                    {
                        throw new ArgumentException("A multiply-value effect names the value it multiplies.", nameof(value));
                    }

                    break;
                case EffectModifier.DealDamage:
                case EffectModifier.DealTrueDamage:
                case EffectModifier.GainBlock:
                case EffectModifier.AddValue:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifier), modifier, "Unknown modifier.");
            }

            if (modifier != EffectModifier.ApplyStatus && status != null)
            {
                throw new ArgumentException("Only an apply-status effect carries a status.", nameof(status));
            }

            if (modifier != EffectModifier.ChangeStat && stat != null)
            {
                throw new ArgumentException("Only a change-stat effect carries a stat.", nameof(stat));
            }

            if (modifier != EffectModifier.MultiplyValue && value != null)
            {
                throw new ArgumentException("Only a multiply-value effect carries a value.", nameof(value));
            }

            if (trigger == EffectTrigger.Passive && modifier != EffectModifier.MultiplyValue)
            {
                throw new ArgumentException("A passive effect is a standing multiplier.", nameof(trigger));
            }

            bool shapesCardValue = modifier == EffectModifier.AddValue
                || (modifier == EffectModifier.MultiplyValue && value == EffectValue.CardValue);
            if (shapesCardValue && trigger != EffectTrigger.OnPlay)
            {
                throw new ArgumentException("A card-value modifier fires only on the play of its card.", nameof(trigger));
            }

            if (lifetime == EffectLifetime.Beats ? lifetimeBeats < 1 : lifetimeBeats != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lifetimeBeats), "Only a lifetime in beats has a beat count, and it is at least 1.");
            }

            bool standing = modifier == EffectModifier.MultiplyValue && !shapesCardValue;
            if (standing ? lifetime == EffectLifetime.Instant : lifetime != EffectLifetime.Instant)
            {
                throw new ArgumentException("Exactly a damage multiplier outlives its trigger: give it a lifetime in beats, the battle or the run.", nameof(lifetime));
            }

            Trigger = trigger;
            Condition = condition;
            Modifier = modifier;
            Target = target;
            Amount = amount;
            Status = status;
            Stat = stat;
            Value = value;
            Lifetime = lifetime;
            LifetimeBeats = lifetimeBeats;
        }
    }
}
