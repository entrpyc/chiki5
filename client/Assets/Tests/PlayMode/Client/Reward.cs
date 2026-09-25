#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Presenter;
using Chiki.Client.Profiles;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Client
{
    /// <summary>The reward panel after a won battle (PRD 3.7.2, P7.4).</summary>
    public class Reward
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "reward-" + Guid.NewGuid().ToString("N"));
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
        public IEnumerator three_offers_as_card_faces()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            var battle = ClientTestContent.FightNodeBattle(run);
            Assume.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won), "the battle must be won");
            flow.SettleBattle(battle);
            yield return null;
            var panel = flow.Reward;
            Assume.That(panel, Is.Not.Null, "the reward panel did not open");
            var offer = panel!.Offer;
            Assume.That(offer.Tier, Is.EqualTo(EncounterTier.Normal), "the win must be a Normal one");
            Assume.That(offer.Cards, Has.Count.EqualTo(3), "a Normal win offers three cards");
            var second = offer.Cards[1];
            int before = run.Binder.Cards.Count(c => c.Definition == second);
            var faces = panel.Faces.ToArray();

            panel.KeyDown(Key.RightArrow);
            panel.KeyDown(Key.Enter);
            yield return null;

            Assert.That(faces, Has.Length.EqualTo(3), "the panel does not show three faces");
            Assert.That(faces.Select(f => f.Size), Has.All.EqualTo(CardFaceSize.Full), "an offer is not a full face");
            Assert.That(faces.Select(f => f.Card), Is.EqualTo(offer.Cards), "the faces do not show the offered cards in order");
            Assert.That(offer.Picked, Is.SameAs(second), "choosing the second face did not take the second card");
            Assert.That(run.Binder.Cards.Count(c => c.Definition == second), Is.EqualTo(before + 1), "the second card is not in the Binder");
            Assert.That(flow.Reward, Is.Null, "the reward panel stayed open");
        }
    }
}
