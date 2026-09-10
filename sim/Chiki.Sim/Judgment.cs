using System;

namespace Chiki.Sim
{
    /// <summary>The grade of a pressed input (PRD 3.3.3.1). A Miss is only ever the grade of a press.</summary>
    public enum Judgment
    {
        Perfect,
        Good,
        Miss,
    }

    /// <summary>
    /// Judgment Window arithmetic (PRD 3.3.3.1, 3.3.1.5). Widths are stored as fractions of a
    /// beat in <see cref="Tuning"/> and converted to milliseconds at the BPM in force.
    /// </summary>
    public static class JudgmentWindow
    {
        /// <summary>Half-width of the Perfect window in milliseconds at the given BPM.</summary>
        public static int PerfectHalfWidthMs(int bpm)
        {
            return Beats.BeatFractionToMs(Tuning.PerfectWindowBeatThousandths, bpm);
        }

        /// <summary>Half-width of the Good window in milliseconds at the given BPM.</summary>
        public static int GoodHalfWidthMs(int bpm)
        {
            return Beats.BeatFractionToMs(Tuning.GoodWindowBeatThousandths, bpm);
        }

        /// <summary>Half-width of the whole Judgment Window (accept region) in milliseconds at the given BPM.</summary>
        public static int AcceptHalfWidthMs(int bpm)
        {
            return Beats.BeatFractionToMs(Tuning.JudgmentWindowBeatThousandths, bpm);
        }

        /// <summary>
        /// Grades a press by its signed offset from the window centre: Perfect inside the inner
        /// window, Good inside the outer window, Miss outside both.
        /// </summary>
        public static Judgment Grade(int offsetMs, int bpm)
        {
            int distance = Math.Abs(offsetMs);
            if (distance <= PerfectHalfWidthMs(bpm))
            {
                return Judgment.Perfect;
            }

            return distance <= GoodHalfWidthMs(bpm) ? Judgment.Good : Judgment.Miss;
        }
    }
}
