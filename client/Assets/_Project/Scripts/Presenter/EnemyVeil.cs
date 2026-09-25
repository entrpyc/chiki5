#nullable enable
using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// Reads the battle's stream for the abilities that veil the enemy — Iron Veil today
    /// (PRD 3.6.9), any later ability that cuts the damage the enemy takes. A veil shows as a
    /// darker Rhythm Line and the ability's icon at the enemy bar, so both presenters ask the
    /// same question of the same events rather than each deciding for itself.
    /// </summary>
    internal static class EnemyVeil
    {
        /// <summary>Whether a modifier that has just come alive veils the enemy: a multiplier under one on the damage it takes.</summary>
        public static bool Veils(ModifierActivated activated)
        {
            return activated != null
                && activated.Value == EffectValue.EnemyDamageTaken
                && !activated.Additive
                && activated.Amount < Fixed.One;
        }

        /// <summary>
        /// The content id of the enemy's veiling ability, for its icon; null when this enemy has
        /// none. The ability is found by the effect it registers, so an ability added later needs
        /// no change here.
        /// </summary>
        public static string? AbilityId(Sim.Battle battle)
        {
            if (battle == null)
            {
                return null;
            }

            foreach (var ability in battle.Enemy.Abilities)
            {
                foreach (var effect in EnemyPowerEffects.Of(ability))
                {
                    if (effect.Value == EffectValue.EnemyDamageTaken
                        && effect.Modifier == EffectModifier.MultiplyValue
                        && effect.Amount < Fixed.One)
                    {
                        return EnemyLoader.AbilityToId(ability);
                    }
                }
            }

            return null;
        }
    }
}
