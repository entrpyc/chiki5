#nullable enable
using System;
using System.Collections.Generic;
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
    /// The Binder screen (PRD 3.5.5, P23.3): every card the run owns with its preview, and the
    /// sixteen slots of the loadout. The arrow keys move between slots, Up and Down cycle the
    /// Binder cards the selected slot may hold, Enter places the highlighted card, Delete or
    /// Backspace clears the slot. Confirm is disabled while any slot is empty and the panel
    /// names the empty slots; Confirm proceeds to the battle. Back returns to the pre-battle
    /// panel with the loadout as edited.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BinderScreen : MonoBehaviour
    {
        private Binder _binder = null!;
        private readonly Dictionary<Slot, UnityEngine.UI.Text> _slotLabels = new Dictionary<Slot, UnityEngine.UI.Text>();
        private UnityEngine.UI.Text _cards = null!;
        private UnityEngine.UI.Text _candidate = null!;
        private UnityEngine.UI.Text _empty = null!;
        private Button _confirm = null!;
        private Button _back = null!;
        private ScreenKeys? _keys;
        private int _cursor;
        private int _candidateIndex;

        /// <summary>Confirm was chosen with every slot filled: the battle may start.</summary>
        public event Action? Confirmed;

        public event Action? BackChosen;

        public Slot SelectedSlot => Slot.All[_cursor];

        /// <summary>Whether Confirm accepts input: only with every slot filled (PRD 3.5.5).</summary>
        public bool ConfirmEnabled => _confirm.interactable;

        /// <summary>The panel's line naming the empty slots; empty when the loadout is complete.</summary>
        public string EmptySlotsText => _empty.text;

        /// <summary>The Binder cards the selected slot may hold, in acquisition order.</summary>
        public IReadOnlyList<CardInstance> Candidates
        {
            get
            {
                var candidates = new List<CardInstance>();
                var category = CardCategories.ForKey(SelectedSlot.Key);
                foreach (var card in _binder.Cards)
                {
                    if (card.Definition.Category == category)
                    {
                        candidates.Add(card);
                    }
                }

                return candidates;
            }
        }

        public static BinderScreen Build(Transform? parent, Binder binder)
        {
            if (binder is null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            var canvas = ScreenFactory.Canvas("BinderScreen", parent, 45);
            var screen = canvas.gameObject.AddComponent<BinderScreen>();
            screen._binder = binder;
            var root = canvas.transform;
            ScreenFactory.Fill("Backdrop", root, ScreenFactory.Backdrop);
            ScreenFactory.Label("Title", root, Strings.Get("binder.title"), 56, new Vector2(0f, 470f), new Vector2(800f, 80f), TextAnchor.MiddleCenter);
            ScreenFactory.Label("Hint", root, Strings.Get("binder.hint"), 24, new Vector2(0f, 410f), new Vector2(1600f, 50f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);

            var slots = HudFactory.Rect("Slots", root, new Vector2(-440f, 120f), new Vector2(900f, 440f));
            foreach (var slot in Slot.All)
            {
                int column = InputMap.SlotIndex(slot.Key);
                var face = HudFactory.Image("Slot " + slot.Line + slot.Key, slots, ScreenFactory.Panel, new Vector2(-390f + column * 112f, 120f - slot.Line * 220f), new Vector2(100f, 180f));
                var key = HudFactory.Text("Key", face.transform, 26, ScreenFactory.Accent, TextAnchor.UpperCenter);
                key.rectTransform.anchoredPosition = new Vector2(0f, 60f);
                key.rectTransform.sizeDelta = new Vector2(96f, 40f);
                key.text = Strings.Format("binder.slot", slot.Line + 1, slot.Key);
                var label = HudFactory.Text("Card", face.transform, 20, ScreenFactory.TextColor, TextAnchor.MiddleCenter);
                label.rectTransform.anchoredPosition = new Vector2(0f, -20f);
                label.rectTransform.sizeDelta = new Vector2(96f, 110f);
                screen._slotLabels[slot] = label;
            }

            screen._candidate = ScreenFactory.Label("Candidate", root, "", 28, new Vector2(-440f, -160f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._cards = ScreenFactory.Label("Cards", root, "", 22, new Vector2(500f, 40f), new Vector2(800f, 700f), TextAnchor.UpperLeft);
            screen._empty = ScreenFactory.Label("Empty", root, "", 30, new Vector2(-440f, -260f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._confirm = ScreenFactory.Button("Confirm", root, Strings.Get("binder.confirm"), new Vector2(-640f, -400f), new Vector2(400f, 84f), screen.Confirm);
            screen._back = ScreenFactory.Button("Back", root, Strings.Get("binder.back"), new Vector2(-200f, -400f), new Vector2(400f, 84f), () => screen.BackChosen?.Invoke());
            screen._keys = new ScreenKeys(new[] { Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.Enter, Key.NumpadEnter, Key.Delete, Key.Backspace }, screen.KeyDown);
            screen.Refresh();
            return screen;
        }

        public void Select(Slot slot)
        {
            for (int i = 0; i < Slot.All.Count; i++)
            {
                if (Slot.All[i].Equals(slot))
                {
                    _cursor = i;
                    _candidateIndex = 0;
                    Refresh();
                    return;
                }
            }
        }

        /// <summary>Empties a slot (PRD 3.5.5); false when it was empty already.</summary>
        public bool ClearSlot(Slot slot)
        {
            bool cleared = _binder.Clear(slot) != null;
            Refresh();
            return cleared;
        }

        /// <summary>Places a Binder card in a slot (PRD 3.5.5).</summary>
        public Placement Fill(Slot slot, CardInstance card)
        {
            var placement = _binder.Assign(slot, card);
            Refresh();
            return placement;
        }

        /// <summary>Activates Confirm the way the player would; false while any slot is empty.</summary>
        public bool ChooseConfirm()
        {
            return ScreenFactory.Submit(_confirm);
        }

        public bool ChooseBack()
        {
            return ScreenFactory.Submit(_back);
        }

        public void KeyDown(Key key)
        {
            switch (key)
            {
                case Key.LeftArrow:
                    _cursor = (_cursor + Slot.All.Count - 1) % Slot.All.Count;
                    _candidateIndex = 0;
                    break;
                case Key.RightArrow:
                    _cursor = (_cursor + 1) % Slot.All.Count;
                    _candidateIndex = 0;
                    break;
                case Key.UpArrow:
                    _candidateIndex--;
                    break;
                case Key.DownArrow:
                    _candidateIndex++;
                    break;
                case Key.Delete:
                case Key.Backspace:
                    _binder.Clear(SelectedSlot);
                    break;
                case Key.Enter:
                case Key.NumpadEnter:
                    var candidates = Candidates;
                    if (candidates.Count > 0)
                    {
                        _binder.Assign(SelectedSlot, candidates[Wrap(_candidateIndex, candidates.Count)]);
                    }

                    break;
                default:
                    return;
            }

            Refresh();
        }

        private void Confirm()
        {
            if (_binder.Loadout.IsComplete)
            {
                Confirmed?.Invoke();
            }
        }

        private void Refresh()
        {
            var loadout = _binder.Loadout;
            foreach (var slot in Slot.All)
            {
                var card = loadout[slot];
                var label = _slotLabels[slot];
                label.text = card == null ? Strings.Get("slot.empty") : card.Definition.Name;
                label.color = slot.Equals(SelectedSlot) ? ScreenFactory.Accent : ScreenFactory.TextColor;
            }

            var candidates = Candidates;
            if (candidates.Count == 0)
            {
                _candidate.text = "";
            }
            else
            {
                var candidate = candidates[Wrap(_candidateIndex, candidates.Count)];
                _candidate.text = Strings.Format("binder.candidate", Strings.Format("binder.slot", SelectedSlot.Line + 1, SelectedSlot.Key), Preview(candidate));
            }

            var text = new StringBuilder();
            foreach (var card in _binder.Cards)
            {
                var slot = loadout.SlotOf(card);
                text.Append(slot is null ? "  " : "* ").Append(Preview(card)).Append('\n');
            }

            _cards.text = text.ToString();
            var empty = loadout.EmptySlots;
            _confirm.interactable = empty.Count == 0;
            _empty.text = empty.Count == 0 ? "" : Strings.Format("binder.empty_slots", Loadout.Describe(empty));
        }

        /// <summary>A card's preview line: name, Category, rarity and CardValue (PRD 3.5.5).</summary>
        public static string Preview(CardInstance card)
        {
            var definition = card.Definition;
            return Strings.Format("binder.card", definition.Name, definition.Category, definition.Rarity, card.Value);
        }

        private static int Wrap(int index, int count)
        {
            return ((index % count) + count) % count;
        }

        private void OnDestroy()
        {
            _keys?.Dispose();
            _keys = null;
        }
    }
}
