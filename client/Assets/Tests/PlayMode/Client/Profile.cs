#nullable enable
using System;
using System.IO;
using Chiki.Client.Profiles;
using NUnit.Framework;
using UnityEngine;
using ProfileRecord = Chiki.Client.Profiles.Profile;

namespace Client
{
    public class Profile
    {
        private string _root = null!;

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "profiles-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveRoot()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void profiles_share_nothing()
        {
            var store = new ProfileStore(_root);
            ProfileRecord a = store.Create("A");
            ProfileRecord b = store.Create("B");

            a.CalibrationOffsetMs = 80;
            a.Calibrated = true;
            a.Settings.MetronomeOn = true;
            a.Meta.CharmUnlocks.Add("charm-test");
            store.Save(a);

            var reloaded = new ProfileStore(_root);
            ProfileRecord aAgain = reloaded.Load("A");
            ProfileRecord bAgain = reloaded.Load("B");

            Assert.That(bAgain.CalibrationOffsetMs, Is.EqualTo(0), "B took A's calibration offset");
            Assert.That(bAgain.Calibrated, Is.False);
            Assert.That(bAgain.Settings.MetronomeOn, Is.False, "B took A's settings");
            Assert.That(bAgain.Meta.CharmUnlocks, Is.Empty, "B took A's unlocks");
            Assert.That(aAgain.CalibrationOffsetMs, Is.EqualTo(80), "A's offset did not survive reload");
            Assert.That(aAgain.SchemaVersion, Is.EqualTo(ProfileRecord.CurrentSchemaVersion));
            Assert.That(reloaded.List(), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(reloaded.FileOf("A"), Is.Not.EqualTo(reloaded.FileOf("B")));
            Assert.That(aAgain.RunLogFolder, Is.Not.EqualTo(bAgain.RunLogFolder), "the profiles share a run-log folder");
            Assert.That(Directory.Exists(aAgain.RunLogFolder), Is.True);
            Assert.That(File.ReadAllText(reloaded.FileOf("B")), Does.Not.Contain("charm-test"), "B's file holds A's data");
        }
    }
}
