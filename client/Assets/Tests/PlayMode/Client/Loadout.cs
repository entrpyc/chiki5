#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Presenter;
using Chiki.Client.Profiles;
using Chiki.Client.Text;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator binder_previews_card_faces()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            Assume.That(flow.PreBattle!.ChooseEdit(), Is.True, "Edit did not open the Binder");
            yield return null;
            var binder = flow.Binder;
            Assume.That(binder, Is.Not.Null, "the Binder screen did not open");
            Assume.That(binder!.Candidates, Has.Count.GreaterThanOrEqualTo(2), "the selected slot needs two cards to move between");

            var slotFaces = binder.SlotFaces;
            var owned = binder.OwnedFaces.ToArray();
            var first = binder.PreviewFace.Card;
            binder.KeyDown(Key.DownArrow);
            var moved = binder.Highlighted;
            var preview = binder.PreviewFace;

            Assert.That(slotFaces, Has.Count.EqualTo(16), "the Binder does not show sixteen slot faces");
            foreach (var slot in Slot.All)
            {
                var face = slotFaces[slot];
                Assert.That(face.Size, Is.EqualTo(CardFaceSize.Compact), slot + " is not a compact face");
                Assert.That(face.Card, Is.SameAs(run.Loadout[slot]!.Definition), slot + "'s face does not show the card in the slot");
            }

            Assert.That(owned.Select(f => f.Size), Has.All.EqualTo(CardFaceSize.Compact), "an owned card is not a compact face");
            Assert.That(owned.Select(f => f.Card), Is.EqualTo(run.Binder.Cards.Select(c => c.Definition)), "not every owned card appears as a face");
            Assert.That(first, Is.SameAs(binder.Candidates[0].Definition), "the preview did not start on the highlighted card");
            Assert.That(moved, Is.SameAs(binder.Candidates[1]), "Down did not move the selection to the next card");
            Assert.That(preview.Size, Is.EqualTo(CardFaceSize.Full), "the preview is not a full face");
            Assert.That(preview.Card, Is.SameAs(moved!.Definition), "the preview does not show the card the selection moved to");
        }

        /// <summary>P9.2: the Binder opened from the map shows lifespans and Traits on its faces, refuses every edit and returns to the same node.</summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator map_binder_is_read_only()
        {
            var fixtures = ClientTestContent.LoadRunContent();
            var unstable = new CardDefinition("card-test-unstable", "Flicker", CardCategory.LeftAttack, 8, Tuning.CooldownMinBeats, CardRarity.Common, CardClass.Unstable, lifespan: 3);
            var content = new RunContent(
                fixtures.Starter,
                fixtures.Charms,
                fixtures.Imprints,
                new[] { new CardSet("test-unstable", new[] { unstable }) },
                fixtures.Enemies,
                fixtures.Traits);
            var trait = content.FindTrait("trait-tempered");
            Assume.That(trait, Is.Not.Null, "the fixture Trait is missing");

            var starter = Binder.Starter(content.Starter);
            int next = starter.NextId;
            var jab = content.FindCard("card-jab")!;
            var traited = CardInstance.Restore(next, jab, false, trait!.Id, null, null);
            var fading = CardInstance.Restore(next + 1, unstable, false, null, 2, null);
            var cards = starter.Cards.Concat(new[] { traited, fading }).ToList();
            var run = ClientTestContent.RunAtEntry(content, "chiki-1", cards: cards);
            var flow = ClientTestContent.FlowResuming(_root, "A", run, content, _hosts);
            yield return null;
            var map = flow.Map;
            Assume.That(map, Is.Not.Null, "the run did not open on the map");
            string node = run.CurrentNodeId;
            var loadoutBefore = LoadoutIds(run);

            bool opened = map!.ChooseBinder();
            yield return null;
            var binder = flow.Binder;
            Assume.That(binder, Is.Not.Null, "the Binder button did not open the Binder");

            var owned = binder!.OwnedFaces;
            var traitedFace = owned[run.Binder.Cards.ToList().IndexOf(traited)];
            var fadingFace = owned[run.Binder.Cards.ToList().IndexOf(fading)];
            var traitedCard = traitedFace.Card;
            string traitText = traitedFace.TraitText;
            var fadingCard = fadingFace.Card;
            string lifespanText = fadingFace.LifespanText;
            bool readOnly = binder.ReadOnly;
            bool confirmShown = binder.ConfirmShown;
            var slot = new Slot(0, SlotKey.E);
            var other = binder.Candidates.FirstOrDefault(c => !ReferenceEquals(c, run.Loadout[slot]));
            Assume.That(other, Is.Not.Null, "slot E has no other card to place");
            binder.Select(slot);
            var placement = binder.Fill(slot, other!);
            bool cleared = binder.ClearSlot(slot);
            binder.KeyDown(Key.Enter);
            binder.KeyDown(Key.Delete);
            var loadoutAfter = LoadoutIds(run);
            bool back = binder.ChooseBack();
            yield return null;

            Assert.That(opened, Is.True, "the Binder button did not accept the press");
            Assert.That(readOnly, Is.True, "the Binder opened from the map is editable");
            Assert.That(traitedCard, Is.SameAs(jab), "the card with a Trait is not shown as a face");
            Assert.That(traitText, Is.EqualTo(trait.Name), "the face does not name the card's Trait");
            Assert.That(fadingCard, Is.SameAs(unstable), "the Unstable card is not shown as a face");
            Assert.That(lifespanText, Is.EqualTo(Strings.Format("card.battles_left", 2)).And.Contain("2 battles"), "the face does not show the battles left");
            Assert.That(placement, Is.Null, "a placement was not refused");
            Assert.That(cleared, Is.False, "clearing a slot was not refused");
            Assert.That(loadoutAfter, Is.EqualTo(loadoutBefore), "the loadout changed in the read-only Binder");
            Assert.That(confirmShown, Is.False, "the read-only Binder shows Confirm");
            Assert.That(back, Is.True, "Back did not accept the press");
            Assert.That(flow.Binder, Is.Null, "Back did not close the Binder");
            Assert.That(flow.Map, Is.Not.Null, "Back did not return to the map");
            Assert.That(run.CurrentNodeId, Is.EqualTo(node), "the run left its node");
            Assert.That(flow.Map!.Covered, Is.False, "the map's keys stayed resting after Back");
        }
    }
}
