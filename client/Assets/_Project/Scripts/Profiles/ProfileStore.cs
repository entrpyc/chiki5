#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Chiki.Client.Profiles
{
    /// <summary>A profile file written by a newer build than this one; it is left untouched (P22.3).</summary>
    public sealed class ProfileVersionException : Exception
    {
        public ProfileVersionException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Loads, saves and lists the profiles under one root folder (PRD 3.1.1, 3.1.2): one folder
    /// per profile named after it, holding <c>profile.json</c> and the run-log folder. The
    /// default root is the persistent data path; tests point a store at a scratch folder. The
    /// picker is out of scope, so callers create profiles by name.
    /// </summary>
    public sealed class ProfileStore
    {
        public const string FileName = "profile.json";
        public const string RunLogFolderName = "run-logs";
        public const int MaxNameLength = 64;

        public static string DefaultRoot => Path.Combine(Application.persistentDataPath, "profiles");

        public string Root { get; }

        public ProfileStore() : this(DefaultRoot)
        {
        }

        public ProfileStore(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("A root folder is required.", nameof(root));
            }

            Root = Path.GetFullPath(root);
        }

        /// <summary>An ISO 8601 UTC timestamp for the profile's dates.</summary>
        public static string Now()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        public string FolderOf(string name)
        {
            return Path.Combine(Root, ValidName(name));
        }

        public string FileOf(string name)
        {
            return Path.Combine(FolderOf(name), FileName);
        }

        public bool Exists(string name)
        {
            return File.Exists(FileOf(name));
        }

        /// <summary>The names of every profile under the root, sorted.</summary>
        public IReadOnlyList<string> List()
        {
            var names = new List<string>();
            if (!Directory.Exists(Root))
            {
                return names;
            }

            foreach (var folder in Directory.GetDirectories(Root))
            {
                if (File.Exists(Path.Combine(folder, FileName)))
                {
                    names.Add(Path.GetFileName(folder));
                }
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>Creates and saves a new profile; the name must be free.</summary>
        public Profile Create(string name)
        {
            name = ValidName(name);
            if (Exists(name))
            {
                throw new InvalidOperationException("A profile named '" + name + "' already exists.");
            }

            var profile = new Profile
            {
                Name = name,
                Created = Now(),
                RunLogFolder = Path.Combine(FolderOf(name), RunLogFolderName),
            };
            profile.EnsureRelationships();
            Save(profile);
            return profile;
        }

        public Profile Load(string name)
        {
            name = ValidName(name);
            var path = FileOf(name);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("No profile named '" + name + "'.", path);
            }

            var text = File.ReadAllText(path);
            var probe = JsonUtility.FromJson<VersionProbe>(text);
            if (probe.schemaVersion > Profile.CurrentSchemaVersion)
            {
                throw new ProfileVersionException("Profile '" + name + "' was written by a newer build (schema " + probe.schemaVersion + ", this build reads up to " + Profile.CurrentSchemaVersion + ").");
            }

            var profile = JsonUtility.FromJson<Profile>(text);
            profile.Name = name;
            profile.RunLogFolder = Path.Combine(FolderOf(name), RunLogFolderName);
            profile.EnsureRelationships();
            return profile;
        }

        public Profile LoadOrCreate(string name)
        {
            return Exists(name) ? Load(name) : Create(name);
        }

        /// <summary>Writes the profile to its file, replacing the previous copy only once the new one is complete.</summary>
        public void Save(Profile profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var folder = FolderOf(profile.Name);
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(profile.RunLogFolder);

            var path = Path.Combine(folder, FileName);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(profile, true));
            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }

        /// <summary>A name that is safe as a folder name; trimmed, non-empty and free of path characters.</summary>
        public static string ValidName(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var trimmed = name.Trim();
            if (trimmed.Length == 0 || trimmed.Length > MaxNameLength || trimmed == "." || trimmed == "..")
            {
                throw new ArgumentException("A profile name must be 1 to " + MaxNameLength + " characters.", nameof(name));
            }

            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmed.IndexOf('/') >= 0 || trimmed.IndexOf('\\') >= 0)
            {
                throw new ArgumentException("A profile name cannot contain path characters.", nameof(name));
            }

            return trimmed;
        }

        [Serializable]
        private sealed class VersionProbe
        {
            public int schemaVersion;
        }
    }
}
