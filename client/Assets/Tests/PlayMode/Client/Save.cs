#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Client
{
    public class Save
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "save-" + Guid.NewGuid().ToString("N"));
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
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private GameFlow NewFlow(string name)
        {
            var host = new GameObject(name);
            _hosts.Add(host);
            return host.AddComponent<GameFlow>();
        }

        private GameFlow FlowWithRun(string profileName, string seed, out ProfileStore store)
        {
            store = new ProfileStore(_root);
            var profile = store.Create(profileName);
            profile.Calibrated = true;
            store.Save(profile);
            var flow = NewFlow("flow-" + profileName);
            flow.Begin(profile, store);
            Assume.That(flow.TryStartRun(seed), Is.EqualTo(StartRunResult.Started));
            return flow;
        }

        /// <summary>Quits without a normal save: the flow object goes away as a killed process would leave it.</summary>
        private void Quit(GameFlow flow)
        {
            Object.DestroyImmediate(flow.gameObject);
        }

        [Test]
        public void resume_at_last_node()
        {
            var flow = FlowWithRun("A", "chiki-1", out var store);
            var run = flow.Run!;
            for (int i = 0; i < 5; i++)
            {
                if (!run.CurrentNodeCompleted)
                {
                    run.CompleteNode();
                }

                if (i == 4)
                {
                    run.Stats.Essence = 40;
                }

                Assume.That(flow.MoveTo(run.ForwardNodes[0].Id), Is.EqualTo(MoveResult.Moved));
            }

            string node5 = run.CurrentNodeId;
            var loadout = run.Loadout.Slots.Select(s => run.Loadout[s]?.Id).ToArray();
            int binderCards = run.Binder.Cards.Count;
            Quit(flow);

            var restarted = NewFlow("flow-restarted");
            var reloadedStore = new ProfileStore(_root);
            restarted.Begin(reloadedStore.Load("A"), reloadedStore);
            var resumed = restarted.Run;

            Assert.That(resumed, Is.Not.Null, "the run did not resume");
            Assert.That(resumed!.Seed, Is.EqualTo("chiki-1"));
            Assert.That(resumed.CurrentNodeId, Is.EqualTo(node5), "the run did not resume at the last saved node");
            Assert.That(resumed.Stats.Essence, Is.EqualTo(40));
            Assert.That(resumed.Binder.Cards.Count, Is.EqualTo(binderCards));
            Assert.That(resumed.Loadout.Slots.Select(s => resumed.Loadout[s]?.Id).ToArray(), Is.EqualTo(loadout), "the loadout did not survive");
            Assert.That(resumed.Route.Count, Is.EqualTo(6), "the route did not survive");
            Assert.That(restarted.Map, Is.Not.Null, "the resumed run is not on the map");
            Assert.That(restarted.PreRun, Is.Null);
        }

        [Test]
        public void one_run_per_profile()
        {
            var flow = FlowWithRun("A", "chiki-1", out var store);
            var run = flow.Run;

            var second = flow.TryStartRun("chiki-2");
            Quit(flow);
            var restarted = NewFlow("flow-restarted");
            var reloadedStore = new ProfileStore(_root);
            restarted.Begin(reloadedStore.Load("A"), reloadedStore);
            var third = restarted.TryStartRun("chiki-3");

            Assert.That(second, Is.EqualTo(StartRunResult.RunInProgress));
            Assert.That(flow == null || ReferenceEquals(flow.Run, run), Is.True, "the run in progress was replaced");
            Assert.That(third, Is.EqualTo(StartRunResult.RunInProgress));
            Assert.That(restarted.Run!.Seed, Is.EqualTo("chiki-1"));
        }

        [Test]
        public void quit_mid_battle_restarts_battle()
        {
            var flow = FlowWithRun("A", "chiki-1", out var store);
            var run = flow.Run!;
            while (!run.CurrentNode.IsBattle || run.CurrentNodeCompleted)
            {
                var next = run.ForwardNodes.FirstOrDefault(n => n.IsBattle) ?? run.ForwardNodes[0];
                Assume.That(flow.MoveTo(next.Id), Is.EqualTo(MoveResult.Moved));
            }

            run.Stats.Ard = 260;
            flow.SaveRun();
            var battle = run.StartNodeBattle(enemyHp: 1000);
            battle.AdvanceToBeat(12);
            run.Stats.Ard = 200;
            Assume.That(battle.Outcome, Is.Null, "the battle must still be in progress");
            string nodeId = run.CurrentNodeId;
            flow.SaveRun();
            Quit(flow);

            var restarted = NewFlow("flow-restarted");
            var reloadedStore = new ProfileStore(_root);
            restarted.Begin(reloadedStore.Load("A"), reloadedStore);
            var resumed = restarted.Run!;
            var again = resumed.StartNodeBattle(enemyHp: 1000);

            Assert.That(resumed.CurrentNodeId, Is.EqualTo(nodeId));
            Assert.That(resumed.CurrentNodeCompleted, Is.False, "the battle node counts as done");
            Assert.That(resumed.Stats.Ard, Is.EqualTo(260), "ARD was not restored to its value at battle start");
            Assert.That(again.Enemy.Id, Is.EqualTo(battle.Enemy.Id));
            Assert.That(again.CurrentTimeMs, Is.EqualTo(0), "the battle did not restart at beat 0");
            Assert.That(again.Loop, Is.EqualTo(0));
            Assert.That(again.Events.OfType<BeatStarted>().Count(), Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void unlock_survives_simulated_crash()
        {
            var flow = FlowWithRun("A", "chiki-1", out var store);
            var run = flow.Run!;
            var charms = flow.Content!.Charms.Charms.Select(c => c.Id).ToArray();
            Assume.That(charms, Has.Length.EqualTo(2));
            Assume.That(flow.Profile!.Meta.CharmUnlocks, Is.Empty);
            ClientTestContent.WalkToBoss(run);
            var battle = ClientTestContent.FightNodeBattle(run);
            Assume.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));

            flow.SettleBattle(battle);
            int gained = flow.GrantRp(Npc.Gambler, 1, RpSource.Dialogue);
            Quit(flow);
            var reloaded = new ProfileStore(_root).Load("A");
            var gambler = reloaded.RelationshipWith(Npc.Gambler);

            Assert.That(reloaded.BossesDefeated, Is.EqualTo(1));
            Assert.That(charms.All(reloaded.Meta.HasCharm), Is.True, "the Charm unlock was lost");
            Assert.That(gambler.LastSource, Is.EqualTo(RpSources.ToId(RpSource.Dialogue)), "the RP grant was lost");
            Assert.That(gambler.Level > 1 || gambler.Points == 1, Is.True, "the RP grant was lost");
            Assert.That(gained, Is.GreaterThanOrEqualTo(0));
        }
    }
}
