#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Client.Screens;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProfileRecord = Chiki.Client.Profiles.Profile;

namespace Client
{
    public class Calibration
    {
        private string _root = null!;
        private ProfileStore _store = null!;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void FreshStore()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "calibration-" + Guid.NewGuid().ToString("N"));
            _store = new ProfileStore(_root);
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var go in _spawned)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            _spawned.Clear();
            ActiveProfile.Clear();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private GameObject Host(string name)
        {
            var host = new GameObject(name);
            host.AddComponent<AudioListener>();
            _spawned.Add(host);
            return host;
        }

        private GameFlow NewFlow(string name)
        {
            return Host(name).AddComponent<GameFlow>();
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator median_offset_stored()
        {
            ProfileRecord profile = _store.Create("cal");
            var screen = CalibrationScreen.Open(Host("calibration-host").transform, profile, _store);
            var clock = screen.Clock!;
            var map = screen.Track.BeatMap;

            Assert.That(screen.BluetoothNoteShown, Is.True, "the Bluetooth note is not on screen");
            Assert.That(screen.BluetoothNote, Does.Contain("Bluetooth").And.Contain("100").And.Contain("300"));
            Assert.That(clock.Track!.BeatMap.BpmAt(0), Is.EqualTo(120));
            Assert.That(screen.Track.LengthBeats, Is.EqualTo(16));

            // Let the clock's offset estimate settle during the lead-in, then tap 60 ms after every beat.
            while (clock.NowMs < -200)
            {
                yield return null;
            }

            for (int beat = 0; beat < CalibrationScreen.BeatCount; beat++)
            {
                double inputTime = clock.RealtimeAt(map.TimeAtBeat(beat) + 60);
                while (Time.realtimeSinceStartupAsDouble < inputTime)
                {
                    yield return null;
                }

                screen.Tap(inputTime);
            }

            Assert.That(screen.Offsets, Has.Count.EqualTo(16));
            Assert.That(screen.Finished, Is.True, "the test did not finish after 16 taps");
            Assert.That(screen.ResultMs, Is.EqualTo(60));
            Assert.That(profile.CalibrationOffsetMs, Is.EqualTo(60));
            Assert.That(profile.Calibrated, Is.True);
            Assert.That(new ProfileStore(_root).Load("cal").CalibrationOffsetMs, Is.EqualTo(60), "the offset was not saved to the profile file");
            Assert.That(screen.DoneEnabled, Is.True);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator first_launch_forces_calibration()
        {
            ProfileRecord profile = _store.Create("fresh");
            var flow = NewFlow("flow-fresh");
            flow.Begin(profile, _store);
            yield return null;

            Assert.That(flow.PreRun, Is.Not.Null, "the pre-run screen did not open");
            Assert.That(flow.Calibration, Is.Not.Null, "the calibration screen did not open on first launch");
            Assert.That(flow.Calibration!.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.PreRun!.StartRunEnabled, Is.False, "Start Run is enabled while calibration is open");
            Assert.That(flow.PreRun.ChooseStartRun(), Is.False, "Start Run accepted input while calibration is open");
            yield return null;
            Assert.That(flow.Map, Is.Null, "a run started while calibration was open");

            var map = flow.Calibration.Track.BeatMap;
            for (int beat = 0; beat < CalibrationScreen.BeatCount; beat++)
            {
                flow.Calibration.TapAt(map.TimeAtBeat(beat) + 20);
            }

            Assert.That(flow.Calibration.Finished, Is.True);
            Assert.That(flow.Calibration.ChooseDone(), Is.True, "Done did not accept input after the test finished");
            yield return null;

            Assert.That(flow.Calibration, Is.Null, "the calibration screen is still open");
            Assert.That(flow.PreRun.StartRunEnabled, Is.True, "Start Run stayed disabled after calibration closed");
            Assert.That(new ProfileStore(_root).Load("fresh").Calibrated, Is.True, "the profile file does not record the calibration");
            Assert.That(flow.PreRun.ChooseStartRun(), Is.True);
            yield return null;
            Assert.That(flow.Map, Is.Not.Null, "Start Run did not proceed after calibration closed");
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator reachable_from_map()
        {
            ProfileRecord profile = _store.Create("veteran");
            profile.Calibrated = true;
            profile.CalibrationOffsetMs = 30;
            _store.Save(profile);
            var flow = NewFlow("flow-veteran");
            flow.Begin(profile, _store);
            yield return null;

            Assert.That(flow.Calibration, Is.Null, "a calibrated profile was forced into calibration");
            flow.EnterMap();
            yield return null;
            Assert.That(flow.Map, Is.Not.Null);
            Assert.That(flow.Map!.ChooseSettings(), Is.True, "the map has no working Settings entry");
            Assert.That(flow.Settings, Is.Not.Null, "Settings did not open from the map");
            Assert.That(flow.Settings!.ChooseCalibrate(), Is.True, "Settings has no working Calibrate entry");
            yield return null;

            Assert.That(flow.Calibration, Is.Not.Null, "Calibrate did not open the calibration screen");
            Assert.That(flow.Calibration!.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.Calibration.Clock, Is.Not.Null.And.Property("IsScheduled").True, "the calibration metronome is not running");
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator offset_shifts_grading()
        {
            // Attacks at beat 6 (3000 ms) and beat 10 (5000 ms); both presses land 60 ms late.
            var rig = ClientTestContent.ScheduledRig("calibration-offset", Beats.ToQuarterBeats(6), Beats.ToQuarterBeats(10));
            _spawned.Add(rig.Root);
            var battle = rig.Driver.Battle!;

            rig.Driver.CalibrationOffsetMs = 60;
            yield return rig.WaitUntilAudioMs(3060);
            var first = rig.Driver.Receive(new SlotPress(ClientTestContent.SlotE, 3060, false), ClientTestContent.LeftAttack10);

            rig.Driver.CalibrationOffsetMs = 0;
            yield return rig.WaitUntilAudioMs(5060);
            var second = rig.Driver.Receive(new SlotPress(ClientTestContent.SlotR, 5060, false), ClientTestContent.LeftAttack10);

            var judged = battle.Events.OfType<InputJudged>().ToList();
            Assert.That(judged, Has.Count.EqualTo(2), "both presses were not graded");
            Assert.That(first!.Grade, Is.EqualTo(Judgment.Perfect), "with offset 60 the late press was not Perfect");
            Assert.That(judged[0].OffsetMs, Is.EqualTo(0));
            Assert.That(second!.Grade, Is.EqualTo(Judgment.Good), "with offset 0 the late press was not Good");
            Assert.That(judged[1].OffsetMs, Is.EqualTo(60));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator metronome_toggle_clicks_on_beats()
        {
            var rig = ClientTestContent.ScheduledRig("calibration-metronome", Beats.ToQuarterBeats(60));
            _spawned.Add(rig.Root);
            var metronome = rig.Root.AddComponent<Metronome>();
            metronome.Bind(rig.Clock);
            metronome.On = true;
            var map = rig.Clock.Track!.BeatMap;

            yield return rig.WaitUntilAudioMs(map.TimeAtBeat(4));

            var clicks = metronome.Scheduled.Where(c => c.Beat < 4).ToList();
            Assert.That(clicks.Select(c => c.Beat), Is.EqualTo(new[] { 0, 1, 2, 3 }), "four clicks were not scheduled for beats 0-3");
            foreach (var click in clicks)
            {
                Assert.That(click.AudioTimeMs, Is.EqualTo(map.TimeAtBeat(click.Beat)), $"beat {click.Beat} audio time");
                Assert.That(click.DspTime, Is.EqualTo(rig.Clock.ToDspTime(map.TimeAtBeat(click.Beat))).Within(0.000001), $"beat {click.Beat} DSP time");
            }

            Assert.That(metronome.Sources, Is.Not.Empty);
            Assert.That(metronome.Sources, Has.None.SameAs(rig.Source), "clicks play through the track's source");
        }
    }
}
