#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Client.Screens;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Client
{
    /// <summary>The enemy card before a fight (PRD 3.6.26, P8.2).</summary>
    public class EnemyCard
    {
        private const string Ren = "enemy-ren";

        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "enemycard-" + Guid.NewGuid().ToString("N"));
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
            ClientTestContent.ClearCatalogues();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator shows_every_field()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            string seed = ClientTestContent.SeedFacing(Ren, out var nodeId);
            var flow = ClientTestContent.FlowWithRun(_root, "A", seed, _hosts, out var store);
            yield return ClientTestContent.OpenBattleAgainst(flow, nodeId);
            Assume.That(flow.PreBattle, Is.Not.Null, "Ren's battle node did not open");
            var ren = flow.Run!.CurrentNodeEnemy;
            Assume.That(ren.Id, Is.EqualTo(Ren));
            Assume.That(flow.Profile!.HasFought(Ren), Is.False, "the fresh profile has fought Ren already");

            var card = flow.PreBattle!.Enemy;
            Assert.That(card, Is.Not.Null, "the pre-battle panel has no enemy card");
            Assert.That(card!.Portrait.sprite, Is.Not.Null.And.SameAs(catalogue.Find(Chiki.Client.Screens.EnemyCard.PortraitKind, "ren")), "the card does not show Ren's portrait");
            Assert.That(card.NameText.text, Is.EqualTo("Ren"));
            Assert.That(card.BpmText.text, Is.EqualTo("120 BPM"));
            Assert.That(card.Powers.Select(p => p.Name.text), Is.EqualTo(new[] { "Iron Veil", "Guard" }), "the powers are not Iron Veil and Guard by name");
            Assert.That(card.Powers[0].Icon.sprite, Is.Not.Null.And.SameAs(catalogue.Find("ability", "iron-veil")), "Iron Veil does not carry its icon");
            Assert.That(card.Powers[1].Icon.sprite, Is.Not.Null.And.SameAs(catalogue.Find("trait", "guard")), "Guard does not carry its icon");
            Assert.That(card.QuoteText, Is.Not.Null);
            Assert.That(card.QuoteText!.text, Does.Contain("Count with me. One... two... and hold."), "the quote line is missing");
            Assert.That(card.QuoteText.gameObject.activeInHierarchy, Is.True);
            Assert.That(card.BadgeIsNew, Is.True, "a never-fought enemy's badge is not New");
            Assert.That(card.BadgeLabel.text, Is.EqualTo("New"));
            Assert.That(card.Badge.sprite, Is.Not.Null.And.SameAs(catalogue.Find("ui", Chiki.Client.Screens.EnemyCard.BadgeNewId)), "the New badge is not on its plate");
            Assert.That(flow.PreBattle.EditVisible, Is.True, "the Edit (Binder) button is missing");
            Assert.That(flow.PreBattle.FightVisible, Is.True, "the Fight button is missing");

            // One battle against Ren, then the same node on a new run of the same seed.
            var battle = flow.EnterBattle(enemyHp: 1).Driver!.Battle!;
            ClientTestContent.FightToWin(battle);
            Assume.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
            yield return null;
            yield return null;
            Assume.That(flow.Profile!.HasFought(Ren), Is.True, "the battle against Ren was not recorded");

            var profile = store.Load("A");
            profile.RunInProgress = null;
            store.Save(profile);
            var again = new GameObject("flow-again");
            _hosts.Add(again);
            var second = again.AddComponent<GameFlow>();
            second.Begin(profile, store);
            Assume.That(second.TryStartRun(seed), Is.EqualTo(StartRunResult.Started));
            yield return ClientTestContent.OpenBattleAgainst(second, nodeId);
            Assume.That(second.PreBattle, Is.Not.Null, "Ren's battle node did not open on the second run");

            var fought = second.PreBattle!.Enemy!;
            Assert.That(fought.BadgeIsNew, Is.False, "the badge still reads New after a battle against Ren");
            Assert.That(fought.BadgeLabel.text, Is.EqualTo("Tank"), "the badge does not name Ren's role");
            Assert.That(fought.Badge.sprite, Is.Not.Null.And.SameAs(catalogue.Find("role", "tank")), "the badge does not show the Tank icon");
        }
    }
}
