#nullable enable
using System.Collections;
using Chiki.Client.Perf;
using Chiki.Client.Scene;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>
    /// The frame-rate measurement (PRD 6.2): the Battle scene with the fixture Boss and every
    /// presenter active, at 1080p, for 30 seconds. The numbers are logged for the build machine's
    /// record; the assertions are the plan's bar.
    /// </summary>
    public class Perf
    {
        private const double FrameBudgetMs = 16.7;
        private const double WindowSeconds = 30.0;
        private const int SettleMs = 2000;

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator battle_scene_60fps_no_audio_dropouts()
        {
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
    }
}
