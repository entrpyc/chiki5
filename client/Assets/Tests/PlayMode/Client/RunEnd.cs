#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Profiles;
using Chiki.Client.Text;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Client
{
    public class RunEnd
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "runend-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveRoot()
        {
            foreach (var host in _hosts)
            {
                if (host != null)
                {
                    Object.DestroyImmediate(host);
                }
            }

            _hosts.Clear();
            ActiveProfile.Clear();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator death_shows_summary_and_returns()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out var store);
            var run = flow.Run!;
            var charm = flow.Content!.Charms.Charms.First();
            ClientTestContent.WalkToBoss(run);
            var boss = ClientTestContent.FightNodeBattle(run);
            Assume.That(boss.Outcome, Is.EqualTo(BattleOutcome.Won));
            flow.SettleBattle(boss);
            Assume.That(flow.UnlocksThisRun, Does.Contain(charm.Id), "the Boss defeat did not unlock the Charm");
            flow.SkipReward();
            yield return null;

            run.Stats.Ard = 5;
            var enemy = flow.Content.FindEnemy("enemy-kess")!;
            var fatal = run.StartBattle(enemy, enemyHp: 1000);
            fatal.AdvanceToBeat(enemy.Track.LengthBeats);
            Assume.That(fatal.Outcome, Is.EqualTo(BattleOutcome.Died), "the last battle must end by death");
            flow.SettleBattle(fatal);
            yield return null;
            var screen = flow.RunEnd;
            Assert.That(screen, Is.Not.Null, "the run-end screen did not open");
            string outcome = screen!.OutcomeText;
            string seed = screen.SeedText;
            string stats = screen.StatsText;
            string unlocks = screen.UnlocksText;

            bool continued = screen.ChooseContinue();
            yield return null;
            string profileFile = File.ReadAllText(store.FileOf("A"));

            Assert.That(run.Status, Is.EqualTo(RunStatus.Died));
            Assert.That(outcome, Is.EqualTo(Strings.Get("runend.died")));
            Assert.That(outcome, Is.EqualTo("Died"));
            Assert.That(seed, Does.Contain("chiki-1"));
            Assert.That(stats, Does.Contain("2"), "the stats do not count the two battles");
            Assert.That(screen.Summary.Battles, Is.EqualTo(2));
            Assert.That(screen.Summary.EssenceEarned, Is.GreaterThan(0), "the Boss win paid no Essence");
            Assert.That(screen.Summary.CrpPeak, Is.GreaterThanOrEqualTo(run.Stats.Crp));
            Assert.That(unlocks, Does.Contain(charm.Name), "the unlocked Charm is not shown");
            Assert.That(continued, Is.True);
            Assert.That(flow.PreRun, Is.Not.Null, "Continue did not return to the pre-run screen");
            Assert.That(flow.RunEnd, Is.Null);
            Assert.That(profileFile, Does.Contain(charm.Id), "the profile file lacks the unlock");
            Assert.That(new ProfileStore(_root).Load("A").RunInProgress, Is.Null, "the ended run is still in progress");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator victory_shows_won()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            for (int world = 1; world <= 3; world++)
            {
                Assume.That(run.World, Is.EqualTo(world));
                ClientTestContent.WalkToBoss(run);
                var boss = ClientTestContent.FightNodeBattle(run);
                Assume.That(boss.Outcome, Is.EqualTo(BattleOutcome.Won), "the World " + world + " Boss must be beaten");
                flow.SettleBattle(boss);
                Assume.That(flow.Reward, Is.Not.Null, "the Boss reward did not open");
                flow.SkipReward();
                yield return null;
            }

            Assert.That(run.Status, Is.EqualTo(RunStatus.Won));
            Assert.That(flow.RunEnd, Is.Not.Null, "the run-end screen did not open");
            Assert.That(flow.RunEnd!.OutcomeText, Is.EqualTo(Strings.Get("runend.won")));
            Assert.That(flow.RunEnd.OutcomeText, Is.EqualTo("Won"));
            Assert.That(flow.RunEnd.Summary.Battles, Is.EqualTo(3));
        }
    }
}
