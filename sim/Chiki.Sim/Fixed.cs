using System;

namespace Chiki.Sim
{
    /// <summary>
    /// Fixed-point arithmetic in thousandths. Rule code multiplies in thousandths and
    /// calls <see cref="Round(long)"/> exactly once at the end; halves round up (PRD 3.3.4.5).
    /// </summary>
    public static class Fixed
    {
        /// <summary>One whole unit expressed in thousandths.</summary>
        public const int One = 1000;

        /// <summary>
        /// The single rounding helper. Converts a value in thousandths to whole units,
        /// rounding to nearest with halves rounding up (toward positive infinity).
        /// </summary>
        public static int Round(long thousandths)
        {
            return Round(thousandths, One);
        }

        /// <summary>
        /// The same rounding for a value whose whole unit is <paramref name="scale"/> (for a
        /// product of several thousandths multipliers, a power of 1000). Rounds to nearest with
        /// halves rounding up; the division happens once, so no intermediate truncation occurs.
        /// </summary>
        public static int Round(long value, long scale)
        {
            if (scale <= 0 || scale % 2 != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be a positive even number.");
            }

            long shifted = value + scale / 2;
            long quotient = shifted / scale;
            if (shifted < 0 && shifted % scale != 0)
            {
                quotient--; // floor division for negatives
            }

            return checked((int)quotient);
        }

        /// <summary>Multiplies a whole value by a multiplier in thousandths and rounds once.</summary>
        public static int Mul(int value, int multiplierThousandths)
        {
            return Round((long)value * multiplierThousandths);
        }
    }
}
