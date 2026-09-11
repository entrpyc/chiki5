#nullable enable
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One of the sixteen slots on screen (PRD 3.3.5.4, 3.3.8.1): the key's label, the card it
    /// holds, a dim overlay with the beats of cooldown left, a glow while the card can be played
    /// into the open Judgment Window, and a flash when its key is pressed. The flash decays over
    /// a fraction of a beat measured on the beat clock, never on frame time.
    /// </summary>
    public sealed class SlotWidget
    {
        /// <summary>How long a key flash lasts, in beats.</summary>
        public const float FlashBeats = 0.5f;

        private static readonly Color FrameColor = new Color(0.16f, 0.16f, 0.2f, 1f);
        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color GlowColor = new Color(1f, 0.9f, 0.4f, 0.55f);
        private static readonly Color FlashColor = new Color(1f, 1f, 1f, 0.8f);

        private readonly UnityEngine.UI.Text _keyLabel;
        private readonly UnityEngine.UI.Text _cardName;
        private readonly Image _overlay;
        private readonly UnityEngine.UI.Text _countdown;
        private readonly Image _glow;
        private readonly Image _flash;
        private int _flashStartMs;
        private bool _flashing;

        public Slot Slot { get; }

        public RectTransform Rect { get; }

        public int CooldownBeats { get; private set; }

        public bool IsOnCooldown => CooldownBeats > 0;

        public string CountdownText => _countdown.text;

        public bool OverlayShown => _overlay.gameObject.activeSelf;

        public bool IsGlowing => _glow.gameObject.activeSelf;

        public bool IsFlashing => _flashing;

        public string KeyLabelText => _keyLabel.text;

        public string CardNameText => _cardName.text;

        internal SlotWidget(Slot slot, RectTransform parent)
        {
            Slot = slot;
            var frame = HudFactory.Image("Slot-" + slot.Line + "-" + slot.Key, parent, FrameColor);
            Rect = frame.rectTransform;

            _glow = HudFactory.StretchedImage("Glow", Rect, GlowColor);
            _glow.rectTransform.offsetMin = new Vector2(-6f, -6f);
            _glow.rectTransform.offsetMax = new Vector2(6f, 6f);
            _glow.gameObject.SetActive(false);

            _keyLabel = HudFactory.StretchedText("Key", Rect, 18, Color.white, TextAnchor.UpperLeft);
            _keyLabel.rectTransform.offsetMin = new Vector2(6f, 0f);
            _keyLabel.rectTransform.offsetMax = new Vector2(0f, -4f);

            _cardName = HudFactory.StretchedText("Card", Rect, 14, new Color(0.85f, 0.85f, 0.9f, 1f), TextAnchor.LowerCenter);
            _cardName.rectTransform.offsetMin = new Vector2(2f, 6f);
            _cardName.rectTransform.offsetMax = new Vector2(-2f, 0f);

            _overlay = HudFactory.StretchedImage("Cooldown", Rect, OverlayColor);
            _countdown = HudFactory.StretchedText("Countdown", _overlay.rectTransform, 36, Color.white, TextAnchor.MiddleCenter);
            _overlay.gameObject.SetActive(false);

            _flash = HudFactory.StretchedImage("Flash", Rect, FlashColor);
            _flash.gameObject.SetActive(false);
        }

        public void SetCard(string name)
        {
            _cardName.text = name;
        }

        public void SetKeyLabel(string label)
        {
            _keyLabel.text = label;
        }

        /// <summary>Shows the overlay and the beats remaining, or clears both at zero (PRD 3.3.5.4).</summary>
        public void SetCooldown(int beats)
        {
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
            SetFlashAlpha(1f);
            _flash.gameObject.SetActive(true);
        }

        /// <summary>Advances the flash to an audio time; <paramref name="beatMs"/> is the length of a beat right now.</summary>
        public void Tick(int audioTimeMs, float beatMs)
        {
            if (!_flashing)
            {
                return;
            }

            float t = (audioTimeMs - _flashStartMs) / (FlashBeats * beatMs);
            if (t >= 1f)
            {
                _flashing = false;
                _flash.gameObject.SetActive(false);
                return;
            }

            SetFlashAlpha(1f - Mathf.Max(0f, t));
        }

        private void SetFlashAlpha(float alpha)
        {
            var color = FlashColor;
            color.a = FlashColor.a * alpha;
            _flash.color = color;
        }
    }
}
