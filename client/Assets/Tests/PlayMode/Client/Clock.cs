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
    }
}
