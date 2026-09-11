#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Sim;
using Chiki.Sim.Data;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Client
{
    public class RunLog
    {
        private const string ProfileName = "profile-zulu-77";
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "runlog-" + Guid.NewGuid().ToString("N"));
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

        [Test]
        public void log_written_with_fields_and_no_identity()
        {
            var store = new ProfileStore(_root);
            var profile = store.Create(ProfileName);
            profile.Calibrated = true;
            store.Save(profile);
            var host = new GameObject("flow-log");
            _hosts.Add(host);
            var flow = host.AddComponent<GameFlow>();
            flow.Begin(profile, store);
            Assume.That(flow.TryStartRun("chiki-log"), Is.EqualTo(StartRunResult.Started));
            var run = flow.Run!;
            var enemy = flow.Content!.FindEnemy("enemy-kess")!;

            for (int i = 0; i < 2; i++)
            {
                var won = run.StartBattle(enemy, enemyHp: 1);
                ClientTestContent.FightToWin(won);
                Assume.That(won.Outcome, Is.EqualTo(BattleOutcome.Won));
                flow.SettleBattle(won);
            }

            run.Stats.Ard = 5;
            var fatal = run.StartBattle(enemy, enemyHp: 1000);
            fatal.AdvanceToBeat(enemy.Track.LengthBeats);
            Assume.That(fatal.Outcome, Is.EqualTo(BattleOutcome.Died), "the third battle must end by death");
            flow.SettleBattle(fatal);

            Assume.That(run.IsOver, Is.True);
            var path = flow.LastRunLogPath;
            Assert.That(path, Is.Not.Null, "no run log was written");
            Assert.That(File.Exists(path), Is.True);
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(path!)), Is.EqualTo(Path.GetFullPath(profile.RunLogFolder).TrimEnd(Path.DirectorySeparatorChar)));
            var text = File.ReadAllText(path!);
            var log = JsonValue.Parse(text);
            var battles = log["battles"].Items;
            var battleFields = new[] { "enemy", "durationBeats", "durationSeconds", "judgments", "damageTaken", "signaturesFired", "cardsPerSlot", "outcome", "sameEnemyAsPrevious" };
            var judgmentFields = new[] { "perfect", "good", "miss", "noInput" };

            Assert.That(log["seed"].AsString(), Is.EqualTo("chiki-log"));
            Assert.That(log["outcome"].AsString(), Is.EqualTo("died"));
            Assert.That(log["assist"].AsBool(), Is.False);
            Assert.That(log["difficultyModifiers"].Items, Is.Empty);
            Assert.That(log["route"].Items, Is.Not.Empty);
            Assert.That(battles, Has.Count.EqualTo(3));
            foreach (var battle in battles)
            {
                foreach (var field in battleFields)
                {
                    Assert.That(battle.Has(field), Is.True, "a battle record lacks " + field);
                }

                foreach (var field in judgmentFields)
                {
                    Assert.That(battle["judgments"].Has(field), Is.True, "a battle record lacks judgments." + field);
                }

                Assert.That(battle["enemy"].AsString(), Is.EqualTo("enemy-kess"));
            }

            Assert.That(battles[2]["outcome"].AsString(), Is.EqualTo("died"));
            Assert.That(battles[2]["damageTaken"].AsInt(), Is.GreaterThan(0));
            Assert.That(battles.Select(b => b["sameEnemyAsPrevious"].AsBool()), Is.EqualTo(new[] { false, true, true }));
            Assert.That(text, Does.Not.Contain(ProfileName), "the log names the profile");
            Assert.That(text, Does.Not.Contain(Environment.UserName), "the log names the OS user");
            Assert.That(text, Does.Not.Contain(_root), "the log holds the profile's path");
            Assert.That(profile.RunInProgress, Is.Null, "the ended run is still in progress");
            Assert.That(new ProfileStore(_root).Load(ProfileName).RunHistory.Select(e => (e.Seed, e.Outcome)), Is.EqualTo(new[] { ("chiki-log", "died") }));
        }
    }
}
