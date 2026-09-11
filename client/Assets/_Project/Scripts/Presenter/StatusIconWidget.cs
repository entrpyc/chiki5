#nullable enable
using Chiki.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One status icon above a side's bar (PRD 3.3.7.1): the status's colour until the visual
    /// catalogue carries its icon, its stack count, and a tooltip on hover naming the status,
    /// its effect and the beats remaining.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusIconWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color TooltipBackground = new Color(0.05f, 0.05f, 0.07f, 0.95f);

        private Image _icon = null!;
        private UnityEngine.UI.Text _stacks = null!;
        private RectTransform _tooltip = null!;
        private UnityEngine.UI.Text _tooltipLabel = null!;

        public RectTransform Rect => (RectTransform)transform;

        public StatusKind Kind { get; private set; }

        public int Stacks { get; private set; }

        /// <summary>Beats left, or null for a status that lasts until consumed.</summary>
        public int? RemainingBeats { get; private set; }

        public string StacksText => _stacks.text;

        public string TooltipText => _tooltipLabel.text;

        public bool TooltipShown => _tooltip.gameObject.activeSelf;

        public static StatusIconWidget Create(RectTransform parent, float size)
        {
            var icon = HudFactory.Image("Status", parent, Color.white, Vector2.zero, new Vector2(size, size));
            icon.raycastTarget = true;
            var widget = icon.gameObject.AddComponent<StatusIconWidget>();
            widget._icon = icon;

            widget._stacks = HudFactory.StretchedText("Stacks", icon.rectTransform, 16, Color.white, TextAnchor.LowerRight);
            widget._stacks.rectTransform.offsetMin = new Vector2(0f, 2f);
            widget._stacks.rectTransform.offsetMax = new Vector2(-3f, 0f);

            var tooltip = HudFactory.Image("Tooltip", icon.rectTransform, TooltipBackground, new Vector2(0f, size / 2f + 40f), new Vector2(260f, 70f));
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
            _icon.color = ColorFor(kind);
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
