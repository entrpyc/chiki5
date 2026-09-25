#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Effects;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>The two sizes a card face comes in (P7.1).</summary>
    public enum CardFaceSize
    {
        /// <summary>256 by 360, the whole anatomy, for previews and offers.</summary>
        Full,

        /// <summary>100 by 140, illustration, frame, rarity and name, for Binder slots.</summary>
        Compact,
    }

    /// <summary>
    /// A card face (PRD 3.4.7): the card's illustration seen through its Category's frame
    /// (PRD 3.4.2), its rarity's border and gem, and over them the name, the value, the cooldown
    /// in beats, the icons of the statuses it applies, its special rules and its flavour text
    /// when it has any. Every piece of art comes from the visual catalogue and every word from
    /// the card definition or the string table.
    ///
    /// The three art layers share one 512 by 720 geometry (tools/gen-phase7-art.mjs), so the
    /// face is laid out once in that space and scaled to its size; the numbers below are the
    /// generator's. Assumption: the face is built in code like every other screen piece
    /// (HudFactory, ScreenFactory) rather than as a prefab asset, since a prefab could only
    /// restate this layout by hand.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardFace : MonoBehaviour
    {
        /// <summary>The kind card illustrations are catalogued under; the id is the card id without its <c>card-</c> prefix.</summary>
        public const string CardKind = "card";

        /// <summary>The kind the frames and rarity treatments are catalogued under.</summary>
        public const string UiKind = "ui";

        public const string FrameIdPrefix = "card-frame-";
        public const string RarityIdPrefix = "card-rarity-";

        /// <summary>The size the art is drawn at and the face is laid out in (P7.1).</summary>
        public static readonly Vector2 ArtSize = new Vector2(512f, 720f);

        public static readonly Vector2 FullSize = new Vector2(256f, 360f);
        public static readonly Vector2 CompactSize = new Vector2(100f, 140f);

        // The generator's geometry, in art pixels from the top-left corner.
        private static readonly UnityEngine.Rect WindowArea = UnityEngine.Rect.MinMaxRect(28f, 96f, 484f, 536f);
        private static readonly UnityEngine.Rect NameArea = UnityEngine.Rect.MinMaxRect(40f, 22f, 472f, 84f);
        private static readonly Vector2 ValueCentre = new Vector2(76f, 536f);
        private static readonly Vector2 CooldownCentre = new Vector2(436f, 536f);
        private static readonly UnityEngine.Rect RulesArea = UnityEngine.Rect.MinMaxRect(52f, 576f, 460f, 646f);
        private static readonly UnityEngine.Rect FlavourArea = UnityEngine.Rect.MinMaxRect(52f, 646f, 460f, 692f);

        /// <summary>The status icons' drawn size on the art, in a row over the window's foot.</summary>
        private const float StatusIconSize = 48f;
        private const float StatusRowY = 492f;

        private static readonly Color IllustrationPlaceholder = new Color(0.2f, 0.2f, 0.26f, 1f);
        private static readonly Color RarityPlaceholder = new Color(1f, 1f, 1f, 0f);
        private static readonly Color NameColor = new Color(0.97f, 0.95f, 0.9f, 1f);
        private static readonly Color RulesColor = new Color(0.92f, 0.92f, 0.95f, 1f);
        private static readonly Color FlavourColor = new Color(0.72f, 0.72f, 0.78f, 1f);

        private RectTransform _art = null!;
        private Image _illustration = null!;
        private Image _frame = null!;
        private Image _rarity = null!;
        private Image _categoryIcon = null!;
        private Image _highlight = null!;
        private UnityEngine.UI.Text _name = null!;
        private UnityEngine.UI.Text _value = null!;
        private UnityEngine.UI.Text _cooldown = null!;
        private UnityEngine.UI.Text _rules = null!;
        private UnityEngine.UI.Text _flavour = null!;
        private RectTransform _statusRow = null!;
        private readonly List<Image> _statusIcons = new List<Image>();
        private readonly List<StatusKind> _statusKinds = new List<StatusKind>();

        public CardFaceSize Size { get; private set; }

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>The card shown; null while the face stands for an empty slot.</summary>
        public CardDefinition? Card { get; private set; }

        public bool IsEmpty => Card == null;

        public Image Illustration => _illustration;

        public Image Frame => _frame;

        public Image Rarity => _rarity;

        public Image CategoryIcon => _categoryIcon;

        public string NameText => _name.text;

        public string ValueText => _value.text;

        public bool ValueShown => _value.gameObject.activeSelf;

        public string CooldownText => _cooldown.text;

        public bool CooldownShown => _cooldown.gameObject.activeSelf;

        public string RulesText => _rules.text;

        public string FlavourText => _flavour.text;

        /// <summary>Whether the flavour line is on the face: only on a full face of a card that has flavour (PRD 3.4.7).</summary>
        public bool FlavourShown => _flavour.gameObject.activeSelf;

        /// <summary>The icons of the statuses the card applies, one per status, in the order its effects name them.</summary>
        public IReadOnlyList<Image> StatusIcons => _statusIcons.GetRange(0, _statusKinds.Count);

        public IReadOnlyList<StatusKind> StatusKinds => _statusKinds;

        public bool IsHighlighted => _highlight.gameObject.activeSelf;

        /// <summary>The catalogue id of a card's illustration: its id without the <c>card-</c> prefix (<c>rend</c> for <c>card-rend</c>).</summary>
        public static string IllustrationId(CardDefinition card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            const string prefix = "card-";
            return card.Id.StartsWith(prefix, StringComparison.Ordinal) ? card.Id.Substring(prefix.Length) : card.Id;
        }

        /// <summary>The catalogue id of a Category's card frame: the two attacks share one (PRD 3.4.2).</summary>
        public static string FrameId(CardCategory category)
        {
            return FrameIdPrefix + SlotWidget.CategoryId(category);
        }

        public static string RarityId(CardRarity rarity)
        {
            return RarityIdPrefix + rarity.ToString().ToLowerInvariant();
        }

        /// <summary>The distinct statuses a card's effects apply, in the order they are named.</summary>
        public static IReadOnlyList<StatusKind> AppliedStatuses(CardDefinition card)
        {
            var kinds = new List<StatusKind>();
            foreach (var effect in card.Effects)
            {
                if (effect.Modifier == EffectModifier.ApplyStatus && effect.Status != null && !kinds.Contains(effect.Status.Kind))
                {
                    kinds.Add(effect.Status.Kind);
                }
            }

            return kinds;
        }

        public static CardFace Create(string name, Transform parent, CardFaceSize size)
        {
            var host = HudFactory.Rect(name, parent);
            var dimensions = size == CardFaceSize.Full ? FullSize : CompactSize;
            host.sizeDelta = dimensions;
            var face = host.gameObject.AddComponent<CardFace>();
            face.Size = size;
            face.Build(dimensions);
            return face;
        }

        public static CardFace Create(string name, Transform parent, CardFaceSize size, Vector2 anchoredPosition)
        {
            var face = Create(name, parent, size);
            face.Rect.anchoredPosition = anchoredPosition;
            return face;
        }

        public void Show(CardInstance card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            Show(card.Definition, card.Value);
        }

        public void Show(CardDefinition card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            Show(card, card.Value);
        }

        /// <summary>Shows a card with the value it plays at, which an upgrade raises (PRD 3.4.18).</summary>
        public void Show(CardDefinition card, int value)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            bool full = Size == CardFaceSize.Full;

            var illustration = new LinePiece(CardKind, IllustrationId(card), IllustrationPlaceholder);
            SetArt(_illustration, illustration, 1f);
            SetFrame(card.Category, 1f);
            var rarity = new LinePiece(UiKind, RarityId(card.Rarity), RarityPlaceholder);
            SetArt(_rarity, rarity, 1f);

            _name.text = card.Name;
            _categoryIcon.gameObject.SetActive(full);

            // An Ability's worth is in its rules; a value badge reading 0 would say it does nothing.
            bool hasValue = card.Value > 0 || card.Category != CardCategory.Ability;
            _value.text = value.ToString();
            _value.gameObject.SetActive(full && hasValue);
            _cooldown.text = Strings.Format("card.cooldown", card.CooldownBeats);
            _cooldown.gameObject.SetActive(full);

            _rules.text = card.SpecialRules ?? "";
            _rules.gameObject.SetActive(full && !string.IsNullOrEmpty(card.SpecialRules));
            _flavour.text = card.FlavorText ?? "";
            _flavour.gameObject.SetActive(full && !string.IsNullOrEmpty(card.FlavorText));

            ShowStatuses(full ? AppliedStatuses(card) : Array.Empty<StatusKind>());
        }

        /// <summary>A slot with no card: the slot's Category frame at half opacity with the empty label, as the battle slots show it (P3.1).</summary>
        public void ShowEmpty(CardCategory category)
        {
            Card = null;
            _illustration.gameObject.SetActive(false);
            _rarity.gameObject.SetActive(false);
            SetFrame(category, SlotWidget.EmptyOpacity);
            _name.text = Strings.Get("slot.empty");
            _categoryIcon.gameObject.SetActive(false);
            _value.gameObject.SetActive(false);
            _cooldown.gameObject.SetActive(false);
            _rules.gameObject.SetActive(false);
            _flavour.gameObject.SetActive(false);
            ShowStatuses(Array.Empty<StatusKind>());
        }

        /// <summary>Marks the face as the one the keys are on.</summary>
        public void SetHighlighted(bool highlighted)
        {
            _highlight.gameObject.SetActive(highlighted);
        }

        private void Build(Vector2 dimensions)
        {
            _highlight = HudFactory.StretchedImage("Highlight", Rect, new Color(0.95f, 0.75f, 0.25f, 1f));
            float margin = Size == CardFaceSize.Full ? 6f : 4f;
            _highlight.rectTransform.offsetMin = new Vector2(-margin, -margin);
            _highlight.rectTransform.offsetMax = new Vector2(margin, margin);
            _highlight.gameObject.SetActive(false);

            // Everything below is laid out in art pixels and scaled down to the face's size.
            _art = HudFactory.Rect("Art", Rect, Vector2.zero, ArtSize);
            _art.localScale = new Vector3(dimensions.x / ArtSize.x, dimensions.y / ArtSize.y, 1f);

            // The illustration is drawn at the card's full size and clipped to the window, so the
            // frame's rounded outer corners never show it.
            var window = Area("Window", WindowArea);
            window.gameObject.AddComponent<RectMask2D>();
            _illustration = HudFactory.Image("Illustration", window, Color.white);
            _illustration.rectTransform.sizeDelta = ArtSize;
            _illustration.rectTransform.anchoredPosition = new Vector2(ArtSize.x / 2f - WindowArea.center.x, WindowArea.center.y - ArtSize.y / 2f);

            _frame = HudFactory.StretchedImage("Frame", _art, Color.white);
            _rarity = HudFactory.StretchedImage("Rarity", _art, Color.white);

            bool full = Size == CardFaceSize.Full;
            var nameArea = Area("NameArea", NameArea);
            _categoryIcon = HudFactory.Image("Category", nameArea, Color.white, new Vector2(-NameArea.width / 2f + 34f, 0f), new Vector2(44f, 44f));
            _name = HudFactory.StretchedText("Name", nameArea, full ? 36 : 60, NameColor, TextAnchor.MiddleCenter);
            _name.fontStyle = FontStyle.Bold;
            _name.rectTransform.offsetMin = new Vector2(full ? 64f : 8f, 0f);
            _name.rectTransform.offsetMax = new Vector2(full ? -64f : -8f, 0f);
            _name.horizontalOverflow = HorizontalWrapMode.Wrap;
            _name.verticalOverflow = VerticalWrapMode.Truncate;
            _name.resizeTextForBestFit = true;
            _name.resizeTextMinSize = 12;
            _name.resizeTextMaxSize = full ? 36 : 60;

            _value = Label("Value", Circle(ValueCentre, 72f), 40, NameColor, TextAnchor.MiddleCenter);
            _value.fontStyle = FontStyle.Bold;
            _cooldown = Label("Cooldown", Circle(CooldownCentre, 68f), 24, NameColor, TextAnchor.MiddleCenter);
            _cooldown.horizontalOverflow = HorizontalWrapMode.Wrap;

            _statusRow = HudFactory.Rect("Statuses", _art, ArtPoint(new Vector2(ArtSize.x / 2f, StatusRowY)), new Vector2(WindowArea.width, StatusIconSize));

            _rules = Label("Rules", Area("RulesArea", RulesArea), 28, RulesColor, TextAnchor.UpperCenter);
            _rules.horizontalOverflow = HorizontalWrapMode.Wrap;
            _flavour = Label("Flavour", Area("FlavourArea", FlavourArea), 22, FlavourColor, TextAnchor.LowerCenter);
            _flavour.fontStyle = FontStyle.Italic;
            _flavour.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void SetFrame(CardCategory category, float opacity)
        {
            var placeholder = SlotWidget.ColorOf(category);
            placeholder.a = 0.35f;
            var frame = new LinePiece(UiKind, FrameId(category), placeholder);
            SetArt(_frame, frame, opacity);
            var icon = new LinePiece(SlotWidget.CategoryKind, SlotWidget.CategoryId(category), SlotWidget.ColorOf(category));
            _categoryIcon.color = icon.Tint;
            HudFactory.SetSprite(_categoryIcon, icon.Sprite);
        }

        private static void SetArt(Image image, LinePiece piece, float opacity)
        {
            var colour = piece.Tint;
            colour.a *= opacity;
            image.color = colour;
            HudFactory.SetSprite(image, piece.Sprite);
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.gameObject.SetActive(true);
        }

        private void ShowStatuses(IReadOnlyList<StatusKind> kinds)
        {
            _statusKinds.Clear();
            _statusKinds.AddRange(kinds);
            while (_statusIcons.Count < kinds.Count)
            {
                _statusIcons.Add(HudFactory.Image("Status", _statusRow, Color.white, Vector2.zero, new Vector2(StatusIconSize, StatusIconSize)));
            }

            const float gap = 8f;
            float start = -(kinds.Count - 1) * (StatusIconSize + gap) / 2f;
            for (int i = 0; i < _statusIcons.Count; i++)
            {
                var icon = _statusIcons[i];
                bool used = i < kinds.Count;
                icon.gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                var art = new LinePiece(StatusIconWidget.StatusKindName, StatusIconWidget.IdOf(kinds[i]), Color.white);
                icon.color = art.Tint;
                HudFactory.SetSprite(icon, art.Sprite);
                icon.rectTransform.anchoredPosition = new Vector2(start + i * (StatusIconSize + gap), 0f);
            }
        }

        /// <summary>A point on the art, from its top-left corner, as an anchored position from its centre.</summary>
        private static Vector2 ArtPoint(Vector2 fromTopLeft)
        {
            return new Vector2(fromTopLeft.x - ArtSize.x / 2f, ArtSize.y / 2f - fromTopLeft.y);
        }

        private RectTransform Area(string name, UnityEngine.Rect area)
        {
            return HudFactory.Rect(name, _art, ArtPoint(area.center), area.size);
        }

        private RectTransform Circle(Vector2 centre, float diameter)
        {
            return HudFactory.Rect("Badge", _art, ArtPoint(centre), new Vector2(diameter, diameter));
        }

        private static UnityEngine.UI.Text Label(string name, RectTransform area, int fontSize, Color color, TextAnchor alignment)
        {
            var text = HudFactory.StretchedText(name, area, fontSize, color, alignment);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
