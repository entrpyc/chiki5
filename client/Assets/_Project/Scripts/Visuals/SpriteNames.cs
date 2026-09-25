#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chiki.Client.Visuals
{
    /// <summary>
    /// One sprite file's name taken apart: <c>spr_&lt;kind&gt;_&lt;subject&gt;_&lt;variant&gt;_&lt;nn&gt;.png</c>
    /// (docs/plan.md, Asset conventions). The subject is the content id without its kind prefix
    /// (<c>ren</c> for <c>enemy-ren</c>), the variant is the clip or state, and the frames of one
    /// clip count from 01.
    /// </summary>
    public sealed record SpriteName(string Kind, string Subject, string Variant, int Index)
    {
        /// <summary>The catalogue id: the subject alone for a single-state sprite, subject and variant otherwise.</summary>
        public string Id => Variant == SpriteNames.StaticVariant ? Subject : Subject + "-" + Variant;

        /// <summary>The clip this frame belongs to, for the kinds that animate.</summary>
        public string ClipKey => Kind + "_" + Subject + "_" + Variant;

        public override string ToString()
        {
            return SpriteNames.Prefix + "_" + Kind + "_" + Subject + "_" + Variant + "_" + Index.ToString("00");
        }
    }

    /// <summary>
    /// The sprite naming convention the importer and the catalogues agree on (docs/plan.md,
    /// Asset conventions). A file under <c>Art/</c> that does not match, or whose kind is not
    /// one of the listed ones, is refused at import.
    /// </summary>
    public static class SpriteNames
    {
        public const string Prefix = "spr";

        /// <summary>The variant of a sprite with one state, whose catalogue id is its subject alone.</summary>
        public const string StaticVariant = "static";

        /// <summary>HD art at the 1080p reference imports at 100 pixels per unit (docs/project/unity-setup.md).</summary>
        public const float PixelsPerUnit = 100f;

        /// <summary>Every kind a sprite may carry, in the order the conventions list them.</summary>
        public static readonly IReadOnlyList<string> Kinds = new[]
        {
            "enemy", "player", "vfx", "bg", "card", "portrait", "status", "node",
            "action", "category", "ability", "trait", "role", "charm", "imprint", "ui", "logo",
        };

        /// <summary>The kinds whose frames become a <see cref="SpriteClip"/>: characters and effects.</summary>
        public static readonly IReadOnlyList<string> ClipKinds = new[] { "enemy", "player", "vfx" };

        public static bool IsKind(string kind)
        {
            return IndexOf(Kinds, kind) >= 0;
        }

        public static bool IsClipKind(string kind)
        {
            return IndexOf(ClipKinds, kind) >= 0;
        }

        /// <summary>The pivot a kind's sprites carry: bottom centre for the kinds that stand on the floor line, centred otherwise.</summary>
        public static Vector2 Pivot(string kind)
        {
            return IsClipKind(kind) ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        }

        /// <summary>The sidecar beside the frames of a subject, naming each clip's beats, loop flag and strike frame.</summary>
        public static string SidecarFileName(string kind, string subject)
        {
            return kind + "_" + subject + ".clips.json";
        }

        /// <summary>
        /// Takes a file name apart, with or without its extension. Returns false with a reason
        /// naming what is wrong when the name does not follow the convention.
        /// </summary>
        public static bool TryParse(string fileName, out SpriteName? name, out string error)
        {
            name = null;
            error = "";
            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "the name is empty";
                return false;
            }

            string stem = StripExtension(fileName);
            string[] parts = stem.Split('_');
            if (parts.Length != 5)
            {
                error = "a sprite is named spr_<kind>_<subject>_<variant>_<nn>, which is five parts separated by underscores, not " + parts.Length;
                return false;
            }

            if (parts[0] != Prefix)
            {
                error = "a sprite's name starts with '" + Prefix + "_', not '" + parts[0] + "_'";
                return false;
            }

            if (!IsKind(parts[1]))
            {
                error = "'" + parts[1] + "' is not one of the sprite kinds: " + string.Join(", ", Kinds);
                return false;
            }

            if (!IsSegment(parts[2]))
            {
                error = "the subject '" + parts[2] + "' is not lowercase kebab-case";
                return false;
            }

            if (!IsSegment(parts[3]))
            {
                error = "the variant '" + parts[3] + "' is not lowercase kebab-case";
                return false;
            }

            if (!TryFrameIndex(parts[4], out int index))
            {
                error = "the frame number '" + parts[4] + "' is not two or more digits counting from 01";
                return false;
            }

            name = new SpriteName(parts[1], parts[2], parts[3], index);
            return true;
        }

        private static string StripExtension(string fileName)
        {
            int dot = fileName.IndexOf('.');
            return dot < 0 ? fileName : fileName.Substring(0, dot);
        }

        private static bool TryFrameIndex(string text, out int index)
        {
            index = 0;
            if (text.Length < 2)
            {
                return false;
            }

            foreach (char c in text)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            index = int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            return index >= 1;
        }

        /// <summary>Lowercase kebab-case: letters and digits in groups separated by single hyphens.</summary>
        private static bool IsSegment(string text)
        {
            if (text.Length == 0 || text[0] == '-' || text[text.Length - 1] == '-')
            {
                return false;
            }

            bool previousHyphen = false;
            foreach (char c in text)
            {
                bool hyphen = c == '-';
                if (hyphen && previousHyphen)
                {
                    return false;
                }

                if (!hyphen && !((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                {
                    return false;
                }

                previousHyphen = hyphen;
            }

            return true;
        }

        private static int IndexOf(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
