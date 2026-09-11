using System;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>
    /// The effect framework's runtime (P8.1): registered effects fire when their trigger event is
    /// appended to the stream, a card's on-play effects fire as the card resolves, and standing
    /// bonuses and multipliers live for their lifetime. Cards, enemy powers and Charms act
    /// through nothing else.
    /// </summary>
    public sealed partial class Battle
    {
        private readonly EffectRegistry _effects = new EffectRegistry();
        private BeatContext? _resolving;

        /// <summary>The effects registered on this battle and the modifiers alive right now.</summary>
        public EffectRegistry Effects => _effects;

        /// <summary>
        /// Attaches an effect to an owner (a card instance, the enemy, a Charm) for this battle.
        /// An event-triggered effect fires whenever its event is appended; a passive one comes
        /// alive at once for its lifetime. Battle- and run-long lifetimes both last this battle:
        /// the run-level registry is a later plan's. An on-play effect belongs to the card that
        /// plays it and is refused here.
        /// </summary>
        public RegisteredEffect RegisterEffect(string ownerId, EffectDefinition effect)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                throw new ArgumentException("An effect needs an owner.", nameof(ownerId));
            }

            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (effect.Trigger == EffectTrigger.OnPlay)
            {
                throw new ArgumentException("An on-play effect belongs to the card that plays it; it is not registered.", nameof(effect));
            }

            var registered = _effects.Add(ownerId, effect);
            if (effect.Trigger == EffectTrigger.Passive && Outcome is null)
            {
                Activate(ownerId, effect, PositionAt(CurrentTimeMs));
            }

            return registered;
        }

        /// <summary>Detaches a registered effect; modifiers it already left alive run out on their own.</summary>
        public bool UnregisterEffect(int id)
        {
            return _effects.Remove(id);
        }

        /// <summary>The enemy's abilities and traits act through the framework: each registers its effects on the enemy at battle start (PRD 3.6.4).</summary>
        private void RegisterEnemyPowers()
        {
            foreach (var ability in Enemy.Abilities)
            {
                foreach (var effect in EnemyPowerEffects.Of(ability))
                {
                    RegisterEffect(Enemy.Id, effect);
                }
            }

            foreach (var trait in Enemy.Traits)
            {
                foreach (var effect in EnemyPowerEffects.Of(trait))
                {
                    RegisterEffect(Enemy.Id, effect);
                }
            }
        }

        private static EffectTrigger? TriggerOf(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BattleStarted _: return EffectTrigger.BattleStarted;
                case BeatStarted _: return EffectTrigger.BeatStarted;
                case BeatEnded _: return EffectTrigger.BeatEnded;
                case InputJudged _: return EffectTrigger.InputJudged;
                case ActionResolved _: return EffectTrigger.ActionResolved;
                case DamageDealt _: return EffectTrigger.DamageDealt;
                case DamageTaken _: return EffectTrigger.DamageTaken;
                case BlockGained _: return EffectTrigger.BlockGained;
                case StatusApplied _: return EffectTrigger.StatusApplied;
                case BattleEnded _: return EffectTrigger.BattleEnded;
                default: return null;
            }
        }

        /// <summary>Fires every registered effect whose trigger is the event just appended; nothing fires once the battle has ended.</summary>
        private void Dispatch(BattleEvent battleEvent)
        {
            if (Outcome != null)
            {
                return;
            }

            var trigger = TriggerOf(battleEvent);
            if (trigger is null)
            {
                return;
            }

            int actionIndex = _resolving?.Opportunity?.Index ?? _pending.Index;
            foreach (var registered in _effects.TriggeredBy(trigger.Value))
            {
                if (Outcome != null)
                {
                    break;
                }

                if (!ConditionHolds(registered.Definition, battleEvent))
                {
                    continue;
                }

                ApplyModifier(registered.OwnerId, registered.Definition, Fixed.One, battleEvent.PositionQb, actionIndex);
            }
        }

        /// <summary>
        /// The conditions of PRD 3.4.9 and the enemy powers against the battle right now: the
        /// grade in play is the press of the action being resolved (or the judged press for an
        /// InputJudged trigger); a kill is the enemy at 0 HP; the enemy attacks when the action
        /// being resolved, or else the pending one, is an attack.
        /// </summary>
        private bool ConditionHolds(EffectDefinition effect, BattleEvent? trigger)
        {
            switch (effect.Condition)
            {
                case EffectCondition.None:
                    return true;
                case EffectCondition.OnPerfect:
                    return GradeInPlay(trigger) == Judgment.Perfect;
                case EffectCondition.OnGood:
                    return GradeInPlay(trigger) == Judgment.Good;
                case EffectCondition.OnMiss:
                    return GradeInPlay(trigger) == Judgment.Miss;
                case EffectCondition.IfNoInput:
                    return GradeInPlay(trigger) is null;
                case EffectCondition.IfKills:
                    return EnemyHp == 0;
                case EffectCondition.IfEnemyAttacking:
                    return ActionInPlay().IsAttack;
                case EffectCondition.IfDamageLanded:
                    return trigger is DamageTaken taken ? taken.Amount > 0
                        : trigger is DamageDealt dealt ? dealt.Amount > 0
                        : trigger is TrueDamageDealt trueDamage && trueDamage.Amount > 0;
                case EffectCondition.IfBuffAction:
                    return ActionInPlay().Kind == EnemyActionKind.Buff;
                case EffectCondition.IfEnemyQuietBeats:
                    return EnemyQuietBeats > 0 && EnemyQuietBeats % effect.ConditionAmount == 0;
                case EffectCondition.IfPerfectDefense:
                    return trigger is BattleEnded ended ? ended.PerfectDefense : PerfectDefense;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect.Condition, "Unknown condition.");
            }
        }

        private EnemyAction ActionInPlay()
        {
            return (_resolving?.Opportunity ?? _pending).Action;
        }

        private Judgment? GradeInPlay(BattleEvent? trigger)
        {
            switch (trigger)
            {
                case InputJudged judged:
                    return judged.Grade;
                case ActionResolved resolved:
                    return resolved.Grade;
                default:
                    return _resolving?.Press?.Grade;
            }
        }

        /// <summary>
        /// Applies a fired effect's modifier at <paramref name="scaleThousandths"/>: the JudgmentMult
        /// of the play for a card's on-play effects (PRD 3.3.4.3; a Missed card applies nothing),
        /// 100% for event-triggered ones. Card-value shaping (add-value, multiply card-value) is
        /// folded into the value at play and does nothing here; a standing bonus or multiplier
        /// comes alive for its lifetime.
        /// </summary>
        private void ApplyModifier(string ownerId, EffectDefinition effect, int scaleThousandths, int positionQb, int actionIndex)
        {
            switch (effect.Modifier)
            {
                case EffectModifier.DealDamage:
                    DealEffectDamage(effect.Target, Scale(effect.Amount, scaleThousandths), positionQb, actionIndex);
                    break;

                case EffectModifier.DealTrueDamage:
                    TrueDamage(effect.Target, Scale(effect.Amount, scaleThousandths), positionQb);
                    break;

                case EffectModifier.GainBlock:
                    AddBlock(effect.Target, Scale(effect.Amount, scaleThousandths), positionQb);
                    break;

                case EffectModifier.ApplyStatus:
                {
                    var status = effect.Status!;
                    int stacks = Scale(status.Stacks, scaleThousandths);
                    if (stacks > 0)
                    {
                        Land(effect.Target, new StatusApplication(status.Kind, stacks, status.Value), positionQb);
                    }

                    break;
                }

                case EffectModifier.ChangeStat:
                    ChangeStat(effect.Stat!.Value, Scale(effect.Amount, scaleThousandths), positionQb);
                    break;

                case EffectModifier.MultiplyValue:
                case EffectModifier.AddValue:
                    if (effect.IsStanding)
                    {
                        Activate(ownerId, effect, positionQb);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect.Modifier, "Unknown modifier.");
            }
        }

        private static int Scale(int amount, int thousandths)
        {
            return Fixed.Round((long)amount * thousandths);
        }

        /// <summary>
        /// Damage from an effect rather than an attack card: to the enemy it takes the player's
        /// StatusMults and standing multipliers, then its reduction and Block; to the player it
        /// takes the enemy's StatusMults and standing multipliers, then Block, with no timing
        /// mitigation. Neither goes through the efficacy matrix (PRD 3.3.4.4: an effect resolves).
        /// </summary>
        private void DealEffectDamage(StatusTarget target, int amount, int positionQb, int actionIndex)
        {
            if (target == StatusTarget.Enemy)
            {
                int mult = Fixed.Mul(PlayerStatuses.DealtMultiplierThousandths, _effects.MultiplierFor(EffectValue.DamageDealt));
                int dealt = DamageEnemy(Fixed.Mul(amount, mult), out int absorbed);
                Emit(new DamageDealt(positionQb, actionIndex, dealt, absorbed));
            }
            else
            {
                int mult = Fixed.Mul(EnemyStatuses.DealtMultiplierThousandths, _effects.MultiplierFor(EffectValue.DamageTaken));
                int damage = Fixed.Mul(amount, mult);
                int absorbed = Math.Min(Block, damage);
                Block -= absorbed;
                int lost = TakeArd(damage - absorbed);
                Emit(new DamageTaken(positionQb, actionIndex, lost, absorbed));
            }
        }

        private void ChangeStat(RunStat stat, int delta, int positionQb)
        {
            int total;
            switch (stat)
            {
                case RunStat.Ard:
                    Stats.Ard += delta;
                    total = Stats.Ard;
                    break;
                case RunStat.BaseDmg:
                    Stats.BaseDmg = Math.Max(0, Stats.BaseDmg + delta);
                    total = Stats.BaseDmg;
                    break;
                case RunStat.Essence:
                    Stats.Essence += delta;
                    total = Stats.Essence;
                    break;
                case RunStat.Crp:
                    Stats.Crp += delta;
                    total = Stats.Crp;
                    break;
                case RunStat.MaxArd:
                    Stats.ChangeMaxArd(delta);
                    total = Stats.MaxArd;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), stat, "Unknown stat.");
            }

            Emit(new StatChanged(positionQb, stat, delta, total));
        }

        /// <summary>
        /// Brings a standing modifier alive for its lifetime. A timed or until-consumed modifier
        /// the same owner already has alive is restarted rather than stacked (PRD 3.6.8, 3.6.9);
        /// battle- and run-long ones stack (PRD 3.6.5).
        /// </summary>
        private void Activate(string ownerId, EffectDefinition effect, int positionQb)
        {
            var duplicate = _effects.Duplicate(ownerId, effect);
            if (duplicate != null)
            {
                _effects.Deactivate(duplicate.Id);
                Emit(new ModifierExpired(positionQb, duplicate.Id, duplicate.OwnerId, duplicate.Value));
            }

            var timer = effect.Lifetime == EffectLifetime.Beats ? StartTimer(effect.LifetimeBeats) : null;
            var modifier = _effects.Activate(ownerId, effect, timer);
            Emit(new ModifierActivated(positionQb, modifier.Id, ownerId, modifier.Value, modifier.Additive ? modifier.Bonus : modifier.Thousandths, timer?.TotalBeats, modifier.Additive));
        }

        private void PruneExpiredModifiers(int positionQb)
        {
            foreach (var expired in _effects.PruneExpired())
            {
                Emit(new ModifierExpired(positionQb, expired.Id, expired.OwnerId, expired.Value));
            }
        }

        /// <summary>The value was just used: every modifier on it that lives until consumed is dropped (PRD 3.6.8).</summary>
        private void ConsumeModifiers(EffectValue value, int positionQb)
        {
            foreach (var consumed in _effects.Consume(value))
            {
                Emit(new ModifierExpired(positionQb, consumed.Id, consumed.OwnerId, consumed.Value));
            }
        }

        /// <summary>
        /// The value a card plays with (PRD 3.3.4.3, 3.4.9): CardValue, plus its declared scaling
        /// for the units held right now, plus every on-play add-value whose condition holds, then
        /// times every on-play card-value multiplier whose condition holds.
        /// </summary>
        private int CardValueInPlay(CardDefinition card)
        {
            int value = card.Value;
            if (card.Scaling != null)
            {
                value += card.Scaling.BonusFor(UnitsHeld(card.Scaling));
            }

            foreach (var effect in card.Effects)
            {
                if (effect.Trigger == EffectTrigger.OnPlay && effect.Modifier == EffectModifier.AddValue && effect.Value is null && ConditionHolds(effect, null))
                {
                    value += effect.Amount;
                }
            }

            foreach (var effect in card.Effects)
            {
                if (effect.Trigger == EffectTrigger.OnPlay && effect.Modifier == EffectModifier.MultiplyValue
                    && effect.Value == EffectValue.CardValue && ConditionHolds(effect, null))
                {
                    value = Fixed.Mul(value, effect.Amount);
                }
            }

            return value;
        }

        private int UnitsHeld(ValueScaling scaling)
        {
            switch (scaling.Source)
            {
                case ScalingSource.Block: return Block;
                case ScalingSource.Ard: return Stats.Ard;
                case ScalingSource.BaseDmg: return Stats.BaseDmg;
                case ScalingSource.Essence: return Stats.Essence;
                case ScalingSource.Crp: return Stats.Crp;
                case ScalingSource.Status: return PlayerStatuses.Stacks(scaling.Status!.Value);
                default: throw new ArgumentOutOfRangeException(nameof(scaling), scaling.Source, "Unknown scaling source.");
            }
        }

        /// <summary>A card's on-play effects after its category's own effect, each at the play's JudgmentMult; a Missed card applies nothing (P8.6).</summary>
        private void ResolveOnPlayEffects(CardDefinition card, PendingPress press, ActionOpportunity opportunity)
        {
            int scale = Resolution.JudgmentMultiplier(press.Grade);
            if (scale == 0)
            {
                return;
            }

            foreach (var effect in card.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnPlay || Outcome != null)
                {
                    continue;
                }

                if (effect.ShapesCardValue)
                {
                    continue;
                }

                if (!ConditionHolds(effect, null))
                {
                    continue;
                }

                ApplyModifier(card.Id, effect, scale, opportunity.PositionQb, opportunity.Index);
            }
        }
    }
}
