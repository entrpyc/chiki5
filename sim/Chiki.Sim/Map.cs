using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>The node catalogue (PRD 3.2.8–3.2.14).</summary>
    public enum NodeType
    {
        NormalBattle,
        Elite,
        Boss,
        Shop,
        Event,
        Blacksmith,
        Forge,
    }

    public static class NodeTypes
    {
        /// <summary>The non-Boss types in the order of the share tables (PRD 3.2.6): Normal, Elite, Shop, Event, Blacksmith, Forge.</summary>
        public static readonly IReadOnlyList<NodeType> Shared = new[]
        {
            NodeType.NormalBattle, NodeType.Elite, NodeType.Shop, NodeType.Event, NodeType.Blacksmith, NodeType.Forge,
        };

        /// <summary>Whether entering the node starts a battle (PRD 3.2.8–3.2.10).</summary>
        public static bool IsBattle(this NodeType type)
        {
            return type == NodeType.NormalBattle || type == NodeType.Elite || type == NodeType.Boss;
        }

        /// <summary>The encounter tier a battle node fights at (PRD 3.3.9.1).</summary>
        public static EncounterTier TierOf(NodeType type)
        {
            switch (type)
            {
                case NodeType.NormalBattle: return EncounterTier.Normal;
                case NodeType.Elite: return EncounterTier.Elite;
                case NodeType.Boss: return EncounterTier.Boss;
                default: throw new ArgumentException($"{type} is not a battle node.", nameof(type));
            }
        }

        public static string ToId(NodeType type)
        {
            switch (type)
            {
                case NodeType.NormalBattle: return "normal-battle";
                case NodeType.Elite: return "elite";
                case NodeType.Boss: return "boss";
                case NodeType.Shop: return "shop";
                case NodeType.Event: return "event";
                case NodeType.Blacksmith: return "blacksmith";
                case NodeType.Forge: return "forge";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown node type.");
            }
        }
    }

    /// <summary>
    /// One node of a World graph (PRD 4.3): its layer and index, its type, the nodes it connects
    /// forward to, and its content roll fixed by the seed: the enemy for a battle node
    /// (PRD 3.2.4); null when the pool has none of its tier.
    /// </summary>
    public sealed record MapNode(string Id, int Layer, int Index, NodeType Type, IReadOnlyList<string> Next, string? EnemyId)
    {
        public bool IsBattle => Type.IsBattle();
    }

    /// <summary>
    /// A World's node graph (PRD 3.2.5, 3.2.6, 4.3): layered, forward-only, one entry node in
    /// layer 0 and exactly one Boss in the last layer. Nodes are listed layer by layer.
    /// </summary>
    public sealed class MapGraph
    {
        private readonly Dictionary<string, MapNode> _byId = new Dictionary<string, MapNode>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<MapNode>> _previous = new Dictionary<string, List<MapNode>>(StringComparer.Ordinal);
        private readonly List<List<MapNode>> _layers = new List<List<MapNode>>();

        public int World { get; }

        public IReadOnlyList<MapNode> Nodes { get; }

        public string EntryId { get; }

        public string BossId { get; }

        /// <summary>Layers before the Boss: the nodes on every entry-to-Boss path (PRD 3.2.6).</summary>
        public int PathLength => _layers.Count - 1;

        public IReadOnlyList<IReadOnlyList<MapNode>> Layers => _layers;

        public MapGraph(int world, IReadOnlyList<MapNode> nodes)
        {
            if (nodes is null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (nodes.Count == 0)
            {
                throw new ArgumentException("A graph has nodes.", nameof(nodes));
            }

            World = world;
            Nodes = nodes;
            foreach (var node in nodes)
            {
                if (_byId.ContainsKey(node.Id))
                {
                    throw new ArgumentException($"Node id {node.Id} appears twice.", nameof(nodes));
                }

                _byId[node.Id] = node;
                while (_layers.Count <= node.Layer)
                {
                    _layers.Add(new List<MapNode>());
                }

                _layers[node.Layer].Add(node);
            }

            foreach (var node in nodes)
            {
                foreach (var nextId in node.Next)
                {
                    if (!_byId.TryGetValue(nextId, out var next))
                    {
                        throw new ArgumentException($"Node {node.Id} connects to unknown node {nextId}.", nameof(nodes));
                    }

                    if (next.Layer <= node.Layer)
                    {
                        throw new ArgumentException($"Node {node.Id} connects backward to {nextId}.", nameof(nodes));
                    }

                    if (!_previous.TryGetValue(nextId, out var list))
                    {
                        _previous[nextId] = list = new List<MapNode>();
                    }

                    list.Add(node);
                }
            }

            if (_layers[0].Count != 1)
            {
                throw new ArgumentException("A graph has exactly one entry node.", nameof(nodes));
            }

            var last = _layers[_layers.Count - 1];
            if (last.Count != 1 || last[0].Type != NodeType.Boss)
            {
                throw new ArgumentException("The final layer is exactly one Boss node.", nameof(nodes));
            }

            EntryId = _layers[0][0].Id;
            BossId = last[0].Id;
        }

        public MapNode this[string id] => _byId.TryGetValue(id, out var node) ? node : throw new ArgumentException($"Unknown node '{id}'.", nameof(id));

        public bool Contains(string id)
        {
            return _byId.ContainsKey(id);
        }

        public MapNode Entry => this[EntryId];

        public MapNode Boss => this[BossId];

        /// <summary>The nodes a node connects forward to (PRD 3.2.7).</summary>
        public IReadOnlyList<MapNode> NextOf(string id)
        {
            var next = new List<MapNode>();
            foreach (var nextId in this[id].Next)
            {
                next.Add(_byId[nextId]);
            }

            return next;
        }

        public IReadOnlyList<MapNode> PreviousOf(string id)
        {
            return _previous.TryGetValue(this[id].Id, out var previous) ? previous : (IReadOnlyList<MapNode>)Array.Empty<MapNode>();
        }

        public bool IsForward(string fromId, string toId)
        {
            foreach (var nextId in this[fromId].Next)
            {
                if (nextId == toId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every node reachable from a node by forward moves, the node itself excluded.</summary>
        public HashSet<string> Descendants(string id)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>();
            stack.Push(id);
            while (stack.Count > 0)
            {
                foreach (var nextId in this[stack.Pop()].Next)
                {
                    if (seen.Add(nextId))
                    {
                        stack.Push(nextId);
                    }
                }
            }

            return seen;
        }

        public int CountOf(NodeType type)
        {
            int count = 0;
            foreach (var node in Nodes)
            {
                if (node.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>The share of a node type in percent of all generated nodes, rounded down (PRD 3.2.6).</summary>
        public int PercentOf(NodeType type)
        {
            return CountOf(type) * 100 / Nodes.Count;
        }

        /// <summary>The longest run of consecutive nodes with a single forward connection on any path (PRD 3.2.5).</summary>
        public int LongestChoicelessRun()
        {
            var run = new Dictionary<string, int>(StringComparer.Ordinal);
            int longest = 0;
            foreach (var layer in _layers)
            {
                foreach (var node in layer)
                {
                    int value = 0;
                    if (node.Next.Count == 1)
                    {
                        value = 1;
                        foreach (var previous in PreviousOf(node.Id))
                        {
                            if (previous.Next.Count == 1)
                            {
                                value = Math.Max(value, run[previous.Id] + 1);
                            }
                        }
                    }

                    run[node.Id] = value;
                    longest = Math.Max(longest, value);
                }
            }

            return longest;
        }

        /// <summary>The fewest and most nodes on an entry-to-Boss path before the Boss.</summary>
        public (int Shortest, int Longest) PathBounds()
        {
            var shortest = new Dictionary<string, int>(StringComparer.Ordinal);
            var longest = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var layer in _layers)
            {
                foreach (var node in layer)
                {
                    int min = int.MaxValue;
                    int max = int.MinValue;
                    foreach (var previous in PreviousOf(node.Id))
                    {
                        min = Math.Min(min, shortest[previous.Id] + 1);
                        max = Math.Max(max, longest[previous.Id] + 1);
                    }

                    shortest[node.Id] = min == int.MaxValue ? 0 : min;
                    longest[node.Id] = max == int.MinValue ? 0 : max;
                }
            }

            return (shortest[BossId], longest[BossId]);
        }
    }

    /// <summary>
    /// Builds a World graph from a forked generator (PRD 3.2.5, 3.2.6, 3.2.4): layered, with the
    /// first layer branching into 2–3 routes, every route sharing a node with another before
    /// the Boss, no run of more than 4 choiceless nodes, 55–70 nodes, 13–17 nodes on every
    /// path before the Boss, node types inside the World's bands, and a seeded enemy roll on
    /// every battle node. Draws only from the generator it is given, forking once per attempt,
    /// so the same seed gives the same graph.
    /// </summary>
    public static class MapGenerator
    {
        public static MapGraph Generate(Rng rng, int world, IReadOnlyList<EnemyDefinition> enemyPool)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (enemyPool is null)
            {
                throw new ArgumentNullException(nameof(enemyPool));
            }

            if (world < 1 || world > Tuning.WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(world), $"World is 1 to {Tuning.WorldCount}.");
            }

            for (int attempt = 1; attempt <= Tuning.MapMaxAttempts; attempt++)
            {
                var graph = TryGenerate(rng.Fork("attempt-" + attempt), world, enemyPool);
                if (graph != null && Validate(graph).Count == 0)
                {
                    return graph;
                }
            }

            throw new InvalidOperationException($"No World {world} graph met the constraints in {Tuning.MapMaxAttempts} attempts.");
        }

        /// <summary>The constraints of PRD 3.2.5 and 3.2.6 a graph breaks; empty when it holds them all.</summary>
        public static IReadOnlyList<string> Validate(MapGraph graph)
        {
            if (graph is null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var violations = new List<string>();
            int routes = graph.Layers.Count > 1 ? graph.Layers[1].Count : 0;
            if (routes < Tuning.MapMinRoutes || routes > Tuning.MapMaxRoutes)
            {
                violations.Add($"first layer has {routes} routes, not {Tuning.MapMinRoutes}–{Tuning.MapMaxRoutes}");
            }

            if (graph.Nodes.Count < Tuning.MapMinNodes || graph.Nodes.Count > Tuning.MapMaxNodes)
            {
                violations.Add($"{graph.Nodes.Count} nodes, not {Tuning.MapMinNodes}–{Tuning.MapMaxNodes}");
            }

            var (shortest, longest) = graph.PathBounds();
            if (shortest < Tuning.MapMinPathNodes || longest > Tuning.MapMaxPathNodes)
            {
                violations.Add($"paths of {shortest}–{longest} nodes, not {Tuning.MapMinPathNodes}–{Tuning.MapMaxPathNodes}");
            }

            int choiceless = graph.LongestChoicelessRun();
            if (choiceless > Tuning.MapMaxChoicelessRun)
            {
                violations.Add($"a path runs {choiceless} nodes without a choice");
            }

            if (routes >= 2)
            {
                var descendants = new List<HashSet<string>>();
                foreach (var route in graph.Layers[1])
                {
                    var set = graph.Descendants(route.Id);
                    set.Remove(graph.BossId);
                    descendants.Add(set);
                }

                for (int i = 0; i < descendants.Count; i++)
                {
                    bool reconnects = false;
                    for (int j = 0; j < descendants.Count && !reconnects; j++)
                    {
                        if (i != j && descendants[i].Overlaps(descendants[j]))
                        {
                            reconnects = true;
                        }
                    }

                    if (!reconnects)
                    {
                        violations.Add($"route {graph.Layers[1][i].Id} never reconnects with another before the Boss");
                    }
                }
            }

            if (graph.CountOf(NodeType.Boss) != 1)
            {
                violations.Add("exactly one Boss");
            }

            for (int i = 0; i < NodeTypes.Shared.Count; i++)
            {
                var type = NodeTypes.Shared[i];
                int percent = graph.PercentOf(type);
                int min = Tuning.MapTypeMinPercent[graph.World - 1][i];
                int max = Tuning.MapTypeMaxPercent[graph.World - 1][i];
                if (percent < min || percent > max)
                {
                    violations.Add($"{type} is {percent}% of nodes, not {min}–{max}%");
                }
            }

            return violations;
        }

        private static MapGraph? TryGenerate(Rng rng, int world, IReadOnlyList<EnemyDefinition> enemyPool)
        {
            int pathLayers = rng.NextInt(Tuning.MapMinPathNodes, Tuning.MapMaxPathNodes + 1);
            var widths = new List<int> { 1, rng.NextInt(Tuning.MapMinRoutes, Tuning.MapMaxRoutes + 1) };
            while (widths.Count < pathLayers)
            {
                widths.Add(rng.NextInt(Tuning.MapMinLayerWidth, Tuning.MapMaxLayerWidth + 1));
            }

            int nonBoss = Sum(widths);
            int target = rng.NextInt(Tuning.MapMinNodes, Tuning.MapMaxNodes + 1) - 1;
            for (int guard = 0; guard < 200 && nonBoss != target; guard++)
            {
                int layer = rng.NextInt(2, widths.Count);
                if (nonBoss < target && widths[layer] < Tuning.MapMaxLayerWidth + 1)
                {
                    widths[layer]++;
                    nonBoss++;
                }
                else if (nonBoss > target && widths[layer] > Tuning.MapMinLayerWidth - 1)
                {
                    widths[layer]--;
                    nonBoss--;
                }
            }

            if (nonBoss + 1 < Tuning.MapMinNodes || nonBoss + 1 > Tuning.MapMaxNodes)
            {
                return null;
            }

            widths.Add(1); // the Boss layer
            int layers = widths.Count;
            var next = new List<List<List<int>>>();
            for (int layer = 0; layer < layers; layer++)
            {
                var layerNext = new List<List<int>>();
                for (int index = 0; index < widths[layer]; index++)
                {
                    layerNext.Add(new List<int>());
                }

                next.Add(layerNext);
            }

            for (int layer = 0; layer < layers - 1; layer++)
            {
                int width = widths[layer];
                int nextWidth = widths[layer + 1];
                var hasIncoming = new bool[nextWidth];
                for (int index = 0; index < width; index++)
                {
                    int centre = Centre(index, width, nextWidth);
                    int roll = rng.NextInt(0, 100);
                    int degree = roll < 45 ? 1 : roll < 90 ? 2 : 3;
                    var targets = next[layer][index];
                    var candidates = new List<int>();
                    for (int t = Math.Max(0, centre - 1); t <= Math.Min(nextWidth - 1, centre + 1); t++)
                    {
                        candidates.Add(t);
                    }

                    Shuffle(candidates, rng);
                    candidates.Remove(centre);
                    candidates.Insert(0, centre);
                    for (int k = 0; k < Math.Min(degree, candidates.Count); k++)
                    {
                        targets.Add(candidates[k]);
                        hasIncoming[candidates[k]] = true;
                    }

                    targets.Sort();
                }

                for (int t = 0; t < nextWidth; t++)
                {
                    if (!hasIncoming[t])
                    {
                        int from = Centre(t, nextWidth, width);
                        if (!next[layer][from].Contains(t))
                        {
                            next[layer][from].Add(t);
                            next[layer][from].Sort();
                        }
                    }
                }
            }

            var types = RollTypes(rng, world, nonBoss);
            var nodes = new List<MapNode>();
            int typeIndex = 0;
            for (int layer = 0; layer < layers; layer++)
            {
                for (int index = 0; index < widths[layer]; index++)
                {
                    var type = layer == layers - 1 ? NodeType.Boss : types[typeIndex++];
                    var nextIds = new List<string>();
                    foreach (int t in next[layer][index])
                    {
                        nextIds.Add(NodeId(world, layer + 1, t));
                    }

                    string? enemyId = type.IsBattle() ? RollEnemy(rng, NodeTypes.TierOf(type), enemyPool) : null;
                    nodes.Add(new MapNode(NodeId(world, layer, index), layer, index, type, nextIds, enemyId));
                }
            }

            return new MapGraph(world, nodes);
        }

        /// <summary>The non-Boss node types by target share (PRD 3.2.6), the entry a Normal battle, the rest shuffled.</summary>
        private static List<NodeType> RollTypes(Rng rng, int world, int nonBoss)
        {
            int total = nonBoss + 1;
            var counts = new int[NodeTypes.Shared.Count];
            int assigned = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = Tuning.MapTypeTargetPercent[world - 1][i] * total / 100;
                assigned += counts[i];
            }

            counts[0] += nonBoss - assigned;
            var types = new List<NodeType>();
            for (int i = 0; i < counts.Length; i++)
            {
                for (int n = 0; n < counts[i]; n++)
                {
                    types.Add(NodeTypes.Shared[i]);
                }
            }

            types.Remove(NodeType.NormalBattle);
            Shuffle(types, rng);
            types.Insert(0, NodeType.NormalBattle);
            return types;
        }

        private static string? RollEnemy(Rng rng, EncounterTier tier, IReadOnlyList<EnemyDefinition> pool)
        {
            var ofTier = new List<EnemyDefinition>();
            foreach (var enemy in pool)
            {
                if (enemy.Tier == tier)
                {
                    ofTier.Add(enemy);
                }
            }

            return ofTier.Count == 0 ? null : ofTier[rng.NextInt(0, ofTier.Count)].Id;
        }

        private static int Centre(int index, int width, int nextWidth)
        {
            if (width <= 1)
            {
                return (nextWidth - 1) / 2;
            }

            return (index * (nextWidth - 1) * 2 + (width - 1)) / ((width - 1) * 2);
        }

        private static void Shuffle<T>(List<T> list, Rng rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static int Sum(List<int> values)
        {
            int sum = 0;
            foreach (int value in values)
            {
                sum += value;
            }

            return sum;
        }

        public static string NodeId(int world, int layer, int index)
        {
            return $"w{world}-{layer}-{index}";
        }
    }
}
