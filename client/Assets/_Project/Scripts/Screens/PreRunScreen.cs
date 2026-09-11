#nullable enable
using System;
using Chiki.Client.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The pre-run screen: where a profile lands after launch and after every run (PRD 3.9.11),
    /// with the Start Run action and the settings entry (PRD 3.12.1). Start Run is disabled
    /// while the calibration screen is open on a first launch. Charm equipping (P17.4) joins
    /// this screen later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreRunScreen : MonoBehaviour
    {
        private Button _start = null!;
        private Button _settings = null!;

        /// <summary>Whether the Start Run action accepts input.</summary>
        public bool StartRunEnabled
        {
            get => _start.interactable;
            set => _start.interactable = value;
        }

        public event Action? StartRunChosen;

        public event Action? SettingsChosen;

        public static PreRunScreen Build(Transform? parent)
        {
            var canvas = ScreenFactory.Canvas("PreRunScreen", parent, 0);
            var screen = canvas.gameObject.AddComponent<PreRunScreen>();
            var root = canvas.transform;
            ScreenFactory.Fill("Backdrop", root, ScreenFactory.Backdrop);
            ScreenFactory.Label("Title", root, Strings.Get("prerun.title"), 96, new Vector2(0f, 260f), new Vector2(1200f, 140f), TextAnchor.MiddleCenter);
            screen._start = ScreenFactory.Button("StartRun", root, Strings.Get("prerun.start_run"), new Vector2(0f, -40f), new Vector2(420f, 96f), () => screen.StartRunChosen?.Invoke());
            screen._settings = ScreenFactory.Button("Settings", root, Strings.Get("menu.settings"), new Vector2(0f, -180f), new Vector2(420f, 96f), () => screen.SettingsChosen?.Invoke());
            return screen;
        }

        /// <summary>Activates Start Run the way the player would; false when the action is disabled.</summary>
        public bool ChooseStartRun()
        {
            return ScreenFactory.Submit(_start);
        }

        public bool ChooseSettings()
        {
            return ScreenFactory.Submit(_settings);
        }
    }
}
