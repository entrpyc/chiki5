#nullable enable
using System;
using Chiki.Sim;
using UnityEngine.InputSystem;

namespace Chiki.Client.Keys
{
    /// <summary>
    /// The fixed keyboard layout (PRD 3.3.2.1): A and S are Ability slots, D and F Left Attack,
    /// J and K Right Attack, L and ; Defense, on whichever line is active. V switches
    /// lines (PRD 3.3.2.6) and Space held with a slot key sends to the Signature Chain
    /// (PRD 3.3.2.3). Keys are physical positions (PRD 3.3.2.5), so the home-row shape holds on
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
                case SlotKey.A: return Key.A;
                case SlotKey.S: return Key.S;
                case SlotKey.D: return Key.D;
                case SlotKey.F: return Key.F;
                case SlotKey.J: return Key.J;
                case SlotKey.K: return Key.K;
                case SlotKey.L: return Key.L;
                case SlotKey.Semicolon: return Key.Semicolon;
                default: throw new ArgumentOutOfRangeException(nameof(slotKey), slotKey, "Unknown slot key.");
            }
        }

        /// <summary>The slot key a physical key drives, or null when the key drives no slot.</summary>
        public static SlotKey? SlotKeyFor(Key key)
        {
            switch (key)
            {
                case Key.A: return SlotKey.A;
                case Key.S: return SlotKey.S;
                case Key.D: return SlotKey.D;
                case Key.F: return SlotKey.F;
                case Key.J: return SlotKey.J;
                case Key.K: return SlotKey.K;
                case Key.L: return SlotKey.L;
                case Key.Semicolon: return SlotKey.Semicolon;
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
                name = key == Key.Semicolon ? ";" : key.ToString();
            }

            return name.ToUpperInvariant();
        }

        private static string ControlName(Key key)
        {
            switch (key)
            {
                case Key.Semicolon: return "semicolon";
                case Key.Space: return "space";
                default: return key.ToString().ToLowerInvariant();
            }
        }
    }
}
