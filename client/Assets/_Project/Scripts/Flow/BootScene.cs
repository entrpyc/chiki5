#nullable enable
using System;
using Chiki.Client.Profiles;
using UnityEngine;

namespace Chiki.Client.Flow
{
    /// <summary>
    /// Composes the persistent Boot scene: opens the profile store under the persistent data
    /// path, loads or creates the configured profile (the picker of PRD 3.1.1 is out of scope,
    /// so one named profile is used) and starts the <see cref="GameFlow"/> on it. Runs on
    /// Start unless <see cref="Begin"/> was called first.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootScene : MonoBehaviour
    {
        public const string DefaultProfileName = "default";

        [SerializeField] private string profileName = DefaultProfileName;

        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
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

            var profile = store.LoadOrCreate(profileName);
            profile.LastPlayed = ProfileStore.Now();
            store.Save(profile);
            Flow = gameObject.AddComponent<GameFlow>();
            Flow.Begin(profile, store);
            Started = true;
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
