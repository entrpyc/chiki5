#nullable enable
using System;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// Builds the plain uGUI pieces the battle presenters are made of. Until the visual
    /// catalogues carry art, every element is a flat-coloured Image or a Text; the layout and
    /// the state they show are what the presenter tests prove.
    /// </summary>
    internal static class HudFactory
    {
        private static Font? _font;
        private static bool _fontTried;

        /// <summary>The engine's built-in runtime font, or null when the platform has none.</summary>
        public static Font? DefaultFont
        {
            get
            {
                if (!_fontTried)
                {
                    _fontTried = true;
                    try
                    {
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                    catch (Exception)
                    {
                        _font = null;
                    }
                }

                return _font;
            }
        }

        /// <summary>A new RectTransform under a parent, centred anchors, no size.</summary>
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = Rect(name, parent);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Makes a rect fill its parent.</summary>
        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image Image(string name, Transform parent, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            var image = Image(name, parent, color);
            image.rectTransform.anchoredPosition = anchoredPosition;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        public static Image StretchedImage(string name, Transform parent, Color color)
        {
            var image = Image(name, parent, color);
            Stretch(image.rectTransform);
            return image;
        }

        public static UnityEngine.UI.Text Text(string name, Transform parent, int fontSize, Color color, TextAnchor alignment)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            var font = DefaultFont;
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static UnityEngine.UI.Text StretchedText(string name, Transform parent, int fontSize, Color color, TextAnchor alignment)
        {
            var text = Text(name, parent, fontSize, color, alignment);
            Stretch(text.rectTransform);
            return text;
        }

        /// <summary>Milliseconds per beat at the battle's current position, for decays measured in beats (PRD 3.3.1.4).</summary>
        public static float BeatMs(Sim.Battle battle)
        {
            int bpm = battle.BeatMap.BpmAt(Math.Max(0, battle.CurrentPositionQb));
            return 60_000f / bpm;
        }
    }

    /// <summary>The player-facing names the battle presenters show, all read from the string table (PRD 3.12.7).</summary>
    public static class Labels
    {
        /// <summary>A node type's label on the map and its panels (PRD 3.2.16).</summary>
        public static string NodeLabel(Chiki.Sim.NodeType type)
        {
            return Strings.Get("node." + NodeTypes.ToId(type));
        }

        /// <summary>An encounter tier's label (PRD 3.3.9.1).</summary>
        public static string Tier(EncounterTier tier)
        {
            return Strings.Get("tier." + tier.ToString().ToLowerInvariant());
        }

        /// <summary>A run outcome's label (PRD 3.9.11).</summary>
        public static string RunOutcome(RunStatus status)
        {
            return Strings.Get("runend." + status.ToString().ToLowerInvariant());
        }

        /// <summary>The label of a CRP change's source (PRD 3.8.6): a named source from the table, a card, Imprint or Charm by its id.</summary>
        public static string CrpSource(string source)
        {
            string key = "crp.source." + source.Replace(' ', '-');
            string text = Strings.Get(key);
            return text == "[" + key + "]" ? source : text;
        }

        public static string ActionKind(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.AttackLeft: return Strings.Get("action.left");
                case EnemyActionKind.AttackRight: return Strings.Get("action.right");
                case EnemyActionKind.Defend: return Strings.Get("action.defend");
                case EnemyActionKind.Buff: return Strings.Get("action.buff");
                case EnemyActionKind.Charge: return Strings.Get("action.charge");
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown action kind.");
            }
        }

        public static string StatusName(StatusKind kind)
        {
            return Strings.Get("status." + StatusId(kind) + ".name");
        }

        public static string StatusEffect(StatusKind kind)
        {
            return Strings.Get("status." + StatusId(kind) + ".effect");
        }

        /// <summary>The tooltip of a status icon: name, effect and beats remaining (PRD 3.3.7.1).</summary>
        public static string StatusTooltip(StatusKind kind, int stacks, int? remainingBeats)
        {
            string remaining = remainingBeats.HasValue
                ? Strings.Format("status.beats_left", remainingBeats.Value)
                : Strings.Get("status.until_consumed");
            return Strings.Format("status.tooltip", StatusName(kind), stacks, StatusEffect(kind), remaining);
        }

        private static string StatusId(StatusKind kind)
        {
            return kind.ToString().ToLowerInvariant();
        }
    }
}
