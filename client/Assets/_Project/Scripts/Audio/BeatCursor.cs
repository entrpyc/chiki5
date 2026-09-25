#nullable enable
using System;
using Chiki.Sim;

namespace Chiki.Client.Audio
{
    /// <summary>
    /// Converts audio time to a position in quarter beats on a <see cref="BeatMap"/>, keeping a
    /// cursor so the common case of time moving forward costs one comparison. Tempo changes are
    /// the beat map's business; a caller that walks forward through a track gets the positions
    /// the simulation would give (PRD 3.3.1.9).
    /// </summary>
    public sealed class BeatCursor
    {
        private readonly BeatMap _map;
        private int _qb;

        public BeatCursor(BeatMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public BeatMap Map => _map;

        /// <summary>The whole quarter beats elapsed at an audio time; 0 before the track's first beat.</summary>
        public int QbAt(int audioTimeMs)
        {
            if (audioTimeMs <= _map.TimeAtQb(0))
            {
                _qb = 0;
                return 0;
            }

            if (audioTimeMs < _map.TimeAtQb(_qb))
            {
                _qb = 0;
            }

            while (_map.TimeAtQb(_qb + 1) <= audioTimeMs)
            {
                _qb++;
            }

            return _qb;
        }

        /// <summary>
        /// The continuous beat an audio time falls on: whole quarter beats plus the fraction of
        /// the current one, so anything drawn between beats moves smoothly (P3.2).
        /// </summary>
        public float BeatAt(int audioTimeMs)
        {
            int qb = QbAt(audioTimeMs);
            int start = _map.TimeAtQb(qb);
            int end = _map.TimeAtQb(qb + 1);
            float fraction = end > start ? Math.Max(0, audioTimeMs - start) / (float)(end - start) : 0f;
            return (qb + fraction) / Beats.QuarterBeatsPerBeat;
        }
    }
}
