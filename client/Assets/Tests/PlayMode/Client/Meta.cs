#nullable enable
using System;
using System.IO;
using System.Linq;
using Chiki.Client.Content;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Sim;
using Chiki.Sim.Data;
using NUnit.Framework;
using UnityEngine;

namespace Client
{
    public class Meta
    {
        private string _root = null!;

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "meta-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveRoot()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void unlocks_survive_reload()
        {
            var charm = CharmLoader.SetFromJson(ContentFiles.ReadText("charms/fixtures.json")).Charms.First();
            var store = new ProfileStore(_root);
            var a = store.Create("A");

            bool granted = a.Meta.UnlockCharm(charm.Id);
            bool grantedAgain = a.Meta.UnlockCharm(charm.Id);
            store.Save(a);
            var reloaded = new ProfileStore(_root).Load("A");

            Assert.That(granted, Is.True);
            Assert.That(grantedAgain, Is.False, "an unlock is granted once");
            Assert.That(reloaded.Meta.HasCharm(charm.Id), Is.True, "the Charm unlock did not survive reload");
            Assert.That(reloaded.Meta.CharmUnlocks, Is.EqualTo(new[] { charm.Id }));
        }
        [Test]
        public void first_boss_unlocks_fixture_charms()
        {
            var content = ClientTestContent.LoadRunContent();
            var store = new ProfileStore(_root);
            var profile = store.Create("A");
            Assume.That(profile.Meta.CharmUnlocks, Is.Empty);
            Assume.That(profile.BossesDefeated, Is.EqualTo(0));
            var run = new RunSetup(profile.Meta.CharmUnlocks).Start(content, "chiki-1");
            ClientTestContent.WalkToBoss(run);
            var progress = new RunProgress(profile, content.Charms);

            var battle = ClientTestContent.FightNodeBattle(run);
            Assume.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won), "the Boss battle must be won");
            run.SettleBattle(battle);
            var unlocked = progress.Apply(run, out bool changed);
            store.Save(profile);
            var reloaded = new ProfileStore(_root).Load("A");

            var fixtureCharms = content.Charms.Charms.Select(c => c.Id).ToArray();
            Assert.That(run.Events.OfType<BossDefeated>().Count(), Is.EqualTo(1));
            Assert.That(changed, Is.True);
            Assert.That(profile.BossesDefeated, Is.EqualTo(1));
            Assert.That(unlocked, Is.EquivalentTo(fixtureCharms));
            Assert.That(fixtureCharms, Has.Length.EqualTo(2), "both fixture Charms are in the table");
            Assert.That(fixtureCharms.All(profile.Meta.HasCharm), Is.True, "both fixture Charms are unlocked");
            Assert.That(reloaded.BossesDefeated, Is.EqualTo(1), "the count survives reload");
            Assert.That(reloaded.Meta.CharmUnlocks, Is.EquivalentTo(fixtureCharms));
        }

    }
}
