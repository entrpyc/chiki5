using System;

namespace Chiki.Sim
{
    /// <summary>The eight slot keys of one line, by physical position (PRD 3.3.2.1, 3.3.2.5).</summary>
    public enum SlotKey
    {
        A,
        S,
        D,
        F,
        J,
        K,
        L,
        Semicolon,
    }

    /// <summary>One of the sixteen slots: a key on one of the two lines (PRD 3.3.2.1, 3.5.1).</summary>
    public sealed record Slot
    {
        public const int LineCount = 2;

        /// <summary>0 for the upper line, 1 for the lower line.</summary>
        public int Line { get; }

        public SlotKey Key { get; }

        public Slot(int line, SlotKey key)
        {
            if (line < 0 || line >= LineCount)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "Line must be 0 or 1.");
            }

            Line = line;
            Key = key;
        }
    }
}
