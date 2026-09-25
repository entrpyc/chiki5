#nullable enable
using System;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The audio cue per judgment grade (PRD 3.3.8.1). Cues play through their own source, never
    /// the track's (PRD 3.3.1.6). A grade plays the recording the audio catalogue holds for its
    /// id — <c>cue-perfect</c>, <c>cue-good</c>, <c>cue-miss</c> — and, only for an id the
    /// catalogue lacks, a generated tone: high for Perfect, middle for Good, low for Miss.
    ///
    /// The refused-press sound (PRD 3.3.5.3) plays through the same source and is counted apart,
    /// because a refused press is not a judgment: it records no grade and no judgment cue plays.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JudgmentCues : MonoBehaviour
    {
        private const int SampleRate = 48000;

        /// <summary>The catalogue id of the refused-press sound (PRD 3.3.5.3).</summary>
        public const string DisabledSoundId = "press-disabled";

        [SerializeField] private AudioClip? perfectCue;
        [SerializeField] private AudioClip? goodCue;
        [SerializeField] private AudioClip? missCue;

        private AudioSource? _source;
        private AudioClip? _generatedPerfect;
        private AudioClip? _generatedGood;
        private AudioClip? _generatedMiss;
        private AudioClip? _generatedDisabled;

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

        /// <summary>What the last refused press played (PRD 3.3.5.3).</summary>
        public AudioClip? LastDisabledClip { get; private set; }

        /// <summary>How many refused presses have sounded; judgment cues are counted apart.</summary>
        public int DisabledPlayCount { get; private set; }

        /// <summary>The catalogue id of a grade's cue (PRD 3.3.8.1).</summary>
        public static string IdOf(Judgment grade)
        {
            switch (grade)
            {
                case Judgment.Perfect: return "cue-perfect";
                case Judgment.Good: return "cue-good";
                case Judgment.Miss: return "cue-miss";
                default: throw new ArgumentOutOfRangeException(nameof(grade), grade, "Unknown grade.");
            }
        }

        /// <summary>The clip a grade plays: the assigned cue, the catalogue's recording, or the generated tone.</summary>
        public AudioClip ClipFor(Judgment grade)
        {
            var recorded = AudioCatalogue.Active.Sound(IdOf(grade));
            switch (grade)
            {
                case Judgment.Perfect:
                    return perfectCue != null ? perfectCue : recorded != null ? recorded : _generatedPerfect ??= Tone("cue-perfect", 1760f, 60, 0.5f);
                case Judgment.Good:
                    return goodCue != null ? goodCue : recorded != null ? recorded : _generatedGood ??= Tone("cue-good", 880f, 60, 0.5f);
                case Judgment.Miss:
                    return missCue != null ? missCue : recorded != null ? recorded : _generatedMiss ??= Tone("cue-miss", 220f, 100, 0.6f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, "Unknown grade.");
            }
        }

        /// <summary>Whether a grade plays a recording rather than a generated tone.</summary>
        public bool IsRecorded(Judgment grade)
        {
            return AudioCatalogue.Active.HasSound(IdOf(grade));
        }

        /// <summary>The clip a refused press plays: the catalogue's recording, or a generated thud.</summary>
        public AudioClip DisabledClip()
        {
            var recorded = AudioCatalogue.Active.Sound(DisabledSoundId);
            return recorded != null ? recorded : _generatedDisabled ??= Tone(DisabledSoundId, 110f, 90, 0.35f);
        }

        /// <summary>Plays the refused-press sound; no grade is recorded (PRD 3.3.5.3).</summary>
        public void PlayDisabled()
        {
            var clip = DisabledClip();
            Source.PlayOneShot(clip);
            LastDisabledClip = clip;
            DisabledPlayCount++;
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
