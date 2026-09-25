#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Presenter;
using Chiki.Client.Profiles;
using Chiki.Client.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Client
{
    public class Crp
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "crp-" + Guid.NewGuid().ToString("N"));
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
        public IEnumerator visible_on_map_and_battle_with_change_label()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            run.Stats.Crp = 7;
            flow.EnterMap();
            yield return null;
            string mapCrp = flow.Map!.CrpText;
            Assume.That(flow.Map.CrpChangeText, Is.Null, "no change label shows before a transition");

            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            string? changeLabel = flow.Map!.CrpChangeText;
            int crpAfterTransition = run.Stats.Crp;
            run.Stats.Crp = 7;
            flow.PreBattle!.PressEnter();
            yield return null;
            Assume.That(flow.Battle, Is.Not.Null, "the battle did not start");
            string hudCrp = flow.Battle!.Hud!.Crp.Text;

            Assert.That(mapCrp, Is.EqualTo(Strings.Format("crp.label", 7)), "the map header does not show CRP 7");
            Assert.That(mapCrp, Does.Contain("7"));
            Assert.That(hudCrp, Is.EqualTo(Strings.Format("crp.label", 7)), "the battle HUD does not show CRP 7");
            Assert.That(crpAfterTransition, Is.GreaterThanOrEqualTo(8), "the transition did not add CRP");
            Assert.That(changeLabel, Is.Not.Null, "no change label appeared on the transition");
            Assert.That(changeLabel, Does.Contain("+1"));
            Assert.That(changeLabel, Does.Contain(Strings.Get("crp.source.node-transition")));
        }

        /// <summary>P4.5: the CRP value sits beside the CRP icon, on the map and in battle alike.</summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator crp_badge_on_map_and_battle()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var flow = ClientTestContent.FlowWithRun(_root, "B", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            run.Stats.Crp = 7;
            flow.EnterMap();
            yield return null;

            var map = flow.Map!;
            var badge = catalogue.Sprite(CrpView.UiKind, CrpView.CrpIconId);
            Assert.That(map.CrpText, Does.Contain("7"), "the map header does not show CRP 7");
            Assert.That(map.CrpIcon.gameObject.activeInHierarchy, Is.True, "the map shows no CRP icon");
            Assert.That(map.CrpIcon.sprite, Is.SameAs(badge), "the map's CRP icon is not the catalogue's");
            Assert.That(map.CrpIcon.rectTransform.position.x, Is.LessThan(map.CrpLabel.position.x), "the map's CRP value does not sit beside its icon");

            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            run.Stats.Crp = 7;
            flow.PreBattle!.PressEnter();
            yield return null;
            Assume.That(flow.Battle, Is.Not.Null, "the battle did not start");

            var crp = flow.Battle!.Hud!.Crp;
            Assert.That(crp.Text, Does.Contain("7"), "the battle HUD does not show CRP 7");
            Assert.That(crp.Icon.gameObject.activeInHierarchy, Is.True, "the battle HUD shows no CRP icon");
            Assert.That(crp.Icon.sprite, Is.SameAs(badge), "the battle HUD's CRP icon is not the catalogue's");
            Assert.That(crp.Icon.rectTransform.position.x, Is.LessThan(crp.Label.position.x), "the battle HUD's CRP value does not sit beside its icon");

            Assert.That(catalogue.Missing, Does.Not.Contain(CrpView.UiKind + "/" + CrpView.CrpIconId));
        }
    }
}
