using System;
using System.Collections.Generic;

namespace Chiki.Sim.Effects
{
    /// <summary>
    /// What each enemy ability and trait does, as framework effects registered on the enemy at
    /// battle start (PRD 3.6.4). Seven powers act in this plan (P11): the rest return no effects
    /// until their plan item lands. Charge / Buff (PRD 3.6.16) acts through the chart's Charge
    /// actions (PRD 3.6.31), so it registers nothing here.
    /// </summary>
    public static class EnemyPowerEffects
    {
        public static IReadOnlyList<EffectDefinition> Of(EnemyAbility ability)
        {
            switch (ability)
            {
                case EnemyAbility.RisingTempo:
                    // PRD 3.6.5: each time this enemy deals damage, +3 Base DMG for the rest of the battle.
                    return new[]
                    {
                        new EffectDefinition(
                            EffectTrigger.DamageTaken,
                            EffectModifier.AddValue,
                            Tuning.RisingTempoBaseDmgPerHit,
                            EffectCondition.IfDamageLanded,
                            StatusTarget.Enemy,
                            value: EffectValue.EnemyDamage,
                            lifetime: EffectLifetime.Battle),
                    };

                case EnemyAbility.MisstepPain:
                    // PRD 3.6.6: 5 damage on a Good and 10 on a Miss, after the action's own damage; no input is exempt.
                    return new[]
                    {
                        new EffectDefinition(EffectTrigger.ActionResolved, EffectModifier.DealDamage, Tuning.MisstepPainGoodDamage, EffectCondition.OnGood, StatusTarget.Player),
                        new EffectDefinition(EffectTrigger.ActionResolved, EffectModifier.DealDamage, Tuning.MisstepPainMissDamage, EffectCondition.OnMiss, StatusTarget.Player),
                    };

                case EnemyAbility.Pressure:
                    // PRD 3.6.8: after an action with no input, the next attack deals 2x; consumed by that attack.
                    return new[]
                    {
                        new EffectDefinition(
                            EffectTrigger.ActionResolved,
                            EffectModifier.MultiplyValue,
                            Tuning.PressureMultiplierThousandths,
                            EffectCondition.IfNoInput,
                            StatusTarget.Enemy,
                            value: EffectValue.EnemyDamage,
                            lifetime: EffectLifetime.Consumed),
                    };

                case EnemyAbility.IronVeil:
                    // PRD 3.6.9: on the enemy's Buff, 80% less damage taken for 5 beats.
                    return new[]
                    {
                        new EffectDefinition(
                            EffectTrigger.ActionResolved,
                            EffectModifier.MultiplyValue,
                            Fixed.One - Tuning.IronVeilReductionThousandths,
                            EffectCondition.IfBuffAction,
                            StatusTarget.Enemy,
                            value: EffectValue.EnemyDamageTaken,
                            lifetime: EffectLifetime.Beats,
                            lifetimeBeats: Tuning.IronVeilBeats),
                    };

                case EnemyAbility.ChargeBuff:
                    return Array.Empty<EffectDefinition>();

                case EnemyAbility.Counterblade:
                case EnemyAbility.BackflashBarrier:
                case EnemyAbility.FakeMove:
                case EnemyAbility.HeavyHand:
                case EnemyAbility.BeatRush:
                case EnemyAbility.ChainBreaker:
                case EnemyAbility.DoubleStep:
                case EnemyAbility.FrenzyMode:
                case EnemyAbility.CorruptionAegis:
                    return Array.Empty<EffectDefinition>();

                default:
                    throw new ArgumentOutOfRangeException(nameof(ability), ability, "Unknown ability.");
            }
        }

        public static IReadOnlyList<EffectDefinition> Of(EnemyTrait trait)
        {
            switch (trait)
            {
                case EnemyTrait.Stoneform:
                    // PRD 3.6.20: three consecutive beats without taking damage grant +10 Block.
                    return new[]
                    {
                        new EffectDefinition(
                            EffectTrigger.BeatEnded,
                            EffectModifier.GainBlock,
                            Tuning.StoneformBlock,
                            EffectCondition.IfEnemyQuietBeats,
                            StatusTarget.Enemy,
                            conditionAmount: Tuning.StoneformQuietBeats),
                    };

                case EnemyTrait.Guard:
                    // PRD 3.6.25: starts combat with 30 Block.
                    return new[]
                    {
                        new EffectDefinition(EffectTrigger.BattleStarted, EffectModifier.GainBlock, Tuning.GuardBlock, target: StatusTarget.Enemy),
                    };

                case EnemyTrait.AccuracyBet:
                case EnemyTrait.AbsorbShell:
                case EnemyTrait.PureHeart:
                case EnemyTrait.BloodLeech:
                case EnemyTrait.ThornsShell:
                    return Array.Empty<EffectDefinition>();

                default:
                    throw new ArgumentOutOfRangeException(nameof(trait), trait, "Unknown trait.");
            }
        }
    }
}
