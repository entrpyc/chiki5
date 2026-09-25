#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Chiki.Sim.Data;

namespace Chiki.Client.Editor
{
    /// <summary>One variant's 9-slice border in pixels: the strips that stay put while the middle stretches.</summary>
    public sealed record SliceSpec(string Variant, int Left, int Bottom, int Right, int Top);

    /// <summary>
    /// The sidecar beside a subject's sprites, <c>&lt;kind&gt;_&lt;subject&gt;.slices.json</c>,
    /// giving the 9-slice border of each variant that has one (P2.1). A border is an import
    /// setting rather than anything in the PNG, and the plan states each 9-slice sprite's border
    /// in its item, so the numbers live beside the art in the same shape the clip sidecar uses:
    /// keyed by variant, omitted sides are zero.
    ///
    /// <code>
    /// {
    ///   "borders": {
    ///     "bg":     { "left": 32, "right": 32 },
    ///     "window": { "left": 16, "right": 16 }
    ///   }
    /// }
    /// </code>
    ///
    /// A subject with no 9-slice sprite carries no sidecar, which is not an error; a sidecar that
    /// cannot be read is, and it fails the import of the sprites it governs.
    /// </summary>
    public static class SliceSidecar
    {
        public const string Suffix = ".slices.json";

        private static readonly IReadOnlyDictionary<string, SliceSpec> None = new Dictionary<string, SliceSpec>(StringComparer.Ordinal);

        /// <summary>Reads a subject's borders; an absent sidecar reads as no borders, a malformed one as false with a reason.</summary>
        public static bool TryRead(string fullPath, out IReadOnlyDictionary<string, SliceSpec> borders, out string error)
        {
            borders = None;
            error = "";
            if (!File.Exists(fullPath))
            {
                return true;
            }

            try
            {
                var document = JsonValue.Parse(File.ReadAllText(fullPath));
                var members = document["borders"];
                var read = new Dictionary<string, SliceSpec>(StringComparer.Ordinal);
                foreach (string variant in members.MemberNames)
                {
                    var entry = members[variant];
                    int left = Side(entry, "left", variant, ref error);
                    int bottom = Side(entry, "bottom", variant, ref error);
                    int right = Side(entry, "right", variant, ref error);
                    int top = Side(entry, "top", variant, ref error);
                    if (error.Length > 0)
                    {
                        return false;
                    }

                    if (left + bottom + right + top == 0)
                    {
                        error = "border '" + variant + "' leaves every side at zero, which is no border at all";
                        return false;
                    }

                    read[variant] = new SliceSpec(variant, left, bottom, right, top);
                }

                borders = read;
                return true;
            }
            catch (Exception exception)
            {
                error = Path.GetFileName(fullPath) + " could not be read: " + exception.Message;
                return false;
            }
        }

        private static int Side(JsonValue entry, string name, string variant, ref string error)
        {
            int value = entry.Optional(name)?.AsInt() ?? 0;
            if (value < 0)
            {
                error = "border '" + variant + "' has " + name + " " + value + "; a border is never negative";
            }

            return value;
        }
    }
}
