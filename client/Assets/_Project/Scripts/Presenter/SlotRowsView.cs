#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Keys;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The sixteen slot widgets in two rows (PRD 3.3.5.4, 3.3.2.2): both rows are always on
    /// screen, the active line's row at full width and the other rendered narrower. Each slot
    /// shows its key's label for the current layout (PRD 3.3.2.5), the card it holds, and while
    /// on cooldown a dim overlay with the beats left, read from the battle after every event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlotRowsView : MonoBehaviour, IBattlePresenter
    {
        public const float DefaultRowWidth = 1200f;
        public const float DefaultRowHeight = 110f;
        public const float RowSpacing = 30f;

        /// <summary>The inactive row's width and height as a fraction of the active row's.</summary>
        public const float InactiveScale = 0.7f;

        private const float Gap = 10f;

        private readonly RectTransform[] _rows = new RectTransform[Slot.LineCount];
        private readonly List<SlotWidget> _widgets = new List<SlotWidget>();
        private readonly Dictionary<Slot, SlotWidget> _bySlot = new Dictionary<Slot, SlotWidget>();
        private BeatClock? _clock;
        private Sim.Battle? _battle;
        private BattleInput? _input;
        private Func<Slot, CardDefinition?>? _cardInSlot;

        /// <summary>The active line, 0 or 1 (PRD 3.3.2.2).</summary>
        public int ActiveLine { get; private set; }

        public IReadOnlyList<RectTransform> Rows => _rows;

        public IReadOnlyList<SlotWidget> Widgets => _widgets;

        public static SlotRowsView Build(BattleDriver driver, RectTransform parent, Vector2 anchoredPosition, Func<Slot, CardDefinition?>? cardInSlot, BattleInput? input)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var root = HudFactory.Rect("SlotRows", parent, anchoredPosition, new Vector2(DefaultRowWidth, DefaultRowHeight * 2f + RowSpacing));
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
                widget.SetCard(card != null ? card.Name : Strings.Get("slot.empty"));
            }
        }

        private void Init(BattleDriver driver, RectTransform root, Func<Slot, CardDefinition?>? cardInSlot, BattleInput? input)
        {
            _clock = driver.Clock;
            _battle = driver.Battle;
            _cardInSlot = cardInSlot;
            _input = input;

            for (int line = 0; line < Slot.LineCount; line++)
            {
                _rows[line] = HudFactory.Rect("Line" + (line + 1), root);
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

        private void Layout()
        {
            for (int line = 0; line < Slot.LineCount; line++)
            {
                float scale = line == ActiveLine ? 1f : InactiveScale;
                float width = DefaultRowWidth * scale;
                float height = DefaultRowHeight * scale;
                float gap = Gap * scale;
                var row = _rows[line];
                row.sizeDelta = new Vector2(width, height);
                row.anchoredPosition = new Vector2(0f, line == 0 ? (DefaultRowHeight + RowSpacing) / 2f : -(DefaultRowHeight + RowSpacing) / 2f);

                int perLine = _widgets.Count / Slot.LineCount;
                float slotWidth = (width - gap * (perLine - 1)) / perLine;
                for (int i = 0; i < perLine; i++)
                {
                    var widget = _widgets[line * perLine + i];
                    widget.Rect.sizeDelta = new Vector2(slotWidth, height);
                    widget.Rect.anchoredPosition = new Vector2(-width / 2f + slotWidth / 2f + i * (slotWidth + gap), 0f);
                }
            }
        }

        private void LateUpdate()
        {
            if (_clock == null || !_clock.IsScheduled || _battle == null)
            {
                return;
            }

            int now = Math.Max(0, _clock.NowMs);
            float beatMs = HudFactory.BeatMs(_battle);
            foreach (var widget in _widgets)
            {
                widget.Tick(now, beatMs);
            }
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
