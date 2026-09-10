using System;

namespace Chiki.Sim
{
    /// <summary>
    /// The one seeded pseudo-random generator of the simulation (xorshift64*). It is passed
    /// explicitly and forked per subsystem so that the same seed and the same inputs
    /// replay identically (PRD 6.8). Never use <c>System.Random</c> in rule code.
    /// </summary>
    public sealed class Rng
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        private ulong _state;

        /// <summary>The seed this generator was created from.</summary>
        public ulong Seed { get; }

        public Rng(ulong seed)
        {
            Seed = seed;
            _state = SplitMix(seed);
            if (_state == 0)
            {
                _state = Golden; // xorshift must never sit at zero
            }
        }

        /// <summary>Creates a generator from a run seed string (PRD 3.2.4).</summary>
        public Rng(string seed) : this(Hash(seed))
        {
        }

        /// <summary>Draws the next raw 64-bit value.</summary>
        public ulong NextUInt64()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Draws an integer in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
            }

            ulong range = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextUInt64() % range));
        }

        /// <summary>
        /// Returns an independent generator for a subsystem. The fork is derived from this
        /// generator's seed and the label only, so it neither reads nor advances this
        /// generator's state, and forking the same label twice yields the same stream.
        /// </summary>
        public Rng Fork(string label)
        {
            return new Rng(Mix(Seed, Hash(label)));
        }

        private static ulong Hash(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // FNV-1a, 64-bit
            ulong hash = 14695981039346656037UL;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 1099511628211UL;
            }

            return hash;
        }

        private static ulong Mix(ulong a, ulong b)
        {
            return SplitMix(a ^ (b + Golden + (a << 6) + (a >> 2)));
        }

        private static ulong SplitMix(ulong x)
        {
            x += Golden;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }
}
