#nullable enable
using System;
using System.Collections.Generic;
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
    /// The Binder screen (PRD 3.5.5, P23.3, P7.3): the sixteen slots of the loadout as compact
    /// card faces, every card the run owns as a scrolling column of compact faces, and the
    /// highlighted card as a full face preview. The arrow keys move between slots, Up and Down
    /// cycle the Binder cards the selected slot may hold, Enter places the highlighted card,
    /// Delete or Backspace clears the slot. Confirm is disabled while any slot is empty and the
    /// panel names the empty slots; Confirm proceeds to the battle. Back returns to the
    /// pre-battle panel with the loadout as edited.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BinderScreen : MonoBehaviour
    {
        /// <summary>The gap between two faces in the owned column.</summary>
        private const float ColumnGap = 12f;

        private Binder _binder = null!;
        private readonly Dictionary<Slot, CardFace> _slotFaces = new Dictionary<Slot, CardFace>();
        private readonly Dictionary<Slot, UnityEngine.UI.Text> _slotKeys = new Dictionary<Slot, UnityEngine.UI.Text>();
        private readonly List<CardFace> _ownedFaces = new List<CardFace>();
        private readonly List<GameObject> _inLoadoutMarks = new List<GameObject>();
        private ScrollRect _scroll = null!;
        private RectTransform _column = null!;
        private CardFace _preview = null!;
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

        /// <summary>The sixteen slots as compact faces (P7.3).</summary>
        public IReadOnlyDictionary<Slot, CardFace> SlotFaces => _slotFaces;

        /// <summary>Every card the Binder owns as a compact face, in acquisition order (P7.3).</summary>
        public IReadOnlyList<CardFace> OwnedFaces => _ownedFaces;

        /// <summary>The full face showing the highlighted card (P7.3).</summary>
        public CardFace PreviewFace => _preview;

        /// <summary>The card Enter would place in the selected slot; null when the Binder holds none of its Category.</summary>
        public CardInstance? Highlighted
        {
            get
            {
                var candidates = Candidates;
                return candidates.Count == 0 ? null : candidates[Wrap(_candidateIndex, candidates.Count)];
            }
        }

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
                var position = new Vector2(-390f + column * 112f, 100f - slot.Line * 220f);
                var face = CardFace.Create("Slot " + slot.Line + slot.Key, slots, CardFaceSize.Compact, position);
                var key = HudFactory.Text("Key", slots, 26, ScreenFactory.Accent, TextAnchor.MiddleCenter);
                key.rectTransform.anchoredPosition = position + new Vector2(0f, CardFace.CompactSize.y / 2f + 20f);
                key.rectTransform.sizeDelta = new Vector2(104f, 36f);
                key.text = Strings.Format("binder.slot", slot.Line + 1, slot.Key);
                screen._slotFaces[slot] = face;
                screen._slotKeys[slot] = key;
            }

            screen._candidate = ScreenFactory.Label("Candidate", root, "", 28, new Vector2(-440f, -160f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._preview = CardFace.Create("Preview", root, CardFaceSize.Full, new Vector2(250f, 40f));
            screen._preview.Rect.localScale = new Vector3(1.5f, 1.5f, 1f);
            screen.BuildOwnedColumn(root);
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

        /// <summary>
        /// The owned cards as a column of compact faces inside a scroll view, each marked when it
        /// already sits in the loadout, as the text list starred it before (P7.3).
        /// </summary>
        private void BuildOwnedColumn(Transform root)
        {
            var viewport = HudFactory.Image("Owned", root, ScreenFactory.Panel, new Vector2(700f, -20f), new Vector2(172f, 800f));
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _column = HudFactory.Rect("Column", viewport.transform);
            _column.anchorMin = new Vector2(0.5f, 1f);
            _column.anchorMax = new Vector2(0.5f, 1f);
            _column.pivot = new Vector2(0.5f, 1f);
            float step = CardFace.CompactSize.y + ColumnGap;
            _column.sizeDelta = new Vector2(CardFace.CompactSize.x, ColumnGap + _binder.Cards.Count * step);
            _column.anchoredPosition = Vector2.zero;

            for (int i = 0; i < _binder.Cards.Count; i++)
            {
                var face = CardFace.Create("Owned " + i, _column, CardFaceSize.Compact);
                face.Rect.anchorMin = new Vector2(0.5f, 1f);
                face.Rect.anchorMax = new Vector2(0.5f, 1f);
                face.Rect.anchoredPosition = new Vector2(0f, -ColumnGap - CardFace.CompactSize.y / 2f - i * step);
                face.Show(_binder.Cards[i]);
                var mark = HudFactory.Image("InLoadout", face.transform, ScreenFactory.Accent, new Vector2(CardFace.CompactSize.x / 2f - 2f, CardFace.CompactSize.y / 2f - 2f), new Vector2(16f, 16f));
                _ownedFaces.Add(face);
                _inLoadoutMarks.Add(mark.gameObject);
            }

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport.rectTransform;
            _scroll.content = _column;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
        }

        /// <summary>Scrolls the owned column just enough to bring a face into view.</summary>
        private void ScrollTo(int index)
        {
            var viewport = (RectTransform)_scroll.viewport;
            float step = CardFace.CompactSize.y + ColumnGap;
            float top = index * step;
            float bottom = top + step + ColumnGap;
            float visible = viewport.rect.height;
            float offset = _column.anchoredPosition.y;
            if (top < offset)
            {
                offset = top;
            }
            else if (bottom > offset + visible)
            {
                offset = bottom - visible;
            }

            float max = Mathf.Max(0f, _column.sizeDelta.y - visible);
            _column.anchoredPosition = new Vector2(0f, Mathf.Clamp(offset, 0f, max));
        }

        private void Refresh()
        {
            var loadout = _binder.Loadout;
            foreach (var slot in Slot.All)
            {
                var card = loadout[slot];
                var face = _slotFaces[slot];
                if (card == null)
                {
                    face.ShowEmpty(CardCategories.ForKey(slot.Key));
                }
                else
                {
                    face.Show(card);
                }

                bool selected = slot.Equals(SelectedSlot);
                face.SetHighlighted(selected);
                _slotKeys[slot].color = selected ? ScreenFactory.Accent : ScreenFactory.TextColor;
            }

            var highlighted = Highlighted;
            if (highlighted == null)
            {
                _candidate.text = "";
                var inSlot = loadout[SelectedSlot];
                if (inSlot == null)
                {
                    _preview.ShowEmpty(CardCategories.ForKey(SelectedSlot.Key));
                }
                else
                {
                    _preview.Show(inSlot);
                }
            }
            else
            {
                _candidate.text = Strings.Format("binder.candidate", Strings.Format("binder.slot", SelectedSlot.Line + 1, SelectedSlot.Key), Preview(highlighted));
                _preview.Show(highlighted);
            }

            for (int i = 0; i < _ownedFaces.Count; i++)
            {
                var card = _binder.Cards[i];
                bool isHighlighted = ReferenceEquals(card, highlighted);
                _ownedFaces[i].SetHighlighted(isHighlighted);
                _inLoadoutMarks[i].SetActive(loadout.SlotOf(card) != null);
                if (isHighlighted)
                {
                    ScrollTo(i);
                }
            }

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
