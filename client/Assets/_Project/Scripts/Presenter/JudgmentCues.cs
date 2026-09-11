#nullable enable
using System;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The audio cue per judgment grade (PRD 3.3.8.1). Cues play through their own source, never
    /// the track's (PRD 3.3.1.6). Until the audio catalogue carries recorded cues, a short tone
    /// per grade is generated: high for Perfect, middle for Good, low for Miss.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JudgmentCues : MonoBehaviour
    {
        private const int SampleRate = 48000;

        [SerializeField] private AudioClip? perfectCue;
        [SerializeField] private AudioClip? goodCue;
        [SerializeField] private AudioClip? missCue;

        private AudioSource? _source;
        private AudioClip? _generatedPerfect;
        private AudioClip? _generatedGood;
        private AudioClip? _generatedMiss;

        /// <summary>The cue source, created on a child of its own so it never shares a source with the track.</summary>
        public AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    var host = new GameObject("JudgmentCues.Source");
                    host.transform.SetParent(transform, false);
                    _source = host.AddComponent<AudioSource>();
                    _source.playOnAwake = false;
                    _source.loop = false;
                }

                return _source;
            }
        }

        public Judgment? LastPlayed { get; private set; }

        public AudioClip? LastClip { get; private set; }

        public int PlayCount { get; private set; }

        /// <summary>The clip a grade plays: the assigned cue, or the generated tone when none is assigned.</summary>
        public AudioClip ClipFor(Judgment grade)
        {
            switch (grade)
            {
                case Judgment.Perfect:
                    return perfectCue != null ? perfectCue : _generatedPerfect ??= Tone("cue-perfect", 1760f, 60, 0.5f);
                case Judgment.Good:
                    return goodCue != null ? goodCue : _generatedGood ??= Tone("cue-good", 880f, 60, 0.5f);
                case Judgment.Miss:
                    return missCue != null ? missCue : _generatedMiss ??= Tone("cue-miss", 220f, 100, 0.6f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, "Unknown grade.");
            }
        }

        public void Play(Judgment grade)
        {
            var clip = ClipFor(grade);
            Source.PlayOneShot(clip);
            LastPlayed = grade;
            LastClip = clip;
            PlayCount++;
        }

        private static AudioClip Tone(string name, float frequency, int milliseconds, float amplitude)
        {
            int samples = SampleRate * milliseconds / 1000;
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = 1f - i / (float)samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope * envelope;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
