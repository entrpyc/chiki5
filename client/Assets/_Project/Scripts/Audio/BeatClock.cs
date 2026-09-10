#nullable enable
using System;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Audio
{
    /// <summary>
    /// The only clock in the client (PRD 3.3.1.1, 6.1). It schedules the battle's track on the
    /// audio DSP clock (<see cref="AudioSettings.dspTime"/>), converts DSP time to the
    /// simulation's millisecond beat map (audio time 0 is the first sample of the track) and
    /// ticks whoever listens from that time, never from <see cref="Time.deltaTime"/>.
    ///
    /// Input events carry timestamps on the <see cref="Time.realtimeSinceStartupAsDouble"/>
    /// timeline (the Input System's). The clock keeps a running estimate of the offset between
    /// that timeline and DSP time so an event is stamped with the audio time at which it was
    /// generated, not the frame it was read (P12.3). DSP time only advances per audio buffer, so
    /// the estimate is the largest offset observed: every observation lags the true offset by at
    /// most one buffer, and the best one converges within a few frames.
    ///
    /// Gameplay never touches the source's pitch, position or pause state (PRD 3.3.1.6); the
    /// track is scheduled once and loops from its start together with the chart (PRD 3.6.32).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeatClock : MonoBehaviour
    {
        private AudioSource? _source;
        private double _startDsp;
        private double _offset;
        private bool _hasOffset;

        /// <summary>The track scheduled, once <see cref="Schedule"/> has run.</summary>
        public Track? Track { get; private set; }

        public AudioSource? Source => _source;

        public bool IsScheduled => Track != null;

        /// <summary>The DSP time at which the track's first sample plays.</summary>
        public double StartDspTime => _startDsp;

        /// <summary>The audio time right now in the simulation's milliseconds; negative before the track starts.</summary>
        public int NowMs => IsScheduled ? ToAudioTimeMs(AudioSettings.dspTime) : 0;

        /// <summary>Fired every frame with the audio time in milliseconds, after the offset estimate is refreshed.</summary>
        public event Action<int>? Ticked;

        /// <summary>
        /// Schedules the track's clip to start <paramref name="leadSeconds"/> of DSP time from
        /// now and makes that start audio time 0. Looping is on so the track restarts seamlessly
        /// at its end (PRD 3.6.32).
        /// </summary>
        public void Schedule(Track track, AudioClip clip, double leadSeconds = 0.2)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (IsScheduled)
            {
                throw new InvalidOperationException("The clock already has a track scheduled; the music is never restarted by gameplay (PRD 3.3.1.6).");
            }

            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.playOnAwake = false;
            _source.loop = true;
            _source.clip = clip;
            Track = track;
            _startDsp = AudioSettings.dspTime + leadSeconds;
            RefreshOffset();
            _source.PlayScheduled(_startDsp);
        }

        /// <summary>The simulation's audio time of a DSP time.</summary>
        public int ToAudioTimeMs(double dspTime)
        {
            return checked((int)Math.Round((dspTime - _startDsp) * 1000.0));
        }

        /// <summary>The DSP time of a simulation audio time.</summary>
        public double ToDspTime(int audioTimeMs)
        {
            return _startDsp + audioTimeMs / 1000.0;
        }

        /// <summary>
        /// The audio time at which an input event was generated, from its timestamp on the
        /// <see cref="Time.realtimeSinceStartupAsDouble"/> timeline (PRD 6.1).
        /// </summary>
        public int AudioTimeMsAt(double inputTime)
        {
            return ToAudioTimeMs(inputTime + _offset);
        }

        /// <summary>The realtime timestamp at which an audio time is reached; the inverse of <see cref="AudioTimeMsAt"/> for tests and schedulers.</summary>
        public double RealtimeAt(int audioTimeMs)
        {
            return ToDspTime(audioTimeMs) - _offset;
        }

        /// <summary>The current estimate of DSP time minus realtime, in seconds.</summary>
        public double OffsetEstimate => _offset;

        private void Update()
        {
            if (!IsScheduled)
            {
                return;
            }

            RefreshOffset();
            Ticked?.Invoke(NowMs);
        }

        private void RefreshOffset()
        {
            double observed = AudioSettings.dspTime - Time.realtimeSinceStartupAsDouble;
            if (!_hasOffset || observed > _offset || observed < _offset - 0.05)
            {
                // A larger observation is closer to the truth; a much smaller one means the DSP clock was reset.
                _offset = observed;
                _hasOffset = true;
            }
        }
    }
}
