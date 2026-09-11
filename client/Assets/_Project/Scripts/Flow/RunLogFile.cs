#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Chiki.Client.Profiles;
using Chiki.Sim;
using Chiki.Sim.Data;

namespace Chiki.Client.Flow
{
    /// <summary>
    /// Writes an ended run's log under the profile's run-log folder (PRD 3.15.1): the content
    /// is the simulation's <see cref="RunLog"/>, which holds no profile name and nothing about
    /// the machine; the file name carries only a UTC timestamp and the seed.
    /// </summary>
    public static class RunLogFile
    {
        public const string Prefix = "run-";

        /// <summary>Writes the log and returns its path.</summary>
        public static string Write(Profile profile, Run run)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (string.IsNullOrEmpty(profile.RunLogFolder))
            {
                throw new InvalidOperationException("The profile has no run-log folder; load it through a ProfileStore.");
            }

            Directory.CreateDirectory(profile.RunLogFolder);
            var path = Path.Combine(profile.RunLogFolder, FileName(run.Seed, DateTime.UtcNow));
            File.WriteAllText(path, RunLogSerializer.ToJson(RunLog.From(run)));
            return path;
        }

        /// <summary>The log's file name: the prefix, the UTC time and the seed reduced to letters, digits and dashes.</summary>
        public static string FileName(string seed, DateTime utc)
        {
            var safe = new StringBuilder();
            foreach (char c in seed ?? "")
            {
                safe.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            }

            return Prefix + utc.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + safe + ".json";
        }
    }
}
