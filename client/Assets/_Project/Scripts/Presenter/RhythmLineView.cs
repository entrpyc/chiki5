#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One drawn piece of the Rhythm Line: the catalogue's art shown in its own colours, or the
    /// flat placeholder colour and the built-in geometry while the art is owed (P1.3, P2.1).
    /// </summary>
    internal readonly struct LinePiece
    {
        public LinePiece(string kind, string id, Color placeholder)
        {
            var catalogue = VisualCatalogue.Active;
            Shipped = catalogue.Has(kind, id);
            Sprite = catalogue.Sprite(kind, id);
            Tint = Shipped ? Color.white : placeholder;
        }

        public Sprite Sprite { get; }

        /// <summary>White where the art ships and carries its own colours, the placeholder colour otherwise.</summary>
        public Color Tint { get; }

        public bool Shipped { get; }

        /// <summary>The art's own pixel size where it ships, the size built in code otherwise.</summary>
        public Vector2 Size(Vector2 builtIn)
        {
            return Shipped ? Sprite.rect.size : builtIn;
        }
    }

    /// <summary>
    /// The Rhythm Line (PRD 3.3.1.1, 3.6.3): a horizontal timeline that scrolls with the beat
    /// clock for the whole battle. It marks every beat and the quarter-beat grid, draws every
    /// upcoming enemy action at its beat with its kind's icon and the beats remaining, shows a
    /// Charge's wind-up as a bar leading into its landing (PRD 3.6.16), and bands the Judgment
    /// Window of the next enemy action, glowing that action while its window is open
    /// (PRD 3.3.8.1). Iron Veil and every later ability that veils the enemy darken the whole
    /// strip (PRD 3.6.9). Every piece is drawn from the visual catalogue (P2.1); every position
    /// is derived from audio time through the beat map, and nothing here reads <c>Time.time</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RhythmLineView : MonoBehaviour, IBattlePresenter
    {
        public const float DefaultBeatWidth = 120f;
        public const int DefaultHorizonBeats = 8;
        public const int DefaultBehindBeats = 3;

        /// <summary>Where "now" sits along the line, as a fraction of its width from the left.</summary>
        public const float NowFraction = 0.3f;

        /// <summary>The kind every piece of the line and every telegraph decoration is catalogued under.</summary>
        public const string UiKind = "ui";

        public const string BackgroundId = "rhythmline-bg";
        public const string DarkBackgroundId = "rhythmline-bg-dark";
        public const string BeatTickId = "rhythmline-beat";
        public const string QuarterTickId = "rhythmline-quarter";
        public const string PlayheadId = "rhythmline-playhead";
        public const string WindowId = "rhythmline-window";
        public const string WindowOpenId = "rhythmline-window-open";

        private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        private static readonly Color DarkBackgroundColor = new Color(0.04f, 0.03f, 0.06f, 0.95f);
        private static readonly Color BeatColor = new Color(0.8f, 0.8f, 0.85f, 0.9f);
        private static readonly Color QuarterColor = new Color(0.5f, 0.5f, 0.55f, 0.5f);
        private static readonly Color NowColor = new Color(1f, 0.95f, 0.6f, 1f);
        private static readonly Color WindowColor = new Color(1f, 0.95f, 0.6f, 0.18f);
        private static readonly Color WindowOpenColor = new Color(1f, 0.95f, 0.6f, 0.4f);

        private readonly List<BeatMarker> _beatMarkers = new List<BeatMarker>();
        private readonly List<ActionMarker> _actionMarkers = new List<ActionMarker>();
        private readonly List<ActionMarker> _shown = new List<ActionMarker>();

        private BeatClock? _clock;
        private Sim.Battle? _battle;
        private RectTransform _viewport = null!;
        private RectTransform _content = null!;
        private RectTransform _nowLine = null!;
        private Image _background = null!;
        private Image _playhead = null!;
        private Image _window = null!;
        private LinePiece _lightBackground;
        private LinePiece _darkBackground;
        private LinePiece _windowPiece;
        private LinePiece _windowOpenPiece;
        private int? _veilModifierId;
        private float _height;
        private int _qb;

        public float BeatWidth { get; private set; } = DefaultBeatWidth;

        public int HorizonBeats { get; private set; } = DefaultHorizonBeats;

        public int BehindBeats { get; private set; } = DefaultBehindBeats;

        /// <summary>The x of "now" in the line's own coordinates (centre-anchored).</summary>
        public float NowX { get; private set; }

        /// <summary>The scrolled container every marker sits in; its x moves with the beat clock.</summary>
        public RectTransform Content => _content;

        public RectTransform NowLine => _nowLine;

        /// <summary>The strip behind everything, light or dark by whether the enemy is veiled (PRD 3.6.9).</summary>
        public Image Background => _background;

        /// <summary>The playhead standing at "now".</summary>
        public Image Playhead => _playhead;

        /// <summary>The band covering the Judgment Window of the next enemy action.</summary>
        public Image Window => _window;

        public RectTransform WindowBand => _window.rectTransform;

        /// <summary>True while the next enemy action's Judgment Window contains the rendered audio time.</summary>
        public bool WindowOpen { get; private set; }

        /// <summary>True while a modifier that veils the enemy stands, which darkens the whole line (PRD 3.6.9).</summary>
        public bool Veiled => _veilModifierId.HasValue;

        public bool HasRendered { get; private set; }

        /// <summary>The audio time in milliseconds of the last render.</summary>
        public int RenderedAtMs { get; private set; }

        /// <summary>The continuous beat position the line is scrolled to.</summary>
        public float ScrollBeats { get; private set; }

        /// <summary>The pooled beat markers; each holds the beat it currently shows.</summary>
        public IReadOnlyList<BeatMarker> BeatMarkers => _beatMarkers;

        /// <summary>The action markers on screen, in order of landing.</summary>
        public IReadOnlyList<ActionMarker> ActionMarkers => _shown;

        public static RhythmLineView Build(BattleDriver driver, RectTransform parent, Vector2 anchoredPosition, Vector2 size, float beatWidth = DefaultBeatWidth)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var viewport = HudFactory.Rect("RhythmLine", parent, anchoredPosition, size);
            var view = viewport.gameObject.AddComponent<RhythmLineView>();
            view.Init(driver, viewport, size, beatWidth);
            driver.AttachPresenter(view);
            return view;
        }

        /// <summary>The marker currently showing a beat, or null when that beat is off the line.</summary>
        public BeatMarker? BeatMarkerFor(int beat)
        {
            var marker = _beatMarkers[Ring(beat)];
            return marker.Beat == beat && marker.Visible ? marker : null;
        }

        /// <summary>The x of a marker in the line's own coordinates: its place on the content plus the scroll.</summary>
        public float ViewportX(RectTransform marker)
        {
            return _content.anchoredPosition.x + marker.anchoredPosition.x;
        }

        /// <summary>The x at which a beat position sits right now, in the line's own coordinates.</summary>
        public float ViewportXOfBeat(float beat)
        {
            return NowX + (beat - ScrollBeats) * BeatWidth;
        }

        /// <summary>
        /// Follows the stream for the modifiers that veil the enemy (PRD 3.6.9): the line goes
        /// dark while one stands and light again when the same modifier expires.
        /// </summary>
        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            _battle = battle;
            if (battleEvent is ModifierActivated activated && EnemyVeil.Veils(activated))
            {
                _veilModifierId = activated.ModifierId;
                SetBackground(_darkBackground);
            }
            else if (battleEvent is ModifierExpired expired && _veilModifierId == expired.ModifierId)
            {
                _veilModifierId = null;
                SetBackground(_lightBackground);
            }
        }

        /// <summary>Scrolls the line to an audio time and redraws every marker from the battle's state.</summary>
        public void Render(int audioTimeMs)
        {
            if (_battle == null)
            {
                return;
            }

            ScrollBeats = BeatAt(audioTimeMs);
            _content.anchoredPosition = new Vector2(NowX - ScrollBeats * BeatWidth, 0f);

            int centre = Mathf.FloorToInt(ScrollBeats);
            int lengthBeats = _battle.Track.LengthBeats;
            for (int beat = centre - BehindBeats; beat <= centre + HorizonBeats; beat++)
            {
                var marker = _beatMarkers[Ring(beat)];
                if (marker.Beat != beat)
                {
                    marker.Assign(beat, beat * BeatWidth, (Mod(beat, lengthBeats) + 1).ToString());
                }

                marker.SetVisible(beat >= 0);
            }

            RenderActions(audioTimeMs);
            RenderedAtMs = audioTimeMs;
            HasRendered = true;
        }

        private void Init(BattleDriver driver, RectTransform viewport, Vector2 size, float beatWidth)
        {
            _clock = driver.Clock;
            _battle = driver.Battle;
            _viewport = viewport;
            _height = size.y;
            BeatWidth = beatWidth;
            NowX = -size.x / 2f + size.x * NowFraction;

            viewport.gameObject.AddComponent<RectMask2D>();
            _lightBackground = new LinePiece(UiKind, BackgroundId, BackgroundColor);
            _darkBackground = new LinePiece(UiKind, DarkBackgroundId, DarkBackgroundColor);
            _background = HudFactory.StretchedImage("Background", viewport, _lightBackground.Tint, _lightBackground.Sprite);

            _content = HudFactory.Rect("Content", viewport);
            _content.anchoredPosition = new Vector2(NowX, 0f);

            _windowPiece = new LinePiece(UiKind, WindowId, WindowColor);
            _windowOpenPiece = new LinePiece(UiKind, WindowOpenId, WindowOpenColor);
            _window = HudFactory.Image("JudgmentWindow", _content, _windowPiece.Tint, Vector2.zero, new Vector2(0f, size.y * 0.8f), _windowPiece.Sprite);
            _window.gameObject.SetActive(false);

            var beatPiece = new LinePiece(UiKind, BeatTickId, BeatColor);
            var quarterPiece = new LinePiece(UiKind, QuarterTickId, QuarterColor);
            int count = HorizonBeats + BehindBeats + 2;
            for (int i = 0; i < count; i++)
            {
                _beatMarkers.Add(new BeatMarker(_content, size.y, BeatWidth, beatPiece, quarterPiece));
            }

            var playheadPiece = new LinePiece(UiKind, PlayheadId, NowColor);
            _playhead = HudFactory.Image("Now", viewport, playheadPiece.Tint, new Vector2(NowX, 0f), playheadPiece.Size(new Vector2(3f, size.y)), playheadPiece.Sprite);
            _nowLine = _playhead.rectTransform;

            if (_battle != null)
            {
                Render(Math.Max(0, _battle.CurrentTimeMs));
            }
        }

        private void LateUpdate()
        {
            if (_clock == null || !_clock.IsScheduled || _battle == null)
            {
                return;
            }

            Render(Math.Max(0, _clock.NowMs));
        }

        private void SetBackground(LinePiece piece)
        {
            HudFactory.SetSprite(_background, piece.Sprite);
            _background.color = piece.Tint;
        }

        private void RenderActions(int audioTimeMs)
        {
            var battle = _battle!;
            var upcoming = battle.UpcomingActions(HorizonBeats);
            var pending = battle.Outcome == null ? battle.PendingAction : null;
            WindowOpen = pending != null && pending.Contains(audioTimeMs);

            _shown.Clear();
            for (int i = 0; i < upcoming.Count; i++)
            {
                if (i >= _actionMarkers.Count)
                {
                    _actionMarkers.Add(new ActionMarker(_content, _height));
                }

                var marker = _actionMarkers[i];
                var action = upcoming[i];
                bool highlighted = WindowOpen && pending != null && action.Index == pending.Index;
                marker.Assign(action, BeatWidth, highlighted);
                _shown.Add(marker);
            }

            for (int i = upcoming.Count; i < _actionMarkers.Count; i++)
            {
                _actionMarkers[i].Hide();
            }

            if (pending == null)
            {
                _window.gameObject.SetActive(false);
                return;
            }

            float beatMs = 60_000f / pending.Bpm;
            float centreX = pending.PositionQb / (float)Beats.QuarterBeatsPerBeat * BeatWidth;
            float width = (pending.CloseMs - pending.OpenMs) / beatMs * BeatWidth;
            float offset = ((pending.OpenMs + pending.CloseMs) / 2f - pending.CentreMs) / beatMs * BeatWidth;
            var piece = WindowOpen ? _windowOpenPiece : _windowPiece;
            _window.rectTransform.anchoredPosition = new Vector2(centreX + offset, 0f);
            _window.rectTransform.sizeDelta = new Vector2(width, _height * 0.8f);
            HudFactory.SetSprite(_window, piece.Sprite);
            _window.color = piece.Tint;
            _window.gameObject.SetActive(true);
        }

        /// <summary>The continuous beat an audio time falls on, from the beat map: whole quarter beats plus the fraction of the current one.</summary>
        private float BeatAt(int audioTimeMs)
        {
            var map = _battle!.BeatMap;
            if (audioTimeMs < map.TimeAtQb(_qb))
            {
                _qb = 0;
            }

            while (map.TimeAtQb(_qb + 1) <= audioTimeMs)
            {
                _qb++;
            }

            int start = map.TimeAtQb(_qb);
            int end = map.TimeAtQb(_qb + 1);
            float fraction = end > start ? (audioTimeMs - start) / (float)(end - start) : 0f;
            return (_qb + fraction) / Beats.QuarterBeatsPerBeat;
        }

        private int Ring(int beat)
        {
            return Mod(beat, _beatMarkers.Count);
        }

        private static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    /// <summary>One beat's mark on the Rhythm Line with its three quarter-beat ticks; pooled and reassigned as the line scrolls.</summary>
    public sealed class BeatMarker
    {
        private readonly UnityEngine.UI.Text _label;
        private readonly Image _line;
        private readonly List<Image> _quarters = new List<Image>();

        public RectTransform Rect { get; }

        /// <summary>The beat's own rule.</summary>
        public Image Line => _line;

        /// <summary>The three quarter-beat rules between this beat and the next (PRD 3.3.1.4).</summary>
        public IReadOnlyList<Image> Quarters => _quarters;

        /// <summary>The beat this marker shows; <see cref="int.MinValue"/> before its first assignment.</summary>
        public int Beat { get; private set; } = int.MinValue;

        public bool Visible => Rect.gameObject.activeSelf;

        public string LabelText => _label.text;

        internal BeatMarker(RectTransform parent, float height, float beatWidth, LinePiece beat, LinePiece quarter)
        {
            _line = HudFactory.Image("Beat", parent, beat.Tint, Vector2.zero, beat.Size(new Vector2(2f, height * 0.7f)), beat.Sprite);
            Rect = _line.rectTransform;
            var quarterSize = quarter.Size(new Vector2(1f, height * 0.3f));
            for (int index = 1; index < Beats.QuarterBeatsPerBeat; index++)
            {
                _quarters.Add(HudFactory.Image("Quarter" + index, Rect, quarter.Tint, new Vector2(beatWidth * index / Beats.QuarterBeatsPerBeat, 0f), quarterSize, quarter.Sprite));
            }

            _label = HudFactory.Text("Label", Rect, 14, beat.Tint, TextAnchor.LowerCenter);
            _label.rectTransform.anchoredPosition = new Vector2(0f, -height * 0.45f);
            _label.rectTransform.sizeDelta = new Vector2(beatWidth, 18f);
            Rect.gameObject.SetActive(false);
        }

        internal void Assign(int beat, float x, string label)
        {
            Beat = beat;
            Rect.anchoredPosition = new Vector2(x, 0f);
            _label.text = label;
        }

        internal void SetVisible(bool visible)
        {
            if (Rect.gameObject.activeSelf != visible)
            {
                Rect.gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// One upcoming enemy action on the Rhythm Line (PRD 3.6.3): its kind's icon, the beat it
    /// lands on and the whole beats remaining as text; a Charge also shows its wind-up as a bar
    /// (PRD 3.6.16). While the action's Judgment Window is open a glow sits behind the icon,
    /// tinted by kind (PRD 3.3.8.1).
    /// </summary>
    public sealed class ActionMarker
    {
        /// <summary>The drawn size of every telegraph icon (P2.2).</summary>
        public const float IconSize = 80f;

        public const string GlowId = "telegraph-glow";
        public const string WindUpBarId = "windup-bar";

        /// <summary>The kind every telegraph icon is catalogued under; the id is the kind's chart id.</summary>
        public const string ActionKindName = "action";

        // Telegraph colours stand clear of the card categories of PRD 3.4.2 — Attack red,
        // Defense blue, Ability green — so a marker is never read as a slot's colour. Both
        // attacks share one colour and are told apart by the direction their icon points.
        private static readonly Color AttackColor = new Color(0.949f, 0.639f, 0.235f, 1f);
        private static readonly Color DefendColor = new Color(0.659f, 0.690f, 0.769f, 1f);
        private static readonly Color BuffColor = new Color(0.773f, 0.514f, 0.910f, 1f);
        private static readonly Color ChargeColor = new Color(1f, 0.847f, 0.420f, 1f);

        private readonly Image _glow;
        private readonly Image _icon;
        private readonly Image _windUp;
        private readonly UnityEngine.UI.Text _kindLabel;
        private readonly UnityEngine.UI.Text _countdown;
        private readonly bool _iconsShipped;
        private string _iconId = "";

        public RectTransform Rect { get; }

        public EnemyActionKind Kind { get; private set; }

        /// <summary>The action's index from the start of the battle across laps.</summary>
        public int Index { get; private set; }

        /// <summary>The absolute quarter-beat position the action lands on.</summary>
        public int PositionQb { get; private set; }

        /// <summary>The beat the action lands on, in whole beats plus quarters.</summary>
        public float Beat => PositionQb / (float)Beats.QuarterBeatsPerBeat;

        /// <summary>Whole beats until the action lands, as shown.</summary>
        public int RemainingBeats { get; private set; }

        public bool Highlighted { get; private set; }

        public bool Visible => Rect.gameObject.activeSelf;

        /// <summary>The kind's icon (PRD 3.6.3).</summary>
        public Image Icon => _icon;

        /// <summary>The glow behind the icon while the window is open (PRD 3.3.8.1).</summary>
        public Image Glow => _glow;

        public bool GlowShown => _glow.gameObject.activeSelf;

        /// <summary>A Charge's wind-up bar, running back from the icon to where the wind-up starts (PRD 3.6.16).</summary>
        public Image WindUp => _windUp;

        public bool WindUpShown => _windUp.gameObject.activeSelf;

        public string KindText => _kindLabel.text;

        public string CountdownText => _countdown.text;

        internal ActionMarker(RectTransform parent, float height)
        {
            Rect = HudFactory.Rect("Action", parent, Vector2.zero, new Vector2(IconSize, IconSize));

            var glow = new LinePiece(RhythmLineView.UiKind, GlowId, Color.white);
            _glow = HudFactory.Image("Glow", Rect, Color.white, Vector2.zero, glow.Size(new Vector2(IconSize * 1.5f, IconSize * 1.5f)), glow.Sprite);
            _glow.gameObject.SetActive(false);

            _iconsShipped = VisualCatalogue.Active.Has(ActionKindName, ChartLoader.KindToId(EnemyActionKind.AttackLeft));
            _icon = HudFactory.Image("Icon", Rect, AttackColor, Vector2.zero, new Vector2(IconSize, IconSize));

            var windUp = new LinePiece(RhythmLineView.UiKind, WindUpBarId, ChargeColor);
            _windUp = HudFactory.Image("WindUp", Rect, windUp.Tint, Vector2.zero, new Vector2(0f, windUp.Size(new Vector2(0f, 6f)).y), windUp.Sprite);
            _windUp.rectTransform.pivot = new Vector2(1f, 0.5f);
            _windUp.rectTransform.anchoredPosition = new Vector2(-IconSize / 2f, 0f);
            _windUp.gameObject.SetActive(false);

            _kindLabel = HudFactory.Text("Kind", Rect, 14, Color.white, TextAnchor.UpperCenter);
            _kindLabel.rectTransform.anchoredPosition = new Vector2(0f, IconSize / 2f + 12f);
            _kindLabel.rectTransform.sizeDelta = new Vector2(IconSize * 2f, 18f);

            _countdown = HudFactory.Text("Countdown", Rect, 22, Color.white, TextAnchor.LowerCenter);
            _countdown.rectTransform.anchoredPosition = new Vector2(0f, -IconSize / 2f - 22f);
            _countdown.rectTransform.sizeDelta = new Vector2(IconSize * 2f, 24f);
            Rect.gameObject.SetActive(false);
        }

        internal void Assign(UpcomingAction action, float beatWidth, bool highlighted)
        {
            Kind = action.Action.Kind;
            Index = action.Index;
            PositionQb = action.PositionQb;
            RemainingBeats = action.RemainingBeats;
            Highlighted = highlighted;

            Rect.anchoredPosition = new Vector2(Beat * beatWidth, 0f);
            Rect.localScale = highlighted ? new Vector3(1.25f, 1.25f, 1f) : Vector3.one;
            SetIcon(Kind, action.RemainingQb <= Beats.QuarterBeatsPerBeat, highlighted);
            _kindLabel.text = Labels.ActionKind(Kind);
            _countdown.text = RemainingBeats.ToString();
            SetGlow(highlighted);

            if (action.WindUpBeats > 0)
            {
                _windUp.rectTransform.sizeDelta = new Vector2(action.WindUpBeats * beatWidth - IconSize / 2f, _windUp.rectTransform.sizeDelta.y);
                _windUp.gameObject.SetActive(true);
            }
            else if (_windUp.gameObject.activeSelf)
            {
                _windUp.gameObject.SetActive(false);
            }

            if (!Rect.gameObject.activeSelf)
            {
                Rect.gameObject.SetActive(true);
            }
        }

        internal void Hide()
        {
            SetGlow(false);
            if (Rect.gameObject.activeSelf)
            {
                Rect.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Puts the kind's icon on the marker. Where the icons ship they carry their own colours
        /// and are drawn untinted; where they are owed the flat placeholder colour stands in and
        /// still brightens as the action nears.
        /// </summary>
        private void SetIcon(EnemyActionKind kind, bool imminent, bool highlighted)
        {
            string id = ChartLoader.KindToId(kind);
            if (_iconId != id)
            {
                _iconId = id;
                HudFactory.SetSprite(_icon, VisualCatalogue.Active.Sprite(ActionKindName, id));
            }

            if (_iconsShipped)
            {
                _icon.color = Color.white;
                return;
            }

            var color = ColorFor(kind);
            _icon.color = highlighted ? Color.Lerp(color, Color.white, 0.4f) : imminent ? Color.Lerp(color, Color.white, 0.2f) : color;
        }

        /// <summary>Shows or hides the glow behind the icon, tinted by kind (PRD 3.3.8.1).</summary>
        private void SetGlow(bool open)
        {
            if (open)
            {
                var color = ColorFor(Kind);
                _glow.color = new Color(color.r, color.g, color.b, 0.85f);
            }

            if (_glow.gameObject.activeSelf != open)
            {
                _glow.gameObject.SetActive(open);
            }
        }

        private static Color ColorFor(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.AttackLeft: return AttackColor;
                case EnemyActionKind.AttackRight: return AttackColor;
                case EnemyActionKind.Defend: return DefendColor;
                case EnemyActionKind.Buff: return BuffColor;
                case EnemyActionKind.Charge: return ChargeColor;
                default: return Color.white;
            }
        }
    }
}
