#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Profiles;
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
    }
}
