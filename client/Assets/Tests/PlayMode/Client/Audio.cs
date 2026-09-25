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
    }
}
