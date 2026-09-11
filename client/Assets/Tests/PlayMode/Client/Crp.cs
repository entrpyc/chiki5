#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    }
}
