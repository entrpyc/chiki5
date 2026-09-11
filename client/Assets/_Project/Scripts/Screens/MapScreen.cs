#nullable enable
using System;
using Chiki.Client.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The map screen's frame: its header and the settings entry (PRD 3.2.16, 3.12.1). The
    /// nodes, connections and player marker are P23.1's; until then this is where the map's
    /// settings entry lives so calibration is reachable from the map.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapScreen : MonoBehaviour
    {
        private Button _settings = null!;

        public event Action? SettingsChosen;

        public static MapScreen Build(Transform? parent)
        {
            var canvas = ScreenFactory.Canvas("MapScreen", parent, 0);
            var screen = canvas.gameObject.AddComponent<MapScreen>();
            var root = canvas.transform;
            ScreenFactory.Fill("Backdrop", root, ScreenFactory.Backdrop);
            ScreenFactory.Label("Title", root, Strings.Get("map.title"), 64, new Vector2(0f, 460f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter);
            screen._settings = ScreenFactory.Button("Settings", root, Strings.Get("menu.settings"), new Vector2(780f, 460f), new Vector2(300f, 80f), () => screen.SettingsChosen?.Invoke());
            return screen;
        }

        public bool ChooseSettings()
        {
            return ScreenFactory.Submit(_settings);
        }
    }
}
