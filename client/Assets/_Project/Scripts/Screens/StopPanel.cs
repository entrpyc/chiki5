#nullable enable
using System;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The stop opened on a node without content in this plan (Shop, Event, Blacksmith,
    /// Forge; PRD 3.2.11 to 3.2.14): it names the node and Continue returns to the map. The
    /// node counts as completed on arrival (P19.4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StopPanel : MonoBehaviour
    {
        private Button _continue = null!;

        public NodeType NodeType { get; private set; }

        public event Action? Continued;

        public static StopPanel Build(Transform? parent, MapNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var canvas = ScreenFactory.Canvas("StopPanel", parent, 40);
            var panel = canvas.gameObject.AddComponent<StopPanel>();
            panel.NodeType = node.Type;
            var root = canvas.transform;
            ScreenFactory.Fill("Dim", root, new Color(0f, 0f, 0f, 0.6f));
            var face = HudFactory.Image("Panel", root, ScreenFactory.Panel, Vector2.zero, new Vector2(800f, 420f));
            face.raycastTarget = true;
            ScreenFactory.Label("Title", face.transform, Labels.NodeLabel(node.Type), 52, new Vector2(0f, 130f), new Vector2(720f, 80f), TextAnchor.MiddleCenter);
            ScreenFactory.Label("Body", face.transform, Strings.Get("stop.empty"), 30, new Vector2(0f, 20f), new Vector2(720f, 100f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            panel._continue = ScreenFactory.Button("Continue", face.transform, Strings.Get("stop.continue"), new Vector2(0f, -130f), new Vector2(400f, 80f), () => panel.Continued?.Invoke());
            return panel;
        }

        public bool ChooseContinue()
        {
            return ScreenFactory.Submit(_continue);
        }
    }
}
