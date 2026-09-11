#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Profiles;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Client
{
    public class Loadout
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "loadout-" + Guid.NewGuid().ToString("N"));
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

        private static int[] LoadoutIds(Run run)
        {
            return run.Loadout.Slots.Select(s => run.Loadout[s]?.Id ?? -1).ToArray();
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator enter_starts_battle_with_kept_loadout()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            var first = flow.EnterBattle(enemyHp: 1).Driver!.Battle!;
            var previousLoadout = LoadoutIds(run);
            ClientTestContent.FightToWin(first);
            Assume.That(first.Outcome, Is.EqualTo(BattleOutcome.Won), "the previous battle must be won");
            yield return null;
            yield return null;
            Assume.That(run.CurrentBattle, Is.Null, "the previous battle was not settled");
            Assume.That(flow.Battle, Is.Null, "the previous battle is still on screen");
            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no second battle node could be opened");
            Assume.That(flow.PreBattle!.LoadoutText, Does.Contain("Line 1"));

            flow.PreBattle.PressEnter();
            yield return null;

            Assert.That(flow.Battle, Is.Not.Null, "Enter did not start the battle");
            var second = flow.Battle!.Driver!.Battle;
            Assert.That(second, Is.Not.Null.And.Not.SameAs(first));
            Assert.That(run.CurrentBattle, Is.SameAs(second), "the run did not start the battle");
            Assert.That(second!.Loadout, Is.SameAs(run.Loadout), "the battle reads another loadout");
            Assert.That(LoadoutIds(run), Is.EqualTo(previousLoadout), "the loadout changed between battles");
            Assert.That(flow.PreBattle, Is.Null, "the pre-battle panel stayed open");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator cannot_confirm_with_empty_slot()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            Assume.That(flow.PreBattle!.ChooseEdit(), Is.True, "Edit did not open the Binder");
            yield return null;
            var binder = flow.Binder;
            Assume.That(binder, Is.Not.Null, "the Binder screen did not open");
            var slot = new Slot(1, SlotKey.I);
            var card = run.Loadout[slot];
            Assume.That(card, Is.Not.Null);

            bool cleared = binder!.ClearSlot(slot);
            bool confirmedEmpty = binder.ChooseConfirm();
            bool enabledEmpty = binder.ConfirmEnabled;
            string emptyText = binder.EmptySlotsText;
            var placement = binder.Fill(slot, card!);
            bool enabledFilled = binder.ConfirmEnabled;
            bool confirmedFilled = binder.ChooseConfirm();
            yield return null;

            Assert.That(cleared, Is.True);
            Assert.That(confirmedEmpty, Is.False, "Confirm proceeded with an empty slot");
            Assert.That(enabledEmpty, Is.False, "Confirm was enabled with an empty slot");
            Assert.That(emptyText, Does.Contain(Chiki.Sim.Loadout.Describe(new[] { slot })), "the panel does not name the empty slot");
            Assert.That(placement, Is.EqualTo(Placement.Accepted));
            Assert.That(enabledFilled, Is.True, "Confirm stayed disabled after refilling");
            Assert.That(confirmedFilled, Is.True, "Confirm did not proceed after refilling");
            Assert.That(flow.Battle, Is.Not.Null, "Confirm did not start the battle");
            Assert.That(run.CurrentBattle, Is.Not.Null);
            Assert.That(flow.Binder, Is.Null, "the Binder stayed open");
        }
    }
}
