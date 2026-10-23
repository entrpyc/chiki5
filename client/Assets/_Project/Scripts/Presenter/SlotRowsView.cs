#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Keys;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The sixteen slot widgets in two rows (PRD 3.3.5.4, 3.3.2.2): both rows are always on
    /// screen, the active line's row at full width and the other rendered narrower. Each slot
    /// shows its Category's frame and icon (PRD 3.4.2), its key's label for the current layout
    /// (PRD 3.3.2.5), the card it holds, and while on cooldown a radial sweep with the beats
    /// left, read from the battle after every event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlotRowsView : MonoBehaviour, IBattlePresenter
    {
        /// <summary>The panel art's id; it bakes the four key-pair groups, their Category icons and the key squares.</summary>
        public const string PanelId = "card-panel";

        /// <summary>The panel art, 2620 by 530, drawn at half size at the 1080p reference.</summary>
        public static readonly Vector2 PanelSize = new Vector2(1310f, 265f);

        /// <summary>
        /// Each card's centre from the panel's centre, Q to P: the centres of the eight key squares
        /// measured in the panel art (at 4K: 181.5, 519.5, 859.5, 1169.5, 1490.5, 1801.5, 2122.5,
        /// 2443.5 from its left edge), halved for the 1080p reference. The art is not evenly spaced.
        /// </summary>
        public static readonly float[] CardX = { -564.25f, -395.25f, -225.25f, -70.25f, 90.25f, 245.75f, 406.25f, 566.75f };

        /// <summary>The cards' vertical centre within the panel: the panel body's own centre, measured in the art.</summary>
        public const float CardY = -4f;

        /// <summary>The inactive line's cards and spacing as a fraction of the active line's (PRD 3.3.5.4).</summary>
        public const float InactiveScale = 0.5f;

        public const float DefaultRowWidth = 1310f;
        public const float DefaultRowHeight = 265f;

        private readonly RectTransform[] _rows = new RectTransform[Slot.LineCount];
        private readonly List<SlotWidget> _widgets = new List<SlotWidget>();
        private readonly Dictionary<Slot, SlotWidget> _bySlot = new Dictionary<Slot, SlotWidget>();
        private BeatClock? _clock;
        private BeatCursor? _cursor;
        private Sim.Battle? _battle;
        private BattleInput? _input;
        private Func<Slot, CardDefinition?>? _cardInSlot;

        /// <summary>The active line, 0 or 1 (PRD 3.3.2.2).</summary>
        public int ActiveLine { get; private set; }

        public IReadOnlyList<RectTransform> Rows => _rows;

        /// <summary>The panel art behind the cards.</summary>
        public Image Panel { get; private set; } = null!;

        public IReadOnlyList<SlotWidget> Widgets => _widgets;

        public static SlotRowsView Build(BattleDriver driver, RectTransform parent, Vector2 anchoredPosition, Func<Slot, CardDefinition?>? cardInSlot, BattleInput? input)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var root = HudFactory.Rect("SlotRows", parent, anchoredPosition, PanelSize);
            var view = root.gameObject.AddComponent<SlotRowsView>();
            view.Init(driver, root, cardInSlot, input);
            driver.AttachPresenter(view);
            return view;
        }

        public SlotWidget Widget(Slot slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            return _bySlot[slot];
        }

        /// <summary>The width a line's row is rendered at.</summary>
        public float RowWidth(int line)
        {
            return _rows[line].sizeDelta.x;
        }

        /// <summary>Makes a line the active one and re-lays both rows out (PRD 3.3.2.2).</summary>
        public void SetActiveLine(int line)
        {
            if (line < 0 || line >= Slot.LineCount)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "Line must be 0 or 1.");
            }

            ActiveLine = line;
            Layout();
        }

        public void SetGlow(Slot slot, bool on)
        {
            Widget(slot).SetGlow(on);
        }

        /// <summary>Flashes a slot's key at an audio time (PRD 3.3.8.1).</summary>
        public void Flash(Slot slot, int audioTimeMs)
        {
            Widget(slot).Flash(audioTimeMs);
        }

        /// <summary>Flashes a slot as refused at an audio time; no press flash shows (PRD 3.3.5.3).</summary>
        public void FlashDisabled(Slot slot, int audioTimeMs)
        {
            Widget(slot).FlashDisabled(audioTimeMs);
        }

        /// <summary>
        /// Moves every widget's flashes and cooldown sweep to an audio time, as the frame loop
        /// does; the tests drive it at an exact time rather than at whatever the frame landed on.
        /// </summary>
        public void TickAt(int audioTimeMs)
        {
            if (_battle == null)
            {
                return;
            }

            _cursor ??= new BeatCursor(_battle.BeatMap);
            float beatMs = HudFactory.BeatMs(_battle);
            float beatNow = _cursor.BeatAt(audioTimeMs);
            foreach (var widget in _widgets)
            {
                widget.Tick(audioTimeMs, beatMs, beatNow);
            }
        }

        /// <summary>Re-reads every key's label from the current keyboard layout (PRD 3.3.2.5).</summary>
        public void RefreshLabels()
        {
            var keyboard = Keyboard.current;
            foreach (var widget in _widgets)
            {
                widget.SetKeyLabel(keyboard != null ? InputMap.Label(widget.Slot.Key, keyboard) : InputMap.PhysicalKey(widget.Slot.Key).ToString());
            }
        }

        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            _battle = battle;
            Refresh(battle);
        }

        /// <summary>Reads every slot's card and cooldown from the battle (PRD 3.3.5.4).</summary>
        public void Refresh(Sim.Battle battle)
        {
            foreach (var widget in _widgets)
            {
                widget.SetCooldown(battle.CooldownOf(widget.Slot));
                var card = _cardInSlot?.Invoke(widget.Slot);
                widget.SetCard(card);
            }
        }

        private void Init(BattleDriver driver, RectTransform root, Func<Slot, CardDefinition?>? cardInSlot, BattleInput? input)
        {
            _clock = driver.Clock;
            _battle = driver.Battle;
            _cardInSlot = cardInSlot;
            _input = input;

            // The panel first, so every row draws over it.
            var panelPiece = new LinePiece(SlotWidget.UiKind, PanelId, new Color(0.1f, 0.1f, 0.12f, 0.9f));
            Panel = HudFactory.StretchedImage("Panel", root, panelPiece.Tint, panelPiece.Sprite);

            for (int line = 0; line < Slot.LineCount; line++)
            {
                _rows[line] = HudFactory.Rect("Line" + (line + 1), root, Vector2.zero, PanelSize);
            }

            foreach (var slot in Slot.All)
            {
                var widget = new SlotWidget(slot, _rows[slot.Line]);
                _widgets.Add(widget);
                _bySlot[slot] = widget;
            }

            RefreshLabels();
            if (input != null)
            {
                ActiveLine = input.ActiveLine;
                input.LineSwitched += SetActiveLine;
            }

            Layout();
            if (_battle != null)
            {
                Refresh(_battle);
            }
        }

        /// <summary>
        /// Both lines share the one panel: each row lays its eight cards over the baked key
        /// squares, and only the active line's row is shown (operator layout, 2026-10-23, in
        /// place of the thinner inactive row of PRD 3.3.5.4). The inactive row keeps the
        /// compact size so <see cref="RowWidth"/> still tells the lines apart.
        /// </summary>
        private void Layout()
        {
            int perLine = _widgets.Count / Slot.LineCount;
            for (int line = 0; line < Slot.LineCount; line++)
            {
                bool active = line == ActiveLine;
                float scale = active ? 1f : InactiveScale;
                var row = _rows[line];
                row.sizeDelta = PanelSize * scale;
                row.gameObject.SetActive(active);
                for (int i = 0; i < perLine; i++)
                {
                    var widget = _widgets[line * perLine + i];
                    widget.SetCompact(!active, scale);
                    widget.Rect.anchoredPosition = new Vector2(CardX[i] * scale, CardY);
                }
            }
        }

        private void LateUpdate()
        {
            if (_clock == null || !_clock.IsScheduled || _battle == null)
            {
                return;
            }

            TickAt(Math.Max(0, _clock.NowMs));
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.LineSwitched -= SetActiveLine;
            }
        }
    }
}
