#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chiki.Client.Audio
{
    /// <summary>One click the metronome scheduled: the beat, its audio time on the beat map and the DSP time it plays at.</summary>
    public sealed record ScheduledClick(int Beat, int AudioTimeMs, double DspTime);

    /// <summary>
    /// The optional metronome (PRD 3.12.2, 3.3.8.2): a click on every beat of the track, placed
    /// on the DSP clock at the beat map's time through the <see cref="BeatClock"/>, never from
    /// frame time. Clicks play through sources of their own, never the track's (PRD 3.3.1.6).
    /// Each clock tick schedules the beats that fall inside the lookahead; a beat already
    /// behind the clock when it is reached is skipped, never played late.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Metronome : MonoBehaviour
    {
        public const int LookaheadMs = 250;
        private const int SampleRate = 48000;
        private const int SourceCount = 2;

        private readonly List<ScheduledClick> _scheduled = new List<ScheduledClick>();
        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private AudioClip? _click;
        private BeatClock? _clock;
        private int _nextBeat = -1;
        private int _nextSource;
        private bool _on;
        private float _volume = 1f;

        public BeatClock? Clock => _clock;

        /// <summary>The toggle (PRD 3.12.2). Turning it on resynchronises to the next beat.</summary>
        public bool On
        {
            get => _on;
            set
            {
                if (_on == value)
                {
                    return;
                }

                _on = value;
                _nextBeat = -1;
            }
        }

        /// <summary>The metronome's own volume, 0..1 (PRD 3.12.4).</summary>
        public float Volume
        {
            get => _volume;
            set => _volume = Mathf.Clamp01(value);
        }

        /// <summary>Every click scheduled so far, in beat order.</summary>
        public IReadOnlyList<ScheduledClick> Scheduled => _scheduled;

        /// <summary>The sources the clicks play through.</summary>
        public IReadOnlyList<AudioSource> Sources => _sources;

        public void Bind(BeatClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (_clock != null)
            {
                throw new InvalidOperationException("The metronome is already bound to a clock.");
            }

            _clock = clock;
            _clock.Ticked += OnTick;
        }

        private void OnTick(int nowMs)
        {
            if (!_on || _clock == null || _clock.Track == null)
            {
                return;
            }

            var map = _clock.Track.BeatMap;
            if (_nextBeat < 0)
            {
                _nextBeat = FirstBeatAtOrAfter(map, nowMs);
            }

            while (map.TimeAtBeat(_nextBeat) <= nowMs + LookaheadMs)
            {
                int beatMs = map.TimeAtBeat(_nextBeat);
                if (beatMs >= nowMs)
                {
                    Schedule(_nextBeat, beatMs);
                }

                _nextBeat++;
            }
        }

        private static int FirstBeatAtOrAfter(Chiki.Sim.BeatMap map, int nowMs)
        {
            int beat = 0;
            while (map.TimeAtBeat(beat) < nowMs)
            {
                beat++;
            }

            return beat;
        }

        private void Schedule(int beat, int beatMs)
        {
            var source = SourceAt(_nextSource);
            _nextSource = (_nextSource + 1) % SourceCount;
            source.clip = Click;
            source.volume = _volume;
            double dsp = _clock!.ToDspTime(beatMs);
            source.PlayScheduled(dsp);
            _scheduled.Add(new ScheduledClick(beat, beatMs, dsp));
        }

        private AudioSource SourceAt(int index)
        {
            while (_sources.Count <= index)
            {
                var host = new GameObject("Metronome.Source" + _sources.Count);
                host.transform.SetParent(transform, false);
                var source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                _sources.Add(source);
            }

            return _sources[index];
        }

        private AudioClip Click
        {
            get
            {
                if (_click == null)
                {
                    const int milliseconds = 15;
                    const float frequency = 1000f;
                    int samples = SampleRate * milliseconds / 1000;
                    var data = new float[samples];
                    for (int i = 0; i < samples; i++)
                    {
                        float t = i / (float)SampleRate;
                        float envelope = 1f - i / (float)samples;
                        data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * envelope * envelope;
                    }

                    _click = AudioClip.Create("metronome-click", samples, 1, SampleRate, false);
                    _click.SetData(data, 0);
                }

                return _click;
            }
        }

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.Ticked -= OnTick;
            }
        }
    }
}
