#nullable enable
using Chiki.Client.Presenter;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// Builds the plain uGUI pieces the menu screens are made of: a screen-space canvas scaled
    /// to the 1920 by 1080 reference, flat-coloured panels, labels, buttons and toggles. Menus
    /// are screen-space; the battle HUD is world-space (docs/project/unity-setup.md).
    /// </summary>
    internal static class ScreenFactory
    {
        public const float Width = 1920f;
        public const float Height = 1080f;

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

        /// <summary>A full-screen image that also swallows clicks meant for whatever lies beneath.</summary>
        public static Image Fill(string name, Transform parent, Color color)
        {
            var image = HudFactory.StretchedImage(name, parent, color);
            image.raycastTarget = true;
            return image;
        }

        public static UnityEngine.UI.Text Label(string name, Transform parent, string text, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, Color? color = null)
        {
            var label = HudFactory.Text(name, parent, fontSize, color ?? TextColor, alignment);
            label.rectTransform.anchoredPosition = anchoredPosition;
            label.rectTransform.sizeDelta = size;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.text = text;
            return label;
        }

        public static Button Button(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityAction onClick)
        {
            var face = HudFactory.Image(name, parent, ButtonFace, anchoredPosition, size);
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(onClick);
            var text = HudFactory.StretchedText("Label", face.transform, 34, TextColor, TextAnchor.MiddleCenter);
            text.text = label;
            return button;
        }

        public static Toggle Toggle(string name, Transform parent, string label, bool isOn, Vector2 anchoredPosition, Vector2 size, UnityAction<bool> onChanged)
        {
            var host = HudFactory.Rect(name, parent, anchoredPosition, size);
            var toggle = host.gameObject.AddComponent<Toggle>();
            var box = HudFactory.Image("Box", host, ButtonFace, new Vector2(-size.x / 2f + 28f, 0f), new Vector2(44f, 44f));
            box.raycastTarget = true;
            var check = HudFactory.Image("Check", box.transform, Accent, Vector2.zero, new Vector2(26f, 26f));
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
    }
}
