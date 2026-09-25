#nullable enable
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One status icon above a side's bar (PRD 3.3.7.1): the icon the visual catalogue holds
    /// for the status at <see cref="SideBarView.IconSize"/> on screen, its stack count, and a
    /// tooltip on hover naming the status, its effect and the beats remaining. The icon carries
    /// the status's colour only while its art is owed; shipped art brings its own (P4.3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusIconWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>The kind the status icons are catalogued under; the id is the status's (PRD 3.3.7.1).</summary>
        public const string StatusKindName = "status";

        /// <summary>The kind the tooltip panel is catalogued under (P4.3).</summary>
        public const string UiKind = "ui";

        public const string TooltipId = "tooltip";

        /// <summary>The tooltip panel's drawn size, the sprite's own (P4.3).</summary>
        public static readonly Vector2 TooltipSize = new Vector2(260f, 70f);

        private static readonly Color TooltipBackground = new Color(0.05f, 0.05f, 0.07f, 0.95f);

        private Image _icon = null!;
        private UnityEngine.UI.Text _stacks = null!;
        private Image _tooltipPanel = null!;
        private RectTransform _tooltip = null!;
        private UnityEngine.UI.Text _tooltipLabel = null!;
        private StatusKind? _drawn;

        public RectTransform Rect => (RectTransform)transform;

        public StatusKind Kind { get; private set; }

        public int Stacks { get; private set; }

        /// <summary>Beats left, or null for a status that lasts until consumed.</summary>
        public int? RemainingBeats { get; private set; }

        public string StacksText => _stacks.text;

        public string TooltipText => _tooltipLabel.text;

        public bool TooltipShown => _tooltip.gameObject.activeSelf;

        /// <summary>The icon's sprite, from the visual catalogue under <c>status/&lt;kind&gt;</c> (P1.3).</summary>
        public Sprite? Sprite => _icon.sprite;

        /// <summary>The icon on screen, at <see cref="SideBarView.IconSize"/> square (P4.3).</summary>
        public Image Icon => _icon;

        /// <summary>The panel behind the tooltip's text, drawn from the catalogue (P4.3).</summary>
        public Image TooltipPanel => _tooltipPanel;

        /// <summary>The catalogue id of a status kind, the lowercase name (<c>bleed</c>).</summary>
        public static string IdOf(StatusKind kind)
        {
            return kind.ToString().ToLowerInvariant();
        }

        public static StatusIconWidget Create(RectTransform parent, float size)
        {
            var icon = HudFactory.Image("Status", parent, Color.white, Vector2.zero, new Vector2(size, size));
            icon.raycastTarget = true;
            var widget = icon.gameObject.AddComponent<StatusIconWidget>();
            widget._icon = icon;

            widget._stacks = HudFactory.StretchedText("Stacks", icon.rectTransform, 16, Color.white, TextAnchor.LowerRight);
            widget._stacks.rectTransform.offsetMin = new Vector2(0f, 2f);
            widget._stacks.rectTransform.offsetMax = new Vector2(-3f, 0f);

            var panel = new LinePiece(UiKind, TooltipId, TooltipBackground);
            var tooltip = HudFactory.Image("Tooltip", icon.rectTransform, panel.Tint, new Vector2(0f, size / 2f + 40f), TooltipSize, panel.Sprite);
            widget._tooltipPanel = tooltip;
            widget._tooltip = tooltip.rectTransform;
            widget._tooltipLabel = HudFactory.StretchedText("Text", widget._tooltip, 14, Color.white, TextAnchor.MiddleLeft);
            widget._tooltipLabel.rectTransform.offsetMin = new Vector2(8f, 4f);
            widget._tooltipLabel.rectTransform.offsetMax = new Vector2(-8f, -4f);
            widget._tooltip.gameObject.SetActive(false);
            return widget;
        }

        public void Set(StatusKind kind, int stacks, int? remainingBeats)
        {
            Kind = kind;
            Stacks = stacks;
            RemainingBeats = remainingBeats;
            if (_drawn != kind)
            {
                _drawn = kind;
                var art = new LinePiece(StatusKindName, IdOf(kind), ColorFor(kind));
                _icon.color = art.Tint;
                HudFactory.SetSprite(_icon, art.Sprite);
            }

            _stacks.text = stacks.ToString();
            _tooltipLabel.text = Labels.StatusTooltip(kind, stacks, remainingBeats);
        }

        public void ShowTooltip()
        {
            _tooltip.gameObject.SetActive(true);
        }

        public void HideTooltip()
        {
            _tooltip.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private static Color ColorFor(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Scar: return new Color(0.85f, 0.6f, 0.25f, 1f);
                case StatusKind.Weak: return new Color(0.55f, 0.55f, 0.75f, 1f);
                case StatusKind.Stun: return new Color(0.95f, 0.9f, 0.35f, 1f);
                case StatusKind.Bleed: return new Color(0.8f, 0.15f, 0.2f, 1f);
                case StatusKind.Thorns: return new Color(0.35f, 0.7f, 0.35f, 1f);
                case StatusKind.Disarmed: return new Color(0.5f, 0.5f, 0.5f, 1f);
                default: return Color.white;
            }
        }
    }
}
