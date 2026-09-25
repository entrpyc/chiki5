#nullable enable
using System.Collections;
using Chiki.Client.Audio;
using Chiki.Client.Visuals;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SpriteAnimator = Chiki.Client.Visuals.BeatAnimator;

namespace Client
{
    /// <summary>
    /// Sprite clips stepped from the beat clock (P1.5): 8 frames per second at BPM 120, scaled by
    /// the BPM in force, the strike frame on the action's beat, and never a frame taken from
    /// engine time.
    /// </summary>
    public class BeatAnimator
    {
        private GameObject? _host;
        private int _targetFrameRate;
        private int _vSync;

        [SetUp]
        public void SetUp()
        {
            _targetFrameRate = Application.targetFrameRate;
            _vSync = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = _targetFrameRate;
            QualitySettings.vSyncCount = _vSync;
            if (_host != null)
            {
                Object.Destroy(_host);
                _host = null;
            }

            ClientTestContent.ClearCatalogues();
        }

        /// <summary>A clock scheduled on a track, with an animator on a sprite renderer beside it.</summary>
        private SpriteAnimator Rig(Track track, bool live)
        {
            _host = new GameObject("beat-animator");
            _host.AddComponent<AudioListener>();
            _host.AddComponent<AudioSource>();
            _host.AddComponent<SpriteRenderer>();
            var clock = _host.AddComponent<BeatClock>();
            clock.Schedule(track, ClientTestContent.SilentClip(track));
            var animator = SpriteAnimator.On(_host, clock);
            animator.enabled = live;
            return animator;
        }

        /// <summary>Renders every millisecond of a span and counts how many times the frame on screen changed.</summary>
        private static int FramesAdvanced(SpriteAnimator animator, int fromMs, int toMs)
        {
            animator.Render(fromMs);
            int last = animator.FrameIndex;
            int advanced = 0;
            for (int ms = fromMs + 1; ms <= toMs; ms++)
            {
                animator.Render(ms);
                if (animator.FrameIndex != last)
                {
                    advanced++;
                    last = animator.FrameIndex;
                }
            }

            return advanced;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator frame_rate_scales_with_bpm()
        {
            // 120 BPM to beat 8, 180 from there; one second holds 2 beats then 3.
            var tempo = new TempoMap(120, new[] { new TempoChange(8, 180) });
            var track = new Track("track-animator-tempo", 1, EncounterTier.Normal, 32, 0, tempo);
            var animator = Rig(track, live: false);
            animator.PlayIdle(ClientTestContent.SpriteClip("anim", "idle", frames: 8, lengthBeats: 2, loop: true));
            yield return null;

            int beat8Ms = track.BeatMap.TimeAtBeat(8);
            Assert.That(beat8Ms, Is.EqualTo(4000), "the fixture track does not put beat 8 at 4 seconds");

            Assert.That(FramesAdvanced(animator, 0, 1000), Is.EqualTo(8), "one second at 120 BPM did not advance 8 frames");
            Assert.That(FramesAdvanced(animator, beat8Ms, beat8Ms + 1000), Is.EqualTo(12), "one second at 180 BPM did not advance 12 frames");
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator strike_frame_lands_on_beat()
        {
            var track = ClientTestContent.FixtureTrack();
            var animator = Rig(track, live: false);
            animator.PlayIdle(ClientTestContent.SpriteClip("anim", "idle", frames: 8, lengthBeats: 2, loop: true));
            var attack = ClientTestContent.SpriteClip("anim", "attack-left", frames: 6, lengthBeats: 2, loop: false, strikeFrame: 4);
            yield return null;

            int beat8Ms = track.BeatMap.TimeAtBeat(8);
            animator.PlayStrikeAt(attack, beat8Ms);
            animator.Render(beat8Ms);

            Assert.That(animator.Playing, Is.SameAs(attack));
            Assert.That(animator.Frame, Is.EqualTo(4), "the strike frame is not on screen on the action's beat");

            // A quarter beat is one frame; the strike frame holds the whole of it.
            animator.Render(beat8Ms + 124);
            Assert.That(animator.Frame, Is.EqualTo(4), "the strike frame left before its frame was over");
            animator.Render(beat8Ms - 1);
            Assert.That(animator.Frame, Is.EqualTo(3), "the frame before the beat is not the one before the strike");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator follows_audio_not_frame_time()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 20;

            var track = ClientTestContent.FixtureTrack();
            var animator = Rig(track, live: true);
            animator.PlayIdle(ClientTestContent.SpriteClip("anim", "idle", frames: 8, lengthBeats: 2, loop: true));

            float deadline = Time.realtimeSinceStartup + 40f;
            while ((!animator.HasRendered || animator.RenderedAtMs < 0) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            int startMs = animator.RenderedAtMs;
            int startEngineFrames = Time.frameCount;
            int mismatches = 0;
            while (animator.RenderedAtMs < startMs + 2000 && Time.realtimeSinceStartup < deadline)
            {
                if (animator.FrameIndex != animator.FrameIndexAt(animator.RenderedAtMs))
                {
                    mismatches++;
                }

                yield return null;
            }

            int elapsedMs = animator.RenderedAtMs - startMs;
            int engineFrames = Time.frameCount - startEngineFrames;
            int fromAudio = animator.FrameOrdinalAt(animator.RenderedAtMs) - animator.FrameOrdinalAt(startMs);

            Assert.That(elapsedMs, Is.GreaterThanOrEqualTo(2000), "two seconds of audio time did not elapse");
            Assert.That(mismatches, Is.Zero, "the frame on screen left the frame audio time computes");
            Assert.That(animator.FrameIndex, Is.EqualTo(animator.FrameIndexAt(animator.RenderedAtMs)), "the frame on screen is not the frame audio time computes");
            Assert.That(fromAudio, Is.EqualTo(elapsedMs * 8 / 1000).Within(1), "two seconds of audio time did not advance 16 frames at 120 BPM");
            Assert.That(engineFrames, Is.GreaterThan(fromAudio), "the animation advanced once per engine frame rather than from audio time");
        }
    }
}
