#nullable enable
using System;
using Chiki.Client.Scene;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Audio
{
    /// <summary>
    /// What a track sounds like (P1.6, PRD 4.14): the recording the audio catalogue holds under
    /// the track's id, or the generated click track with a warning naming the track when no
    /// recording has shipped. The sidecar's offset puts beat 0 that many milliseconds into the
    /// clip and its tempo map places every later beat (PRD 3.3.1.9), so the same beat map serves
    /// a recording and a click track alike.
    /// </summary>
    public static class TrackAudio
    {
        /// <summary>The clip to schedule on the beat clock for a track.</summary>
        public static AudioClip For(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var recorded = AudioCatalogue.Active.Track(track.Id);
            if (recorded != null)
            {
                return recorded;
            }

            Debug.LogWarning("Chiki: no recording has shipped for track '" + track.Id + "'; the generated click track is playing instead.");
            return PlaceholderAudio.ClickTrack(track);
        }

        /// <summary>Whether a track plays a recording rather than the generated click track.</summary>
        public static bool IsRecorded(Track track)
        {
            return track != null && AudioCatalogue.Active.HasTrack(track.Id);
        }
    }
}
