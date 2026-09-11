#nullable enable
using System;
using System.Text;
using Chiki.Client.Keys;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The pre-battle panel (PRD 3.5.6, P23.2): opened over the map on a battle node, it names
    /// the enemy and lists the loadout as it stands, kept from the previous battle. Enter
    /// starts the battle at once, the one input the rule asks for; Edit opens the Binder
    /// (PRD 3.5.5).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreBattlePanel : MonoBehaviour
    {
        private Button _edit = null!;
        private Button _fight = null!;
        private UnityEngine.UI.Text _loadout = null!;
        private ScreenKeys? _keys;

        /// <summary>Enter was pressed: the battle starts with the loadout as listed.</summary>
        public event Action? EnterChosen;

        public event Action? EditChosen;

        public string LoadoutText => _loadout.text;

        public static PreBattlePanel Build(Transform? parent, Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var canvas = ScreenFactory.Canvas("PreBattlePanel", parent, 40);
            var panel = canvas.gameObject.AddComponent<PreBattlePanel>();
            var root = canvas.transform;
            ScreenFactory.Fill("Dim", root, new Color(0f, 0f, 0f, 0.6f));
            var face = HudFactory.Image("Panel", root, ScreenFactory.Panel, Vector2.zero, new Vector2(1100f, 760f));
            face.raycastTarget = true;
            var enemy = run.CurrentNode.IsBattle ? run.CurrentNodeEnemy.Name : "";
            ScreenFactory.Label("Title", face.transform, Strings.Format("prebattle.title", enemy, Labels.NodeLabel(run.CurrentNode.Type)), 52, new Vector2(0f, 320f), new Vector2(1000f, 80f), TextAnchor.MiddleCenter);
            ScreenFactory.Label("Kept", face.transform, Strings.Get("prebattle.kept"), 30, new Vector2(0f, 250f), new Vector2(1000f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            panel._loadout = ScreenFactory.Label("Loadout", face.transform, Describe(run.Loadout), 24, new Vector2(0f, -20f), new Vector2(1000f, 440f), TextAnchor.UpperLeft);
            panel._fight = ScreenFactory.Button("Fight", face.transform, Strings.Get("prebattle.fight"), new Vector2(-240f, -320f), new Vector2(400f, 80f), () => panel.EnterChosen?.Invoke());
            panel._edit = ScreenFactory.Button("Edit", face.transform, Strings.Get("prebattle.edit"), new Vector2(240f, -320f), new Vector2(400f, 80f), () => panel.EditChosen?.Invoke());
            panel._keys = new ScreenKeys(new[] { Key.Enter, Key.NumpadEnter }, panel.KeyDown);
            return panel;
        }

        /// <summary>The loadout as the player reads it: both lines, slot by slot, with the card each holds.</summary>
        public static string Describe(Loadout loadout)
        {
            var text = new StringBuilder();
            for (int line = 0; line < Slot.LineCount; line++)
            {
                text.Append(Strings.Format("line.label", line + 1)).Append(": ");
                bool first = true;
                foreach (var slot in loadout.Slots)
                {
                    if (slot.Line != line)
                    {
                        continue;
                    }

                    if (!first)
                    {
                        text.Append("  ");
                    }

                    first = false;
                    var card = loadout[slot];
                    text.Append(slot.Key).Append(' ').Append(card == null ? Strings.Get("slot.empty") : card.Definition.Name);
                }

                text.Append('\n');
            }

            return text.ToString();
        }

        /// <summary>Presses Enter the way the player would.</summary>
        public void PressEnter()
        {
            KeyDown(Key.Enter);
        }

        public bool ChooseFight()
        {
            return ScreenFactory.Submit(_fight);
        }

        public bool ChooseEdit()
        {
            return ScreenFactory.Submit(_edit);
        }

        public void KeyDown(Key key)
        {
            if (key == Key.Enter || key == Key.NumpadEnter)
            {
                EnterChosen?.Invoke();
            }
        }

        private void OnDestroy()
        {
            _keys?.Dispose();
            _keys = null;
        }
    }
}
