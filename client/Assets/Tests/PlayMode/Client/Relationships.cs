#nullable enable
using System;
using System.IO;
using System.Linq;
using Chiki.Client.Profiles;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;

namespace Client
{
    public class Relationships
    {
        private string _root = null!;

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "relationships-" + Guid.NewGuid().ToString("N"));
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
        public void five_records_on_new_profile()
        {
            var store = new ProfileStore(_root);
            var fresh = store.Create("A");

            var records = fresh.Relationships;
            var reloaded = new ProfileStore(_root).Load("A").Relationships;

            Assert.That(records, Has.Count.EqualTo(5));
            Assert.That(records.Select(r => r.NpcId), Is.EquivalentTo(Npc.All.Select(n => n.Id)));
            Assert.That(records.Select(r => r.ToSim().Level), Has.All.EqualTo(1));
            Assert.That(records.Select(r => r.ToSim().RpTowardNext), Has.All.EqualTo(0));
            Assert.That(reloaded, Has.Count.EqualTo(5), "the five records did not survive reload");
            Assert.That(reloaded.Select(r => r.ToSim().Level), Has.All.EqualTo(1));
        }
    }
}
