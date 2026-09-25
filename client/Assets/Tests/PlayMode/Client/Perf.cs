#nullable enable
using System.Collections;
using System.Linq;
using Chiki.Client.Audio;
using Chiki.Client.Perf;
using Chiki.Client.Presenter;
using Chiki.Client.Scene;
using Chiki.Client.Visuals;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>
    /// The frame-rate measurement (PRD 6.2): the Battle scene with the fixture Boss and every
    /// presenter active, at 1080p, for 30 seconds, and again on Malk's fight with the shipped
    /// catalogues (P11.4). The numbers are logged for the build machine's
    /// record; the assertions are the plan's bar.
    /// </summary>
    public class Perf
    {
        private const double FrameBudgetMs = 16.7;
        private const double WindowSeconds = 30.0;
        private const int SettleMs = 2000;

        [TearDown]
        public void TearDown()
        {
            ClientTestContent.ClearCatalogues();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator battle_scene_60fps_no_audio_dropouts()
        {
            // 1080p is asked for; the batch-mode runner grants what it has, and the report records it.
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Battle");
            var battleScene = Object.FindAnyObjectByType<BattleScene>();
            Assert.That(battleScene, Is.Not.Null, "the Battle scene holds no BattleScene bootstrap");
            yield return null;

            Assert.That(battleScene!.Composed, Is.True, "the Battle scene did not compose on Start");
            var battle = battleScene.Driver!.Battle!;
            Assert.That(battle.Enemy.Id, Is.EqualTo(BattleScene.DefaultEnemyId));
            Assert.That(battle.Tier, Is.EqualTo(EncounterTier.Boss), "the scene is not against the fixture Boss");
            Assert.That(battleScene.Hud, Is.Not.Null);

            // The measurement needs the whole window; a run without inputs would otherwise end at ARD 0 first.
            battle.Stats.RaiseMaxArd(100_000);

            // Let the track start and the scene settle before the window opens.
            float deadline = Time.realtimeSinceStartup + 10f;
            while (battleScene.Clock!.NowMs < SettleMs && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var meter = battleScene.gameObject.AddComponent<PerfMeter>();
            meter.Begin();
            double start = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - start < WindowSeconds)
            {
                yield return null;
            }

            var report = meter.Stop();
            string line = "PERF battle_scene_60fps_no_audio_dropouts: " + report;
            Debug.Log(line);
            TestContext.Out.WriteLine(line);

            Assert.That(battle.Outcome, Is.Null, "the battle ended inside the measurement window");
            Assert.That(battle.CurrentBeat, Is.GreaterThanOrEqualTo(50), "the battle did not keep running through the window");
            Assert.That(report.Frames, Is.GreaterThan(0));
            Assert.That(report.OnePercentLowMs, Is.LessThan(FrameBudgetMs), "the 1% low frame time misses 60 fps");
            Assert.That(report.AudioUnderruns, Is.EqualTo(0), "audio underruns were recorded");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

#if UNITY_EDITOR
        /// <summary>
        /// P11.4, PRD 6.2: the same measurement on the heaviest shipped fight, Malk's, with the
        /// shipped catalogues handed over as Boot hands them over, and every shipped sprite,
        /// clip and track resident before the window opens so no first-use load lands inside it.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator malk_battle_60fps_with_shipped_art()
        {
            var visuals = ClientTestContent.ShippedVisuals();
            var audio = ClientTestContent.ShippedAudio();
            int loadedSprites = LoadEverySprite(visuals);
            int loadedClips = LoadEveryClip(visuals);
            int loadedAudio = LoadEveryRecording(audio);
            Assert.That(loadedSprites, Is.GreaterThan(0), "the shipped visual catalogue holds no sprites");
            Assert.That(loadedClips, Is.GreaterThan(0), "the shipped visual catalogue holds no clips");
            Assert.That(loadedAudio, Is.GreaterThan(0), "the shipped audio catalogue holds no recordings");

            // 1080p is asked for; a batch-mode runner grants what it has, and the report records what ran.
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Battle");
            var battleScene = Object.FindAnyObjectByType<BattleScene>();
            Assert.That(battleScene, Is.Not.Null, "the Battle scene holds no BattleScene bootstrap");
            yield return null;

            Assert.That(battleScene!.Composed, Is.True, "the Battle scene did not compose on Start");
            var battle = battleScene.Driver!.Battle!;
            Assert.That(battle.Enemy.Id, Is.EqualTo("enemy-malk"), "the scene is not Malk's fight");
            Assert.That(battle.Tier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(battleScene.Hud, Is.Not.Null);
            Assert.That(TrackAudio.IsRecorded(battle.Enemy.Track), Is.True, "Malk's track has no shipped recording");
            Assert.That(battleScene.Clock!.Source!.clip, Is.SameAs(audio.Track(battle.Enemy.Track.Id)), "the battle is not playing the shipped recording");
            foreach (string variant in FighterClips.Enemy)
            {
                Assert.That(visuals.FindClip(StageView.EnemyKind, "malk", variant), Is.Not.Null, "Malk's " + variant + " clip is not in the shipped catalogue");
            }

            // The measurement needs the whole window; a run without inputs would otherwise end at ARD 0 first.
            battle.Stats.RaiseMaxArd(100_000);

            // Let the track start and the scene settle before the window opens.
            float deadline = Time.realtimeSinceStartup + 10f;
            while (battleScene.Clock!.NowMs < SettleMs && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var meter = battleScene.gameObject.AddComponent<PerfMeter>();
            meter.Begin();
            double start = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - start < WindowSeconds)
            {
                yield return null;
            }

            var report = meter.Stop();
            string line = "PERF malk_battle_60fps_with_shipped_art: " + report
                + " sprites=" + loadedSprites + " clips=" + loadedClips + " recordings=" + loadedAudio;
            Debug.Log(line);
            TestContext.Out.WriteLine(line);

            Assert.That(battle.Outcome, Is.Null, "the battle ended inside the measurement window");
            Assert.That(battle.CurrentBeat, Is.GreaterThanOrEqualTo(50), "the battle did not keep running through the window");
            Assert.That(report.Frames, Is.GreaterThan(0));
            Assert.That(report.OnePercentLowMs, Is.LessThan(FrameBudgetMs), "the 1% low frame time misses 60 fps");
            Assert.That(report.AudioUnderruns, Is.EqualTo(0), "audio underruns were recorded");
            Assert.That(visuals.Missing, Is.Empty, "a presenter fell back for " + string.Join(", ", visuals.Missing));

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>Touches every shipped sprite's texture so its page is resident before the window.</summary>
        private static int LoadEverySprite(VisualCatalogue catalogue)
        {
            int loaded = 0;
            foreach (var entry in catalogue.Sprites)
            {
                if (entry.sprite != null && entry.sprite.texture != null)
                {
                    loaded += entry.sprite.texture.width > 0 ? 1 : 0;
                }
            }

            return loaded;
        }

        /// <summary>Touches every frame of every shipped clip.</summary>
        private static int LoadEveryClip(VisualCatalogue catalogue)
        {
            int loaded = 0;
            foreach (var entry in catalogue.Clips)
            {
                if (entry.clip == null)
                {
                    continue;
                }

                foreach (var frame in entry.clip.Frames)
                {
                    if (frame != null && frame.texture != null && frame.texture.width > 0)
                    {
                        loaded++;
                    }
                }
            }

            return loaded;
        }

        /// <summary>Loads the sample data of every shipped sound and track.</summary>
        private static int LoadEveryRecording(AudioCatalogue catalogue)
        {
            int loaded = 0;
            foreach (var entry in catalogue.Sounds)
            {
                loaded += Resident(entry.clip) ? 1 : 0;
            }

            foreach (var entry in catalogue.Tracks)
            {
                loaded += Resident(entry.clip) ? 1 : 0;
            }

            return loaded;
        }

        private static bool Resident(AudioClip? clip)
        {
            if (clip == null)
            {
                return false;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
            }

            return clip.samples > 0;
        }
#endif
    }
}
