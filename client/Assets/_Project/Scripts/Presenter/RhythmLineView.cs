#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The Rhythm Line (PRD 3.3.1.1, 3.6.3): a horizontal timeline that scrolls with the beat
    /// clock for the whole battle. It marks every beat and the quarter-beat grid, draws every
    /// upcoming enemy action at its beat with its kind and the beats remaining, shows a Charge's
    /// wind-up as a bar leading into its landing (PRD 3.6.16), and bands the Judgment Window of
    /// the next enemy action, highlighting that action while its window is open (PRD 3.3.8.1).
    /// Every position is derived from audio time through the beat map; nothing here reads
    /// <c>Time.time</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RhythmLineView : MonoBehaviour, IBattlePresenter
    {
        public const float DefaultBeatWidth = 120f;
        public const int DefaultHorizonBeats = 8;
        public const int DefaultBehindBeats = 3;

        /// <summary>Where "now" sits along the line, as a fraction of its width from the left.</summary>
        public const float NowFraction = 0.3f;

        private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.9f);
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
        private Image _window = null!;
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

        /// <summary>The band covering the Judgment Window of the next enemy action.</summary>
        public RectTransform WindowBand => _window.rectTransform;

        /// <summary>True while the next enemy action's Judgment Window contains the rendered audio time.</summary>
        public bool WindowOpen { get; private set; }

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

        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            _battle = battle;
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
            HudFactory.StretchedImage("Background", viewport, BackgroundColor);

            _content = HudFactory.Rect("Content", viewport);
            _content.anchoredPosition = new Vector2(NowX, 0f);

            _window = HudFactory.Image("JudgmentWindow", _content, WindowColor, Vector2.zero, new Vector2(0f, size.y * 0.8f));
            _window.gameObject.SetActive(false);

            int count = HorizonBeats + BehindBeats + 2;
            for (int i = 0; i < count; i++)
            {
                _beatMarkers.Add(new BeatMarker(_content, size.y, BeatWidth, BeatColor, QuarterColor));
            }

            var now = HudFactory.Image("Now", viewport, NowColor, new Vector2(NowX, 0f), new Vector2(3f, size.y));
            _nowLine = now.rectTransform;

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
            _window.rectTransform.anchoredPosition = new Vector2(centreX + offset, 0f);
            _window.rectTransform.sizeDelta = new Vector2(width, _height * 0.8f);
            _window.color = WindowOpen ? WindowOpenColor : WindowColor;
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

        public RectTransform Rect { get; }

        /// <summary>The beat this marker shows; <see cref="int.MinValue"/> before its first assignment.</summary>
        public int Beat { get; private set; } = int.MinValue;

        public bool Visible => Rect.gameObject.activeSelf;

        public string LabelText => _label.text;

        internal BeatMarker(RectTransform parent, float height, float beatWidth, Color beatColor, Color quarterColor)
        {
            var line = HudFactory.Image("Beat", parent, beatColor, Vector2.zero, new Vector2(2f, height * 0.7f));
            Rect = line.rectTransform;
            for (int quarter = 1; quarter < Beats.QuarterBeatsPerBeat; quarter++)
            {
                HudFactory.Image("Quarter" + quarter, Rect, quarterColor, new Vector2(beatWidth * quarter / Beats.QuarterBeatsPerBeat, 0f), new Vector2(1f, height * 0.3f));
            }

            _label = HudFactory.Text("Label", Rect, 14, beatColor, TextAnchor.LowerCenter);
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
    /// One upcoming enemy action on the Rhythm Line (PRD 3.6.3): its kind, the beat it lands on
    /// and the whole beats remaining; a Charge also shows its wind-up as a bar (PRD 3.6.16).
    /// </summary>
    public sealed class ActionMarker
    {
        private static readonly Color LeftColor = new Color(0.9f, 0.35f, 0.3f, 1f);
        private static readonly Color RightColor = new Color(0.3f, 0.55f, 0.95f, 1f);
        private static readonly Color DefendColor = new Color(0.4f, 0.8f, 0.5f, 1f);
        private static readonly Color BuffColor = new Color(0.8f, 0.5f, 0.9f, 1f);
        private static readonly Color ChargeColor = new Color(1f, 0.7f, 0.2f, 1f);

        private readonly Image _body;
        private readonly Image _windUp;
        private readonly UnityEngine.UI.Text _kindLabel;
        private readonly UnityEngine.UI.Text _countdown;

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

        public string KindText => _kindLabel.text;

        public string CountdownText => _countdown.text;

        internal ActionMarker(RectTransform parent, float height)
        {
            float size = height * 0.42f;
            _body = HudFactory.Image("Action", parent, LeftColor, Vector2.zero, new Vector2(size, size));
            Rect = _body.rectTransform;

            _windUp = HudFactory.Image("WindUp", Rect, ChargeColor, Vector2.zero, new Vector2(0f, 6f));
            _windUp.rectTransform.pivot = new Vector2(1f, 0.5f);
            _windUp.rectTransform.anchoredPosition = new Vector2(-size / 2f, 0f);
            _windUp.gameObject.SetActive(false);

            _kindLabel = HudFactory.Text("Kind", Rect, 14, Color.white, TextAnchor.UpperCenter);
            _kindLabel.rectTransform.anchoredPosition = new Vector2(0f, size / 2f + 12f);
            _kindLabel.rectTransform.sizeDelta = new Vector2(size * 2f, 18f);

            _countdown = HudFactory.StretchedText("Countdown", Rect, 22, Color.white, TextAnchor.MiddleCenter);
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
            var color = ColorFor(Kind);
            _body.color = highlighted ? Color.Lerp(color, Color.white, 0.4f) : action.RemainingQb <= Beats.QuarterBeatsPerBeat ? Color.Lerp(color, Color.white, 0.2f) : color;
            _kindLabel.text = Labels.ActionKind(Kind);
            _countdown.text = RemainingBeats.ToString();

            if (action.WindUpBeats > 0)
            {
                _windUp.rectTransform.sizeDelta = new Vector2(action.WindUpBeats * beatWidth - Rect.sizeDelta.x / 2f, 6f);
                _windUp.gameObject.SetActive(true);
            }
            else
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
            if (Rect.gameObject.activeSelf)
            {
                Rect.gameObject.SetActive(false);
            }
        }

        private static Color ColorFor(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.AttackLeft: return LeftColor;
                case EnemyActionKind.AttackRight: return RightColor;
                case EnemyActionKind.Defend: return DefendColor;
                case EnemyActionKind.Buff: return BuffColor;
                case EnemyActionKind.Charge: return ChargeColor;
                default: return Color.white;
            }
        }
    }
}
