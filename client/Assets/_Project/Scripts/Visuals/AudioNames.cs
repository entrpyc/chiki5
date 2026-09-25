#nullable enable
using System;

namespace Chiki.Client.Visuals
{
    /// <summary>
    /// The audio naming convention the importer and the catalogue agree on (docs/plan.md, Asset
    /// conventions): sound effects are <c>sfx_&lt;subject&gt;_&lt;variant&gt;.wav</c> and their
    /// catalogue id is <c>&lt;subject&gt;-&lt;variant&gt;</c>; music is
    /// <c>mus_w&lt;world&gt;_&lt;subject&gt;.ogg</c> and its catalogue id is the id of the track
    /// sidecar under <c>data/tracks/</c> that names the same World and subject.
    /// </summary>
    public static class AudioNames
    {
        public const string SoundPrefix = "sfx";
        public const string MusicPrefix = "mus";

        /// <summary>The sound-effect id of a file name, or null when the name does not follow the convention.</summary>
        public static string? SoundId(string fileName)
        {
            string[] parts = Split(fileName);
            if (parts.Length != 3 || parts[0] != SoundPrefix)
            {
                return null;
            }

            return parts[1] + "-" + parts[2];
        }

        /// <summary>The World and subject of a music file name, or false when the name does not follow the convention.</summary>
        public static bool TryMusic(string fileName, out int world, out string subject)
        {
            world = 0;
            subject = "";
            string[] parts = Split(fileName);
            if (parts.Length != 3 || parts[0] != MusicPrefix || parts[1].Length < 2 || parts[1][0] != 'w')
            {
                return false;
            }

            if (!int.TryParse(parts[1].Substring(1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out world))
            {
                return false;
            }

            subject = parts[2];
            return subject.Length > 0;
        }

        /// <summary>Whether a track id belongs to a music file's subject: its last hyphen-separated part is that subject.</summary>
        public static bool TrackIdNames(string trackId, string subject)
        {
            if (string.IsNullOrEmpty(trackId) || string.IsNullOrEmpty(subject))
            {
                return false;
            }

            int hyphen = trackId.LastIndexOf('-');
            string last = hyphen < 0 ? trackId : trackId.Substring(hyphen + 1);
            return string.Equals(last, subject, StringComparison.Ordinal);
        }

        private static string[] Split(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Array.Empty<string>();
            }

            int dot = fileName.IndexOf('.');
            string stem = dot < 0 ? fileName : fileName.Substring(0, dot);
            return stem.Split('_');
        }
    }
}
