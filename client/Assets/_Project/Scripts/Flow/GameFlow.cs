#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Chiki.Client.Content;
using Chiki.Client.Profiles;
using Chiki.Client.Scene;
using Chiki.Client.Screens;
using Chiki.Sim;
using Chiki.Sim.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Chiki.Client.Flow
{
    /// <summary>The outcome of Start Run (PRD 3.1.5, 3.12.1).</summary>
    public enum StartRunResult
    {
        Started,

        /// <summary>The profile already holds a run in progress; resume or end it first (PRD 3.1.5).</summary>
        RunInProgress,

        /// <summary>The calibration screen is open (PRD 3.12.1).</summary>
        CalibrationOpen,
    }

    /// <summary>
    /// The screens outside battle and how they follow each other (P15.3): the pre-run screen,
    /// the map, the settings menu and the calibration screen. On a profile's first launch the
    /// calibration screen opens over the pre-run screen and Start Run stays disabled until it
    /// closes (PRD 3.12.1); afterwards Calibrate sits in the settings menu on the pre-run
    /// screen and on the map. Start Run creates the run (P17.5) from the profile's unlocked
    /// Charms with none equipped, since the equip screen joins with P23, and opens the map.
    /// The profile holds at most one run in progress: it is saved on every node transition
    /// and whenever the map is entered, never during a battle (PRD 3.1.5, 3.1.6), and a
    /// profile with a run in progress resumes it on the map. Everything permanent a run earns
    /// is written the moment it happens (PRD 3.1.8), and a run that ends leaves its log
    /// (PRD 3.15.1).
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

        /// <summary>The content the run draws on; null before Start Run.</summary>
        public RunContent? Content { get; private set; }

        /// <summary>Applies the run's permanent consequences to the profile (P21.5); null before Start Run.</summary>
        public RunProgress? Progress { get; private set; }

        /// <summary>The path of the log the last ended run wrote (PRD 3.15.1); null before a run ends.</summary>
        public string? LastRunLogPath { get; private set; }

        public bool CalibrationOpen => Calibration != null;

        /// <summary>Runs the flow for a profile: it becomes the active profile, and a run in progress resumes on the map, otherwise the pre-run screen opens.</summary>
        public void Begin(Profile profile, ProfileStore store)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            ActiveProfile.Set(profile, store);
            EnsureEventSystem();
            if (profile.RunInProgress != null)
            {
                ResumeRun();
            }
            else
            {
                EnterPreRun();
            }
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

        /// <summary>Opens the map; the run in progress is saved on every return to it (PRD 3.1.5).</summary>
        public void EnterMap()
        {
            RequireProfile();
            CloseAll();
            Map = MapScreen.Build(transform);
            Map.SettingsChosen += OpenSettings;
            SaveRun();
        }

        /// <summary>Start Run from the pre-run screen; refused while the calibration screen is open or a run is in progress.</summary>
        public void StartRun()
        {
            TryStartRun();
        }

        /// <summary>Starts a run on a custom or generated seed (PRD 3.2.4) and opens the map; refused while the calibration screen is open or the profile holds a run in progress (PRD 3.1.5).</summary>
        public StartRunResult TryStartRun(string? seed = null)
        {
            if (CalibrationOpen)
            {
                return StartRunResult.CalibrationOpen;
            }

            RequireProfile();
            if ((Run != null && !Run.IsOver) || Profile!.RunInProgress != null)
            {
                return StartRunResult.RunInProgress;
            }

            var content = LoadContent();
            var setup = new RunSetup(Profile.Meta.CharmUnlocks);
            Run = setup.Start(content, seed, entropy: (ulong)DateTime.UtcNow.Ticks);
            Content = content;
            Progress = new RunProgress(Profile, content.Charms);
            EnterMap();
            return StartRunResult.Started;
        }

        /// <summary>Resumes the profile's run in progress at its last saved node, on the map (PRD 3.1.5).</summary>
        public void ResumeRun()
        {
            RequireProfile();
            var saved = Profile!.RunInProgress ?? throw new InvalidOperationException("The profile holds no run in progress.");
            var content = LoadContent();
            Run = RunSerializer.FromJson(saved, content);
            Content = content;
            Progress = new RunProgress(Profile, content.Charms);
            EnterMap();
        }

        /// <summary>The fixture content a run draws on (P17.5): the fixture cards, Charms, Imprints and enemies from data/.</summary>
        public static RunContent LoadContent()
        {
            var content = BattleContent.LoadFixtures();
            return new RunContent(
                content.Cards,
                CharmLoader.SetFromJson(ContentFiles.ReadText("charms/fixtures.json")),
                ImprintLoader.SetFromJson(ContentFiles.ReadText("imprints/fixtures.json")),
                enemies: new EnemySet("fixtures", content.Enemies.Values.ToList()));
        }

        /// <summary>Commits to a forward node (PRD 3.2.7); the transition saves the run (PRD 3.1.5).</summary>
        public MoveResult MoveTo(string nodeId)
        {
            var run = RequireRun();
            var result = run.MoveTo(nodeId);
            if (result == MoveResult.Moved)
            {
                SaveRun();
            }

            return result;
        }

        /// <summary>
        /// Writes the run in progress into the profile and the profile to disk. Nothing is
        /// written while a battle is in progress (PRD 3.1.6): the save made at the node
        /// transition holds the stats as they were when the battle began, so a resume restarts
        /// the battle from its first beat. An ended run clears the slot.
        /// </summary>
        public void SaveRun()
        {
            RequireProfile();
            if (Run == null || Run.CurrentBattle != null)
            {
                return;
            }

            Profile!.RunInProgress = Run.IsOver ? null : RunSerializer.ToJson(Run);
            Store!.Save(Profile);
        }

        /// <summary>
        /// Settles a battle the run started: a Boss defeat reaches the profile at once
        /// (PRD 3.9.10, 3.1.8), the run is saved, and a run that ended writes its log and
        /// clears the slot (PRD 3.15.1). Returns the cards the Binder destroyed.
        /// </summary>
        public IReadOnlyList<CardInstance> SettleBattle(Battle battle)
        {
            var run = RequireRun();
            var destroyed = run.SettleBattle(battle);
            Progress!.Apply(run);
            AfterRunChanged();
            return destroyed;
        }

        /// <summary>Takes a card of the open reward offer (PRD 3.7.2) and saves the run.</summary>
        public CardInstance PickReward(CardDefinition card)
        {
            var instance = RequireRun().PickReward(card);
            AfterRunChanged();
            return instance;
        }

        /// <summary>Declines the open reward offer (PRD 3.7.2) and saves the run.</summary>
        public void SkipReward()
        {
            RequireRun().SkipReward();
            AfterRunChanged();
        }

        /// <summary>Grants RP to an NPC (PRD 3.10.4) and writes the profile before returning (PRD 3.1.8); returns the levels gained.</summary>
        public int GrantRp(Npc npc, int amount, RpSource source)
        {
            RequireProfile();
            int gained = Profile!.GrantRp(npc, amount, source);
            Store!.Save(Profile);
            return gained;
        }

        private void AfterRunChanged()
        {
            if (Run!.IsOver)
            {
                EndRun();
            }
            else
            {
                SaveRun();
            }
        }

        /// <summary>The run ended (PRD 3.9.11): its log is written (PRD 3.15.1), the history takes its outcome, and the slot is cleared.</summary>
        private void EndRun()
        {
            LastRunLogPath = RunLogFile.Write(Profile!, Run!);
            Profile!.RunHistory.Add(new RunHistoryEntry(Run!.Seed, RunSerializer.StatusToId(Run.Status), ProfileStore.Now()));
            Profile.RunInProgress = null;
            Store!.Save(Profile);
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

        private Run RequireRun()
        {
            RequireProfile();
            if (Run == null || Progress == null)
            {
                throw new InvalidOperationException("No run is in progress.");
            }

            return Run;
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
