using System;

namespace Chiki.Sim
{
    /// <summary>
    /// A music track (PRD 4.14): World, encounter tier, length in beats, start offset and tempo map.
    /// The <see cref="BeatMap"/> derived from it gives the audio time of every quarter beat.
    /// </summary>
    public sealed record Track
    {
        public string Id { get; }
        public int World { get; }
        public EncounterTier Tier { get; }

        /// <summary>Length in beats; where the chart and track loop (PRD 3.6.32).</summary>
        public int LengthBeats { get; }

        /// <summary>Audio time of beat 0 in milliseconds from the start of the audio.</summary>
        public int OffsetMs { get; }

        public TempoMap Tempo { get; }

        /// <summary>Audio time of every beat and quarter beat (PRD 3.3.1.9).</summary>
        public BeatMap BeatMap { get; }

        /// <summary>Length in quarter beats.</summary>
        public int LengthQb => Beats.ToQuarterBeats(LengthBeats);

        public Track(string id, int world, EncounterTier tier, int lengthBeats, int offsetMs, TempoMap tempo)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Track id is required.", nameof(id));
            }

            if (lengthBeats <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthBeats), "Length must be at least one beat.");
            }

            if (offsetMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offsetMs), "Offset must not be negative.");
            }

            Tempo = tempo ?? throw new ArgumentNullException(nameof(tempo));
            if (tempo.Changes.Count > 0 && tempo.Changes[tempo.Changes.Count - 1].Beat >= lengthBeats)
            {
                throw new ArgumentException("Tempo changes must lie inside the track's length.", nameof(tempo));
            }

            Id = id;
            World = world;
            Tier = tier;
            LengthBeats = lengthBeats;
            OffsetMs = offsetMs;
            BeatMap = new BeatMap(this);
        }
    }
}
