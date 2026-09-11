#nullable enable
using System;
using Chiki.Client.Profiles;
using Chiki.Client.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The settings menu (PRD 3.12): reachable from the pre-run screen and the map. It holds
    /// the Calibrate entry (PRD 3.12.1) and the metronome toggle (PRD 3.12.2), each written to
    /// the profile and saved the moment it changes. Volumes and display settings join later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsMenu : MonoBehaviour
    {
        private Profile _profile = null!;
        private ProfileStore _store = null!;
        private Toggle _metronome = null!;
        private Button _calibrate = null!;
        private Button _close = null!;

        public bool MetronomeOn => _metronome.isOn;

        public event Action? CalibrateChosen;

        public event Action? Closed;

        public static SettingsMenu Build(Transform? parent, Profile profile, ProfileStore store)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var canvas = ScreenFactory.Canvas("SettingsMenu", parent, 50);
            var menu = canvas.gameObject.AddComponent<SettingsMenu>();
            menu._profile = profile;
            menu._store = store;
            var root = canvas.transform;
            ScreenFactory.Fill("Dim", root, new Color(0f, 0f, 0f, 0.6f));
            var panel = Presenter.HudFactory.Image("Panel", root, ScreenFactory.Panel, Vector2.zero, new Vector2(720f, 520f));
            panel.raycastTarget = true;
            ScreenFactory.Label("Title", panel.transform, Strings.Get("settings.title"), 56, new Vector2(0f, 190f), new Vector2(640f, 80f), TextAnchor.MiddleCenter);
            menu._metronome = ScreenFactory.Toggle("Metronome", panel.transform, Strings.Get("settings.metronome"), profile.Settings.MetronomeOn, new Vector2(0f, 70f), new Vector2(560f, 60f), menu.OnMetronomeChanged);
            menu._calibrate = ScreenFactory.Button("Calibrate", panel.transform, Strings.Get("settings.calibrate"), new Vector2(0f, -40f), new Vector2(420f, 84f), () => menu.CalibrateChosen?.Invoke());
            menu._close = ScreenFactory.Button("Close", panel.transform, Strings.Get("settings.close"), new Vector2(0f, -170f), new Vector2(420f, 84f), menu.Close);
            return menu;
        }

        public bool ChooseCalibrate()
        {
            return ScreenFactory.Submit(_calibrate);
        }

        public bool ChooseClose()
        {
            return ScreenFactory.Submit(_close);
        }

        /// <summary>Sets the metronome toggle the way the player would.</summary>
        public void SetMetronome(bool on)
        {
            _metronome.isOn = on;
        }

        public void Close()
        {
            Closed?.Invoke();
            Destroy(gameObject);
        }

        private void OnMetronomeChanged(bool on)
        {
            _profile.Settings.MetronomeOn = on;
            _store.Save(_profile);
        }
    }
}
