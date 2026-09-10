using System;
using System.Collections.Generic;

namespace Chiki.Sim.Effects
{
    /// <summary>An effect attached to an owner (a card, an enemy, a Charm) for the battle to evaluate.</summary>
    public sealed record RegisteredEffect(int Id, string OwnerId, EffectDefinition Definition);

    /// <summary>
    /// A standing modifier that a fired or passive effect left behind, alive for its lifetime:
    /// <see cref="Thousandths"/> on every value of kind <see cref="Value"/> for a multiplier, or
    /// <see cref="Bonus"/> added to it for an additive one, until it expires or is consumed.
    /// </summary>
    public sealed class ActiveModifier
    {
        public int Id { get; }

        public string OwnerId { get; }

        public EffectValue Value { get; }

        /// <summary>Whether the modifier adds <see cref="Bonus"/> rather than multiplying by <see cref="Thousandths"/>.</summary>
        public bool Additive { get; }

        /// <summary>The multiplier in thousandths; <see cref="Fixed.One"/> for an additive modifier.</summary>
        public int Thousandths { get; }

        /// <summary>The whole-number bonus; 0 for a multiplier.</summary>
        public int Bonus { get; }

        public EffectLifetime Lifetime { get; }

        /// <summary>The beat clock of a lifetime in beats; null for the other lifetimes.</summary>
        public BeatTimer? Timer { get; }

        public bool IsExpired => Timer != null && Timer.IsExpired;

        internal ActiveModifier(int id, string ownerId, EffectValue value, bool additive, int amount, EffectLifetime lifetime, BeatTimer? timer)
        {
            Id = id;
            OwnerId = ownerId;
            Value = value;
            Additive = additive;
            Thousandths = additive ? Fixed.One : amount;
            Bonus = additive ? amount : 0;
            Lifetime = lifetime;
            Timer = timer;
        }
    }

    /// <summary>
    /// The effects registered on a battle and the modifiers currently alive (P8.1). The battle
    /// evaluates a registered effect when its trigger event is appended to the stream and reads
    /// the alive bonuses and multipliers when it resolves damage. Only the battle mutates the registry.
    /// </summary>
    public sealed class EffectRegistry
    {
        private readonly List<RegisteredEffect> _registered = new List<RegisteredEffect>();
        private readonly List<ActiveModifier> _active = new List<ActiveModifier>();
        private int _nextId = 1;

        public IReadOnlyList<RegisteredEffect> Registered => _registered;

        public IReadOnlyList<ActiveModifier> ActiveModifiers => _active;

        /// <summary>The product of every alive multiplier on a value, in thousandths; <see cref="Fixed.One"/> when none.</summary>
        public int MultiplierFor(EffectValue value)
        {
            int product = Fixed.One;
            foreach (var modifier in _active)
            {
                if (modifier.Value == value && !modifier.Additive && !modifier.IsExpired)
                {
                    product = Fixed.Mul(product, modifier.Thousandths);
                }
            }

            return product;
        }

        /// <summary>The sum of every alive additive bonus on a value; 0 when none.</summary>
        public int BonusFor(EffectValue value)
        {
            int sum = 0;
            foreach (var modifier in _active)
            {
                if (modifier.Value == value && modifier.Additive && !modifier.IsExpired)
                {
                    sum += modifier.Bonus;
                }
            }

            return sum;
        }

        internal RegisteredEffect Add(string ownerId, EffectDefinition definition)
        {
            var registered = new RegisteredEffect(_nextId++, ownerId, definition);
            _registered.Add(registered);
            return registered;
        }

        internal bool Remove(int id)
        {
            return _registered.RemoveAll(r => r.Id == id) > 0;
        }

        /// <summary>A snapshot of the effects an event of the given trigger fires, so firing may register more.</summary>
        internal List<RegisteredEffect> TriggeredBy(EffectTrigger trigger)
        {
            var fired = new List<RegisteredEffect>();
            foreach (var registered in _registered)
            {
                if (registered.Definition.Trigger == trigger)
                {
                    fired.Add(registered);
                }
            }

            return fired;
        }

        /// <summary>
        /// A modifier already alive that a new activation of the same effect by the same owner
        /// would restart rather than stack: a lifetime in beats or until consumed (PRD 3.6.8,
        /// 3.6.9); battle- and run-long modifiers stack (PRD 3.6.5).
        /// </summary>
        internal ActiveModifier? Duplicate(string ownerId, EffectDefinition definition)
        {
            if (definition.Lifetime != EffectLifetime.Beats && definition.Lifetime != EffectLifetime.Consumed)
            {
                return null;
            }

            bool additive = definition.Modifier == EffectModifier.AddValue;
            foreach (var modifier in _active)
            {
                if (modifier.OwnerId == ownerId
                    && modifier.Value == definition.Value
                    && modifier.Additive == additive
                    && modifier.Lifetime == definition.Lifetime
                    && (additive ? modifier.Bonus : modifier.Thousandths) == definition.Amount
                    && !modifier.IsExpired)
                {
                    return modifier;
                }
            }

            return null;
        }

        internal ActiveModifier Activate(string ownerId, EffectDefinition definition, BeatTimer? timer)
        {
            var modifier = new ActiveModifier(
                _nextId++,
                ownerId,
                definition.Value!.Value,
                definition.Modifier == EffectModifier.AddValue,
                definition.Amount,
                definition.Lifetime,
                timer);
            _active.Add(modifier);
            return modifier;
        }

        internal bool Deactivate(int id)
        {
            return _active.RemoveAll(m => m.Id == id) > 0;
        }

        /// <summary>Drops the modifiers whose beats have run out, returning them in order.</summary>
        internal List<ActiveModifier> PruneExpired()
        {
            var expired = new List<ActiveModifier>();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].IsExpired)
                {
                    expired.Add(_active[i]);
                    _active.RemoveAt(i);
                }
            }

            expired.Reverse();
            return expired;
        }

        /// <summary>Drops the modifiers on a value that live until it is used, returning them in order; the value has just been used.</summary>
        internal List<ActiveModifier> Consume(EffectValue value)
        {
            var consumed = new List<ActiveModifier>();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Value == value && _active[i].Lifetime == EffectLifetime.Consumed)
                {
                    consumed.Add(_active[i]);
                    _active.RemoveAt(i);
                }
            }

            consumed.Reverse();
            return consumed;
        }
    }
}
