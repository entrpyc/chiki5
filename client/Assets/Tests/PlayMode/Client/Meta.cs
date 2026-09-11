#nullable enable
using System;
using System.IO;
using System.Linq;
using Chiki.Client.Content;
using Chiki.Client.Profiles;
using Chiki.Sim.Data;
using NUnit.Framework;
using UnityEngine;

namespace Client
{
    public class Meta
    {
        private string _root = null!;

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "meta-" + Guid.NewGuid().ToString("N"));
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
        public void unlocks_survive_reload()
        {
            var charm = CharmLoader.SetFromJson(ContentFiles.ReadText("charms/fixtures.json")).Charms.First();
            var store = new ProfileStore(_root);
            var a = store.Create("A");

            bool granted = a.Meta.UnlockCharm(charm.Id);
            bool grantedAgain = a.Meta.UnlockCharm(charm.Id);
            store.Save(a);
            var reloaded = new ProfileStore(_root).Load("A");

            Assert.That(granted, Is.True);
            Assert.That(grantedAgain, Is.False, "an unlock is granted once");
            Assert.That(reloaded.Meta.HasCharm(charm.Id), Is.True, "the Charm unlock did not survive reload");
            Assert.That(reloaded.Meta.CharmUnlocks, Is.EqualTo(new[] { charm.Id }));
        }
    }
}
