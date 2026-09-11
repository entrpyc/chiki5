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

        /// <summary>The battle began, before its first beat (PRD 3.6.25 Guard).</summary>
        BattleStarted,

        BeatStarted,

        /// <summary>A beat ended: damage over time acted and statuses ticked (PRD 3.3.7.1, 3.6.20 Stoneform).</summary>
        BeatEnded,

        InputJudged,

        /// <summary>An enemy action finished resolving with the grade that answered it (PRD 3.3.4.1); the hook for grade-conditioned enemy powers (PRD 3.6.6, 3.6.8, 3.6.9).</summary>
        ActionResolved,

        DamageDealt,
        DamageTaken,
        BlockGained,
        StatusApplied,
        BattleEnded,

        /// <summary>The owner entered the run: an Imprint was gained or a Charm equipped (PRD 3.9.3, 3.9.6). Fired once by the run when it registers the owner (P18.3, P18.4), never by a battle event.</summary>
        Acquired,
    }

    /// <summary>The reaction conditions of PRD 3.4.9 and the enemy powers' (PRD 3.6), evaluated against battle state when the trigger fires.</summary>
    public enum EffectCondition
    {
        None,

        /// <summary>"on Perfect": the press that answered the action was Perfect.</summary>
        OnPerfect,

        /// <summary>The press that answered the action was Good (PRD 3.6.6).</summary>
        OnGood,

        /// <summary>The press that answered the action was a Miss (PRD 3.6.6).</summary>
        OnMiss,

        /// <summary>The action went unanswered: no press, or a Stunned player's (PRD 3.3.3.2, 3.6.8).</summary>
        IfNoInput,

        /// <summary>"if this kills": the card's damage took the enemy to 0 HP.</summary>
        IfKills,

        /// <summary>"if the enemy is attacking this beat": the action being answered is an attack.</summary>
        IfEnemyAttacking,

        /// <summary>The triggering damage event took HP or ARD, not 0 (PRD 3.6.5).</summary>
        IfDamageLanded,

        /// <summary>The action being resolved is the enemy's Buff, the moment it uses a timed ability (PRD 3.6.9, 3.6.10).</summary>
        IfBuffAction,

        /// <summary>The enemy has just completed a run of <see cref="EffectDefinition.ConditionAmount"/> consecutive beats without taking damage (PRD 3.6.20).</summary>
        IfEnemyQuietBeats,

        /// <summary>The battle ended with Perfect Defense: zero damage taken for the whole battle (PRD 3.3.9.4, 3.9.8).</summary>
        IfPerfectDefense,
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

        /// <summary>
        /// With no <see cref="EffectDefinition.Value"/>: the card's value gains the amount before
        /// the multipliers (PRD 3.4.9 reaction bonus). With one: a standing bonus on that value
        /// for the lifetime (PRD 3.6.5 Rising Tempo).
        /// </summary>
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

        /// <summary>Maximum ARD (PRD 3.2.3); raising it raises current ARD by the same amount.</summary>
        MaxArd,
    }

    /// <summary>What a standing <see cref="EffectModifier.MultiplyValue"/> or <see cref="EffectModifier.AddValue"/> modifier acts on.</summary>
    public enum EffectValue
    {
        /// <summary>The playing card's value (PRD 3.3.4.3).</summary>
        CardValue,

        /// <summary>Damage the player deals.</summary>
        DamageDealt,

        /// <summary>Damage the player takes.</summary>
        DamageTaken,

        /// <summary>The enemy's damage per hit: additive bonuses are its Base DMG (PRD 3.6.5), multipliers empower its next attacks (PRD 3.6.8).</summary>
        EnemyDamage,

        /// <summary>Damage the enemy takes other than True DMG (PRD 3.6.9); True DMG ignores it (PRD 3.3.4.7).</summary>
        EnemyDamageTaken,
    }

    /// <summary>How long a fired effect's modifier lives (P8.1): once, some beats, until consumed, the battle or the run.</summary>
    public enum EffectLifetime
    {
        Instant,
        Beats,

        /// <summary>Until the value it modifies is next used: an enemy attack for <see cref="EffectValue.EnemyDamage"/> (PRD 3.6.8).</summary>
        Consumed,

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

        /// <summary>The condition's number: the beats of <see cref="EffectCondition.IfEnemyQuietBeats"/>; 0 for the rest.</summary>
        public int ConditionAmount { get; }

        public EffectModifier Modifier { get; }

        /// <summary>The side the modifier acts on.</summary>
        public StatusTarget Target { get; }

        public int Amount { get; }

        /// <summary>The status an <see cref="EffectModifier.ApplyStatus"/> modifier applies, with its stacks and per-source value.</summary>
        public StatusApplication? Status { get; }

        /// <summary>The stat a <see cref="EffectModifier.ChangeStat"/> modifier changes.</summary>
        public RunStat? Stat { get; }

        /// <summary>The value a standing <see cref="EffectModifier.MultiplyValue"/> or <see cref="EffectModifier.AddValue"/> modifier acts on.</summary>
        public EffectValue? Value { get; }

        public EffectLifetime Lifetime { get; }

        /// <summary>Beats the modifier lives when <see cref="Lifetime"/> is <see cref="EffectLifetime.Beats"/>; otherwise 0.</summary>
        public int LifetimeBeats { get; }

        /// <summary>Whether the modifier is a card-value bonus or multiplier, folded into the value at play (P8.7).</summary>
        public bool ShapesCardValue =>
            (Modifier == EffectModifier.AddValue && Value is null)
            || (Modifier == EffectModifier.MultiplyValue && Value == EffectValue.CardValue);

        /// <summary>Whether the modifier outlives its trigger as a standing bonus or multiplier on a battle value.</summary>
        public bool IsStanding =>
            (Modifier == EffectModifier.MultiplyValue && Value != EffectValue.CardValue)
            || (Modifier == EffectModifier.AddValue && Value != null);

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
            int lifetimeBeats = 0,
            int conditionAmount = 0)
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
                case EffectModifier.AddValue:
                    if (value == EffectValue.CardValue)
                    {
                        throw new ArgumentException("An add-value effect on the card's own value names no value.", nameof(value));
                    }

                    break;
                case EffectModifier.DealDamage:
                case EffectModifier.DealTrueDamage:
                case EffectModifier.GainBlock:
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

            if (modifier != EffectModifier.MultiplyValue && modifier != EffectModifier.AddValue && value != null)
            {
                throw new ArgumentException("Only a multiply-value or add-value effect carries a value.", nameof(value));
            }

            if (condition == EffectCondition.IfEnemyQuietBeats ? conditionAmount < 1 : conditionAmount != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conditionAmount), "Only a quiet-beats condition has an amount, and it is at least 1.");
            }

            Trigger = trigger;
            Condition = condition;
            ConditionAmount = conditionAmount;
            Modifier = modifier;
            Target = target;
            Amount = amount;
            Status = status;
            Stat = stat;
            Value = value;
            Lifetime = lifetime;
            LifetimeBeats = lifetimeBeats;

            if (trigger == EffectTrigger.Passive && !IsStanding)
            {
                throw new ArgumentException("A passive effect is a standing bonus or multiplier.", nameof(trigger));
            }

            if (ShapesCardValue && trigger != EffectTrigger.OnPlay)
            {
                throw new ArgumentException("A card-value modifier fires only on the play of its card.", nameof(trigger));
            }

            if (lifetime == EffectLifetime.Beats ? lifetimeBeats < 1 : lifetimeBeats != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lifetimeBeats), "Only a lifetime in beats has a beat count, and it is at least 1.");
            }

            if (IsStanding ? lifetime == EffectLifetime.Instant : lifetime != EffectLifetime.Instant)
            {
                throw new ArgumentException("Exactly a standing bonus or multiplier outlives its trigger: give it a lifetime in beats, until consumed, the battle or the run.", nameof(lifetime));
            }
        }
    }
}
