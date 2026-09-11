#nullable enable
using System;
using Chiki.Client.Content;
using Chiki.Client.Profiles;
using Chiki.Client.Screens;
using Chiki.Sim;
using Chiki.Sim.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Chiki.Client.Flow
{
    /// <summary>
    /// The screens outside battle and how they follow each other (P15.3): the pre-run screen,
    /// the map, the settings menu and the calibration screen. On a profile's first launch the
    /// calibration screen opens over the pre-run screen and Start Run stays disabled until it
    /// closes (PRD 3.12.1); afterwards Calibrate sits in the settings menu on the pre-run
    /// screen and on the map. Start Run creates the run (P17.5) from the profile's unlocked
    /// Charms with none equipped, since the equip screen joins with P23, and opens the map.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlow : MonoBehaviour
    {
        public Profile? Profile { get; private set; }

        public ProfileStore? Store { get; private set; }

        public PreRunScreen? PreRun { get; private set; }

        public MapScreen? Map { get; private set; }

        public SettingsMenu? Settings { get; private set; }

        public CalibrationScreen? Calibration { get; private set; }

        /// <summary>The run in progress (PRD 4.2); null before Start Run.</summary>
        public Run? Run { get; private set; }

        public bool CalibrationOpen => Calibration != null;

        /// <summary>Runs the flow for a profile: it becomes the active profile and the pre-run screen opens.</summary>
        public void Begin(Profile profile, ProfileStore store)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            ActiveProfile.Set(profile, store);
            EnsureEventSystem();
            EnterPreRun();
        }

        public void EnterPreRun()
        {
            RequireProfile();
            CloseAll();
            PreRun = PreRunScreen.Build(transform);
            PreRun.StartRunChosen += StartRun;
            PreRun.SettingsChosen += OpenSettings;
            if (!Profile!.Calibrated)
            {
                OpenCalibration();
            }
        }

        public void EnterMap()
        {
            RequireProfile();
            CloseAll();
            Map = MapScreen.Build(transform);
            Map.SettingsChosen += OpenSettings;
        }

        /// <summary>Start Run from the pre-run screen; refused while the calibration screen is open.</summary>
        public void StartRun()
        {
            if (CalibrationOpen)
            {
                return;
            }

            RequireProfile();
            var starter = CardLoader.SetFromJson(ContentFiles.ReadText("sets/starter.json"));
            var setup = new RunSetup(Profile!.Meta.CharmUnlocks);
            Run = setup.Start(starter, seed: null, entropy: (ulong)DateTime.UtcNow.Ticks);
            EnterMap();
        }

        public void OpenSettings()
        {
            RequireProfile();
            if (Settings != null)
            {
                return;
            }

            Settings = SettingsMenu.Build(transform, Profile!, Store!);
            Settings.CalibrateChosen += OpenCalibration;
            Settings.Closed += () => Settings = null;
        }

        public void OpenCalibration()
        {
            RequireProfile();
            if (Calibration != null)
            {
                return;
            }

            Calibration = CalibrationScreen.Open(transform, Profile!, Store!);
            Calibration.Closed += OnCalibrationClosed;
            if (PreRun != null)
            {
                PreRun.StartRunEnabled = false;
            }
        }

        private void OnCalibrationClosed(CalibrationScreen screen)
        {
            Calibration = null;
            if (PreRun != null)
            {
                PreRun.StartRunEnabled = true;
            }
        }

        private void CloseAll()
        {
            if (Calibration != null)
            {
                Calibration.Closed -= OnCalibrationClosed;
                Destroy(Calibration.gameObject);
                Calibration = null;
            }

            if (Settings != null)
            {
                Destroy(Settings.gameObject);
                Settings = null;
            }

            if (PreRun != null)
            {
                Destroy(PreRun.gameObject);
                PreRun = null;
            }

            if (Map != null)
            {
                Destroy(Map.gameObject);
                Map = null;
            }
        }

        private void RequireProfile()
        {
            if (Profile == null || Store == null)
            {
                throw new InvalidOperationException("The flow has no profile; call Begin first.");
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var host = new GameObject("EventSystem");
            host.transform.SetParent(transform, false);
            host.AddComponent<EventSystem>();
            host.AddComponent<InputSystemUIInputModule>();
        }

        private void OnDestroy()
        {
            if (Profile != null && ReferenceEquals(ActiveProfile.Current, Profile))
            {
                ActiveProfile.Clear();
            }
        }
    }
}
