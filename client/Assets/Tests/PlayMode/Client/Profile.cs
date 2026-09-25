#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using ProfileRecord = Chiki.Client.Profiles.Profile;

namespace Client
{
    public class Profile
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "profiles-" + Guid.NewGuid().ToString("N"));
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

        [Test]
        public void profiles_share_nothing()
        {
            var store = new ProfileStore(_root);
            ProfileRecord a = store.Create("A");
            ProfileRecord b = store.Create("B");

            a.CalibrationOffsetMs = 80;
            a.Calibrated = true;
            a.Settings.MetronomeOn = true;
            a.Meta.CharmUnlocks.Add("charm-test");
            store.Save(a);

            var reloaded = new ProfileStore(_root);
            ProfileRecord aAgain = reloaded.Load("A");
            ProfileRecord bAgain = reloaded.Load("B");

            Assert.That(bAgain.CalibrationOffsetMs, Is.EqualTo(0), "B took A's calibration offset");
            Assert.That(bAgain.Calibrated, Is.False);
            Assert.That(bAgain.Settings.MetronomeOn, Is.False, "B took A's settings");
            Assert.That(bAgain.Meta.CharmUnlocks, Is.Empty, "B took A's unlocks");
            Assert.That(aAgain.CalibrationOffsetMs, Is.EqualTo(80), "A's offset did not survive reload");
            Assert.That(aAgain.SchemaVersion, Is.EqualTo(ProfileRecord.CurrentSchemaVersion));
            Assert.That(reloaded.List(), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(reloaded.FileOf("A"), Is.Not.EqualTo(reloaded.FileOf("B")));
            Assert.That(aAgain.RunLogFolder, Is.Not.EqualTo(bAgain.RunLogFolder), "the profiles share a run-log folder");
            Assert.That(Directory.Exists(aAgain.RunLogFolder), Is.True);
            Assert.That(File.ReadAllText(reloaded.FileOf("B")), Does.Not.Contain("charm-test"), "B's file holds A's data");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator fought_enemies_recorded_and_migrated()
        {
            const string ren = "enemy-ren";
            var store = new ProfileStore(_root);
            Directory.CreateDirectory(store.FolderOf("A"));
            File.WriteAllText(
                store.FileOf("A"),
                "{ \"schemaVersion\": 2, \"name\": \"A\", \"created\": \"2026-09-01T00:00:00.0000000Z\", \"calibrated\": true, \"bossesDefeated\": 1 }");

            ProfileRecord migrated = store.Load("A");

            Assert.That(migrated.SchemaVersion, Is.EqualTo(3), "the schema-2 file did not migrate to 3");
            Assert.That(migrated.EnemiesFought, Is.Not.Null.And.Empty, "the migrated profile does not start with an empty enemiesFought");
            Assert.That(migrated.BossesDefeated, Is.EqualTo(1), "the migration lost a schema-2 field");

            string seed = ClientTestContent.SeedFacing(ren, out var nodeId);
            var host = new GameObject("flow-profile");
            _hosts.Add(host);
            var flow = host.AddComponent<GameFlow>();
            flow.Begin(migrated, store);
            Assume.That(flow.TryStartRun(seed), Is.EqualTo(StartRunResult.Started));
            yield return ClientTestContent.OpenBattleAgainst(flow, nodeId);
            Assume.That(flow.PreBattle, Is.Not.Null, "Ren's battle node did not open");
            var battle = flow.EnterBattle(enemyHp: 1).Driver!.Battle!;
            Assume.That(battle.Enemy.Id, Is.EqualTo(ren));
            ClientTestContent.FightToWin(battle);
            Assume.That(battle.Outcome, Is.Not.Null, "the battle against Ren did not end");
            yield return null;
            yield return null;
            Assume.That(flow.Battle, Is.Null, "the battle against Ren was not settled");

            ProfileRecord reloaded = new ProfileStore(_root).Load("A");

            Assert.That(reloaded.EnemiesFought, Is.EqualTo(new[] { ren }), "the profile does not hold enemy-ren exactly once");
            Assert.That(File.ReadAllText(store.FileOf("A")), Does.Contain("\"schemaVersion\": 3"), "the file was not written at schema 3");
        }
    }
}
