#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Driver;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chiki.Client.Keys
{
    /// <summary>
    /// Turns keyboard events into slot presses for the driver (PRD 3.3.2). Eight physical keys
    /// drive the eight slots of the active line (<see cref="InputMap"/>); V toggles the active
    /// line on key-down without touching the battle (PRD 3.3.2.6); Space held with a slot key
    /// makes the press a Signature send (PRD 3.3.2.3). Every press carries the audio time of
    /// its input event (P12.3) and is handed to the driver with the card the slot holds; the
    /// loadout that fills the slots is <see cref="CardInSlot"/>'s.
    ///
    /// The Input System actions bound here deliver their events through <see cref="KeyDown"/>
    /// and <see cref="KeyUp"/>, which tests and replays call directly with a key and its timestamp.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleInput : MonoBehaviour
    {
        private readonly Dictionary<Key, InputAction> _actions = new Dictionary<Key, InputAction>();
        private bool _chordHeld;

        /// <summary>The driver the presses go to.</summary>
        public BattleDriver? Driver { get; set; }

        /// <summary>The card each slot holds right now; null for an empty slot. Both lines are present at all times (PRD 3.3.2.2).</summary>
        public Func<Slot, CardDefinition?>? CardInSlot { get; set; }

        /// <summary>The active line, 0 or 1 (PRD 3.3.2.2).</summary>
        public int ActiveLine { get; private set; }

        /// <summary>Whether Space is held right now (PRD 3.3.2.3).</summary>
        public bool ChordHeld => _chordHeld;

        /// <summary>A slot key went down: the slot on the active line, the audio time of the event and whether Space was held.</summary>
        public event Action<SlotPress>? SlotPressed;

        /// <summary>The active line changed (PRD 3.3.2.6).</summary>
        public event Action<int>? LineSwitched;

        /// <summary>The keys the component listens to: the eight slot keys, the line switch and the chord key.</summary>
        public static IEnumerable<Key> BoundKeys
        {
            get
            {
                foreach (SlotKey slotKey in Enum.GetValues(typeof(SlotKey)))
                {
                    yield return InputMap.PhysicalKey(slotKey);
                }

                yield return InputMap.LineSwitch;
                yield return InputMap.Chord;
            }
        }

        /// <summary>The control a key's action resolved to on the current devices, or null when none is connected; proves the binding is by physical key (PRD 3.3.2.5).</summary>
        public InputControl? BoundControl(Key key)
        {
            if (!_actions.TryGetValue(key, out var action) || action.controls.Count == 0)
            {
                return null;
            }

            return action.controls[0];
        }

        private void Awake()
        {
            foreach (var key in BoundKeys)
            {
                var captured = key;
                var action = new InputAction("key-" + key, InputActionType.Button, InputMap.ControlPath(key));
                action.performed += context => KeyDown(captured, context.time);
                action.canceled += context => KeyUp(captured, context.time);
                _actions[key] = action;
            }
        }

        private void OnEnable()
        {
            foreach (var action in _actions.Values)
            {
                action.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (var action in _actions.Values)
            {
                action.Disable();
            }

            _chordHeld = false;
        }

        private void OnDestroy()
        {
            foreach (var action in _actions.Values)
            {
                action.Dispose();
            }

            _actions.Clear();
        }

        /// <summary>
        /// A physical key went down at <paramref name="inputTime"/> on the realtime-since-startup
        /// timeline (the Input System's event time). A slot key produces a press on the active
        /// line; V toggles the line unless Space is held; Space starts a chord; any other key
        /// does nothing.
        /// </summary>
        public void KeyDown(Key key, double inputTime)
        {
            if (key == InputMap.Chord)
            {
                _chordHeld = true;
                return;
            }

            if (key == InputMap.LineSwitch)
            {
                OnLineSwitch();
                return;
            }

            var slotKey = InputMap.SlotKeyFor(key);
            if (slotKey != null)
            {
                OnSlotKey(slotKey.Value, inputTime);
            }
        }

        /// <summary>A physical key came up; only the chord key's release matters, and it produces no event (PRD 3.3.2.3).</summary>
        public void KeyUp(Key key, double inputTime)
        {
            if (key == InputMap.Chord)
            {
                _chordHeld = false;
            }
        }

        private void OnSlotKey(SlotKey slotKey, double inputTime)
        {
            var slot = new Slot(ActiveLine, slotKey);
            var clock = Driver != null ? Driver.Clock : null;
            int audioTimeMs = clock != null && clock.IsScheduled ? clock.AudioTimeMsAt(inputTime) : 0;
            var press = new SlotPress(slot, audioTimeMs, _chordHeld);
            SlotPressed?.Invoke(press);
            if (Driver != null)
            {
                Driver.Receive(press, CardInSlot?.Invoke(slot));
            }
        }

        /// <summary>V on key-down toggles the line; nothing reaches the battle, and it is ignored while Space is held (PRD 3.3.2.6).</summary>
        private void OnLineSwitch()
        {
            if (_chordHeld)
            {
                return;
            }

            ActiveLine = 1 - ActiveLine;
            LineSwitched?.Invoke(ActiveLine);
        }
    }
}
