#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Profiles;
using Chiki.Client.Screens;
using Chiki.Client.Text;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Client
{
    public class Map
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "map-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveRoot()
        {
            foreach (var host in _hosts)
            {
                if (host != null)
                {
                    Object.DestroyImmediate(host);
                }
            }

            _hosts.Clear();
            ActiveProfile.Clear();
            ClientTestContent.ClearCatalogues();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator select_neighbour_moves()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            yield return null;
            var run = flow.Run!;
            var map = flow.Map;
            Assume.That(map, Is.Not.Null, "the run did not open on the map");
            Assume.That(run.CurrentNodeId, Is.EqualTo(run.CurrentMap.EntryId));
            var first = map!.Neighbours[0];
            Assume.That(map.Neighbours, Has.Count.GreaterThanOrEqualTo(2), "the entry node branches into routes");

            bool chosen = map.ChooseNeighbour(0);
            yield return null;

            Assert.That(chosen, Is.True, "the first neighbour could not be selected");
            Assert.That(run.CurrentNodeId, Is.EqualTo(first.Id), "the run did not move to the chosen neighbour");
            Assert.That(flow.OpenedNodeId, Is.EqualTo(first.Id), "the node was not opened");
            Assert.That(flow.PreBattle != null, Is.EqualTo(first.IsBattle), "a battle node opens the pre-battle panel");
            Assert.That(flow.Stop != null, Is.EqualTo(!first.IsBattle), "a stop node opens its panel");
            Assert.That(flow.Map, Is.Not.Null, "the map is gone");
            Assert.That(flow.Map!.Neighbours, Is.EqualTo(run.ForwardNodes), "the map does not show the new node's neighbours");
        }

        /// <summary>
        /// A run saved standing on a battle node it never fought reopens that node when the map
        /// opens again. Without it the map refuses every move (the node is not completed) and
        /// offers no way back into the fight, which strands the run for good.
        /// </summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator resumed_run_on_unfought_battle_node_reopens_it()
        {
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            yield return null;
            var run = flow.Run!;

            // Walk to a battle node and leave it unfought, as quitting on the pre-battle panel does.
            yield return ClientTestContent.OpenBattleNode(flow);
            Assume.That(flow.PreBattle, Is.Not.Null, "no battle node could be opened");
            Assume.That(run.CurrentNode.IsBattle, Is.True);
            Assume.That(run.CurrentNodeCompleted, Is.False, "the battle node counts as completed already");
            string stuckAt = run.CurrentNodeId;

            // Reopening the map is what a resume does, and what left the run stranded before.
            flow.EnterMap();
            yield return null;

            Assert.That(flow.Map, Is.Not.Null, "the map did not open");
            Assert.That(flow.Run!.CurrentNodeId, Is.EqualTo(stuckAt), "the run moved on its own");
            Assert.That(flow.PreBattle, Is.Not.Null, "the unfought battle node did not reopen, so nothing can move the run on");
            Assert.That(flow.OpenedNodeId, Is.EqualTo(stuckAt), "the reopened node is not the one the run stands on");

            // And the way out works: Enter starts the battle the map alone could never reach.
            flow.PreBattle!.PressEnter();
            yield return null;
            Assert.That(flow.Battle, Is.Not.Null, "Enter on the reopened panel did not start the battle");
        }

        /// <summary>P9.3: every node wears its type's icon, the marker and every path are catalogue art, and the backdrop covers the screen.</summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator map_drawn_from_catalogue()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var flow = ClientTestContent.FlowWithRun(_root, "A", "chiki-1", _hosts, out _);
            var run = flow.Run!;
            Assume.That(flow.Map, Is.Not.Null, "the run did not open on the map");

            // One step along the map, so the board holds a walked connection as well as unwalked ones.
            flow.Map!.ChooseNeighbour(0);
            yield return null;

            // Back on the map with the node's panel closed, so only the map's own lookups are counted.
            catalogue.ClearMissing();
            flow.CloseNode();
            yield return null;
            var map = flow.Map!;

            foreach (var node in run.CurrentMap.Nodes)
            {
                var image = map.NodeImage(node.Id);
                Assert.That(image, Is.Not.Null, node.Id + " is not on the board");
                Assert.That(image!.sprite, Is.SameAs(catalogue.Sprite(MapScreen.NodeKind, MapScreen.NodeIconId(node.Type))), node.Id + " does not carry its type's icon");
            }

            Assert.That(map.PlayerMarker, Is.Not.Null, "the board has no player marker");
            Assert.That(map.PlayerMarker!.sprite, Is.SameAs(catalogue.Sprite(MapScreen.UiKind, MapScreen.MarkerId)), "the marker is not the catalogue's");
            Assert.That(map.Connections, Is.Not.Empty, "the board has no connections");
            Assert.That(map.Connections.Any(c => c.Walked), Is.True, "the step taken left no walked connection");
            Assert.That(map.Connections.Any(c => !c.Walked), Is.True, "every connection counts as walked");
            foreach (var connection in map.Connections)
            {
                string id = connection.Walked ? MapScreen.WalkedPathId : MapScreen.PathId;
                Assert.That(connection.Image.sprite, Is.SameAs(catalogue.Sprite(MapScreen.UiKind, id)), connection.FromId + " to " + connection.ToId + " does not carry the " + id + " sprite");
            }

            Assert.That(map.Backdrop.sprite, Is.SameAs(catalogue.Sprite(MapScreen.BackdropKind, MapScreen.BackdropId)), "the backdrop is not the catalogue's");
            Assert.That(map.BackdropCoversScreen(), Is.True, "the backdrop leaves part of the screen uncovered");
            Assert.That(catalogue.Missing, Is.Empty, "the map fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>P9.4: the header names the continent and shows ARD, CRP, Base DMG, Essence and the seed, beside the Binder and Settings buttons.</summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator header_shows_every_stat()
        {
            var content = ClientTestContent.LoadRunContent();
            var run = ClientTestContent.RunAtEntry(content, "chiki-1", new RunStats(300, 250, 2, 40, 7));
            var flow = ClientTestContent.FlowResuming(_root, "A", run, content, _hosts);
            yield return null;
            var map = flow.Map;
            Assume.That(map, Is.Not.Null, "the run did not open on the map");
            Assume.That(run.World, Is.EqualTo(1));

            Assert.That(map!.ContinentText, Is.EqualTo(Strings.Get("world.1.name")), "the header does not name World 1's continent");
            Assert.That(map.ContinentText, Does.Not.StartWith("["), "World 1 has no continent name in the string table");
            Assert.That(map.ArdText, Does.Contain("250").And.Contain("300"), "the header does not show ARD 250 of 300");
            Assert.That(map.CrpText, Does.Contain("7"), "the header does not show CRP 7");
            Assert.That(map.CrpIcon.gameObject.activeInHierarchy, Is.True, "the CRP value has no badge");
            Assert.That(map.BaseDmgText, Does.Contain("2"), "the header does not show Base DMG 2");
            Assert.That(map.EssenceText, Does.Contain("40"), "the header does not show Essence 40");
            Assert.That(map.SeedText, Does.Contain("chiki-1"), "the header does not show the seed");
            Assert.That(map.BinderButton.gameObject.activeInHierarchy, Is.True, "the header has no Binder button");
            Assert.That(map.SettingsButton.gameObject.activeInHierarchy, Is.True, "the header has no Settings button");
        }

        /// <summary>P9.5: the equipped Charm and the held Imprints show as catalogue icons, each naming itself in its tooltip.</summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator charms_and_imprints_shown()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var content = ClientTestContent.LoadRunContent();
            var run = ClientTestContent.RunAtEntry(content, "chiki-1", charms: new[] { "charm-clean-victory" });
            run.AcquireImprint("imprint-keen-edge");
            run.AcquireImprint("imprint-thick-hide");
            var flow = ClientTestContent.FlowResuming(_root, "A", run, content, _hosts);
            yield return null;
            var map = flow.Map;
            Assume.That(map, Is.Not.Null, "the run did not open on the map");

            var charms = map!.HeldIcons.Where(i => i.Kind == HeldIcon.CharmKind).ToList();
            var imprints = map.HeldIcons.Where(i => i.Kind == HeldIcon.ImprintKind).ToList();

            Assert.That(charms, Has.Count.EqualTo(1), "one Charm icon should show");
            Assert.That(imprints, Has.Count.EqualTo(2), "two Imprint icons should show");
            foreach (var icon in charms.Concat(imprints))
            {
                string id = HeldIcon.IconId(icon.Kind, icon.ContentId);
                Assert.That(icon.Icon.sprite, Is.SameAs(catalogue.Sprite(icon.Kind, id)), icon.ContentId + " is not drawn from the catalogue");
                string name = icon.Kind == HeldIcon.CharmKind ? content.FindCharm(icon.ContentId)!.Name : content.FindImprint(icon.ContentId)!.Name;
                Assert.That(icon.TooltipText, Does.Contain(name), icon.ContentId + "'s tooltip does not name it");
            }

            Assert.That(charms.Select(c => c.ContentId), Is.EqualTo(new[] { "charm-clean-victory" }));
            Assert.That(imprints.Select(i => i.ContentId), Is.EqualTo(new[] { "imprint-keen-edge", "imprint-thick-hide" }));
            Assert.That(catalogue.Missing, Is.Empty, "an icon fell back: " + string.Join(", ", catalogue.Missing));
        }
    }
}
