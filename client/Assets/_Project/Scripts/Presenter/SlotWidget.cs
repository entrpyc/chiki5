#nullable enable
using System;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One of the sixteen slots on screen (PRD 3.4.2, 3.3.5.3, 3.3.5.4, 3.3.8.1): its Category's
    /// frame and icon, the key's label, the card it holds, a radial sweep with the beats of
    /// cooldown left, a glow while the card can be played into the open Judgment Window, a flash
    /// when its key is pressed and a different flash when a press is refused. Both flashes and
    /// the sweep are measured on the beat clock, never on frame time.
    /// </summary>
    public sealed class SlotWidget
    {
        /// <summary>How long a key flash lasts, in beats.</summary>
        public const float FlashBeats = 0.5f;

        /// <summary>The kind the slot's frame, sweep, glow and flashes are catalogued under.</summary>
        public const string UiKind = "ui";

        /// <summary>The kind the three Category icons are catalogued under; the id is the Category's (PRD 3.4.2).</summary>
        public const string CategoryKind = "category";

        public const string FrameIdPrefix = "slot-frame-";
        public const string CooldownId = "slot-cooldown";
        public const string GlowId = "slot-glow";
        public const string FlashId = "slot-flash";
        public const string DisabledId = "slot-disabled";

        /// <summary>The Category icon's drawn size in the frame's top-right corner (P3.1).</summary>
        public const float IconSize = 26f;

        /// <summary>What an empty slot's frame is drawn at, so it reads as a place for a card (P3.1).</summary>
        public const float EmptyOpacity = 0.5f;

        /// <summary>How far the glow stands proud of the frame on every side; the art is 6 px larger (P3.3).</summary>
        private const float GlowMargin = 6f;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.65f);

        // The Category colours of PRD 3.4.2, standing in until the frames ship and behind the
        // icons wherever one is owed.
        private static readonly Color AttackColor = new Color(0.839f, 0.278f, 0.259f, 1f);
        private static readonly Color DefenseColor = new Color(0.243f, 0.494f, 0.839f, 1f);
        private static readonly Color AbilityColor = new Color(0.282f, 0.718f, 0.424f, 1f);

        private readonly Image _frame;
        private readonly Image _categoryIcon;
        private readonly UnityEngine.UI.Text _keyLabel;
        private readonly UnityEngine.UI.Text _cardName;
        private readonly Image _overlay;
        private readonly UnityEngine.UI.Text _countdown;
        private readonly Image _glow;
        private readonly Image _flash;
        private readonly Image _disabled;
        private readonly Color _frameColor;
        private readonly Color _flashColor;
        private readonly Color _disabledColor;
        private int _flashStartMs;
        private int _disabledStartMs;
        private bool _flashing;
        private bool _disabledFlashing;
        private int _cooldownTotal;
        private bool _empty = true;

        public Slot Slot { get; }

        /// <summary>The Category the slot's key holds (PRD 3.4.1); its colour and icon are this slot's.</summary>
        public CardCategory Category { get; }

        public RectTransform Rect { get; }

        public int CooldownBeats { get; private set; }

        public bool IsOnCooldown => CooldownBeats > 0;

        public string CountdownText => _countdown.text;

        public bool OverlayShown => _overlay.gameObject.activeSelf;

        public bool IsGlowing => _glow.gameObject.activeSelf;

        public bool IsFlashing => _flashing;

        /// <summary>Whether the refused-press flash is on (PRD 3.3.5.3).</summary>
        public bool IsDisabledFlashing => _disabledFlashing;

        public string KeyLabelText => _keyLabel.text;

        public string CardNameText => _cardName.text;

        /// <summary>The Category's frame (PRD 3.4.2), at half opacity while the slot holds no card.</summary>
        public Image Frame => _frame;

        /// <summary>The Category's icon in the frame's top-right corner (PRD 3.4.2).</summary>
        public Image CategoryIcon => _categoryIcon;

        /// <summary>The radial cooldown sweep (PRD 3.3.5.4).</summary>
        public Image Overlay => _overlay;

        /// <summary>How much of the cooldown is left, 1 at its start and 0 at its end (PRD 3.3.5.4).</summary>
        public float CooldownFill => _overlay.fillAmount;

        public Image Glow => _glow;

        public Image FlashImage => _flash;

        /// <summary>The refused-press flash, which never shows with the press flash (PRD 3.3.5.3).</summary>
        public Image DisabledFlashImage => _disabled;

        public float FlashAlpha => _flash.color.a;

        public bool IsEmpty => _empty;

        internal SlotWidget(Slot slot, RectTransform parent)
        {
            Slot = slot;
            Category = CardCategories.ForKey(slot.Key);
            var colour = ColorOf(Category);
            string categoryId = CategoryId(Category);

            var framePiece = new LinePiece(UiKind, FrameIdPrefix + categoryId, colour);
            _frameColor = framePiece.Tint;
            _frame = HudFactory.Image("Slot-" + slot.Line + "-" + slot.Key, parent, _frameColor, framePiece.Sprite);
            Rect = _frame.rectTransform;

            var glowPiece = new LinePiece(UiKind, GlowId, new Color(1f, 0.9f, 0.4f, 0.55f));
            _glow = HudFactory.StretchedImage("Glow", Rect, glowPiece.Tint, glowPiece.Sprite);
            _glow.rectTransform.offsetMin = new Vector2(-GlowMargin, -GlowMargin);
            _glow.rectTransform.offsetMax = new Vector2(GlowMargin, GlowMargin);
            _glow.gameObject.SetActive(false);

            var iconPiece = new LinePiece(CategoryKind, categoryId, colour);
            _categoryIcon = HudFactory.Image("Category", Rect, iconPiece.Tint, Vector2.zero, new Vector2(IconSize, IconSize), iconPiece.Sprite);
            _categoryIcon.rectTransform.anchorMin = new Vector2(1f, 1f);
            _categoryIcon.rectTransform.anchorMax = new Vector2(1f, 1f);
            _categoryIcon.rectTransform.pivot = new Vector2(1f, 1f);
            _categoryIcon.rectTransform.anchoredPosition = new Vector2(-6f, -6f);

            _keyLabel = HudFactory.StretchedText("Key", Rect, 18, Color.white, TextAnchor.UpperLeft);
            _keyLabel.rectTransform.offsetMin = new Vector2(6f, 0f);
            _keyLabel.rectTransform.offsetMax = new Vector2(0f, -4f);

            _cardName = HudFactory.StretchedText("Card", Rect, 14, new Color(0.85f, 0.85f, 0.9f, 1f), TextAnchor.LowerCenter);
            _cardName.rectTransform.offsetMin = new Vector2(2f, 6f);
            _cardName.rectTransform.offsetMax = new Vector2(-2f, 0f);

            var sweep = new LinePiece(UiKind, CooldownId, Color.white);
            _overlay = HudFactory.StretchedImage("Cooldown", Rect, OverlayColor, sweep.Sprite);
            _overlay.type = UnityEngine.UI.Image.Type.Filled;
            _overlay.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            _overlay.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            _overlay.fillClockwise = true;
            _overlay.fillAmount = 0f;
            _overlay.gameObject.SetActive(false);

            var flashPiece = new LinePiece(UiKind, FlashId, Color.white);
            _flashColor = new Color(flashPiece.Tint.r, flashPiece.Tint.g, flashPiece.Tint.b, 0.8f);
            _flash = HudFactory.StretchedImage("Flash", Rect, _flashColor, flashPiece.Sprite);
            _flash.gameObject.SetActive(false);

            var disabledPiece = new LinePiece(UiKind, DisabledId, new Color(0.8f, 0.26f, 0.24f, 1f));
            _disabledColor = new Color(disabledPiece.Tint.r, disabledPiece.Tint.g, disabledPiece.Tint.b, 0.9f);
            _disabled = HudFactory.StretchedImage("DisabledFlash", Rect, _disabledColor, disabledPiece.Sprite);
            _disabled.gameObject.SetActive(false);

            // Last, so the beats remaining stay readable through either flash (PRD 3.3.5.4).
            _countdown = HudFactory.StretchedText("Countdown", Rect, 36, Color.white, TextAnchor.MiddleCenter);

            SetCard(null);
        }

        /// <summary>The catalogue id of a Category's frame and icon: the two attacks share one Category's art (PRD 3.4.2).</summary>
        public static string CategoryId(CardCategory category)
        {
            switch (category)
            {
                case CardCategory.Ability: return "ability";
                case CardCategory.LeftAttack:
                case CardCategory.RightAttack: return "attack";
                case CardCategory.Defense: return "defense";
                default: throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown Category.");
            }
        }

        /// <summary>A Category's colour (PRD 3.4.2): Attack red, Defense blue, Ability green.</summary>
        public static Color ColorOf(CardCategory category)
        {
            switch (CategoryId(category))
            {
                case "attack": return AttackColor;
                case "defense": return DefenseColor;
                default: return AbilityColor;
            }
        }

        /// <summary>Shows the card a slot holds, or the empty label with the frame at half opacity (P3.1).</summary>
        public void SetCard(CardDefinition? card)
        {
            _empty = card == null;
            _cardName.text = card != null ? card.Name : Strings.Get("slot.empty");
            var colour = _frameColor;
            colour.a = _frameColor.a * (_empty ? EmptyOpacity : 1f);
            _frame.color = colour;
        }

        public void SetKeyLabel(string label)
        {
            _keyLabel.text = label;
        }

        /// <summary>Shows the sweep and the beats remaining, or clears both at zero (PRD 3.3.5.4).</summary>
        public void SetCooldown(int beats)
        {
            if (beats <= 0)
            {
                _cooldownTotal = 0;
                _overlay.fillAmount = 0f;
            }
            else if (beats > _cooldownTotal)
            {
                // A cooling slot cannot be pressed (PRD 3.3.5.3), so a rise is always a new cooldown.
                _cooldownTotal = beats;
                _overlay.fillAmount = 1f;
            }

            CooldownBeats = beats;
            _countdown.text = beats > 0 ? beats.ToString() : string.Empty;
            if (_overlay.gameObject.activeSelf != beats > 0)
            {
                _overlay.gameObject.SetActive(beats > 0);
            }
        }

        public void SetGlow(bool on)
        {
            if (_glow.gameObject.activeSelf != on)
            {
                _glow.gameObject.SetActive(on);
            }
        }

        /// <summary>Starts the key-press flash at an audio time (PRD 3.3.8.1).</summary>
        public void Flash(int audioTimeMs)
        {
            _flashStartMs = audioTimeMs;
            _flashing = true;
            SetAlpha(_flash, _flashColor, 1f);
            _flash.gameObject.SetActive(true);
        }

        /// <summary>Starts the refused-press flash at an audio time; the press flash stays down (PRD 3.3.5.3).</summary>
        public void FlashDisabled(int audioTimeMs)
        {
            _disabledStartMs = audioTimeMs;
            _disabledFlashing = true;
            SetAlpha(_disabled, _disabledColor, 1f);
            _disabled.gameObject.SetActive(true);
        }

        /// <summary>
        /// Advances both flashes and the cooldown sweep to an audio time. <paramref name="beatMs"/>
        /// is the length of a beat right now and <paramref name="beatNow"/> the continuous beat
        /// that time falls on, so the sweep moves between beats (PRD 3.3.5.4).
        /// </summary>
        public void Tick(int audioTimeMs, float beatMs, float beatNow)
        {
            if (_flashing)
            {
                _flashing = Fade(_flash, _flashColor, audioTimeMs - _flashStartMs, beatMs);
            }

            if (_disabledFlashing)
            {
                _disabledFlashing = Fade(_disabled, _disabledColor, audioTimeMs - _disabledStartMs, beatMs);
            }

            if (_cooldownTotal > 0 && CooldownBeats > 0)
            {
                // The beats remaining expire at the end of that many whole beats from this one.
                float endBeat = Mathf.Floor(beatNow) + CooldownBeats;
                _overlay.fillAmount = Mathf.Clamp01((endBeat - beatNow) / _cooldownTotal);
            }
        }

        /// <summary>Fades one flash over half a beat of audio time; false once it is spent.</summary>
        private static bool Fade(Image image, Color full, int elapsedMs, float beatMs)
        {
            float t = elapsedMs / (FlashBeats * beatMs);
            if (t >= 1f)
            {
                SetAlpha(image, full, 0f);
                image.gameObject.SetActive(false);
                return false;
            }

            SetAlpha(image, full, 1f - Mathf.Max(0f, t));
            return true;
        }

        private static void SetAlpha(Image image, Color full, float alpha)
        {
            var color = full;
            color.a = full.a * alpha;
            image.color = color;
        }
    }
}
