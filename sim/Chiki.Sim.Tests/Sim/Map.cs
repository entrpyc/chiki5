using Chiki.Sim;
using SimRng = Chiki.Sim.Rng;

namespace Sim;

public class Map
{
    private const int Seeds = 200;

    private static IReadOnlyList<EnemyDefinition> Pool() => TestContent.LoadFixtureEnemies().Enemies;

    private static MapGraph Generate(string seed, int world, IReadOnlyList<EnemyDefinition> pool) =>
        MapGenerator.Generate(new SimRng(seed).Fork("map-w" + world), world, pool);

    [Test]
    public void shape_constraints_hold_over_200_seeds()
    {
        var pool = Pool();
        var failures = new List<string>();

        for (int i = 0; i < Seeds; i++)
        {
            var graph = Generate("shape-" + i, 1 + i % Tuning.WorldCount, pool);
            int routes = graph.Layers[1].Count;
            var routeDescendants = graph.Layers[1].Select(r => { var d = graph.Descendants(r.Id); d.Remove(graph.BossId); return d; }).ToList();
            bool everyRouteReconnects = routeDescendants.All(d => routeDescendants.Any(o => !ReferenceEquals(o, d) && o.Overlaps(d)));
            var lastLayer = graph.Layers[graph.Layers.Count - 1];

            if (graph.Layers[0].Count != 1) failures.Add($"seed {i}: {graph.Layers[0].Count} entry nodes");
            if (routes < 2 || routes > 3) failures.Add($"seed {i}: {routes} first-layer routes");
            if (!everyRouteReconnects) failures.Add($"seed {i}: a route never reconnects");
            if (graph.LongestChoicelessRun() > 4) failures.Add($"seed {i}: {graph.LongestChoicelessRun()} choiceless nodes in a row");
            if (lastLayer.Count != 1 || lastLayer[0].Type != NodeType.Boss || graph.CountOf(NodeType.Boss) != 1) failures.Add($"seed {i}: the final node is not the one Boss");
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void size_and_distribution_over_200_seeds()
    {
        var pool = Pool();
        var failures = new List<string>();

        for (int i = 0; i < Seeds; i++)
        {
            var graph = Generate("size-" + i, 1, pool);
            var (shortest, longest) = graph.PathBounds();
            if (graph.Nodes.Count < 55 || graph.Nodes.Count > 70) failures.Add($"seed {i}: {graph.Nodes.Count} nodes");
            if (shortest < 13 || longest > 17) failures.Add($"seed {i}: paths of {shortest}–{longest} nodes");
            for (int t = 0; t < NodeTypes.Shared.Count; t++)
            {
                int percent = graph.PercentOf(NodeTypes.Shared[t]);
                if (percent < Tuning.MapTypeMinPercent[0][t] || percent > Tuning.MapTypeMaxPercent[0][t])
                {
                    failures.Add($"seed {i}: {NodeTypes.Shared[t]} at {percent}%");
                }
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void same_seed_identical_graph()
    {
        var first = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        var second = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        var other = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-2");

        var firstNodes = first.MapOf(1).Nodes.Select(Snapshot).ToList();
        var secondNodes = second.MapOf(1).Nodes.Select(Snapshot).ToList();
        var otherNodes = other.MapOf(1).Nodes.Select(Snapshot).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(firstNodes, Is.EqualTo(secondNodes));
            Assert.That(firstNodes.Select(n => n.EnemyId).Where(e => e != null), Is.Not.Empty, "battle nodes carry enemy rolls");
            Assert.That(otherNodes, Is.Not.EqualTo(firstNodes));
        });
    }

    [Test]
    public void only_forward_neighbours_allowed()
    {
        var content = TestContent.LoadRunContent();
        var run = new RunSetup(Array.Empty<string>()).Start(content, "chiki-1");
        var start = run.CurrentNodeId;
        var forward = run.ForwardNodes.Select(n => n.Id).ToList();
        Assume.That(forward, Has.Count.GreaterThanOrEqualTo(2), "the entry node branches into routes");
        var nonNeighbour = run.CurrentMap.Nodes.First(n => n.Layer == 3).Id; // two layers ahead of where the run will stand

        var results = new List<MoveResult>();
        foreach (var neighbour in forward)
        {
            var fresh = new RunSetup(Array.Empty<string>()).Start(content, "chiki-1");
            results.Add(fresh.MoveTo(neighbour));
        }

        var moved = run.MoveTo(forward[0]);
        var backward = run.MoveTo(start);
        var elsewhere = run.MoveTo(nonNeighbour);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.All.EqualTo(MoveResult.Moved));
            Assert.That(moved, Is.EqualTo(MoveResult.Moved));
            Assert.That(run.CurrentNodeId, Is.EqualTo(forward[0]));
            Assert.That(backward, Is.EqualTo(MoveResult.NotForward));
            Assert.That(elsewhere, Is.EqualTo(MoveResult.NotForward));
            Assert.That(run.CurrentNodeId, Is.EqualTo(forward[0]));
            Assert.That(run.Events.OfType<NodeTransition>().Single(), Is.EqualTo(new NodeTransition(1, start, forward[0], run.CurrentNode.Type)));
        });
    }

    private static (string Id, int Layer, NodeType Type, string Next, string? EnemyId) Snapshot(MapNode node) =>
        (node.Id, node.Layer, node.Type, string.Join(",", node.Next), node.EnemyId);
}
