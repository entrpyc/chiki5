#nullable enable
using System;

namespace Chiki.Client.Profiles
{
    /// <summary>
    /// The profile the game is running under and the store it came from, set by the game flow
    /// when a profile is chosen. Scenes composed afterwards read the settings and the
    /// calibration offset from here (PRD 3.3.8.2); with no profile set they use the defaults.
    /// </summary>
    public static class ActiveProfile
    {
        public static Profile? Current { get; private set; }

        public static ProfileStore? Store { get; private set; }

        /// <summary>The calibration offset to subtract from every input stamp (PRD 3.12.1); 0 without a profile.</summary>
        public static int CalibrationOffsetMs => Current?.CalibrationOffsetMs ?? 0;

        /// <summary>The metronome toggle (PRD 3.12.2); off without a profile.</summary>
        public static bool MetronomeOn => Current?.Settings.MetronomeOn ?? false;

        /// <summary>The metronome volume as a factor 0..1 (PRD 3.12.4); full without a profile.</summary>
        public static float MetronomeVolume => Current != null ? Math.Max(0, Math.Min(100, Current.Settings.MetronomeVolume)) / 100f : 1f;

        public static void Set(Profile profile, ProfileStore store)
        {
            Current = profile ?? throw new ArgumentNullException(nameof(profile));
            Store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public static void Clear()
        {
            Current = null;
            Store = null;
        }

        /// <summary>Writes the current profile to disk, when there is one.</summary>
        public static void Save()
        {
            if (Current != null && Store != null)
            {
                Store.Save(Current);
            }
        }
    }
}
