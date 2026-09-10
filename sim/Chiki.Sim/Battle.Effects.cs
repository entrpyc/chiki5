using System;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>
    /// The effect framework's runtime (P8.1): registered effects fire when their trigger event is
    /// appended to the stream, a card's on-play effects fire as the card resolves, and standing
    /// multipliers live for their lifetime. Cards, enemy powers and Charms act through nothing else.
    /// </summary>
    public sealed partial class Battle
    {
        private readonly EffectRegistry _effects = new EffectRegistry();
        private BeatContext? _resolving;

        /// <summary>The effects registered on this battle and the multipliers alive right now.</summary>
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

        private static EffectTrigger? TriggerOf(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BeatStarted _: return EffectTrigger.BeatStarted;
                case InputJudged _: return EffectTrigger.InputJudged;
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

                if (!ConditionHolds(registered.Definition.Condition, battleEvent))
                {
                    continue;
                }

                ApplyModifier(registered.OwnerId, registered.Definition, Fixed.One, battleEvent.PositionQb, actionIndex);
            }
        }

        /// <summary>
        /// The reaction conditions of PRD 3.4.9 against the battle right now: the grade in play is
        /// the press of the action being resolved (or the judged press for an InputJudged
        /// trigger); a kill is the enemy at 0 HP; the enemy attacks when the action being
        /// resolved, or else the pending one, is an attack.
        /// </summary>
        private bool ConditionHolds(EffectCondition condition, BattleEvent? trigger)
        {
            switch (condition)
            {
                case EffectCondition.None:
                    return true;
                case EffectCondition.OnPerfect:
                    return GradeInPlay(trigger) == Judgment.Perfect;
                case EffectCondition.IfKills:
                    return EnemyHp == 0;
                case EffectCondition.IfEnemyAttacking:
                    return (_resolving?.Opportunity ?? _pending).Action.IsAttack;
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition), condition, "Unknown condition.");
            }
        }

        private Judgment? GradeInPlay(BattleEvent? trigger)
        {
            if (trigger is InputJudged judged)
            {
                return judged.Grade;
            }

            return _resolving?.Press?.Grade;
        }

        /// <summary>
        /// Applies a fired effect's modifier at <paramref name="scaleThousandths"/>: the JudgmentMult
        /// of the play for a card's on-play effects (PRD 3.3.4.3; a Missed card applies nothing),
        /// 100% for event-triggered ones. Card-value shaping (add-value, multiply card-value) is
        /// folded into the value at play and does nothing here.
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
                    if (effect.Value != EffectValue.CardValue)
                    {
                        Activate(ownerId, effect, positionQb);
                    }

                    break;

                case EffectModifier.AddValue:
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), stat, "Unknown stat.");
            }

            Emit(new StatChanged(positionQb, stat, delta, total));
        }

        private void Activate(string ownerId, EffectDefinition effect, int positionQb)
        {
            var timer = effect.Lifetime == EffectLifetime.Beats ? StartTimer(effect.LifetimeBeats) : null;
            var modifier = _effects.Activate(ownerId, effect, timer);
            Emit(new ModifierActivated(positionQb, modifier.Id, ownerId, modifier.Value, modifier.Thousandths, timer?.TotalBeats));
        }

        private void PruneExpiredModifiers(int positionQb)
        {
            foreach (var expired in _effects.PruneExpired())
            {
                Emit(new ModifierExpired(positionQb, expired.Id, expired.OwnerId, expired.Value));
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
                if (effect.Trigger == EffectTrigger.OnPlay && effect.Modifier == EffectModifier.AddValue && ConditionHolds(effect.Condition, null))
                {
                    value += effect.Amount;
                }
            }

            foreach (var effect in card.Effects)
            {
                if (effect.Trigger == EffectTrigger.OnPlay && effect.Modifier == EffectModifier.MultiplyValue
                    && effect.Value == EffectValue.CardValue && ConditionHolds(effect.Condition, null))
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

                if (effect.Modifier == EffectModifier.AddValue
                    || (effect.Modifier == EffectModifier.MultiplyValue && effect.Value == EffectValue.CardValue))
                {
                    continue;
                }

                if (!ConditionHolds(effect.Condition, null))
                {
                    continue;
                }

                ApplyModifier(card.Id, effect, scale, opportunity.PositionQb, opportunity.Index);
            }
        }
    }
}
