#nullable enable
using System;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Scene
{
    /// <summary>
    /// Generated audio for tracks that have a JSON sidecar but no recording yet: a click on
    /// every beat of the track, accented every fourth beat, placed from the beat map so the
    /// clicks and the chart agree. One lap long, so the clock's looping keeps it on the map.
    ///
    /// The clicks are the audio catalogue's <c>click-beat</c> and <c>click-accent</c> recordings
    /// when they have shipped (P1.4, P10.4), and generated tones only for the ids it lacks.
    /// </summary>
    public static class PlaceholderAudio
    {
        public const int SampleRate = 48000;
        public const string BeatClickId = "click-beat";
        public const string AccentClickId = "click-accent";

        public static AudioClip ClickTrack(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            int samples = checked((int)((long)(track.OffsetMs + track.BeatMap.LengthMs) * SampleRate / 1000));
            var data = new float[samples];
            var beatSample = Samples(AudioCatalogue.Active.Sound(BeatClickId));
            var accentSample = Samples(AudioCatalogue.Active.Sound(AccentClickId));

            for (int beat = 0; beat < track.LengthBeats; beat++)
            {
                int start = checked((int)((long)track.BeatMap.TimeAtBeat(beat) * SampleRate / 1000));
                bool accent = beat % 4 == 0;
                var recorded = accent ? accentSample : beatSample;
                if (recorded != null)
                {
                    Mix(data, start, recorded);
                }
                else
                {
                    Click(data, start, accent ? 1500f : 1000f, accent ? 0.6f : 0.4f, 12);
                }
            }

            var clip = AudioClip.Create("click-" + track.Id, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A clip's samples as one mono channel, or null when there is no clip to read.</summary>
        private static float[]? Samples(AudioClip? clip)
        {
            if (clip == null || clip.samples <= 0)
            {
                return null;
            }

            var interleaved = new float[clip.samples * clip.channels];
            if (!clip.GetData(interleaved, 0))
            {
                return null;
            }

            if (clip.channels == 1)
            {
                return interleaved;
            }

            var mono = new float[clip.samples];
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0f;
                for (int channel = 0; channel < clip.channels; channel++)
                {
                    sum += interleaved[i * clip.channels + channel];
                }

                mono[i] = sum / clip.channels;
            }

            return mono;
        }

        private static void Mix(float[] data, int start, float[] samples)
        {
            for (int i = 0; i < samples.Length && start + i < data.Length; i++)
            {
                data[start + i] += samples[i];
            }
        }

        private static void Click(float[] data, int start, float frequency, float amplitude, int milliseconds)
        {
            int length = SampleRate * milliseconds / 1000;
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = 1f - i / (float)length;
                data[start + i] += Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope * envelope;
            }
        }
    }
}
