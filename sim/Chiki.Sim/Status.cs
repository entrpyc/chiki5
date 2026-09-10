using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>The statuses in scope (PRD 3.3.7.3–3.3.7.7).</summary>
    public enum StatusKind
    {
        Scar,
        Weak,
        Stun,
        Bleed,
        Thorns,
    }

    /// <summary>Which side a status sits on (PRD 3.3.7.1).</summary>
    public enum StatusTarget
    {
        Player,
        Enemy,
    }

    /// <summary>The moments of a beat at which statuses act, in the order they occur (PRD 3.3.7.2).</summary>
    public enum StatusMoment
    {
        /// <summary>An enemy action arrives, before any damage: Stun and the damage multipliers.</summary>
        ActionArrives,

        /// <summary>An enemy attack has landed on the player: Thorns.</summary>
        AttackLanded,

        /// <summary>The beat ends: damage over time.</summary>
        BeatEnd,
    }

    /// <summary>
    /// The one table of same-beat status priority (PRD 3.3.7.2): Stun, then damage multipliers
    /// (Weak), then Reflect and Thorns, then damage over time (Bleed) at beat end. The battle
    /// walks this table at each moment of a beat; a status acts at the moment the table gives
    /// it. Scar acts on the player's own hit (PRD 3.3.7.3) and is not a same-beat trigger.
    /// </summary>
    public static class StatusPriority
    {
        public static readonly IReadOnlyList<StatusKind> SameBeatOrder = new[]
        {
            StatusKind.Stun,
            StatusKind.Weak,
            StatusKind.Thorns,
            StatusKind.Bleed,
        };

        /// <summary>The moment of the beat at which a same-beat status acts.</summary>
        public static StatusMoment MomentOf(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Stun:
                case StatusKind.Weak:
                    return StatusMoment.ActionArrives;
                case StatusKind.Thorns:
                    return StatusMoment.AttackLanded;
                case StatusKind.Bleed:
                    return StatusMoment.BeatEnd;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a same-beat status.");
            }
        }

        /// <summary>
        /// Beats a fresh application lasts (PRD 3.3.7.3, 3.3.7.4, 3.3.7.6), or null for a status
        /// that lasts until consumed: Stun for one action beat (PRD 3.3.7.5), Thorns until an
        /// attack takes it (PRD 3.3.7.7).
        /// </summary>
        public static int? DurationBeats(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Scar:
                    return Tuning.ScarStackBeats;
                case StatusKind.Weak:
                    return Tuning.WeakBeats;
                case StatusKind.Bleed:
                    return Tuning.BleedBeats;
                case StatusKind.Stun:
                case StatusKind.Thorns:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown status.");
            }
        }
    }

    /// <summary>
    /// One applied status on one side (PRD 3.3.7.1). <see cref="Value"/> is the per-source
    /// number where the status has one: Weak's X in thousandths (PRD 3.3.7.4), Thorns' damage
    /// (PRD 3.3.7.7). <see cref="RemainingBeats"/> is null for a status that lasts until consumed.
    /// </summary>
    public sealed class StatusInstance
    {
        public StatusKind Kind { get; }

        public int Stacks { get; internal set; }

        public int Value { get; internal set; }

        public int? RemainingBeats { get; internal set; }

        internal StatusInstance(StatusKind kind, int stacks, int value, int? remainingBeats)
        {
            Kind = kind;
            Stacks = stacks;
            Value = value;
            RemainingBeats = remainingBeats;
        }
    }

    /// <summary>
    /// The statuses on one side of a battle (PRD 3.3.7.1, 4.8). Stacks are additive and a
    /// reapplication resets the timer to the new source's full duration; the exceptions the
    /// table states are Scar, whose every application keeps its own 10-beat clock
    /// (PRD 3.3.7.3), and Thorns, whose every source keeps its own damage until consumed
    /// (PRD 3.3.7.7). Only the battle mutates a set.
    /// </summary>
    public sealed class StatusSet
    {
        private readonly List<StatusInstance> _instances = new List<StatusInstance>();

        public IReadOnlyList<StatusInstance> Instances => _instances;

        public bool Has(StatusKind kind)
        {
            return Find(kind) != null;
        }

        /// <summary>Total stacks of a status across its instances.</summary>
        public int Stacks(StatusKind kind)
        {
            int total = 0;
            foreach (var instance in _instances)
            {
                if (instance.Kind == kind)
                {
                    total += instance.Stacks;
                }
            }

            return total;
        }

        /// <summary>The longest remaining duration of a status in beats; 0 when absent or untimed.</summary>
        public int RemainingBeats(StatusKind kind)
        {
            int longest = 0;
            foreach (var instance in _instances)
            {
                if (instance.Kind == kind && instance.RemainingBeats.HasValue)
                {
                    longest = Math.Max(longest, instance.RemainingBeats.Value);
                }
            }

            return longest;
        }

        /// <summary>Total −X% of Weak on this side in thousandths, capped at 100% (PRD 3.3.7.4).</summary>
        public int WeakThousandths
        {
            get
            {
                var weak = Find(StatusKind.Weak);
                return weak is null ? 0 : Math.Min(Fixed.One, weak.Value);
            }
        }

        /// <summary>The StatusMults term for damage this side deals: 1 − Weak (PRD 3.3.4.2, 3.3.4.3, 3.3.7.4).</summary>
        public int DealtMultiplierThousandths => Fixed.One - WeakThousandths;

        internal StatusInstance Apply(StatusKind kind, int stacks, int value)
        {
            int? duration = StatusPriority.DurationBeats(kind);
            switch (kind)
            {
                case StatusKind.Scar:
                case StatusKind.Thorns:
                {
                    var fresh = new StatusInstance(kind, stacks, value, duration);
                    _instances.Add(fresh);
                    return fresh;
                }

                case StatusKind.Stun:
                {
                    // One skipped action beat; a second Stun before it is consumed adds nothing.
                    var existing = Find(kind);
                    if (existing != null)
                    {
                        return existing;
                    }

                    var fresh = new StatusInstance(kind, 1, 0, null);
                    _instances.Add(fresh);
                    return fresh;
                }

                case StatusKind.Weak:
                case StatusKind.Bleed:
                {
                    var existing = Find(kind);
                    if (existing is null)
                    {
                        var fresh = new StatusInstance(kind, stacks, value, duration);
                        _instances.Add(fresh);
                        return fresh;
                    }

                    existing.Stacks += stacks;
                    existing.Value += value;
                    existing.RemainingBeats = duration;
                    return existing;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown status.");
            }
        }

        /// <summary>
        /// Consumes one stack of the oldest instance of a status; <paramref name="value"/> is that
        /// instance's per-source value and <paramref name="stacksLeft"/> the kind's total afterwards.
        /// </summary>
        internal bool ConsumeOne(StatusKind kind, out int value, out int stacksLeft)
        {
            var instance = Find(kind);
            if (instance is null)
            {
                value = 0;
                stacksLeft = 0;
                return false;
            }

            value = instance.Value;
            instance.Stacks--;
            if (instance.Stacks == 0)
            {
                _instances.Remove(instance);
            }

            stacksLeft = Stacks(kind);
            return true;
        }

        /// <summary>Ticks every timed status down one beat and removes the expired ones, returning them.</summary>
        internal List<StatusInstance> Tick()
        {
            var expired = new List<StatusInstance>();
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var instance = _instances[i];
                if (!instance.RemainingBeats.HasValue)
                {
                    continue;
                }

                instance.RemainingBeats--;
                if (instance.RemainingBeats == 0)
                {
                    _instances.RemoveAt(i);
                    expired.Add(instance);
                }
            }

            expired.Reverse();
            return expired;
        }

        private StatusInstance? Find(StatusKind kind)
        {
            foreach (var instance in _instances)
            {
                if (instance.Kind == kind)
                {
                    return instance;
                }
            }

            return null;
        }
    }
}
