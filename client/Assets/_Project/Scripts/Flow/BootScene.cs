#nullable enable
using System;
using Chiki.Client.Profiles;
using Chiki.Client.Visuals;
using UnityEngine;

namespace Chiki.Client.Flow
{
    /// <summary>
    /// Composes the persistent Boot scene: hands the visual and audio catalogues to the
    /// presenters (P1.3, P1.4), opens the profile store under the persistent data path, loads or
    /// creates the configured profile (the picker of PRD 3.1.1 is out of scope, so one named
    /// profile is used) and starts the <see cref="GameFlow"/> on it. Runs on Start unless
    /// <see cref="Begin"/> was called first.
    ///
    /// The catalogues are assigned in the Boot scene; with none assigned every lookup falls back
    /// and records what is missing, which is how the suites run before art ships.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootScene : MonoBehaviour
    {
        public const string DefaultProfileName = "default";

        [SerializeField] private string profileName = DefaultProfileName;
        [SerializeField] private VisualCatalogue? visuals;
        [SerializeField] private AudioCatalogue? audio;

        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        /// <summary>The visual catalogue Boot hands to the presenters; null leaves them on the fallbacks.</summary>
        public VisualCatalogue? Visuals
        {
            get => visuals;
            set => visuals = value;
        }

        /// <summary>The audio catalogue Boot hands to the cues, the metronome and the track loader.</summary>
        public AudioCatalogue? Audio
        {
            get => audio;
            set => audio = value;
        }

        public bool Started { get; private set; }

        public GameFlow? Flow { get; private set; }

        public void Begin(ProfileStore store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (Started)
            {
                throw new InvalidOperationException("The Boot scene has already started.");
            }

            PublishCatalogues();
            var profile = store.LoadOrCreate(profileName);
            profile.LastPlayed = ProfileStore.Now();
            store.Save(profile);
            Flow = gameObject.AddComponent<GameFlow>();
            Flow.Begin(profile, store);
            Started = true;
        }

        /// <summary>Hands the assigned catalogues to the presenters, once, before anything is built.</summary>
        private void PublishCatalogues()
        {
            if (visuals != null)
            {
                VisualCatalogue.Use(visuals);
            }

            if (audio != null)
            {
                AudioCatalogue.Use(audio);
            }
        }

        private void Start()
        {
            if (!Started)
            {
                Begin(new ProfileStore());
            }
        }
    }
}
