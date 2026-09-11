#nullable enable
using System;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Scene
{
    /// <summary>
    /// Generated audio for tracks that have a JSON sidecar but no recording yet: a click on
    /// every beat of the track, accented every fourth beat, placed from the beat map so the
    /// clicks and the chart agree. One lap long, so the clock's looping keeps it on the map.
    /// </summary>
    public static class PlaceholderAudio
    {
        public const int SampleRate = 48000;

        public static AudioClip ClickTrack(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            int samples = checked((int)((long)(track.OffsetMs + track.BeatMap.LengthMs) * SampleRate / 1000));
            var data = new float[samples];
            for (int beat = 0; beat < track.LengthBeats; beat++)
            {
                int start = checked((int)((long)track.BeatMap.TimeAtBeat(beat) * SampleRate / 1000));
                bool accent = beat % 4 == 0;
                Click(data, start, accent ? 1500f : 1000f, accent ? 0.6f : 0.4f, 12);
            }

            var clip = AudioClip.Create("click-" + track.Id, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
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
