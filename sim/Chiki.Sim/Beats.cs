using System;

namespace Chiki.Sim
{
    /// <summary>
    /// Unit helpers for the beat framework (PRD 3.3.1.4, 3.3.1.9). Positions are integers in
    /// quarter beats; durations are integer beat counts; times are integer milliseconds.
    /// </summary>
    public static class Beats
    {
        /// <summary>Quarter-beat subdivisions in one beat (PRD 3.3.1.8).</summary>
        public const int QuarterBeatsPerBeat = 4;

        /// <summary>Converts whole beats to a quarter-beat position.</summary>
        public static int ToQuarterBeats(int beats)
        {
            return checked(beats * QuarterBeatsPerBeat);
        }

        /// <summary>Length of one beat at the given BPM in microseconds.</summary>
        public static long BeatMicroseconds(int bpm)
        {
            if (bpm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be positive.");
            }

            return 60_000_000L / bpm;
        }

        /// <summary>
        /// Converts a width expressed in thousandths of a beat to whole milliseconds at the
        /// given BPM, rounding once (PRD 3.3.1.5).
        /// </summary>
        public static int BeatFractionToMs(int beatThousandths, int bpm)
        {
            if (bpm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be positive.");
            }

            // beatMs * fraction / 1000 = 60000 * fraction / (bpm * 1000), computed in thousandths of a ms.
            return Fixed.Round(60_000L * beatThousandths / bpm);
        }
    }
}
