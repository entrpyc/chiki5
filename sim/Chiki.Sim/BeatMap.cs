using System;

namespace Chiki.Sim
{
    /// <summary>
    /// The audio time of every beat and quarter beat of a <see cref="Track"/>, derived from its
    /// tempo map (PRD 4.14, 3.3.1.9). Times are piecewise: each tempo segment contributes its
    /// quarter beats at that segment's BPM. Positions past the track's length continue into the
    /// next lap, since the track and its chart loop together from the start (PRD 3.6.32).
    /// </summary>
    public sealed class BeatMap
    {
        private readonly Track _track;

        /// <summary>Length of one lap of the track in milliseconds.</summary>
        public int LengthMs { get; }

        public BeatMap(Track track)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            LengthMs = Fixed.Round(MicrosecondsWithinLap(track.LengthQb));
        }

        /// <summary>Audio time in milliseconds of a position in quarter beats.</summary>
        public int TimeAtQb(int positionQb)
        {
            if (positionQb < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionQb), "Position must not be negative.");
            }

            int lengthQb = _track.LengthQb;
            int lap = positionQb / lengthQb;
            int withinLap = positionQb % lengthQb;
            long ms = _track.OffsetMs + (long)lap * LengthMs + Fixed.Round(MicrosecondsWithinLap(withinLap));
            return checked((int)ms);
        }

        /// <summary>Audio time in milliseconds of a whole beat.</summary>
        public int TimeAtBeat(int beat)
        {
            return TimeAtQb(Beats.ToQuarterBeats(beat));
        }

        /// <summary>The BPM in force at a position (PRD 3.3.1.9); positions wrap per lap.</summary>
        public int BpmAt(int positionQb)
        {
            if (positionQb < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionQb), "Position must not be negative.");
            }

            return _track.Tempo.BpmAt(positionQb % _track.LengthQb);
        }

        private long MicrosecondsWithinLap(int positionQb)
        {
            var tempo = _track.Tempo;
            long micros = 0;
            int segmentStartQb = 0;
            int bpm = tempo.StartBpm;

            foreach (var change in tempo.Changes)
            {
                int changeQb = Beats.ToQuarterBeats(change.Beat);
                if (changeQb >= positionQb)
                {
                    break;
                }

                micros += QuarterBeatsToMicroseconds(changeQb - segmentStartQb, bpm);
                segmentStartQb = changeQb;
                bpm = change.Bpm;
            }

            micros += QuarterBeatsToMicroseconds(positionQb - segmentStartQb, bpm);
            return micros;
        }

        private static long QuarterBeatsToMicroseconds(int quarterBeats, int bpm)
        {
            return (long)quarterBeats * Beats.BeatMicroseconds(bpm) / Beats.QuarterBeatsPerBeat;
        }
    }
}
