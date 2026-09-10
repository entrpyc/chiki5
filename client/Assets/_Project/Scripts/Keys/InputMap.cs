#nullable enable
using System;
using Chiki.Sim;
using UnityEngine.InputSystem;

namespace Chiki.Client.Keys
{
    /// <summary>
    /// The fixed keyboard layout (PRD 3.3.2.1): Q and W are Ability slots, E and R Left Attack,
    /// U and I Right Attack, O and P Defense, on whichever line is active. V switches
    /// lines (PRD 3.3.2.6) and Space held with a slot key sends to the Signature Chain
    /// (PRD 3.3.2.3). Keys are physical positions (PRD 3.3.2.5), so the top-row shape holds on
    /// any layout; the label a slot shows is the character that key produces right now. The map
    /// is a constant: there is no rebinding path.
    /// </summary>
    public static class InputMap
    {
        /// <summary>The dedicated line-switch key (PRD 3.3.2.6); never part of a chord.</summary>
        public const Key LineSwitch = Key.V;

        /// <summary>The chord key: held with a slot key it sends the card to the Signature Chain (PRD 3.3.2.3); alone it does nothing.</summary>
        public const Key Chord = Key.Space;

        /// <summary>The physical key of a slot on the active line.</summary>
        public static Key PhysicalKey(SlotKey slotKey)
        {
            switch (slotKey)
            {
                case SlotKey.Q: return Key.Q;
                case SlotKey.W: return Key.W;
                case SlotKey.E: return Key.E;
                case SlotKey.R: return Key.R;
                case SlotKey.U: return Key.U;
                case SlotKey.I: return Key.I;
                case SlotKey.O: return Key.O;
                case SlotKey.P: return Key.P;
                default: throw new ArgumentOutOfRangeException(nameof(slotKey), slotKey, "Unknown slot key.");
            }
        }

        /// <summary>The slot key a physical key drives, or null when the key drives no slot.</summary>
        public static SlotKey? SlotKeyFor(Key key)
        {
            switch (key)
            {
                case Key.Q: return SlotKey.Q;
                case Key.W: return SlotKey.W;
                case Key.E: return SlotKey.E;
                case Key.R: return SlotKey.R;
                case Key.U: return SlotKey.U;
                case Key.I: return SlotKey.I;
                case Key.O: return SlotKey.O;
                case Key.P: return SlotKey.P;
                default: return null;
            }
        }

        /// <summary>The slot's index within its line, 0–7 in key order (PRD 3.3.2.1).</summary>
        public static int SlotIndex(SlotKey slotKey)
        {
            return (int)slotKey;
        }

        /// <summary>The Input System binding path of a slot key's physical control.</summary>
        public static string ControlPath(SlotKey slotKey)
        {
            return "<Keyboard>/" + ControlName(PhysicalKey(slotKey));
        }

        /// <summary>The Input System binding path of a physical key.</summary>
        public static string ControlPath(Key key)
        {
            return "<Keyboard>/" + ControlName(key);
        }

        /// <summary>
        /// The label a slot shows: the character its physical key produces on the current layout
        /// (PRD 3.3.2.5), upper-cased; the physical key's own name when the layout reports none.
        /// </summary>
        public static string Label(SlotKey slotKey, Keyboard keyboard)
        {
            if (keyboard == null)
            {
                throw new ArgumentNullException(nameof(keyboard));
            }

            var key = PhysicalKey(slotKey);
            string name = keyboard[key].displayName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = key.ToString();
            }

            return name.ToUpperInvariant();
        }

        private static string ControlName(Key key)
        {
            switch (key)
            {
                case Key.Space: return "space";
                default: return key.ToString().ToLowerInvariant();
            }
        }
    }
}
