#nullable enable
using System;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The pre-run screen: where a profile lands after launch and after every run (PRD 3.9.11),
    /// with the Start Run action and the settings entry (PRD 3.12.1). The logo stands over the
    /// title background in place of the title text (P10.2); the text returns only while the
    /// catalogue holds no logo. Start Run is disabled
    /// while the calibration screen is open on a first launch. Charm equipping (P17.4) joins
    /// this screen later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreRunScreen : MonoBehaviour
    {
        private Button _start = null!;
        private Button _settings = null!;

        public const string LogoKind = "logo";
        public const string LogoId = "chiki";
        public const string BackgroundKind = "bg";
        public const string BackgroundId = "title";

        /// <summary>The logo's drawn size (P10.2).</summary>
        public static readonly Vector2 LogoSize = new Vector2(1200f, 400f);

        /// <summary>The title background behind everything on the screen.</summary>
        public Image Background { get; private set; } = null!;

        /// <summary>The logo; null while the catalogue holds none and the title text stands in.</summary>
        public Image? Logo { get; private set; }

        /// <summary>The title text; null while the logo is shown.</summary>
        public UnityEngine.UI.Text? Title { get; private set; }

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
            var catalogue = VisualCatalogue.Active;
            screen.Background = ScreenFactory.FullScreen("TitleBackground", root, BackgroundKind, BackgroundId, ScreenFactory.Backdrop);
            var logo = catalogue.Sprite(LogoKind, LogoId);
            if (catalogue.Has(LogoKind, LogoId))
            {
                screen.Logo = HudFactory.Image("Logo", root, Color.white, new Vector2(0f, 260f), LogoSize, logo);
            }
            else
            {
                screen.Title = ScreenFactory.Label("Title", root, Strings.Get("prerun.title"), 96, new Vector2(0f, 260f), new Vector2(1200f, 140f), TextAnchor.MiddleCenter);
            }

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
