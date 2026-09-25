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
    /// <summary>A connection drawn on the map board: the two nodes it joins and its image (P9.3).</summary>
    public sealed class MapConnectionView
    {
        public MapConnectionView(string fromId, string toId, bool walked, Image image)
        {
            FromId = fromId;
            ToId = toId;
            Walked = walked;
            Image = image;
        }

        public string FromId { get; }

        public string ToId { get; }

        /// <summary>Whether the run has walked it: both its nodes are visited.</summary>
        public bool Walked { get; }

        public Image Image { get; }
    }

    /// <summary>
    /// The map screen (PRD 3.2.16, P23.1, P9.3 to P9.5): the current World's nodes and
    /// connections drawn layer by layer over the map backdrop, each node its type's icon with its
    /// label below, every connection a tiled path (the walked variant once walked), the player
    /// marker pinned over the current node, and the forward neighbours selectable with the arrow
    /// keys and committed with Enter. The header shows the continent name, ARD, CRP beside its
    /// icon (PRD 3.8.1, P4.5), Base DMG, Essence, the seed, the Binder button (PRD 3.5.11) and
    /// settings (PRD 3.12.1); under it a row of the equipped Charms and held Imprints, each with
    /// a tooltip. A floating label shows every CRP change (PRD 3.8.6). Every piece of art comes
    /// from the visual catalogue and every word from the string table. With no run it is only the frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapScreen : MonoBehaviour
    {
        public const float BoardWidth = 1700f;
        public const float BoardHeight = 640f;

        /// <summary>A node icon's drawn size on the board; the art is drawn at 64 (P9.3).</summary>
        public const float NodeSize = 52f;

        public const float CrpChangeSeconds = 1.6f;

        /// <summary>The CRP icon's drawn size in the header, and the gap between it and the value (P4.5).</summary>
        public const float CrpIconSize = 46f;

        public const float CrpIconGap = 12f;

        /// <summary>The Charm and Imprint icons' drawn size in the row under the header (P9.5).</summary>
        public const float HeldIconSize = 48f;

        /// <summary>The kinds and ids the map's art is catalogued under (P9.3).</summary>
        public const string NodeKind = "node";

        public const string UiKind = "ui";
        public const string MarkerId = "map-marker";
        public const string PathId = "map-path";
        public const string WalkedPathId = "map-path-walked";
        public const string BackdropKind = "bg";
        public const string BackdropId = "map";

        /// <summary>The backdrop's drawn size: its central 1920 by 1080 is the 16:9 frame, like the arena's (P5.3).</summary>
        public static readonly Vector2 BackdropSize = new Vector2(2560f, 1080f);

        /// <summary>A path sprite's height on the board, the art's own (P9.3).</summary>
        public const float PathHeight = 8f;

        private static readonly Color Line = new Color(0.35f, 0.36f, 0.45f, 1f);
        private static readonly Color Visited = new Color(0.55f, 0.6f, 0.75f, 1f);
        private static readonly Color Marker = new Color(1f, 1f, 1f, 1f);
        private static readonly Color CharmPlaceholder = new Color(0.88f, 0.77f, 0.47f, 1f);
        private static readonly Color ImprintPlaceholder = new Color(0.6f, 0.75f, 0.9f, 1f);

        private Run? _run;
        private RunContent? _content;
        private Image _backdrop = null!;
        private Button _settings = null!;
        private Button _binder = null!;
        private UnityEngine.UI.Text _continent = null!;
        private UnityEngine.UI.Text _ard = null!;
        private UnityEngine.UI.Text _baseDmg = null!;
        private UnityEngine.UI.Text _essence = null!;
        private UnityEngine.UI.Text _seed = null!;
        private UnityEngine.UI.Text _crp = null!;
        private Image _crpIcon = null!;
        private UnityEngine.UI.Text _crpChange = null!;
        private UnityEngine.UI.Text _hint = null!;
        private RectTransform _held = null!;
        private RectTransform _board = null!;
        private Image? _marker;
        private ScreenKeys? _keys;
        private readonly List<MapNode> _neighbours = new List<MapNode>();
        private readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>(StringComparer.Ordinal);
        private readonly List<MapConnectionView> _connections = new List<MapConnectionView>();
        private readonly List<HeldIcon> _heldIcons = new List<HeldIcon>();
        private int _selected;
        private float _crpChangeUntil;

        public event Action? SettingsChosen;

        /// <summary>The Binder button was pressed: the Binder opens to review (PRD 3.5.11).</summary>
        public event Action? BinderChosen;

        /// <summary>The player committed to a forward neighbour (PRD 3.2.7).</summary>
        public event Action<string>? NeighbourChosen;

        /// <summary>Whether a screen covers the map, such as the read-only Binder; the map's keys rest while it does.</summary>
        public bool Covered { get; set; }

        /// <summary>The forward neighbours in graph order; the selection cycles through them.</summary>
        public IReadOnlyList<MapNode> Neighbours => _neighbours;

        public int SelectedIndex => _selected;

        public MapNode? SelectedNeighbour => _neighbours.Count == 0 ? null : _neighbours[_selected];

        /// <summary>The backdrop behind the board, the same in every World (P9.3).</summary>
        public Image Backdrop => _backdrop;

        /// <summary>The player marker over the current node; null while no run is on.</summary>
        public Image? PlayerMarker => _marker;

        /// <summary>Every connection on the board, in graph order.</summary>
        public IReadOnlyList<MapConnectionView> Connections => _connections;

        /// <summary>The equipped Charms, then the held Imprints, in the row under the header (P9.5).</summary>
        public IReadOnlyList<HeldIcon> HeldIcons => _heldIcons;

        /// <summary>The CRP readout of the header (PRD 3.8.1).</summary>
        public string CrpText => _crp.text;

        /// <summary>The CRP icon the value sits beside (P4.5); hidden while no run is on.</summary>
        public Image CrpIcon => _crpIcon;

        /// <summary>The rect the CRP value is drawn in, to the icon's right.</summary>
        public RectTransform CrpLabel => _crp.rectTransform;

        /// <summary>The current World's continent name (PRD 3.2.16).</summary>
        public string ContinentText => _continent.text;

        public string ArdText => _ard.text;

        public string BaseDmgText => _baseDmg.text;

        public string EssenceText => _essence.text;

        public string SeedText => _seed.text;

        public Button BinderButton => _binder;

        public Button SettingsButton => _settings;

        /// <summary>The floating CRP change label while it shows; null otherwise.</summary>
        public string? CrpChangeText => _crpChange.gameObject.activeSelf ? _crpChange.text : null;

        /// <summary>The catalogue id of a node type's icon: its name in kebab-case (<c>normal-battle</c>).</summary>
        public static string NodeIconId(NodeType type)
        {
            switch (type)
            {
                case NodeType.NormalBattle: return "normal-battle";
                case NodeType.Elite: return "elite";
                case NodeType.Boss: return "boss";
                case NodeType.Shop: return "shop";
                case NodeType.Event: return "event";
                case NodeType.Blacksmith: return "blacksmith";
                case NodeType.Forge: return "forge";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "No icon for this node type.");
            }
        }

        /// <summary>The string-table key of a World's continent name (P9.4).</summary>
        public static string ContinentKey(int world)
        {
            return "world." + world + ".name";
        }

        public static MapScreen Build(Transform? parent, Run? run, RunContent? content = null)
        {
            var canvas = ScreenFactory.Canvas("MapScreen", parent, 0);
            var screen = canvas.gameObject.AddComponent<MapScreen>();
            screen._run = run;
            screen._content = content ?? run?.Content;
            var root = canvas.transform;
            screen.BuildBackdrop(root);

            screen._continent = ScreenFactory.Label("Continent", root, "", 46, new Vector2(-520f, 470f), new Vector2(820f, 80f), TextAnchor.MiddleLeft);
            screen._continent.fontStyle = FontStyle.Bold;
            screen._binder = ScreenFactory.Button("Binder", root, Strings.Get("map.binder"), new Vector2(520f, 470f), new Vector2(260f, 76f), () => screen.BinderChosen?.Invoke());
            screen._settings = ScreenFactory.Button("Settings", root, Strings.Get("menu.settings"), new Vector2(810f, 470f), new Vector2(260f, 76f), () => screen.SettingsChosen?.Invoke());

            const float statsY = 400f;
            screen._ard = Stat("Ard", root, new Vector2(-780f, statsY), 320f);
            screen._baseDmg = Stat("BaseDmg", root, new Vector2(-470f, statsY), 260f);
            screen._essence = Stat("Essence", root, new Vector2(-220f, statsY), 240f);
            var crpPiece = new LinePiece(CrpView.UiKind, CrpView.CrpIconId, ScreenFactory.Accent);
            const float crpIconX = 30f;
            screen._crpIcon = HudFactory.Image("CrpIcon", root, crpPiece.Tint, new Vector2(crpIconX, statsY), new Vector2(CrpIconSize, CrpIconSize), crpPiece.Sprite);
            screen._crp = ScreenFactory.Label("Crp", root, "", 34, new Vector2(crpIconX + CrpIconSize / 2f + CrpIconGap + 110f, statsY), new Vector2(220f, 56f), TextAnchor.MiddleLeft, ScreenFactory.Accent);
            screen._seed = Stat("Seed", root, new Vector2(620f, statsY), 560f);

            screen._held = HudFactory.Rect("Held", root, new Vector2(0f, 330f), new Vector2(1840f, HeldIconSize));
            screen._crpChange = ScreenFactory.Label("CrpChange", root, "", 32, new Vector2(0f, 270f), new Vector2(600f, 50f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._crpChange.gameObject.SetActive(false);
            screen._board = HudFactory.Rect("Board", root, new Vector2(0f, -70f), new Vector2(BoardWidth, BoardHeight));
            screen._hint = ScreenFactory.Label("Hint", root, Strings.Get("map.hint"), 28, new Vector2(0f, -470f), new Vector2(1400f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            screen._keys = new ScreenKeys(new[] { Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.Enter, Key.NumpadEnter }, screen.KeyDown);
            screen.Refresh();
            return screen;
        }

        /// <summary>Redraws the board, the header and the Charm and Imprint row from the run's current state.</summary>
        public void Refresh()
        {
            foreach (Transform child in _board)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in _held)
            {
                Destroy(child.gameObject);
            }

            _nodeImages.Clear();
            _connections.Clear();
            _heldIcons.Clear();
            _neighbours.Clear();
            _marker = null;
            _selected = 0;
            bool on = _run != null && !_run.IsOver;
            _binder.gameObject.SetActive(on);
            if (!on)
            {
                _continent.text = "";
                _ard.text = "";
                _baseDmg.text = "";
                _essence.text = "";
                _seed.text = "";
                _crp.text = "";
                _crpIcon.gameObject.SetActive(false);
                _hint.text = "";
                return;
            }

            var run = _run!;
            var stats = run.Stats;
            _continent.text = Strings.Get(ContinentKey(run.World));
            _ard.text = Strings.Format("map.ard", stats.Ard, stats.MaxArd);
            _baseDmg.text = Strings.Format("map.base_dmg", stats.BaseDmg);
            _essence.text = Strings.Format("map.essence", stats.Essence);
            _seed.text = Strings.Format("map.seed", run.Seed);
            _crp.text = Strings.Format("crp.label", stats.Crp);
            _crpIcon.gameObject.SetActive(true);
            _hint.text = Strings.Get("map.hint");
            _neighbours.AddRange(run.ForwardNodes);
            DrawHeld(run);

            var map = run.CurrentMap;
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
                    bool walked = run.Visited.Contains(node.Id) && run.Visited.Contains(nextId);
                    var image = DrawConnection(positions[node.Id], positions[nextId], walked);
                    _connections.Add(new MapConnectionView(node.Id, nextId, walked, image));
                }
            }

            foreach (var node in map.Nodes)
            {
                var position = positions[node.Id];
                var art = new LinePiece(NodeKind, NodeIconId(node.Type), NodeColor(node.Type));
                var image = HudFactory.Image("Node " + node.Id, _board, art.Tint, position, new Vector2(NodeSize, NodeSize), art.Sprite);
                _nodeImages[node.Id] = image;
                if (run.Visited.Contains(node.Id) && node.Id != run.CurrentNodeId)
                {
                    // A walked node dims, so the road ahead reads brighter than the road behind.
                    image.color = new Color(image.color.r * 0.6f, image.color.g * 0.6f, image.color.b * 0.6f, image.color.a);
                }

                var label = HudFactory.Text("Label", image.transform, 16, ScreenFactory.MutedText, TextAnchor.MiddleCenter);
                label.rectTransform.anchoredPosition = new Vector2(0f, -NodeSize / 2f - 14f);
                label.rectTransform.sizeDelta = new Vector2(140f, 24f);
                label.text = Labels.NodeLabel(node.Type);
            }

            // The pin stands over the current node with its point on the icon's top edge.
            var markerArt = new LinePiece(UiKind, MarkerId, Marker);
            var current = positions[run.CurrentNodeId];
            _marker = HudFactory.Image("PlayerMarker", _board, markerArt.Tint, current + new Vector2(0f, NodeSize / 2f + 22f), new Vector2(48f, 48f), markerArt.Sprite);
            _marker.transform.SetAsLastSibling();
            HighlightSelection();
        }

        /// <summary>The image of a node on the board; null when the current map has no such node.</summary>
        public Image? NodeImage(string nodeId)
        {
            return _nodeImages.TryGetValue(nodeId, out var image) ? image : null;
        }

        /// <summary>Whether the backdrop covers every pixel of the screen the map is drawn on (P9.3).</summary>
        public bool BackdropCoversScreen()
        {
            var screen = (RectTransform)transform;
            var backdrop = new Vector3[4];
            var frame = new Vector3[4];
            _backdrop.rectTransform.GetWorldCorners(backdrop);
            screen.GetWorldCorners(frame);
            const float tolerance = 0.5f;
            return backdrop[0].x <= frame[0].x + tolerance
                && backdrop[0].y <= frame[0].y + tolerance
                && backdrop[2].x >= frame[2].x - tolerance
                && backdrop[2].y >= frame[2].y - tolerance;
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

        /// <summary>Presses the Binder button the way the player would (PRD 3.5.11); false while no run is on.</summary>
        public bool ChooseBinder()
        {
            return ScreenFactory.Submit(_binder);
        }

        /// <summary>Shows a CRP change as a floating label: the signed amount and its source (PRD 3.8.6).</summary>
        public void ShowCrpChange(int amount, string source)
        {
            _crpChange.text = Strings.Format("crp.change", (amount >= 0 ? "+" : "") + amount, Labels.CrpSource(source));
            _crpChange.rectTransform.anchoredPosition = new Vector2(0f, 270f);
            _crpChange.gameObject.SetActive(true);
            _crpChangeUntil = Time.unscaledTime + CrpChangeSeconds;
            if (_run != null)
            {
                _crp.text = Strings.Format("crp.label", _run.Stats.Crp);
            }
        }

        /// <summary>A key went down: the arrows cycle the selection, Enter commits; nothing while a screen covers the map.</summary>
        public void KeyDown(Key key)
        {
            if (Covered)
            {
                return;
            }

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

        /// <summary>
        /// The backdrop fills the canvas and keeps its 2560 by 1080 proportions, growing until it
        /// covers the screen at any aspect: the 16:9 frame shows its centre, an ultrawide more of
        /// its sides, never the clear colour (P5.3, P9.3).
        /// </summary>
        private void BuildBackdrop(Transform root)
        {
            var art = new LinePiece(BackdropKind, BackdropId, ScreenFactory.Backdrop);
            _backdrop = ScreenFactory.Fill("Backdrop", root, art.Tint);
            HudFactory.SetSprite(_backdrop, art.Sprite);
            var fitter = _backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = BackdropSize.x / BackdropSize.y;
        }

        /// <summary>
        /// The equipped Charms, then the held Imprints, left to right under the header (P9.5). A
        /// stackable Imprint held more than once shows once with its copy count.
        /// </summary>
        private void DrawHeld(Run run)
        {
            var entries = new List<(string Kind, string Id, string Name, int Copies)>();
            foreach (var charmId in run.Charms)
            {
                var charm = _content?.FindCharm(charmId);
                entries.Add((HeldIcon.CharmKind, charmId, charm?.Name ?? charmId, 1));
            }

            foreach (var group in run.Imprints.GroupBy(id => id))
            {
                var imprint = _content?.FindImprint(group.Key);
                entries.Add((HeldIcon.ImprintKind, group.Key, imprint?.Name ?? group.Key, group.Count()));
            }

            const float gap = 14f;
            float x = -_held.rect.width / 2f + HeldIconSize / 2f;
            foreach (var entry in entries)
            {
                string effect = Strings.Get(entry.Kind + ".effect." + entry.Id);
                string tooltip = Strings.Format("map.held_tooltip", entry.Name, effect);
                var placeholder = entry.Kind == HeldIcon.CharmKind ? CharmPlaceholder : ImprintPlaceholder;
                _heldIcons.Add(HeldIcon.Create(_held, entry.Kind, entry.Id, tooltip, entry.Copies, new Vector2(x, 0f), HeldIconSize, placeholder));
                x += HeldIconSize + gap;
            }
        }

        private static UnityEngine.UI.Text Stat(string name, Transform root, Vector2 centre, float width)
        {
            return ScreenFactory.Label(name, root, "", 30, centre, new Vector2(width, 56f), TextAnchor.MiddleLeft, ScreenFactory.TextColor);
        }

        private void HighlightSelection()
        {
            for (int i = 0; i < _neighbours.Count; i++)
            {
                if (_nodeImages.TryGetValue(_neighbours[i].Id, out var image))
                {
                    var rect = image.rectTransform;
                    float size = i == _selected ? NodeSize * 1.35f : NodeSize * 1.1f;
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

        /// <summary>A connection as the path sprite tiled along it, the walked variant once both its ends are visited (P9.3).</summary>
        private Image DrawConnection(Vector2 from, Vector2 to, bool walked)
        {
            var delta = to - from;
            var art = new LinePiece(UiKind, walked ? WalkedPathId : PathId, walked ? Visited : Line);
            var line = HudFactory.Image("Connection", _board, art.Tint, (from + to) / 2f, new Vector2(delta.magnitude, art.Shipped ? PathHeight : 3f), art.Sprite);
            if (art.Shipped)
            {
                line.type = Image.Type.Tiled;
            }

            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            return line;
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
