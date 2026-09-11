using System;

namespace Chiki.Sim
{
    /// <summary>
    /// The player's run-wide stats (PRD 3.2.3): ARD, Base DMG, Essence and CRP.
    /// One instance lives for the whole run and is shared by every <see cref="Battle"/>.
    /// </summary>
    public sealed class RunStats
    {
        private int _ard;
        private int _essence;
        private int _crp;

        /// <summary>Maximum ARD; raised through <see cref="RaiseMaxArd"/>.</summary>
        public int MaxArd { get; private set; } = Tuning.ArdBaseline;

        /// <summary>Current ARD, clamped to 0..<see cref="MaxArd"/>.</summary>
        public int Ard
        {
            get => _ard;
            set => _ard = Clamp(value, 0, MaxArd);
        }

        /// <summary>Base DMG; every +1 adds +1 to every attack card (PRD 3.3.4.3).</summary>
        public int BaseDmg { get; set; }

        /// <summary>Essence (PRD 3.7.1); never below 0.</summary>
        public int Essence
        {
            get => _essence;
            set => _essence = value < 0 ? 0 : value;
        }

        /// <summary>CRP (PRD 3.8.1): starts at 0 and is clamped to 0..100 on every change.</summary>
        public int Crp
        {
            get => _crp;
            set => _crp = Clamp(value, Tuning.CrpMin, Tuning.CrpMax);
        }

        public RunStats()
        {
            _ard = MaxArd;
        }

        /// <summary>Stats as a saved run recorded them (PRD 4.2): the maximum is at least 1 and every value is clamped as on a change.</summary>
        public RunStats(int maxArd, int ard, int baseDmg, int essence, int crp)
        {
            MaxArd = Math.Max(1, maxArd);
            _ard = Clamp(ard, 0, MaxArd);
            BaseDmg = Math.Max(0, baseDmg);
            Essence = essence;
            Crp = crp;
        }

        /// <summary>Raises maximum ARD and current ARD by the same amount.</summary>
        public void RaiseMaxArd(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "amount must not be negative.");
            }

            MaxArd += amount;
            _ard = Clamp(_ard + amount, 0, MaxArd);
        }

        /// <summary>Changes maximum ARD by a signed amount, never below 1; current ARD rises with it and is clamped when it falls (PRD 3.2.3).</summary>
        public void ChangeMaxArd(int delta)
        {
            if (delta >= 0)
            {
                RaiseMaxArd(delta);
                return;
            }

            MaxArd = Math.Max(1, MaxArd + delta);
            _ard = Clamp(_ard, 0, MaxArd);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
