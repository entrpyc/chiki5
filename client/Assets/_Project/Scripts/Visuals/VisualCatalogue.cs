#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chiki.Client.Visuals
{
    /// <summary>One sprite in the catalogue, keyed by its kind and content id, with the opaque bounds recorded at import.</summary>
    [Serializable]
    public sealed class SpriteEntry
    {
        public string kind = "";
        public string id = "";
        public string subject = "";
        public string variant = "";
        public Sprite? sprite;
        public OpaqueRect opaque;
    }

    /// <summary>One clip in the catalogue, keyed by its kind and content id.</summary>
    [Serializable]
    public sealed class ClipEntry
    {
        public string kind = "";
        public string id = "";
        public string subject = "";
        public string variant = "";
        public SpriteClip? clip;
    }

    /// <summary>
    /// The map from a content id to its art (docs/project/unity-setup.md): a kind and an id give
    /// a <see cref="Sprite"/>, or a <see cref="SpriteClip"/> for the kinds that animate. A lookup
    /// of an id the catalogue does not hold returns the fallback and records <c>kind/id</c> in
    /// <see cref="Missing"/>, so a screen can be asked to prove it is drawn from shipped art.
    /// Definitions never reference art; the catalogue is the only bridge.
    ///
    /// Boot hands the shipped catalogue to the presenters through <see cref="Active"/>; with none
    /// handed over every lookup falls back, which is how the suites run before art ships.
    /// </summary>
    public sealed class VisualCatalogue : ScriptableObject
    {
        /// <summary>Where the shipped catalogue lives, for the rebuild command and the operator.</summary>
        public const string AssetPath = "Assets/_Project/Data/VisualCatalogue.asset";

        /// <summary>Where the font family lives (P10.1); a licensed family replaces these two files in place.</summary>
        public const string FontFolder = "Assets/_Project/UI/Fonts";

        public const string RegularFontFile = "font_regular.ttf";

        public const string BoldFontFile = "font_bold.ttf";

        private static VisualCatalogue? _active;
        private static Sprite? _white;
        private static Sprite? _fallback;

        [SerializeField] private List<SpriteEntry> sprites = new List<SpriteEntry>();
        [SerializeField] private List<ClipEntry> clips = new List<ClipEntry>();
        [SerializeField] private Font? font;
        [SerializeField] private Font? boldFont;

        private Dictionary<string, SpriteEntry>? _spriteIndex;
        private Dictionary<string, ClipEntry>? _clipIndex;
        private readonly List<string> _missing = new List<string>();
        private SpriteClip? _fallbackClip;

        /// <summary>The catalogue the presenters read; an empty one until Boot hands over the shipped asset.</summary>
        public static VisualCatalogue Active
        {
            get
            {
                if (_active == null)
                {
                    _active = CreateInstance<VisualCatalogue>();
                    _active.name = "VisualCatalogue (empty)";
                }

                return _active;
            }
        }

        /// <summary>Boot hands the shipped catalogue over once (P1.3).</summary>
        public static void Use(VisualCatalogue catalogue)
        {
            _active = catalogue != null ? catalogue : throw new ArgumentNullException(nameof(catalogue));
        }

        public static void Clear()
        {
            _active = null;
        }

        /// <summary>
        /// The 4 by 4 white sprite behind every plain colour fill, scrims included. It is built
        /// in code, belongs to no content id and is never counted as missing.
        /// </summary>
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    _white = SolidSprite("spr-builtin-white", Color.white);
                }

                return _white;
            }
        }

        /// <summary>
        /// What a lookup returns when the catalogue holds nothing for an id. It is white so a
        /// placeholder tinted in code still reads; <see cref="Missing"/> is the signal that art
        /// is owed, not the colour on screen.
        /// </summary>
        public static Sprite FallbackSprite
        {
            get
            {
                if (_fallback == null)
                {
                    _fallback = SolidSprite("spr-fallback", Color.white);
                }

                return _fallback;
            }
        }

        /// <summary>Every <c>kind/id</c> looked up that the catalogue does not hold, in the order they were first asked for.</summary>
        public IReadOnlyList<string> Missing => _missing;

        public IReadOnlyList<SpriteEntry> Sprites => sprites;

        public IReadOnlyList<ClipEntry> Clips => clips;

        public void ClearMissing()
        {
            _missing.Clear();
        }

        /// <summary>The sprite of a content id, or the fallback with <c>kind/id</c> recorded in <see cref="Missing"/>.</summary>
        public Sprite Sprite(string kind, string id)
        {
            var entry = FindSprite(kind, id);
            if (entry?.sprite != null)
            {
                return entry.sprite;
            }

            RecordMissing(kind, id);
            return FallbackSprite;
        }

        /// <summary>The sprite of a content id, or null when the catalogue does not hold it; nothing is recorded as missing.</summary>
        public Sprite? Find(string kind, string id)
        {
            return FindSprite(kind, id)?.sprite;
        }

        /// <summary>The opaque bounds recorded for a sprite at import; empty when the catalogue does not hold it.</summary>
        public OpaqueRect Bounds(string kind, string id)
        {
            return FindSprite(kind, id)?.opaque ?? default;
        }

        public bool Has(string kind, string id)
        {
            return FindSprite(kind, id)?.sprite != null;
        }

        /// <summary>The clip of a content id, or a one-frame fallback with <c>kind/id</c> recorded in <see cref="Missing"/>.</summary>
        public SpriteClip Clip(string kind, string id)
        {
            var entry = FindClip(kind, id);
            if (entry?.clip != null)
            {
                return entry.clip;
            }

            RecordMissing(kind, id);
            return FallbackClip();
        }

        /// <summary>A subject's clip by its variant, such as the <c>idle</c> of enemy <c>ren</c>.</summary>
        public SpriteClip Clip(string kind, string subject, string variant)
        {
            return Clip(kind, variant == SpriteNames.StaticVariant ? subject : subject + "-" + variant);
        }

        /// <summary>The clip of a content id, or null when the catalogue does not hold it; nothing is recorded as missing.</summary>
        public SpriteClip? FindClip(string kind, string subject, string variant)
        {
            return FindClip(kind, variant == SpriteNames.StaticVariant ? subject : subject + "-" + variant)?.clip;
        }

        public bool HasClip(string kind, string id)
        {
            return FindClip(kind, id)?.clip != null;
        }

        /// <summary>Every clip of one subject, in the order the catalogue holds them.</summary>
        public IReadOnlyList<SpriteClip> ClipsOf(string kind, string subject)
        {
            var found = new List<SpriteClip>();
            foreach (var entry in clips)
            {
                if (entry.clip != null && entry.kind == kind && entry.subject == subject)
                {
                    found.Add(entry.clip);
                }
            }

            return found;
        }

        /// <summary>
        /// The shipped font every text is set in (P10.1): the regular weight, or the bold one for
        /// headlines and buttons. Null until the fonts ship; text then falls back to the
        /// engine's built-in font.
        /// </summary>
        public Font? TextFont(bool bold = false)
        {
            return bold && boldFont != null ? boldFont : font;
        }

        /// <summary>Puts the font family; the rebuild command fills it from <see cref="FontFolder"/>.</summary>
        public void PutFonts(Font? regular, Font? bold)
        {
            font = regular;
            boldFont = bold;
        }

        /// <summary>Puts a sprite under a kind and id; the rebuild command and the tests fill a catalogue this way.</summary>
        public void Put(string kind, string id, Sprite sprite, string? subject = null, string? variant = null, OpaqueRect bounds = default)
        {
            RequireKey(kind, id);
            var entry = FindSprite(kind, id);
            if (entry == null)
            {
                entry = new SpriteEntry { kind = kind, id = id };
                sprites.Add(entry);
                Index()[Key(kind, id)] = entry;
            }

            entry.sprite = sprite != null ? sprite : throw new ArgumentNullException(nameof(sprite));
            entry.subject = subject ?? id;
            entry.variant = variant ?? SpriteNames.StaticVariant;
            entry.opaque = bounds;
        }

        /// <summary>Puts a clip under a kind and id.</summary>
        public void PutClip(string kind, string id, SpriteClip clip, string? subject = null, string? variant = null)
        {
            RequireKey(kind, id);
            var entry = FindClip(kind, id);
            if (entry == null)
            {
                entry = new ClipEntry { kind = kind, id = id };
                clips.Add(entry);
                ClipIndex()[Key(kind, id)] = entry;
            }

            entry.clip = clip != null ? clip : throw new ArgumentNullException(nameof(clip));
            entry.subject = subject ?? id;
            entry.variant = variant ?? SpriteNames.StaticVariant;
        }

        /// <summary>Empties the catalogue before a rebuild fills it again.</summary>
        public void RemoveAll()
        {
            sprites.Clear();
            clips.Clear();
            font = null;
            boldFont = null;
            _spriteIndex = null;
            _clipIndex = null;
            _missing.Clear();
        }

        private SpriteEntry? FindSprite(string kind, string id)
        {
            return Index().TryGetValue(Key(kind, id), out var entry) ? entry : null;
        }

        private ClipEntry? FindClip(string kind, string id)
        {
            return ClipIndex().TryGetValue(Key(kind, id), out var entry) ? entry : null;
        }

        private Dictionary<string, SpriteEntry> Index()
        {
            if (_spriteIndex == null)
            {
                _spriteIndex = new Dictionary<string, SpriteEntry>(StringComparer.Ordinal);
                foreach (var entry in sprites)
                {
                    _spriteIndex[Key(entry.kind, entry.id)] = entry;
                }
            }

            return _spriteIndex;
        }

        private Dictionary<string, ClipEntry> ClipIndex()
        {
            if (_clipIndex == null)
            {
                _clipIndex = new Dictionary<string, ClipEntry>(StringComparer.Ordinal);
                foreach (var entry in clips)
                {
                    _clipIndex[Key(entry.kind, entry.id)] = entry;
                }
            }

            return _clipIndex;
        }

        private void RecordMissing(string kind, string id)
        {
            string key = Key(kind, id);
            if (!_missing.Contains(key))
            {
                _missing.Add(key);
            }
        }

        private SpriteClip FallbackClip()
        {
            if (_fallbackClip == null)
            {
                _fallbackClip = SpriteClip.Create("vfx", "fallback", SpriteNames.StaticVariant, new[] { FallbackSprite }, 1, true, null);
            }

            return _fallbackClip;
        }

        private static string Key(string kind, string id)
        {
            return kind + "/" + id;
        }

        private static void RequireKey(string kind, string id)
        {
            if (string.IsNullOrWhiteSpace(kind))
            {
                throw new ArgumentException("A kind is required.", nameof(kind));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An id is required.", nameof(id));
            }
        }

        private static Sprite SolidSprite(string name, Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[16];
            var value = (Color32)color;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = value;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
