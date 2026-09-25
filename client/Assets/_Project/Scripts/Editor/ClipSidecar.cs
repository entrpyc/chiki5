#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Chiki.Sim.Data;

namespace Chiki.Client.Editor
{
    /// <summary>One clip's entry in a subject's sidecar: its length in beats, whether it loops and which frame strikes.</summary>
    public sealed record ClipSpec(string Variant, int Beats, bool Loop, int? StrikeFrame);

    /// <summary>
    /// The sidecar beside a subject's frames, <c>&lt;kind&gt;_&lt;subject&gt;.clips.json</c>,
    /// giving each clip's length in beats, loop flag and strike frame (docs/plan.md, Asset
    /// conventions). Frames are numbered from 1, as the files are.
    ///
    /// <code>
    /// {
    ///   "clips": {
    ///     "idle":        { "beats": 2, "loop": true },
    ///     "attack-left": { "beats": 1, "loop": false, "strikeFrame": 2 }
    ///   }
    /// }
    /// </code>
    /// </summary>
    public static class ClipSidecar
    {
        /// <summary>Reads a sidecar; false with a reason when the file is missing or malformed.</summary>
        public static bool TryRead(string fullPath, out IReadOnlyDictionary<string, ClipSpec> clips, out string error)
        {
            clips = new Dictionary<string, ClipSpec>();
            error = "";
            if (!File.Exists(fullPath))
            {
                error = "no sidecar " + Path.GetFileName(fullPath) + " sits beside the frames";
                return false;
            }

            try
            {
                var document = JsonValue.Parse(File.ReadAllText(fullPath));
                var members = document["clips"];
                var read = new Dictionary<string, ClipSpec>(StringComparer.Ordinal);
                foreach (string variant in members.MemberNames)
                {
                    var entry = members[variant];
                    int beats = entry["beats"].AsInt();
                    if (beats <= 0)
                    {
                        error = "clip '" + variant + "' lasts " + beats + " beats; a clip lasts at least one";
                        return false;
                    }

                    bool loop = entry.Optional("loop")?.AsBool() ?? false;
                    int? strike = entry.Optional("strikeFrame")?.AsInt();
                    if (strike.HasValue && strike.Value < 1)
                    {
                        error = "clip '" + variant + "' has strike frame " + strike.Value + "; frames count from 1";
                        return false;
                    }

                    read[variant] = new ClipSpec(variant, beats, loop, strike);
                }

                clips = read;
                return true;
            }
            catch (Exception exception)
            {
                error = Path.GetFileName(fullPath) + " could not be read: " + exception.Message;
                return false;
            }
        }
    }
}
