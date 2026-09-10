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

        /// <summary>CRP (PRD 3.8.1); clamping is delegated to P17.6.</summary>
        public int Crp { get; set; }

        public RunStats()
        {
            _ard = MaxArd;
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

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
