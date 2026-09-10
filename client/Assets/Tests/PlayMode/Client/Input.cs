#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chiki.Client.Driver;
using Chiki.Client.Keys;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>
    /// Key events are injected at the component's key-event entry point with their timestamps:
    /// an unfocused headless player discards the Input System's synthetic events before they
    /// reach device state, so the bindings are proven by the controls they resolve to instead.
    /// </summary>
    public class Input : InputTestFixture
    {
        private Keyboard _keyboard = null!;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private static BattleInput AddInput(Rig rig, Func<Slot, CardDefinition?>? cardInSlot = null)
        {
            var input = rig.Root.AddComponent<BattleInput>();
            input.Driver = rig.Driver;
            input.CardInSlot = cardInSlot;
            return input;
        }

        private static void Tap(BattleInput input, Key key, double time)
        {
            input.KeyDown(key, time);
            input.KeyUp(key, time);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator eight_keys_map_to_slots()
        {
            var rig = ClientTestContent.ScheduledRig("input-keys", Beats.ToQuarterBeats(60));
            var input = AddInput(rig);
            yield return rig.WaitUntilAudioMs(0);

            foreach (SlotKey slotKey in Enum.GetValues(typeof(SlotKey)))
            {
                Tap(input, InputMap.PhysicalKey(slotKey), Now);
            }

            foreach (var other in new[] { Key.G, Key.H, Key.Enter, Key.Q, Key.LeftShift, Key.Space })
            {
                Tap(input, other, Now);
            }

            var received = rig.Driver.Inputs.Select(i => (i.Slot.Line, InputMap.SlotIndex(i.Slot.Key))).ToArray();
            Assert.That(received, Is.EqualTo(Enumerable.Range(0, 8).Select(i => (0, i))), "the eight keys did not map to slots 0-7 of line 1, or another key produced a slot");
            foreach (SlotKey slotKey in Enum.GetValues(typeof(SlotKey)))
            {
                var key = InputMap.PhysicalKey(slotKey);
                Assert.That(input.BoundControl(key), Is.SameAs(_keyboard[key]), $"slot {slotKey} is not bound to the physical {key} key");
            }

            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator labels_follow_layout()
        {
            // On AZERTY the physical A key produces Q.
            SetKeyInfo(Key.A, "q");
            var rig = ClientTestContent.ScheduledRig("input-labels", Beats.ToQuarterBeats(60));
            var input = AddInput(rig);
            yield return rig.WaitUntilAudioMs(0);

            string label = InputMap.Label(SlotKey.A, Keyboard.current);
            Tap(input, Key.A, Now);

            Assert.That(label, Is.EqualTo("Q"));
            Assert.That(InputMap.PhysicalKey(SlotKey.A), Is.EqualTo(Key.A));
            Assert.That(input.BoundControl(Key.A), Is.SameAs(_keyboard.aKey), "Ability 1 is no longer bound to the physical A key");
            Assert.That(rig.Driver.Inputs.Select(i => i.Slot), Is.EqualTo(new[] { new Slot(0, SlotKey.A) }), "the physical A key no longer maps to Ability 1");
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator switch_key_toggles_ungraded()
        {
            // The only enemy action is on beat 10; the press lands well before it.
            var rig = ClientTestContent.ScheduledRig("input-switch", Beats.ToQuarterBeats(10));
            var input = AddInput(rig, _ => ClientTestContent.LeftAttack10);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(600);
            int judgmentsBefore = battle.JudgmentLog.Count;

            Tap(input, InputMap.LineSwitch, Now);

            Assert.That(input.ActiveLine, Is.EqualTo(1));
            Assert.That(input.BoundControl(InputMap.LineSwitch), Is.SameAs(_keyboard.vKey));
            Assert.That(battle.JudgmentLog.Count, Is.EqualTo(judgmentsBefore), "the judgment log changed");
            Assert.That(battle.Events.OfType<InputJudged>(), Is.Empty, "the switch was graded");
            Assert.That(battle.Events.OfType<CooldownStarted>(), Is.Empty, "the switch started a cooldown");
            Assert.That(rig.Driver.Inputs, Is.Empty, "the switch reached the driver as a slot press");
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator switch_key_ignored_during_chord()
        {
            var rig = ClientTestContent.ScheduledRig("input-chord-switch", Beats.ToQuarterBeats(60));
            var input = AddInput(rig);
            yield return rig.WaitUntilAudioMs(0);

            input.KeyDown(InputMap.Chord, Now);
            Tap(input, InputMap.LineSwitch, Now);
            input.KeyUp(InputMap.Chord, Now);

            Assert.That(input.ActiveLine, Is.EqualTo(0));
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator press_after_switch_hits_line_2()
        {
            var line1 = new CardDefinition("card-line1", "Line 1", CardCategory.LeftAttack, 10, Tuning.CooldownMinBeats);
            var line2 = new CardDefinition("card-line2", "Line 2", CardCategory.LeftAttack, 10, Tuning.CooldownMinBeats);
            var cards = new Dictionary<Slot, CardDefinition>
            {
                [new Slot(0, SlotKey.D)] = line1,
                [new Slot(1, SlotKey.D)] = line2,
            };
            var rig = ClientTestContent.ScheduledRig("input-line2", Beats.ToQuarterBeats(1)); // attack at beat 1 = 500 ms
            var input = AddInput(rig, slot => cards.TryGetValue(slot, out var card) ? card : null);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(200);

            Tap(input, InputMap.LineSwitch, Now);
            yield return rig.WaitUntilAudioMs(500);
            Tap(input, Key.D, rig.Clock.RealtimeAt(500));

            var judged = battle.Events.OfType<InputJudged>().Single();
            Assert.That(input.ActiveLine, Is.EqualTo(1));
            Assert.That(judged.CardId, Is.EqualTo("card-line2"));
            Assert.That(judged.Slot, Is.EqualTo(new Slot(1, SlotKey.D)));
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator space_plus_key_sends()
        {
            var rig = ClientTestContent.ScheduledRig("input-send", Beats.ToQuarterBeats(1)); // attack at beat 1 = 500 ms
            var input = AddInput(rig, slot => slot.Key == SlotKey.D ? ClientTestContent.LeftAttack10 : null);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(500);

            input.KeyDown(InputMap.Chord, Now);
            Tap(input, Key.D, rig.Clock.RealtimeAt(500));
            input.KeyUp(InputMap.Chord, Now);
            yield return rig.WaitUntilAudioMs(800); // the window closes and the send resolves

            var press = rig.Driver.Inputs.Single();
            Assert.That(press.Slot, Is.EqualTo(new Slot(0, SlotKey.D)));
            Assert.That(press.SignatureSend, Is.True, "the driver did not receive a send");
            Assert.That(input.BoundControl(InputMap.Chord), Is.SameAs(_keyboard.spaceKey));
            Assert.That(battle.SignatureChain, Has.Count.EqualTo(1));
            Assert.That(battle.Events.OfType<CardBanked>().Count(), Is.EqualTo(1));
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator space_alone_does_nothing()
        {
            var rig = ClientTestContent.ScheduledRig("input-space", Beats.ToQuarterBeats(1));
            var input = AddInput(rig, _ => ClientTestContent.LeftAttack10);
            int slotEvents = 0;
            input.SlotPressed += _ => slotEvents++;
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(500);

            input.KeyDown(InputMap.Chord, rig.Clock.RealtimeAt(500));
            input.KeyUp(InputMap.Chord, rig.Clock.RealtimeAt(520));

            Assert.That(slotEvents, Is.EqualTo(0));
            Assert.That(rig.Driver.Inputs, Is.Empty);
            Assert.That(battle.Events.OfType<InputJudged>(), Is.Empty);
            Assert.That(input.ActiveLine, Is.EqualTo(0));
            Assert.That(input.ChordHeld, Is.False);
            rig.Destroy();
        }
    }
}
