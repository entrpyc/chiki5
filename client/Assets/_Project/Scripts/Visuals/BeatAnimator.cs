#nullable enable
using System;
using Chiki.Client.Audio;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Visuals
{
    /// <summary>
    /// Plays a <see cref="SpriteClip"/> on a <see cref="SpriteRenderer"/> or a uGUI
    /// <see cref="Image"/>, stepping frames from the <see cref="BeatClock"/>'s audio time only —
    /// never <c>Time.time</c>, <c>Time.deltaTime</c> or Animator time
    /// (docs/project/unity-setup.md).
    ///
    /// Clips are authored at 8 frames per second at BPM 120, which is one frame per quarter beat;
    /// the rate therefore scales with the BPM in force divided by 120, so a tempo change retimes
    /// the clip at the change. A looping clip wraps on whole beats, on its authored length. A
    /// one-shot clip started with <see cref="PlayStrikeAt"/> begins early enough that its strike
    /// frame is on screen at the requested audio time, and the idle loop takes over when it ends.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeatAnimator : MonoBehaviour
    {
        /// <summary>Frames in one beat: 8 frames per second at BPM 120 is one frame per quarter beat (PRD 3.3.1.8).</summary>
        public const int FramesPerBeat = Beats.QuarterBeatsPerBeat;

        private BeatClock? _clock;
        private BeatCursor? _cursor;
        private SpriteRenderer? _renderer;
        private Image? _image;
        private SpriteClip? _idle;
        private SpriteClip? _playing;
        private int _startQb;
        private int _frameIndex = -1;

        /// <summary>The clock every frame is picked from; nothing animates without one.</summary>
        public BeatClock? Clock
        {
            get => _clock;
            set
            {
                _clock = value;
                _cursor = null;
            }
        }

        public SpriteRenderer? Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }

        public Image? Image
        {
            get => _image;
            set => _image = value;
        }

        /// <summary>The loop shown when nothing else is playing.</summary>
        public SpriteClip? Idle => _idle;

        /// <summary>The clip on screen; null before the first <see cref="Play"/>.</summary>
        public SpriteClip? Playing => _playing;

        /// <summary>The frame on screen counting from 0; -1 before anything plays.</summary>
        public int FrameIndex => _frameIndex;

        /// <summary>The frame on screen counting from 1, as the files are numbered; 0 before anything plays.</summary>
        public int Frame => _frameIndex + 1;

        /// <summary>The sprite on screen.</summary>
        public Sprite? Sprite => _playing != null ? _playing.Frame(_frameIndex) : null;

        /// <summary>How many times the frame on screen has changed since the current clip's first frame was shown.</summary>
        public int FrameChanges { get; private set; }

        /// <summary>The audio time of the last render.</summary>
        public int RenderedAtMs { get; private set; }

        public bool HasRendered { get; private set; }

        /// <summary>Attaches the animator to a renderer and a clock.</summary>
        public static BeatAnimator On(GameObject host, BeatClock? clock)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var animator = host.GetComponent<BeatAnimator>();
            if (animator == null)
            {
                animator = host.AddComponent<BeatAnimator>();
            }

            animator.Clock = clock;
            animator.Renderer = host.GetComponent<SpriteRenderer>();
            animator.Image = host.GetComponent<Image>();
            return animator;
        }

        /// <summary>
        /// Sets the loop shown when nothing else plays and shows it. An idle is phase-locked to
        /// the track, not to the moment it started, so every character breathes with the music.
        /// </summary>
        public void PlayIdle(SpriteClip clip)
        {
            _idle = clip != null ? clip : throw new ArgumentNullException(nameof(clip));
            Begin(clip, 0);
        }

        /// <summary>Plays a clip from an audio time; a one-shot hands back to the idle loop when its last frame is past.</summary>
        public void Play(SpriteClip clip, int audioTimeMs)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            Begin(clip, QbAt(audioTimeMs));
        }

        /// <summary>
        /// Plays a one-shot clip early enough that its strike frame is on screen at
        /// <paramref name="audioTimeMs"/>, then returns to the idle loop. A clip with no strike
        /// frame starts at that time.
        /// </summary>
        public void PlayStrikeAt(SpriteClip clip, int audioTimeMs)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            int strike = clip.StrikeFrame ?? 1;
            Begin(clip, QbAt(audioTimeMs) - (strike - 1));
        }

        /// <summary>The frame ordinal of an audio time: quarter beats since the clip started, before any wrap.</summary>
        public int FrameOrdinalAt(int audioTimeMs)
        {
            return Math.Max(0, QbAt(audioTimeMs) - _startQb);
        }

        /// <summary>The frame an audio time shows, counting from 0.</summary>
        public int FrameIndexAt(int audioTimeMs)
        {
            var clip = _playing;
            if (clip == null || clip.FrameCount == 0)
            {
                return -1;
            }

            int ordinal = FrameOrdinalAt(audioTimeMs);
            if (!clip.Loop)
            {
                return Math.Min(ordinal, clip.FrameCount - 1);
            }

            int period = Math.Max(clip.FrameCount, clip.LengthBeats * FramesPerBeat);
            int inPeriod = ordinal % period;
            return Math.Min(inPeriod, clip.FrameCount - 1);
        }

        /// <summary>Whether a one-shot clip has shown its last frame at an audio time.</summary>
        public bool FinishedAt(int audioTimeMs)
        {
            var clip = _playing;
            return clip != null && !clip.Loop && FrameOrdinalAt(audioTimeMs) >= clip.FrameCount;
        }

        /// <summary>Shows the frame an audio time falls on; the only place a sprite is assigned.</summary>
        public void Render(int audioTimeMs)
        {
            var clip = _playing;
            if (clip == null || clip.FrameCount == 0)
            {
                return;
            }

            if (!clip.Loop && _idle != null && !ReferenceEquals(clip, _idle) && FinishedAt(audioTimeMs))
            {
                Begin(_idle, 0);
                clip = _playing!;
            }

            int index = FrameIndexAt(audioTimeMs);
            RenderedAtMs = audioTimeMs;
            HasRendered = true;
            if (index == _frameIndex)
            {
                return;
            }

            if (_frameIndex >= 0)
            {
                FrameChanges++;
            }

            _frameIndex = index;
            Show(clip.Frame(index));
        }

        private void Begin(SpriteClip clip, int startQb)
        {
            _playing = clip;
            _startQb = startQb;
            _frameIndex = -1;
            FrameChanges = 0;
            if (_clock != null && _clock.IsScheduled)
            {
                Render(_clock.NowMs);
            }
        }

        private void Show(Sprite? sprite)
        {
            if (_renderer != null)
            {
                _renderer.sprite = sprite;
            }

            if (_image != null)
            {
                _image.sprite = sprite;
            }
        }

        private int QbAt(int audioTimeMs)
        {
            var map = _clock?.Track?.BeatMap;
            if (map == null)
            {
                return 0;
            }

            if (_cursor == null || !ReferenceEquals(_cursor.Map, map))
            {
                _cursor = new BeatCursor(map);
            }

            return _cursor.QbAt(audioTimeMs);
        }

        private void LateUpdate()
        {
            if (_clock == null || !_clock.IsScheduled)
            {
                return;
            }

            Render(_clock.NowMs);
        }
    }
}
