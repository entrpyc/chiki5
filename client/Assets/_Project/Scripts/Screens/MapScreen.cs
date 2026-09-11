#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Chiki.Client.Keys;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The map screen (PRD 3.2.16, P23.1): the current World's nodes and connections drawn
    /// layer by layer with a type icon and label each, the player marker on the current node,
    /// the forward neighbours selectable with the arrow keys and committed with Enter, a header
    /// with ARD, Essence, the seed and CRP (PRD 3.8.1), a floating label for every CRP change
    /// (PRD 3.8.6) and the settings entry (PRD 3.12.1). Built in code from flat-coloured
    /// elements until the visual catalogues carry art. With no run it is only the frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapScreen : MonoBehaviour
    {
        public const float BoardWidth = 1700f;
        public const float BoardHeight = 700f;
        public const float NodeSize = 34f;
        public const float CrpChangeSeconds = 1.6f;

        private static readonly Color Line = new Color(0.35f, 0.36f, 0.45f, 1f);
        private static readonly Color Visited = new Color(0.55f, 0.6f, 0.75f, 1f);
        private static readonly Color Marker = new Color(1f, 1f, 1f, 1f);

        private Run? _run;
        private Button _settings = null!;
        private UnityEngine.UI.Text _header = null!;
        private UnityEngine.UI.Text _crp = null!;
        private UnityEngine.UI.Text _crpChange = null!;
        private UnityEngine.UI.Text _hint = null!;
        private RectTransform _board = null!;
        private ScreenKeys? _keys;
        private readonly List<MapNode> _neighbours = new List<MapNode>();
        private readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>(StringComparer.Ordinal);
        private int _selected;
        private float _crpChangeUntil;

        public event Action? SettingsChosen;

        /// <summary>The player committed to a forward neighbour (PRD 3.2.7).</summary>
        public event Action<string>? NeighbourChosen;

        /// <summary>The forward neighbours in graph order; the selection cycles through them.</summary>
        public IReadOnlyList<MapNode> Neighbours => _neighbours;

        public int SelectedIndex => _selected;

        public MapNode? SelectedNeighbour => _neighbours.Count == 0 ? null : _neighbours[_selected];

        /// <summary>The CRP readout of the header (PRD 3.8.1).</summary>
        public string CrpText => _crp.text;

        public string HeaderText => _header.text;

        /// <summary>The floating CRP change label while it shows; null otherwise.</summary>
        public string? CrpChangeText => _crpChange.gameObject.activeSelf ? _crpChange.text : null;

        public static MapScreen Build(Transform? parent, Run? run)
        {
            var canvas = ScreenFactory.Canvas("MapScreen", parent, 0);
            var screen = canvas.gameObject.AddComponent<MapScreen>();
            screen._run = run;
            var root = canvas.transform;
            ScreenFactory.Fill("Backdrop", root, ScreenFactory.Backdrop);
            ScreenFactory.Label("Title", root, Strings.Get("map.title"), 48, new Vector2(-780f, 470f), new Vector2(300f, 80f), TextAnchor.MiddleLeft);
            screen._header = ScreenFactory.Label("Header", root, "", 30, new Vector2(0f, 470f), new Vector2(1100f, 80f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            screen._crp = ScreenFactory.Label("Crp", root, "", 40, new Vector2(0f, 410f), new Vector2(600f, 60f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._crpChange = ScreenFactory.Label("CrpChange", root, "", 32, new Vector2(0f, 360f), new Vector2(600f, 50f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._crpChange.gameObject.SetActive(false);
            screen._settings = ScreenFactory.Button("Settings", root, Strings.Get("menu.settings"), new Vector2(780f, 470f), new Vector2(300f, 80f), () => screen.SettingsChosen?.Invoke());
            screen._board = HudFactory.Rect("Board", root, new Vector2(0f, -40f), new Vector2(BoardWidth, BoardHeight));
            screen._hint = ScreenFactory.Label("Hint", root, Strings.Get("map.hint"), 28, new Vector2(0f, -470f), new Vector2(1400f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            screen._keys = new ScreenKeys(new[] { Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.Enter, Key.NumpadEnter }, screen.KeyDown);
            screen.Refresh();
            return screen;
        }

        /// <summary>Redraws the board and the header from the run's current state.</summary>
        public void Refresh()
        {
            foreach (Transform child in _board)
            {
                Destroy(child.gameObject);
            }

            _nodeImages.Clear();
            _neighbours.Clear();
            _selected = 0;
            if (_run == null || _run.IsOver)
            {
                _header.text = "";
                _crp.text = "";
                _hint.text = "";
                return;
            }

            var stats = _run.Stats;
            _header.text = Strings.Format("map.header", _run.World, stats.Ard, stats.MaxArd, stats.Essence, _run.Seed);
            _crp.text = Strings.Format("crp.label", stats.Crp);
            _hint.text = Strings.Get("map.hint");
            _neighbours.AddRange(_run.ForwardNodes);

            var map = _run.CurrentMap;
            var positions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            int layerCount = map.Layers.Count;
            for (int layer = 0; layer < layerCount; layer++)
            {
                var nodes = map.Layers[layer];
                float x = layerCount == 1 ? 0f : -BoardWidth / 2f + NodeSize + layer * (BoardWidth - 2f * NodeSize) / (layerCount - 1);
                for (int i = 0; i < nodes.Count; i++)
                {
                    float y = BoardHeight / 2f - (i + 0.5f) * BoardHeight / nodes.Count;
                    positions[nodes[i].Id] = new Vector2(x, y);
                }
            }

            foreach (var node in map.Nodes)
            {
                foreach (var nextId in node.Next)
                {
                    DrawConnection(positions[node.Id], positions[nextId], _run.Visited.Contains(node.Id) && _run.Visited.Contains(nextId));
                }
            }

            foreach (var node in map.Nodes)
            {
                var position = positions[node.Id];
                var image = HudFactory.Image("Node " + node.Id, _board, NodeColor(node.Type), position, new Vector2(NodeSize, NodeSize));
                _nodeImages[node.Id] = image;
                if (_run.Visited.Contains(node.Id))
                {
                    HudFactory.Image("Visited", image.transform, Visited, Vector2.zero, new Vector2(NodeSize * 0.4f, NodeSize * 0.4f));
                }

                var label = HudFactory.Text("Label", image.transform, 14, ScreenFactory.MutedText, TextAnchor.MiddleCenter);
                label.rectTransform.anchoredPosition = new Vector2(0f, -NodeSize * 0.9f);
                label.rectTransform.sizeDelta = new Vector2(120f, 24f);
                label.text = Labels.NodeLabel(node.Type);
            }

            var current = positions[_run.CurrentNodeId];
            var marker = HudFactory.Image("PlayerMarker", _board, Marker, current, new Vector2(NodeSize + 16f, NodeSize + 16f));
            marker.transform.SetAsFirstSibling();
            HighlightSelection();
        }

        /// <summary>Moves the selection to a forward neighbour.</summary>
        public void Select(int index)
        {
            if (_neighbours.Count == 0)
            {
                return;
            }

            _selected = ((index % _neighbours.Count) + _neighbours.Count) % _neighbours.Count;
            HighlightSelection();
        }

        /// <summary>Commits to the selected neighbour the way Enter would; false when there is none.</summary>
        public bool ChooseSelected()
        {
            var node = SelectedNeighbour;
            if (node == null)
            {
                return false;
            }

            NeighbourChosen?.Invoke(node.Id);
            return true;
        }

        /// <summary>Selects and commits to a forward neighbour by index.</summary>
        public bool ChooseNeighbour(int index)
        {
            Select(index);
            return ChooseSelected();
        }

        public bool ChooseSettings()
        {
            return ScreenFactory.Submit(_settings);
        }

        /// <summary>Shows a CRP change as a floating label: the signed amount and its source (PRD 3.8.6).</summary>
        public void ShowCrpChange(int amount, string source)
        {
            _crpChange.text = Strings.Format("crp.change", (amount >= 0 ? "+" : "") + amount, Labels.CrpSource(source));
            _crpChange.rectTransform.anchoredPosition = new Vector2(0f, 360f);
            _crpChange.gameObject.SetActive(true);
            _crpChangeUntil = Time.unscaledTime + CrpChangeSeconds;
            if (_run != null)
            {
                _crp.text = Strings.Format("crp.label", _run.Stats.Crp);
            }
        }

        /// <summary>A key went down: the arrows cycle the selection, Enter commits.</summary>
        public void KeyDown(Key key)
        {
            switch (key)
            {
                case Key.LeftArrow:
                case Key.UpArrow:
                    Select(_selected - 1);
                    break;
                case Key.RightArrow:
                case Key.DownArrow:
                    Select(_selected + 1);
                    break;
                case Key.Enter:
                case Key.NumpadEnter:
                    ChooseSelected();
                    break;
            }
        }

        private void HighlightSelection()
        {
            for (int i = 0; i < _neighbours.Count; i++)
            {
                if (_nodeImages.TryGetValue(_neighbours[i].Id, out var image))
                {
                    var rect = image.rectTransform;
                    float size = i == _selected ? NodeSize * 1.5f : NodeSize * 1.15f;
                    rect.sizeDelta = new Vector2(size, size);
                    var outline = image.GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = image.gameObject.AddComponent<Outline>();
                        outline.effectDistance = new Vector2(3f, -3f);
                    }

                    outline.effectColor = i == _selected ? ScreenFactory.Accent : Line;
                }
            }
        }

        private void DrawConnection(Vector2 from, Vector2 to, bool walked)
        {
            var delta = to - from;
            var line = HudFactory.Image("Connection", _board, walked ? Visited : Line, (from + to) / 2f, new Vector2(delta.magnitude, 3f));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Color NodeColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.NormalBattle: return new Color(0.8f, 0.3f, 0.3f, 1f);
                case NodeType.Elite: return new Color(0.9f, 0.5f, 0.15f, 1f);
                case NodeType.Boss: return new Color(0.75f, 0.1f, 0.5f, 1f);
                case NodeType.Shop: return new Color(0.25f, 0.6f, 0.9f, 1f);
                case NodeType.Event: return new Color(0.5f, 0.35f, 0.8f, 1f);
                case NodeType.Blacksmith: return new Color(0.5f, 0.5f, 0.55f, 1f);
                case NodeType.Forge: return new Color(0.9f, 0.7f, 0.2f, 1f);
                default: return Line;
            }
        }

        private void Update()
        {
            if (_crpChange.gameObject.activeSelf)
            {
                _crpChange.rectTransform.anchoredPosition += new Vector2(0f, 30f * Time.unscaledDeltaTime);
                if (Time.unscaledTime >= _crpChangeUntil)
                {
                    _crpChange.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            _keys?.Dispose();
            _keys = null;
        }
    }
}
