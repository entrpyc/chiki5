using System;
using System.Collections.Generic;

namespace Chiki.Sim.Effects
{
    /// <summary>An effect attached to an owner (a card, an enemy, a Charm) for the battle to evaluate.</summary>
    public sealed record RegisteredEffect(int Id, string OwnerId, EffectDefinition Definition);

    /// <summary>
    /// A standing multiplier that a fired or passive effect left behind, alive for its lifetime:
    /// <see cref="Thousandths"/> on every value of kind <see cref="Value"/> until it expires.
    /// </summary>
    public sealed class ActiveModifier
    {
        public int Id { get; }

        public string OwnerId { get; }

        public EffectValue Value { get; }

        public int Thousandths { get; }

        public EffectLifetime Lifetime { get; }

        /// <summary>The beat clock of a lifetime in beats; null for a battle- or run-long modifier.</summary>
        public BeatTimer? Timer { get; }

        public bool IsExpired => Timer != null && Timer.IsExpired;

        internal ActiveModifier(int id, string ownerId, EffectValue value, int thousandths, EffectLifetime lifetime, BeatTimer? timer)
        {
            Id = id;
            OwnerId = ownerId;
            Value = value;
            Thousandths = thousandths;
            Lifetime = lifetime;
            Timer = timer;
        }
    }

    /// <summary>
    /// The effects registered on a battle and the modifiers currently alive (P8.1). The battle
    /// evaluates a registered effect when its trigger event is appended to the stream and reads
    /// the alive multipliers when it resolves damage. Only the battle mutates the registry.
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
                if (modifier.Value == value && !modifier.IsExpired)
                {
                    product = Fixed.Mul(product, modifier.Thousandths);
                }
            }

            return product;
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

        internal ActiveModifier Activate(string ownerId, EffectDefinition definition, BeatTimer? timer)
        {
            var modifier = new ActiveModifier(_nextId++, ownerId, definition.Value!.Value, definition.Amount, definition.Lifetime, timer);
            _active.Add(modifier);
            return modifier;
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
    }
}
