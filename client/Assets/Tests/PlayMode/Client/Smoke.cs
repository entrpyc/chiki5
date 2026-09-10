#nullable enable
using System.Collections;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Client
{
    public class Smoke
    {
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator sim_available_in_client()
        {
            var track = ClientTestContent.FixtureTrack();
            var battle = ClientTestContent.Battle(ClientTestContent.Chart(track, 4));
            battle.AdvanceToBeat(2);

            var load = SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Additive);
            yield return load;
            var boot = SceneManager.GetSceneByName("Boot");

            Assert.That(battle.JudgmentLog, Has.Count.EqualTo(1), "the battle from Chiki.Sim did not run its first action");
            Assert.That(battle.Events, Has.Some.InstanceOf<DamageTaken>());
            Assert.That(boot.isLoaded, Is.True, "the Boot scene did not load");

            yield return SceneManager.UnloadSceneAsync(boot);
        }
    }
}
