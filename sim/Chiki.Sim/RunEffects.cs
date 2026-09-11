using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>An effect the run holds on behalf of an owner (an Imprint or a Charm).</summary>
    public sealed record RegisteredRunEffect(int Id, string OwnerId, EffectDefinition Definition);

    /// <summary>A standing modifier that fired "until reset" and outlives its battle (PRD 3.9.8).</summary>
    public sealed record UntilResetModifier(string OwnerId, EffectDefinition Passive);

    /// <summary>
    /// The run-level effect registry (P18.3, P18.4): the effects of every Imprint held and Charm
    /// equipped. An <see cref="EffectTrigger.Acquired"/> effect fires once, on the run's stats,
    /// when its owner is acquired; every other effect is attached to each battle at its start,
    /// and a standing modifier with a run lifetime that a battle leaves alive is carried into
    /// the next battle until the run ends. A Charm's per-run cap limits what its stat changes
    /// may total across the run's battles.
    /// </summary>
    public sealed class RunEffects
    {
        private readonly List<RegisteredRunEffect> _registered = new List<RegisteredRunEffect>();
        private readonly List<UntilResetModifier> _untilReset = new List<UntilResetModifier>();
        private readonly Dictionary<string, int> _charmStatTotals = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly RunStats _stats;
        private readonly RunContent _content;
        private int _nextId = 1;

        public IReadOnlyList<RegisteredRunEffect> Registered => _registered;

        public IReadOnlyList<UntilResetModifier> UntilReset => _untilReset;

        internal RunEffects(RunStats stats, RunContent content)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>How much a Charm's stat changes have totalled this run, for its cap.</summary>
        public int StatTotalOf(string charmId)
        {
            return _charmStatTotals.TryGetValue(charmId, out int total) ? total : 0;
        }

        /// <summary>
        /// Registers an owner's effects. With <paramref name="fireAcquired"/> the acquisition
        /// effects change the run's stats now (PRD 3.9.3 "+2 Base DMG"); a restored run passes
        /// false because its stats already reflect them.
        /// </summary>
        internal List<RegisteredRunEffect> Add(string ownerId, IReadOnlyList<EffectDefinition> effects, bool fireAcquired)
        {
            var added = new List<RegisteredRunEffect>();
            foreach (var effect in effects)
            {
                var registered = new RegisteredRunEffect(_nextId++, ownerId, effect);
                _registered.Add(registered);
                added.Add(registered);
                if (effect.Trigger == EffectTrigger.Acquired && fireAcquired)
                {
                    FireAcquired(effect);
                }
            }

            return added;
        }

        /// <summary>Attaches every battle-triggered effect and every until-reset modifier to a battle that has just started (P18.4).</summary>
        internal void AttachTo(Battle battle)
        {
            foreach (var registered in _registered)
            {
                if (registered.Definition.Trigger == EffectTrigger.Acquired)
                {
                    continue;
                }

                var effect = CapForCharm(registered.OwnerId, registered.Definition);
                if (effect != null)
                {
                    battle.RegisterEffect(registered.OwnerId, effect);
                }
            }

            foreach (var carried in _untilReset)
            {
                battle.RegisterEffect(carried.OwnerId, carried.Passive);
            }
        }

        /// <summary>Reads what an ended battle leaves behind: the run-long modifiers still alive and the stat changes each Charm made.</summary>
        internal void Collect(Battle battle)
        {
            _untilReset.Clear();
            foreach (var modifier in battle.Effects.ActiveModifiers)
            {
                if (modifier.Lifetime == EffectLifetime.Run && !modifier.IsExpired)
                {
                    _untilReset.Add(new UntilResetModifier(modifier.OwnerId, new EffectDefinition(
                        EffectTrigger.Passive,
                        modifier.Additive ? EffectModifier.AddValue : EffectModifier.MultiplyValue,
                        modifier.Additive ? modifier.Bonus : modifier.Thousandths,
                        target: StatusTarget.Player,
                        value: modifier.Value,
                        lifetime: EffectLifetime.Run)));
                }
            }

            foreach (var battleEvent in battle.Events)
            {
                if (battleEvent is StatChanged changed && changed.OwnerId != null && _content.FindCharm(changed.OwnerId) != null)
                {
                    _charmStatTotals[changed.OwnerId] = StatTotalOf(changed.OwnerId) + changed.Delta;
                }
            }
        }

        /// <summary>Everything the run held is lost with it (PRD 3.9.1, 3.9.3, 3.9.8).</summary>
        internal void Clear()
        {
            _registered.Clear();
            _untilReset.Clear();
            _charmStatTotals.Clear();
        }

        /// <summary>A Charm's stat change trimmed to what its per-run cap still allows; null once the cap is reached.</summary>
        private EffectDefinition? CapForCharm(string ownerId, EffectDefinition effect)
        {
            var charm = _content.FindCharm(ownerId);
            if (charm?.RunCap is null || effect.Modifier != EffectModifier.ChangeStat || effect.Amount <= 0)
            {
                return effect;
            }

            int remaining = charm.RunCap.Value - StatTotalOf(ownerId);
            if (remaining <= 0)
            {
                return null;
            }

            if (effect.Amount <= remaining)
            {
                return effect;
            }

            return new EffectDefinition(
                effect.Trigger,
                effect.Modifier,
                remaining,
                effect.Condition,
                effect.Target,
                stat: effect.Stat,
                conditionAmount: effect.ConditionAmount);
        }

        private void FireAcquired(EffectDefinition effect)
        {
            if (effect.Modifier != EffectModifier.ChangeStat)
            {
                throw new InvalidOperationException("An acquisition effect changes a run stat; anything else needs a battle.");
            }

            switch (effect.Stat!.Value)
            {
                case RunStat.Ard:
                    _stats.Ard += effect.Amount;
                    break;
                case RunStat.MaxArd:
                    _stats.ChangeMaxArd(effect.Amount);
                    break;
                case RunStat.BaseDmg:
                    _stats.BaseDmg = Math.Max(0, _stats.BaseDmg + effect.Amount);
                    break;
                case RunStat.Essence:
                    _stats.Essence += effect.Amount;
                    break;
                case RunStat.Crp:
                    _stats.Crp += effect.Amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect.Stat, "Unknown stat.");
            }
        }
    }
}
