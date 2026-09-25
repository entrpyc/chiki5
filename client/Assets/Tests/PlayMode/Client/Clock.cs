#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    public class Clock
    {
        private int _savedTargetFrameRate;
        private int _savedVSync;

        [SetUp]
        public void SaveFramePacing()
        {
            _savedTargetFrameRate = Application.targetFrameRate;
            _savedVSync = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void RestoreFramePacing()
        {
            Application.targetFrameRate = _savedTargetFrameRate;
            QualitySettings.vSyncCount = _savedVSync;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator beats_tick_on_dsp_time()
        {
            // One attack far away so the battle just ticks beats.
            var rig = ClientTestContent.ScheduledRig("clock-beats", Beats.ToQuarterBeats(60));
            var battle = rig.Driver.Battle!;

            yield return rig.WaitUntilAudioMs(3750);
            var ticks = rig.Driver.Ticks.ToList();

            Assert.That(ticks.Select(t => t.Beat), Is.EqualTo(Enumerable.Range(0, 8)), "beats 0-7 were not delivered in order");
            foreach (var tick in ticks)
            {
                int expected = battle.BeatMap.TimeAtBeat(tick.Beat);
                Assert.That(tick.AudioTimeMs, Is.EqualTo(expected).Within(1), $"beat {tick.Beat} audio time");
                Assert.That(tick.DeliveredDspTime, Is.GreaterThanOrEqualTo(rig.Clock.ToDspTime(expected) - 0.001), $"beat {tick.Beat} was delivered before its DSP time");
            }

            Assert.That(battle.BeatMap.BpmAt(0), Is.EqualTo(120));

            rig.Destroy();
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator grade_independent_of_framerate()
        {
            var results = new List<(Judgment grade, int offsetMs)>();
            foreach (int fps in new[] { 30, 144 })
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = fps;
                var rig = ClientTestContent.ScheduledRig("clock-fps-" + fps, Beats.ToQuarterBeats(6)); // attack at beat 6 = 3000 ms
                var battle = rig.Driver.Battle!;

                // Let the offset estimate settle, then generate the input at beat centre +30 ms
                // and hand it to the driver on whatever frame comes next.
                yield return rig.WaitUntilAudioMs(2200);
                double inputTime = rig.Clock.RealtimeAt(3030);
                while (Time.realtimeSinceStartupAsDouble < inputTime)
                {
                    yield return null;
                }

                var result = rig.Driver.Press(ClientTestContent.SlotE, ClientTestContent.LeftAttack10, inputTime);
                var judged = battle.Events.OfType<InputJudged>().Single();
                results.Add((result.Grade!.Value, judged.OffsetMs));
                rig.Destroy();
                yield return null;
            }

            Assert.That(results.Select(r => r.grade), Has.All.EqualTo(Judgment.Perfect));
            Assert.That(Math.Abs(results[0].offsetMs - results[1].offsetMs), Is.LessThan(2), $"offsets at 30 and 144 fps: {results[0].offsetMs} vs {results[1].offsetMs}");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator playback_continuous_through_stun_and_signature()
        {
            // Attacks on every beat from 1 to 15; three sends fire the Signature on beat 3 and a Stun lands on beat 5.
            var positions = Enumerable.Range(1, 15).Select(Beats.ToQuarterBeats).ToArray();
            var rig = ClientTestContent.ScheduledRig("clock-playback", positions);
            var battle = rig.Driver.Battle!;
            rig.Driver.Script(new[]
            {
                new Chiki.Client.Driver.ScriptedInput(500, ClientTestContent.SlotE, ClientTestContent.LeftAttack10, SignatureSend: true),
                new Chiki.Client.Driver.ScriptedInput(1000, ClientTestContent.SlotR, ClientTestContent.LeftAttack10, SignatureSend: true),
                new Chiki.Client.Driver.ScriptedInput(1500, ClientTestContent.SlotELine2, ClientTestContent.LeftAttack10, SignatureSend: true),
            });
            var source = rig.Source;

            yield return rig.WaitUntilAudioMs(0);
            double startDsp = AudioSettings.dspTime;
            int startSamples = source.timeSamples;
            int lastSamples = startSamples;
            bool monotonic = true;
            bool pitchStayed = true;
            bool stunned = false;
            while (AudioSettings.dspTime - startDsp < 8.0)
            {
                yield return null;
                if (!stunned && rig.Clock.NowMs >= 2400)
                {
                    battle.ApplyStatus(StatusTarget.Player, StatusKind.Stun);
                    stunned = true;
                }

                if (source.timeSamples < lastSamples)
                {
                    monotonic = false;
                }

                lastSamples = source.timeSamples;
                pitchStayed &= Mathf.Approximately(source.pitch, 1f);
            }

            double elapsedDsp = AudioSettings.dspTime - startDsp;
            double advanced = (source.timeSamples - startSamples) / (double)source.clip.frequency;

            Assert.That(battle.Events.OfType<SignatureFired>().Count(), Is.EqualTo(1), "the Signature did not fire");
            Assert.That(battle.Events.OfType<StatusTriggered>().Any(e => e.Kind == StatusKind.Stun), Is.True, "the Stun did not act");
            Assert.That(pitchStayed, Is.True, "the pitch changed");
            Assert.That(monotonic, Is.True, "the playback position went backwards");
            Assert.That(source.isPlaying, Is.True, "the track stopped");
            Assert.That(advanced, Is.EqualTo(elapsedDsp).Within(0.005), "playback position did not advance with DSP time");
            Assert.That(elapsedDsp, Is.GreaterThanOrEqualTo(8.0));

            rig.Destroy();
        }

#if UNITY_EDITOR
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator recorded_track_continuous_through_stun_signature_and_loop()
        {
            // P11.3, PRD 3.3.1.6: Ren's recording under a battle with attacks on beats 1 to 8; three
            // sends fire the Signature on beat 3, a Stun lands on beat 5, and the track loops at 16 s.
            ClientTestContent.ShippedAudio();
            try
            {
                var track = Chiki.Client.Scene.BattleContent.LoadFixtures().Enemy("enemy-ren").Track;
                var recorded = Chiki.Client.Visuals.AudioCatalogue.Active.Track(track.Id);
                Assert.That(recorded, Is.Not.Null, "Ren's track has no recording in the shipped audio catalogue");

                var rig = new Rig("clock-recorded");
                rig.Clock.Schedule(track, Chiki.Client.Audio.TrackAudio.For(track));
                var battle = ClientTestContent.Battle(ClientTestContent.Chart(track, Enumerable.Range(1, 8).Select(Beats.ToQuarterBeats).ToArray()));
                rig.Driver.Bind(rig.Clock, battle);
                rig.Driver.Script(new[]
                {
                    new Chiki.Client.Driver.ScriptedInput(500, ClientTestContent.SlotE, ClientTestContent.LeftAttack10, SignatureSend: true),
                    new Chiki.Client.Driver.ScriptedInput(1000, ClientTestContent.SlotR, ClientTestContent.LeftAttack10, SignatureSend: true),
                    new Chiki.Client.Driver.ScriptedInput(1500, ClientTestContent.SlotELine2, ClientTestContent.LeftAttack10, SignatureSend: true),
                });
                var source = rig.Source;
                Assert.That(source.clip, Is.SameAs(recorded), "the battle is not on the shipped recording");

                int lapMs = track.OffsetMs + track.BeatMap.LengthMs;
                double runSeconds = lapMs / 1000.0 + 2.0;
                Assert.That(runSeconds, Is.LessThan(40.0), "the loop does not fall inside 40 seconds");

                AudioSettings.GetDSPBufferSize(out int bufferLength, out _);
                int clipSamples = source.clip.samples;
                int frequency = source.clip.frequency;

                yield return rig.WaitUntilAudioMs(0);
                double startDsp = AudioSettings.dspTime;
                double lastDsp = startDsp;
                int lastSamples = source.timeSamples;
                int wraps = 0;
                int frames = 0;
                double worstDrift = 0;
                bool pitchStayed = true;
                bool stunned = false;
                while (AudioSettings.dspTime - startDsp < runSeconds)
                {
                    yield return null;
                    if (!stunned && rig.Clock.NowMs >= 2400)
                    {
                        battle.ApplyStatus(StatusTarget.Player, StatusKind.Stun);
                        stunned = true;
                    }

                    double nowDsp = AudioSettings.dspTime;
                    int nowSamples = source.timeSamples;
                    long advanced = nowSamples - lastSamples;
                    if (advanced < 0)
                    {
                        advanced += clipSamples; // the loop point: the position wrapped to the start of the clip
                        wraps++;
                    }

                    // DSP time counts whole samples; rounding drops the error of holding it as seconds in a double.
                    double elapsed = Math.Round((nowDsp - lastDsp) * frequency);
                    worstDrift = Math.Max(worstDrift, Math.Abs(advanced - elapsed));
                    pitchStayed &= Mathf.Approximately(source.pitch, 1f);
                    lastDsp = nowDsp;
                    lastSamples = nowSamples;
                    frames++;
                }

                Assert.That(battle.Events.OfType<SignatureFired>().Count(), Is.EqualTo(1), "the Signature did not fire");
                Assert.That(battle.Events.OfType<StatusTriggered>().Any(e => e.Kind == StatusKind.Stun), Is.True, "the Stun did not act");
                Assert.That(battle.Events.OfType<TrackLooped>().Any(), Is.True, "the battle did not loop");
                Assert.That(wraps, Is.EqualTo(1), "the recording did not wrap exactly once at the loop point");
                Assert.That(frames, Is.GreaterThan(0));
                Assert.That(worstDrift, Is.LessThanOrEqualTo(bufferLength), "on some frame the source advanced " + worstDrift + " samples away from the audio time elapsed, more than one " + bufferLength + "-sample buffer");
                Assert.That(pitchStayed, Is.True, "the pitch changed");
                Assert.That(source.isPlaying, Is.True, "the track stopped");

                rig.Destroy();
            }
            finally
            {
                ClientTestContent.ClearCatalogues();
            }
        }
#endif
    }
}
