#nullable enable
using Chiki.Client.Presenter;
using Chiki.Client.Visuals;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>The catalogue ids of the UI skin (P10.1): <c>spr_ui_&lt;subject&gt;_&lt;variant&gt;_01.png</c> under the ui kind.</summary>
    public static class UiSkin
    {
        /// <summary>The kind every skin sprite is catalogued under.</summary>
        public const string Kind = "ui";

        public const string ButtonNormalId = "button-normal";
        public const string ButtonHighlightedId = "button-highlighted";
        public const string ButtonPressedId = "button-pressed";
        public const string ButtonDisabledId = "button-disabled";
        public const string PanelId = "panel";
        public const string BackdropId = "backdrop";
        public const string ToggleBoxId = "toggle-box";
        public const string ToggleCheckId = "toggle-check";
    }

    /// <summary>
    /// Builds the uGUI pieces the menu screens are made of: a screen-space canvas scaled to the
    /// 1920 by 1080 reference, backdrops, panels, labels, buttons and toggles. Menus are
    /// screen-space; the battle HUD is world-space (docs/project/unity-setup.md).
    ///
    /// Every button, panel, toggle and backdrop is drawn from the UI skin in the visual catalogue
    /// (P10.1): buttons swap between their normal, highlighted, pressed and disabled sprites,
    /// panels and buttons are 9-sliced, and every text is set in the shipped font, bold for
    /// headlines and buttons. Plain colour fills such as scrims stay on the built-in white sprite.
    /// While the catalogue holds no skin a piece falls back to the flat colours below, and the
    /// lookup is recorded in <see cref="VisualCatalogue.Missing"/>.
    /// </summary>
    internal static class ScreenFactory
    {
        public const float Width = 1920f;
        public const float Height = 1080f;

        public const string UiKind = UiSkin.Kind;
        public const string ButtonNormalId = UiSkin.ButtonNormalId;
        public const string ButtonHighlightedId = UiSkin.ButtonHighlightedId;
        public const string ButtonPressedId = UiSkin.ButtonPressedId;
        public const string ButtonDisabledId = UiSkin.ButtonDisabledId;
        public const string PanelId = UiSkin.PanelId;
        public const string BackdropId = UiSkin.BackdropId;
        public const string ToggleBoxId = UiSkin.ToggleBoxId;
        public const string ToggleCheckId = UiSkin.ToggleCheckId;

        /// <summary>The backdrop's drawn aspect, 2560 by 1080: it covers a 16:9 window by cropping its sides, and an ultrawide one whole.</summary>
        public const float BackdropAspect = 2560f / 1080f;

        /// <summary>Labels at this size and above are headlines and set in the bold weight.</summary>
        public const int HeadlineSize = 48;

        public static readonly Color Backdrop = new Color(0.06f, 0.06f, 0.09f, 0.98f);
        public static readonly Color Panel = new Color(0.12f, 0.12f, 0.17f, 1f);
        public static readonly Color ButtonFace = new Color(0.22f, 0.24f, 0.36f, 1f);
        public static readonly Color Accent = new Color(0.95f, 0.75f, 0.25f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public static readonly Color MutedText = new Color(0.75f, 0.75f, 0.8f, 1f);

        public static Canvas Canvas(string name, Transform? parent, int sortingOrder)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 0.5f;
            host.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>A full-screen plain colour image that also swallows clicks meant for whatever lies beneath: a scrim.</summary>
        public static Image Fill(string name, Transform parent, Color color)
        {
            var image = HudFactory.StretchedImage(name, parent, color);
            image.raycastTarget = true;
            return image;
        }

        /// <summary>The menu backdrop from the skin (P10.1), filling the screen and swallowing clicks.</summary>
        public static Image BackdropImage(Transform parent)
        {
            return FullScreen("Backdrop", parent, UiKind, BackdropId, Backdrop);
        }

        /// <summary>
        /// A full-screen sprite drawn at the backdrop's 2560 by 1080: kept at its aspect and
        /// sized to cover the screen, so a 16:9 window crops its outer sides and nothing is
        /// stretched. Without the sprite it is a flat fill of <paramref name="fallback"/>.
        /// </summary>
        public static Image FullScreen(string name, Transform parent, string kind, string id, Color fallback)
        {
            var catalogue = VisualCatalogue.Active;
            var sprite = catalogue.Sprite(kind, id);
            if (!catalogue.Has(kind, id))
            {
                return Fill(name, parent, fallback);
            }

            var image = HudFactory.Image(name, parent, Color.white, sprite);
            image.raycastTarget = true;
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectRatio = BackdropAspect;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            return image;
        }

        /// <summary>A panel from the skin (P10.1), 9-sliced to its size and swallowing clicks.</summary>
        public static Image PanelImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var image = Skinned(name, parent, PanelId, Panel, anchoredPosition, size);
            image.raycastTarget = true;
            return image;
        }

        public static UnityEngine.UI.Text Label(string name, Transform parent, string text, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, Color? color = null)
        {
            var label = HudFactory.Text(name, parent, fontSize, color ?? TextColor, alignment, fontSize >= HeadlineSize);
            label.rectTransform.anchoredPosition = anchoredPosition;
            label.rectTransform.sizeDelta = size;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.text = text;
            return label;
        }

        /// <summary>
        /// A button drawn from the skin's four states by Sprite Swap (P10.1): normal at rest,
        /// highlighted when hovered or selected, pressed, and disabled while not interactable.
        /// Its label is bold.
        /// </summary>
        public static Button Button(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityAction onClick)
        {
            var catalogue = VisualCatalogue.Active;
            var face = Skinned(name, parent, ButtonNormalId, ButtonFace, anchoredPosition, size);
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            if (catalogue.Has(UiKind, ButtonNormalId))
            {
                var highlighted = catalogue.Sprite(UiKind, ButtonHighlightedId);
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = highlighted,
                    selectedSprite = highlighted,
                    pressedSprite = catalogue.Sprite(UiKind, ButtonPressedId),
                    disabledSprite = catalogue.Sprite(UiKind, ButtonDisabledId),
                };
            }

            button.onClick.AddListener(onClick);
            var text = HudFactory.StretchedText("Label", face.transform, 34, TextColor, TextAnchor.MiddleCenter, true);
            text.text = label;
            return button;
        }

        /// <summary>A toggle drawn from the skin's box and check (P10.1), its label to their right.</summary>
        public static Toggle Toggle(string name, Transform parent, string label, bool isOn, Vector2 anchoredPosition, Vector2 size, UnityAction<bool> onChanged)
        {
            var catalogue = VisualCatalogue.Active;
            var host = HudFactory.Rect(name, parent, anchoredPosition, size);
            var toggle = host.gameObject.AddComponent<Toggle>();
            var boxSprite = catalogue.Sprite(UiKind, ToggleBoxId);
            var checkSprite = catalogue.Sprite(UiKind, ToggleCheckId);
            bool skinned = catalogue.Has(UiKind, ToggleBoxId) && catalogue.Has(UiKind, ToggleCheckId);
            var box = HudFactory.Image("Box", host, skinned ? Color.white : ButtonFace, new Vector2(-size.x / 2f + 28f, 0f), new Vector2(44f, 44f), skinned ? boxSprite : null);
            box.raycastTarget = true;
            var check = HudFactory.Image("Check", box.transform, skinned ? Color.white : Accent, Vector2.zero, new Vector2(26f, 26f), skinned ? checkSprite : null);
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(onChanged);
            var text = HudFactory.Text("Label", host, 34, TextColor, TextAnchor.MiddleLeft);
            text.rectTransform.anchoredPosition = new Vector2(36f, 0f);
            text.rectTransform.sizeDelta = new Vector2(size.x - 72f, size.y);
            text.text = label;
            return toggle;
        }

        /// <summary>
        /// Activates a button the way the player would: through the event system's submit
        /// path, which honours <see cref="Selectable.interactable"/>. Returns whether the
        /// button accepted it.
        /// </summary>
        public static bool Submit(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable())
            {
                return false;
            }

            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return true;
        }

        /// <summary>An image carrying a skin sprite untinted, or the flat fallback colour while the catalogue lacks it.</summary>
        private static Image Skinned(string name, Transform parent, string id, Color fallback, Vector2 anchoredPosition, Vector2 size)
        {
            var catalogue = VisualCatalogue.Active;
            var sprite = catalogue.Sprite(UiKind, id);
            bool skinned = catalogue.Has(UiKind, id);
            return HudFactory.Image(name, parent, skinned ? Color.white : fallback, anchoredPosition, size, skinned ? sprite : null);
        }
    }
}
