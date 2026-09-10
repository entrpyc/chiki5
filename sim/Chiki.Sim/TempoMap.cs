using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>A BPM change applying from a beat onward (PRD 3.3.1.9).</summary>
    public sealed record TempoChange(int Beat, int Bpm);

    /// <summary>
    /// A track's tempo map (PRD 4.14, 3.3.1.9): a starting BPM and BPM changes at beat positions.
    /// </summary>
    public sealed record TempoMap
    {
        public int StartBpm { get; }

        /// <summary>Changes in ascending beat order; a change at beat n applies from beat n onward.</summary>
        public IReadOnlyList<TempoChange> Changes { get; }

        public TempoMap(int startBpm) : this(startBpm, Array.Empty<TempoChange>())
        {
        }

        public TempoMap(int startBpm, IReadOnlyList<TempoChange> changes)
        {
            if (startBpm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startBpm), "BPM must be positive.");
            }

            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            int lastBeat = 0;
            foreach (var change in changes)
            {
                if (change.Bpm <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(changes), "BPM must be positive.");
                }

                if (change.Beat <= lastBeat)
                {
                    throw new ArgumentException("Tempo changes must be at strictly increasing beats after beat 0.", nameof(changes));
                }

                lastBeat = change.Beat;
            }

            StartBpm = startBpm;
            Changes = changes;
        }

        /// <summary>The BPM in force at a quarter-beat position within one lap of the track.</summary>
        public int BpmAt(int positionQb)
        {
            if (positionQb < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionQb), "Position must not be negative.");
            }

            int bpm = StartBpm;
            foreach (var change in Changes)
            {
                if (Beats.ToQuarterBeats(change.Beat) <= positionQb)
                {
                    bpm = change.Bpm;
                }
                else
                {
                    break;
                }
            }

            return bpm;
        }
    }
}
