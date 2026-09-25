#nullable enable
using System;
using System.Collections;
using System.Linq;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Scene;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>The recorded track loader (P1.6, PRD 4.14): the recording where one has shipped, and the sidecar's offset placing beat 0.</summary>
    public class Audio
    {
        private AudioCatalogue _catalogue = null!;
        private GameObject? _host;

        [SetUp]
        public void SetUp()
        {
            _catalogue = ScriptableObject.CreateInstance<AudioCatalogue>();
            AudioCatalogue.Use(_catalogue);
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
                _host = null;
            }

            ClientTestContent.ClearCatalogues();
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator recorded_clip_preferred_over_click_track()
        {
            var ren = BattleContent.LoadFixtures().Enemy("enemy-ren");
            Assert.That(ren.Track.Id, Is.EqualTo("track-fixture-ren"), "Ren no longer fights on the fixture track");
            var recorded = ClientTestContent.SilentClip(ren.Track);
            recorded.name = "recorded-ren";
            _catalogue.PutTrack(ren.Track.Id, recorded);

            _host = new GameObject("audio-battle");
            var scene = _host.AddComponent<BattleScene>();
            scene.EnemyId = "enemy-ren";
            scene.Compose();
            yield return null;

            Assert.That(scene.Clock, Is.Not.Null);
            Assert.That(scene.Clock!.Source, Is.Not.Null);
            Assert.That(scene.Clock.Source!.clip, Is.SameAs(recorded), "the battle did not schedule the shipped recording");
            Assert.That(TrackAudio.IsRecorded(ren.Track), Is.True);
            Assert.That(_catalogue.Missing, Does.Not.Contain("track/" + ren.Track.Id));
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator sidecar_offset_places_beat_zero()
        {
            // The sidecar says beat 0 is 37 ms into the recording, and the recording clicks there.
            const int OffsetMs = 37;
            const string Sidecar = "{\n  \"id\": \"track-offset-37\",\n  \"world\": 1,\n  \"tier\": \"normal\",\n  \"lengthBeats\": 8,\n  \"offsetMs\": 37,\n  \"tempo\": { \"bpm\": 120, \"changes\": [] }\n}";
            var track = TrackLoader.FromJson(Sidecar);
            Assert.That(track.OffsetMs, Is.EqualTo(OffsetMs));
            Assert.That(track.BeatMap.TimeAtBeat(0), Is.EqualTo(OffsetMs), "the sidecar's offset did not place beat 0");

            _catalogue.PutTrack(track.Id, ClientTestContent.ClickAt(track, OffsetMs));

            var rig = new Rig("audio-offset");
            rig.Clock.Schedule(track, TrackAudio.For(track));
            var battle = ClientTestContent.Battle(ClientTestContent.Chart(track, 0));
            rig.Driver.Bind(rig.Clock, battle);
            rig.Driver.Script(new[] { new ScriptedInput(OffsetMs, ClientTestContent.SlotE, ClientTestContent.LeftAttack10) });

            yield return rig.WaitUntilAudioMs(600);

            Assert.That(battle.JudgmentLog, Has.Count.EqualTo(1), "the action at beat 0 was not judged");
            Assert.That(battle.JudgmentLog[0].Grade, Is.EqualTo(Judgment.Perfect), "the press on the click was not Perfect");
            int error = Math.Abs(battle.Events.OfType<InputJudged>().Single().OffsetMs);
            Assert.That(error, Is.LessThan(2), "the press on the click missed beat 0 by " + error + " ms");

            rig.Destroy();
        }

#if UNITY_EDITOR
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator recorded_tracks_loop_seamlessly()
        {
            // P11.2, PRD 3.6.32: every shipped recording is exactly one lap long, so the chart and the music wrap together.
            var catalogue = ClientTestContent.ShippedAudio();
            var content = BattleContent.LoadFixtures();
            foreach (var enemy in content.Enemies.Values)
            {
                var clip = catalogue.Track(enemy.Track.Id);
                Assert.That(clip, Is.Not.Null, enemy.Track.Id + " has no recording in the shipped audio catalogue");
                long expected = (long)(enemy.Track.OffsetMs + enemy.Track.BeatMap.LengthMs) * clip!.frequency / 1000;
                Assert.That(clip.samples, Is.EqualTo(expected).Within(1), enemy.Track.Id + " is not one lap of its beat map long, in samples");
            }

            // Ren's battle on the recording, played past the loop point: the first action of the
            // second lap is answered on its second-lap time and judged against that time.
            var ren = content.Enemy("enemy-ren");
            var first = ren.Chart.Actions.OrderBy(a => a.LandingQb).First();
            int secondLapQb = ren.Track.LengthQb + first.LandingQb;
            int secondLapMs = ren.Track.BeatMap.TimeAtQb(secondLapQb);
            Assert.That(secondLapMs, Is.GreaterThan(ren.Track.BeatMap.LengthMs), "the second-lap time is not past the loop point");

            var rig = new Rig("audio-loop");
            rig.Clock.Schedule(ren.Track, TrackAudio.For(ren.Track));
            Assert.That(rig.Source.clip, Is.SameAs(catalogue.Track(ren.Track.Id)), "Ren's battle is not on the shipped recording");
            var battle = new Chiki.Sim.Battle(new RunStats(), ren, ClientTestContent.DefaultEnemyHp, new Rng(1));
            rig.Driver.Bind(rig.Clock, battle);
            rig.Driver.Script(new[] { new ScriptedInput(secondLapMs, ClientTestContent.SlotE, ClientTestContent.LeftAttack10) });

            yield return rig.WaitUntilAudioMs(secondLapMs + 600, 40f);

            Assert.That(battle.Events.OfType<TrackLooped>().Any(), Is.True, "the battle did not loop");
            var judged = battle.Events.OfType<InputJudged>().ToList();
            Assert.That(judged, Has.Count.EqualTo(1), "the second-lap press was not judged");
            Assert.That(judged[0].PositionQb, Is.EqualTo(secondLapQb), "the press was judged against another action than the second lap's first");
            Assert.That(Math.Abs(judged[0].OffsetMs), Is.LessThan(2), "the press on the second-lap time missed it by " + judged[0].OffsetMs + " ms");

            rig.Destroy();
        }
#endif
    }
}
