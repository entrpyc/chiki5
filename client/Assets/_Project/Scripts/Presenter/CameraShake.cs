#nullable enable
using Chiki.Client.Audio;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// A light camera shake on heavy hits (PRD 3.3.8.1). The shake lasts a fraction of a beat
    /// and decays on the beat clock, so it reads the music's time and never frame time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShake : MonoBehaviour
    {
        /// <summary>How long a shake lasts, in beats.</summary>
        public const float DurationBeats = 0.5f;

        private Vector3 _rest;
        private int _startMs;
        private float _beatMs = 500f;
        private float _amplitude;

        /// <summary>The clock the decay reads; without one the shake holds until the next trigger.</summary>
        public BeatClock? Clock { get; set; }

        public bool IsShaking { get; private set; }

        public int TriggerCount { get; private set; }

        /// <summary>Starts a shake at an audio time; <paramref name="beatMs"/> is the beat length in force, <paramref name="amplitude"/> the offset in world units.</summary>
        public void Trigger(int audioTimeMs, float beatMs, float amplitude)
        {
            if (!IsShaking)
            {
                _rest = transform.localPosition;
            }

            _startMs = audioTimeMs;
            _beatMs = beatMs;
            _amplitude = amplitude;
            IsShaking = true;
            TriggerCount++;
        }

        private void LateUpdate()
        {
            if (!IsShaking || Clock == null || !Clock.IsScheduled)
            {
                return;
            }

            float t = (Clock.NowMs - _startMs) / (DurationBeats * _beatMs);
            if (t >= 1f)
            {
                transform.localPosition = _rest;
                IsShaking = false;
                return;
            }

            float decay = 1f - Mathf.Max(0f, t);
            var offset = new Vector3(Mathf.Sin(t * 40f) * _amplitude * decay, Mathf.Cos(t * 33f) * _amplitude * decay * 0.6f, 0f);
            transform.localPosition = _rest + offset;
        }
    }
}
