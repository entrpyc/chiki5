#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Keys;
using Chiki.Client.Profiles;
using Chiki.Client.Scene;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The latency calibration screen (PRD 3.12.1): a metronome at BPM 120 plays for 16 beats
    /// on a <see cref="BeatClock"/> of its own while a marker pulses on the beat; the player
    /// taps Space on every click; each tap is stamped with audio time (P12.3) and its offset
    /// from the nearest beat is kept; the median of those offsets becomes the profile's
    /// calibration offset and the profile is saved. The screen states that Bluetooth audio adds
    /// 100 to 300 ms. The test ends after the sixteenth tap, or a beat after the last click with
    /// the taps it has; Done then closes the screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CalibrationScreen : MonoBehaviour
    {
        public const string TrackId = "track-calibration-120";
        public const int Bpm = 120;
        public const int BeatCount = 16;
        public const double LeadSeconds = 1.0;
        public const int GraceMs = 1000;

        private readonly List<int> _offsets = new List<int>();
        private InputAction? _tapAction;
        private Profile? _profile;
        private ProfileStore? _store;
        private BeatClock? _clock;
        private GameObject? _metronomeHost;
        private RectTransform? _marker;
        private UnityEngine.UI.Text? _taps;
        private UnityEngine.UI.Text? _result;
        private UnityEngine.UI.Text? _note;
        private Button? _done;
        private bool _finished;

        /// <summary>The metronome track: 16 beats at BPM 120 with beat 0 on the first sample.</summary>
        public Track Track { get; private set; } = null!;

        /// <summary>The clock the metronome runs on; null once the test has finished.</summary>
        public BeatClock? Clock => _clock;

        public Profile? Profile => _profile;

        /// <summary>Tap-minus-beat offsets in milliseconds, in tap order.</summary>
        public IReadOnlyList<int> Offsets => _offsets;

        /// <summary>Whether the test has ended and the result is stored.</summary>
        public bool Finished => _finished;

        /// <summary>The median offset stored on the profile, or null when the test has not finished or recorded no tap.</summary>
        public int? ResultMs { get; private set; }

        /// <summary>The Bluetooth note as shown on screen.</summary>
        public string BluetoothNote => _note != null ? _note.text : "";

        /// <summary>Whether the Bluetooth note is on screen with the string table's text.</summary>
        public bool BluetoothNoteShown => _note != null && _note.gameObject.activeInHierarchy && _note.text == Strings.Get("calibration.bluetooth_note");

        public bool DoneEnabled => _done != null && _done.interactable;

        /// <summary>The test ended and the profile was saved.</summary>
        public event Action<CalibrationScreen>? Completed;

        /// <summary>The screen is closing.</summary>
        public event Action<CalibrationScreen>? Closed;

        public static CalibrationScreen Open(Transform? parent, Profile profile, ProfileStore store)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var canvas = ScreenFactory.Canvas("CalibrationScreen", parent, 100);
            var screen = canvas.gameObject.AddComponent<CalibrationScreen>();
            screen._profile = profile;
            screen._store = store;
            screen.BuildUi(canvas.transform);
            screen.StartMetronome();
            return screen;
        }

        /// <summary>A Space press at <paramref name="inputTime"/> on the realtime-since-startup timeline (the Input System's event time).</summary>
        public void Tap(double inputTime)
        {
            if (_clock == null || !_clock.IsScheduled)
            {
                return;
            }

            TapAt(_clock.AudioTimeMsAt(inputTime));
        }

        /// <summary>A tap already stamped with its audio time: its offset from the nearest beat is recorded.</summary>
        public void TapAt(int audioTimeMs)
        {
            if (_finished)
            {
                return;
            }

            int beat = NearestBeat(audioTimeMs);
            _offsets.Add(audioTimeMs - Track.BeatMap.TimeAtBeat(beat));
            if (_taps != null)
            {
                _taps.text = Strings.Format("calibration.taps", _offsets.Count, BeatCount);
            }

            if (_offsets.Count >= BeatCount)
            {
                Finish();
            }
        }

        /// <summary>Ends the test with the taps recorded so far, stores the median and saves the profile.</summary>
        public void Finish()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            if (_offsets.Count > 0)
            {
                ResultMs = Median(_offsets);
                _profile!.CalibrationOffsetMs = ResultMs.Value;
            }

            _profile!.Calibrated = true;
            _store!.Save(_profile);

            if (_result != null)
            {
                _result.text = ResultMs.HasValue ? Strings.Format("calibration.result", ResultMs.Value) : Strings.Get("calibration.no_taps");
            }

            if (_done != null)
            {
                _done.interactable = true;
            }

            StopMetronome();
            Completed?.Invoke(this);
        }

        /// <summary>Activates Done the way the player would; it does nothing until the test has finished.</summary>
        public bool ChooseDone()
        {
            return _done != null && ScreenFactory.Submit(_done);
        }

        public void Close()
        {
            Closed?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>The median of the offsets; an even count averages the two middle values, halves rounding away from zero.</summary>
        public static int Median(IReadOnlyList<int> values)
        {
            if (values is null || values.Count == 0)
            {
                throw new ArgumentException("At least one value is required.", nameof(values));
            }

            var sorted = new List<int>(values);
            sorted.Sort();
            int middle = sorted.Count / 2;
            if (sorted.Count % 2 == 1)
            {
                return sorted[middle];
            }

            return (int)Math.Round((sorted[middle - 1] + sorted[middle]) / 2.0, MidpointRounding.AwayFromZero);
        }

        private int NearestBeat(int audioTimeMs)
        {
            var map = Track.BeatMap;
            int best = 0;
            int bestDistance = int.MaxValue;
            for (int beat = 0; beat < BeatCount; beat++)
            {
                int distance = Math.Abs(audioTimeMs - map.TimeAtBeat(beat));
                if (distance < bestDistance)
                {
                    best = beat;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void BuildUi(Transform root)
        {
            ScreenFactory.Fill("Backdrop", root, ScreenFactory.Backdrop);
            ScreenFactory.Label("Title", root, Strings.Get("calibration.title"), 64, new Vector2(0f, 400f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter);
            ScreenFactory.Label("Instructions", root, Strings.Format("calibration.instructions", BeatCount, Bpm), 36, new Vector2(0f, 300f), new Vector2(1400f, 80f), TextAnchor.MiddleCenter);
            _note = ScreenFactory.Label("BluetoothNote", root, Strings.Get("calibration.bluetooth_note"), 30, new Vector2(0f, 220f), new Vector2(1400f, 80f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            _marker = Presenter.HudFactory.Image("Marker", root, ScreenFactory.Accent, new Vector2(0f, 20f), new Vector2(140f, 140f)).rectTransform;
            _taps = ScreenFactory.Label("Taps", root, Strings.Format("calibration.taps", 0, BeatCount), 40, new Vector2(0f, -160f), new Vector2(800f, 70f), TextAnchor.MiddleCenter);
            _result = ScreenFactory.Label("Result", root, "", 40, new Vector2(0f, -240f), new Vector2(1200f, 70f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            _done = ScreenFactory.Button("Done", root, Strings.Get("calibration.done"), new Vector2(0f, -380f), new Vector2(320f, 80f), Close);
            _done.interactable = false;
        }

        private void StartMetronome()
        {
            Track = new Track(TrackId, 0, EncounterTier.Normal, BeatCount, 0, new TempoMap(Bpm));
            _metronomeHost = new GameObject("CalibrationMetronome");
            _metronomeHost.transform.SetParent(transform, false);
            _metronomeHost.AddComponent<AudioSource>();
            _clock = _metronomeHost.AddComponent<BeatClock>();
            _clock.Schedule(Track, PlaceholderAudio.ClickTrack(Track), LeadSeconds);
        }

        private void StopMetronome()
        {
            _clock = null;
            if (_metronomeHost != null)
            {
                Destroy(_metronomeHost);
                _metronomeHost = null;
            }
        }

        private void Awake()
        {
            _tapAction = new InputAction("calibration-tap", InputActionType.Button, InputMap.ControlPath(InputMap.Chord));
            _tapAction.performed += context => Tap(context.time);
        }

        private void OnEnable()
        {
            _tapAction?.Enable();
        }

        private void OnDisable()
        {
            _tapAction?.Disable();
        }

        private void Update()
        {
            if (_finished || _clock == null || !_clock.IsScheduled)
            {
                return;
            }

            int nowMs = _clock.NowMs;
            if (_marker != null)
            {
                int beatMs = 60_000 / Bpm;
                float phase = ((nowMs % beatMs) + beatMs) % beatMs / (float)beatMs;
                float scale = 1f + 0.35f * (1f - phase) * (1f - phase);
                _marker.localScale = new Vector3(scale, scale, 1f);
            }

            if (nowMs > Track.BeatMap.TimeAtBeat(BeatCount - 1) + GraceMs)
            {
                Finish();
            }
        }

        private void OnDestroy()
        {
            _tapAction?.Dispose();
            _tapAction = null;
        }
    }
}
