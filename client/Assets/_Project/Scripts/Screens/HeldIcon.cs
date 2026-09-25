#nullable enable
using Chiki.Client.Presenter;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// One equipped Charm or held Imprint in the map's row under the header (PRD 3.2.16, P9.5):
    /// its icon from the visual catalogue, a copy count when a stackable Imprint is held more
    /// than once, and a tooltip on hover naming it and its effect.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeldIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const string CharmKind = "charm";
        public const string ImprintKind = "imprint";

        private static readonly Color TooltipBackground = new Color(0.05f, 0.05f, 0.07f, 0.95f);

        private Image _icon = null!;
        private UnityEngine.UI.Text _count = null!;
        private RectTransform _tooltip = null!;
        private Image _tooltipPanel = null!;
        private UnityEngine.UI.Text _tooltipLabel = null!;

        /// <summary>The catalogue kind: <see cref="CharmKind"/> or <see cref="ImprintKind"/>.</summary>
        public string Kind { get; private set; } = "";

        /// <summary>The content id, such as <c>imprint-keen-edge</c>.</summary>
        public string ContentId { get; private set; } = "";

        public Image Icon => _icon;

        public Image TooltipPanel => _tooltipPanel;

        public string TooltipText => _tooltipLabel.text;

        public bool TooltipShown => _tooltip.gameObject.activeSelf;

        /// <summary>The copy count shown on the icon; empty for a single copy.</summary>
        public string CountText => _count.text;

        /// <summary>The catalogue id of a Charm or Imprint: its content id without the kind prefix (<c>keen-edge</c> for <c>imprint-keen-edge</c>).</summary>
        public static string IconId(string kind, string contentId)
        {
            string prefix = kind + "-";
            return contentId.StartsWith(prefix, System.StringComparison.Ordinal) ? contentId.Substring(prefix.Length) : contentId;
        }

        public static HeldIcon Create(Transform parent, string kind, string contentId, string tooltip, int copies, Vector2 position, float size, Color placeholder)
        {
            var art = new LinePiece(kind, IconId(kind, contentId), placeholder);
            var icon = HudFactory.Image(kind + " " + contentId, parent, art.Tint, position, new Vector2(size, size), art.Sprite);
            icon.raycastTarget = true;
            var widget = icon.gameObject.AddComponent<HeldIcon>();
            widget.Kind = kind;
            widget.ContentId = contentId;
            widget._icon = icon;

            widget._count = HudFactory.StretchedText("Count", icon.rectTransform, 18, Color.white, TextAnchor.LowerRight);
            widget._count.rectTransform.offsetMax = new Vector2(-2f, 0f);
            widget._count.text = copies > 1 ? "x" + copies : "";

            var panel = new LinePiece(StatusIconWidget.UiKind, StatusIconWidget.TooltipId, TooltipBackground);
            var panelSize = new Vector2(360f, 84f);
            widget._tooltipPanel = HudFactory.Image("Tooltip", icon.rectTransform, panel.Tint, new Vector2(panelSize.x / 2f - size / 2f, -size / 2f - panelSize.y / 2f - 8f), panelSize, panel.Sprite);
            widget._tooltip = widget._tooltipPanel.rectTransform;
            widget._tooltipLabel = HudFactory.StretchedText("Text", widget._tooltip, 18, Color.white, TextAnchor.MiddleLeft);
            widget._tooltipLabel.rectTransform.offsetMin = new Vector2(12f, 6f);
            widget._tooltipLabel.rectTransform.offsetMax = new Vector2(-12f, -6f);
            widget._tooltipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            widget._tooltipLabel.text = tooltip;
            widget._tooltip.gameObject.SetActive(false);
            return widget;
        }

        public void ShowTooltip()
        {
            _tooltip.gameObject.SetActive(true);
            // Drawn after its neighbours so the next icon never covers it.
            transform.SetAsLastSibling();
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
    }
}
