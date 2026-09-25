#nullable enable
using System.Collections;
using System.Linq;
using Chiki.Client.Audio;
using Chiki.Client.Presenter;
using Chiki.Client.Visuals;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>The audio catalogue: recordings where they have shipped, generated tones where they have not (P1.4).</summary>
    public class Catalogue
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
                Object.Destroy(_host);
                _host = null;
            }

            ClientTestContent.ClearCatalogues();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator audio_lookup_or_generated_fallback()
        {
            var perfect = ClientTestContent.TestAudioClip("test-cue-perfect", 40);
            _catalogue.PutSound("cue-perfect", perfect);

            _host = new GameObject("catalogue-cues");
            _host.AddComponent<AudioListener>();
            var cues = _host.AddComponent<JudgmentCues>();
            yield return null;

            cues.Play(Judgment.Perfect);
            Assert.That(cues.LastClip, Is.SameAs(perfect), "Perfect did not play the catalogue's recording");
            Assert.That(cues.IsRecorded(Judgment.Perfect), Is.True);

            cues.Play(Judgment.Good);
            Assert.That(cues.LastClip, Is.Not.SameAs(perfect));
            Assert.That(cues.LastClip!.name, Is.EqualTo("cue-good"), "Good did not fall back to the generated tone");
            Assert.That(cues.IsRecorded(Judgment.Good), Is.False);
            Assert.That(_catalogue.Missing, Does.Contain("sfx/cue-good"), "the missing recording was not recorded");
            Assert.That(_catalogue.Missing, Does.Not.Contain("sfx/cue-perfect"));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator metronome_uses_catalogue_click()
        {
            var click = ClientTestContent.TestAudioClip("test-click-beat", 20);
            _catalogue.PutSound(Metronome.ClickId, click);

            var track = ClientTestContent.FixtureTrack();
            _host = new GameObject("catalogue-metronome");
            _host.AddComponent<AudioListener>();
            _host.AddComponent<AudioSource>();
            var clock = _host.AddComponent<BeatClock>();
            var metronome = _host.AddComponent<Metronome>();
            metronome.Bind(clock);
            metronome.On = true;
            clock.Schedule(track, ClientTestContent.SilentClip(track));

            float deadline = Time.realtimeSinceStartup + 20f;
            while (clock.NowMs < track.BeatMap.TimeAtBeat(3) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var first = metronome.Scheduled.Take(4).ToList();
            Assert.That(first, Has.Count.EqualTo(4), "four beats were not scheduled");
            for (int beat = 0; beat < 4; beat++)
            {
                Assert.That(first[beat].Beat, Is.EqualTo(beat));
                Assert.That(first[beat].AudioTimeMs, Is.EqualTo(track.BeatMap.TimeAtBeat(beat)), "beat " + beat + " was not placed at the beat map's time");
                Assert.That(first[beat].Clip, Is.SameAs(click), "beat " + beat + " did not play the catalogue's click");
            }
        }
    }
}
