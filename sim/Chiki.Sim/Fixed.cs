namespace Chiki.Sim
{
    /// <summary>
    /// Fixed-point arithmetic in thousandths. Rule code multiplies in thousandths and
    /// calls <see cref="Round"/> exactly once at the end; halves round up (PRD 3.3.4.5).
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
            long shifted = thousandths + One / 2;
            long quotient = shifted / One;
            if (shifted < 0 && shifted % One != 0)
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
