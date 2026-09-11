using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>How a run stands (PRD 3.9.11, 4.2).</summary>
    public enum RunStatus
    {
        InProgress,
        Won,
        Died,
        Abandoned,
    }

    /// <summary>The outcome of a move on the map (PRD 3.2.7).</summary>
    public enum MoveResult
    {
        Moved,

        /// <summary>The node is not connected forward from the current one; there is no move backward.</summary>
        NotForward,

        /// <summary>The current node's content is still open (a battle not yet won).</summary>
        NodeNotCompleted,

        /// <summary>A battle is in progress.</summary>
        BattleInProgress,

        /// <summary>The current node's reward offer is still open (PRD 3.3.9.2); pick or skip first.</summary>
        RewardPending,

        RunOver,
    }

    /// <summary>
    /// One run (PRD 4.2, 3.2.2): its seed, World and node, run-wide stats, the Charms equipped
    /// pre-run, the Imprints held, the armor upgrade slots, the Binder with its loadout, the
    /// difficulty modifiers and Assist flag, its status and its World graphs. Created through
    /// <see cref="RunSetup.Start"/>; restored from a save through
    /// <see cref="Data.RunSerializer"/>. Every subsystem's random stream is a fork of the seed
    /// (PRD 3.2.4, 6.8), so the graphs are regenerated from it rather than saved. The run moves
    /// forward only (PRD 3.2.7), adding CRP per transition (PRD 3.8.2), starts and settles its
    /// battles with the effects of its Imprints and Charms attached (P18.3, P18.4), advances a
    /// World when its Boss falls (PRD 3.2.10), and ends by victory, death or abandonment,
    /// discarding everything run-scoped (PRD 3.9.1). Every change outside battle is appended
    /// to <see cref="Events"/>.
    /// </summary>
    public sealed class Run
    {
        private readonly List<string> _imprints;
        private readonly string?[] _armorUpgrades;
        private readonly Rng _imprintRng;
        private readonly Rng _rewardRng;
        private readonly Dictionary<int, MapGraph> _maps = new Dictionary<int, MapGraph>();
        private readonly HashSet<string> _visited = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<RunEvent> _events = new List<RunEvent>();
        private readonly List<BattleRecord> _battles = new List<BattleRecord>();
        private readonly List<string> _route = new List<string>();
        private int _battlesStarted;
        private bool _nodeBattle;

        /// <summary>The run seed, custom or generated (PRD 3.2.4); shown on the map and the run-end screen.</summary>
        public string Seed { get; }

        /// <summary>The World being played, 1 to <see cref="Tuning.WorldCount"/> (PRD 3.2.1).</summary>
        public int World { get; private set; }

        /// <summary>The node the player stands on in the current World's graph (PRD 3.2.7).</summary>
        public string CurrentNodeId { get; private set; } = "";

        public RunStats Stats { get; }

        /// <summary>The ids of the 0–2 Charms equipped pre-run, fixed for the run (PRD 3.9.6).</summary>
        public IReadOnlyList<string> Charms { get; }

        /// <summary>The ids of the Imprints held, in acquisition order (PRD 3.9.3); a stackable one may appear more than once.</summary>
        public IReadOnlyList<string> Imprints => _imprints;

        /// <summary>The four armor upgrade slots (PRD 3.7.13); each null until an upgrade is chosen.</summary>
        public IReadOnlyList<string?> ArmorUpgrades => _armorUpgrades;

        public Binder Binder { get; }

        public Loadout Loadout => Binder.Loadout;

        /// <summary>Difficulty modifier ids switched on pre-run (PRD 3.9.12); empty in this plan.</summary>
        public IReadOnlyList<string> DifficultyModifiers { get; }

        /// <summary>Assist mode (PRD 3.12.3); false in this plan.</summary>
        public bool Assist { get; }

        public RunStatus Status { get; private set; }

        /// <summary>Whether the run has ended by any outcome; nothing further can be entered (PRD 3.9.11).</summary>
        public bool IsOver => Status != RunStatus.InProgress;

        /// <summary>The content the run resolves its ids against.</summary>
        public RunContent Content { get; }

        /// <summary>The effects of the Imprints held and Charms equipped (P18.3, P18.4).</summary>
        public RunEffects Effects { get; }

        /// <summary>The battle in progress, from <see cref="StartBattle(EnemyDefinition, EncounterBalance)"/> until <see cref="SettleBattle"/>; null between battles.</summary>
        public Battle? CurrentBattle { get; private set; }

        /// <summary>The reward offer of the battle node just won, open until the player picks or skips (PRD 3.7.2, 3.3.9.2); null otherwise.</summary>
        public RewardOffer? PendingReward { get; private set; }

        /// <summary>Battles started this run, the label of each battle's random stream.</summary>
        public int BattlesStarted => _battlesStarted;

        /// <summary>The root generator of the seed (PRD 3.2.4); subsystems draw from <see cref="Fork"/>, never from this directly.</summary>
        public Rng Rng { get; }

        /// <summary>The current World's graph (PRD 4.3), generated from the seed on entering the World.</summary>
        public MapGraph CurrentMap => _maps[World];

        public MapNode CurrentNode => CurrentMap[CurrentNodeId];

        /// <summary>The nodes the player may move to next (PRD 3.2.7).</summary>
        public IReadOnlyList<MapNode> ForwardNodes => CurrentMap.NextOf(CurrentNodeId);

        /// <summary>The nodes visited in the current World, the current one included (PRD 4.3).</summary>
        public IReadOnlyCollection<string> Visited => _visited;

        /// <summary>Whether the current node's content is done, so the player may move on (PRD 3.2.7).</summary>
        public bool CurrentNodeCompleted { get; private set; }

        /// <summary>Everything that happened outside battle, in order.</summary>
        public IReadOnlyList<RunEvent> Events => _events;

        /// <summary>The record of every battle settled this run, in order, for the run log (PRD 3.15.1); kept across a resume.</summary>
        public IReadOnlyList<BattleRecord> Battles => _battles;

        /// <summary>The route taken: every node committed to, across Worlds, the entry nodes included (PRD 3.15.1); kept across a resume.</summary>
        public IReadOnlyList<string> Route => _route;

        /// <summary>A run as a save recorded it, or as <see cref="RunSetup.Start"/> builds it. The Imprints held register their battle effects; their acquisition effects are already in the stats.</summary>
        public Run(
            string seed,
            RunStats stats,
            IReadOnlyList<string> charms,
            IReadOnlyList<string> imprints,
            IReadOnlyList<string?> armorUpgrades,
            Binder binder,
            IReadOnlyList<string> difficultyModifiers,
            bool assist,
            RunContent content,
            int world = 1,
            string? currentNodeId = null,
            RunStatus status = RunStatus.InProgress,
            int battlesStarted = 0,
            IReadOnlyCollection<string>? visited = null,
            bool currentNodeCompleted = true,
            IReadOnlyList<BattleRecord>? battles = null,
            IReadOnlyList<string>? route = null,
            RewardOffer? pendingReward = null)
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                throw new ArgumentException("A run has a seed.", nameof(seed));
            }

            if (charms is null)
            {
                throw new ArgumentNullException(nameof(charms));
            }

            if (charms.Count > Tuning.CharmSlots)
            {
                throw new ArgumentException($"A run holds at most {Tuning.CharmSlots} Charms.", nameof(charms));
            }

            if (armorUpgrades is null)
            {
                throw new ArgumentNullException(nameof(armorUpgrades));
            }

            if (armorUpgrades.Count != Tuning.ArmorUpgradeSlots)
            {
                throw new ArgumentException($"A run has exactly {Tuning.ArmorUpgradeSlots} armor upgrade slots.", nameof(armorUpgrades));
            }

            if (world < 1 || world > Tuning.WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(world), $"World is 1 to {Tuning.WorldCount}.");
            }

            if (battlesStarted < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(battlesStarted), "Battles started must not be negative.");
            }

            Seed = seed;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Charms = new List<string>(charms);
            _imprints = new List<string>(imprints ?? throw new ArgumentNullException(nameof(imprints)));
            _armorUpgrades = new string?[Tuning.ArmorUpgradeSlots];
            for (int i = 0; i < _armorUpgrades.Length; i++)
            {
                _armorUpgrades[i] = armorUpgrades[i];
            }

            Binder = binder ?? throw new ArgumentNullException(nameof(binder));
            DifficultyModifiers = new List<string>(difficultyModifiers ?? throw new ArgumentNullException(nameof(difficultyModifiers)));
            Assist = assist;
            Status = status;
            _battlesStarted = battlesStarted;
            Rng = new Rng(seed);
            _imprintRng = Fork("imprints");
            _rewardRng = Fork("rewards");
            Effects = new RunEffects(Stats, Content);

            foreach (var charmId in Charms)
            {
                var charm = Content.FindCharm(charmId);
                if (charm != null)
                {
                    Effects.Add(charm.Id, charm.Effects, fireAcquired: false);
                }
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var imprintId in _imprints)
            {
                var imprint = Content.FindImprint(imprintId);
                if (imprint != null && (imprint.Stackable || seen.Add(imprint.Id)))
                {
                    Effects.Add(imprint.Id, imprint.Effects, fireAcquired: false);
                }
            }

            World = world;
            _maps[world] = GenerateMap(world);
            if (currentNodeId is null)
            {
                CurrentNodeId = CurrentMap.EntryId;
                _visited.Add(CurrentNodeId);
                CurrentNodeCompleted = true;
            }
            else
            {
                if (!CurrentMap.Contains(currentNodeId))
                {
                    throw new ArgumentException($"World {world} has no node '{currentNodeId}'.", nameof(currentNodeId));
                }

                CurrentNodeId = currentNodeId;
                if (visited != null)
                {
                    foreach (var id in visited)
                    {
                        if (CurrentMap.Contains(id))
                        {
                            _visited.Add(id);
                        }
                    }
                }

                _visited.Add(CurrentNodeId);
                CurrentNodeCompleted = currentNodeCompleted;
            }

            if (battles != null)
            {
                _battles.AddRange(battles);
            }

            if (route != null)
            {
                _route.AddRange(route);
            }

            if (_route.Count == 0)
            {
                _route.Add(CurrentNodeId);
            }

            if (pendingReward != null)
            {
                if (pendingReward.IsResolved)
                {
                    throw new ArgumentException("A restored reward offer must still be open.", nameof(pendingReward));
                }

                PendingReward = pendingReward;
                CurrentNodeCompleted = false;
            }
        }

        /// <summary>An independent generator for a subsystem ("map", "shop", "battle"), the same for the same seed and label (PRD 3.2.4).</summary>
        public Rng Fork(string label)
        {
            return Rng.Fork(label);
        }

        /// <summary>The definitions of the equipped Charms that the content knows.</summary>
        public IReadOnlyList<CharmDefinition> EquippedCharms
        {
            get
            {
                var charms = new List<CharmDefinition>();
                foreach (var id in Charms)
                {
                    var charm = Content.FindCharm(id);
                    if (charm != null)
                    {
                        charms.Add(charm);
                    }
                }

                return charms;
            }
        }

        /// <summary>The graph of a World, generated from the seed (PRD 3.2.4); the same for the same seed whenever it is asked for.</summary>
        public MapGraph MapOf(int world)
        {
            if (!_maps.TryGetValue(world, out var map))
            {
                _maps[world] = map = GenerateMap(world);
            }

            return map;
        }

        private MapGraph GenerateMap(int world)
        {
            return MapGenerator.Generate(Fork("map-w" + world), world, Content.Enemies.Enemies);
        }

        /// <summary>
        /// Commits to a node connected forward from the current one (PRD 3.2.7): the move is a
        /// node transition that adds CRP (PRD 3.8.2); a stop without content in this plan
        /// (Shop, Event, Blacksmith, Forge) counts as completed on arrival, a battle node waits
        /// for its battle to be won.
        /// </summary>
        public MoveResult MoveTo(string nodeId)
        {
            if (nodeId is null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            if (IsOver)
            {
                return MoveResult.RunOver;
            }

            if (!CurrentMap.Contains(nodeId) || !CurrentMap.IsForward(CurrentNodeId, nodeId))
            {
                return MoveResult.NotForward;
            }

            if (CurrentBattle != null)
            {
                return MoveResult.BattleInProgress;
            }

            if (PendingReward != null)
            {
                return MoveResult.RewardPending;
            }

            if (!CurrentNodeCompleted)
            {
                return MoveResult.NodeNotCompleted;
            }

            var from = CurrentNodeId;
            var to = CurrentMap[nodeId];
            CurrentNodeId = to.Id;
            _visited.Add(to.Id);
            _route.Add(to.Id);
            CurrentNodeCompleted = !to.IsBattle;
            _events.Add(new NodeTransition(World, from, to.Id, to.Type));
            ChangeCrp(Tuning.CrpPerTransition, CrpSources.NodeTransition);
            return MoveResult.Moved;
        }

        /// <summary>
        /// The current node's content is done (PRD 3.2.8–3.2.14): a Boss node completed advances
        /// to the next World's entry or, after World 3, wins the run (PRD 3.2.10, 3.2.1). The
        /// battle settle calls this for a won battle node; the other node owners call it when
        /// their stop resolves.
        /// </summary>
        public void CompleteNode()
        {
            RequireInProgress();
            if (CurrentBattle != null)
            {
                throw new InvalidOperationException("The battle has not been settled.");
            }

            if (PendingReward != null)
            {
                throw new InvalidOperationException("The reward offer has not been resolved.");
            }

            var node = CurrentNode;
            CurrentNodeCompleted = true;
            _events.Add(new NodeCompleted(World, node.Id, node.Type));
            if (node.Type == NodeType.Boss)
            {
                if (World < Tuning.WorldCount)
                {
                    EnterWorld(World + 1);
                }
                else
                {
                    End(RunStatus.Won);
                }
            }
        }

        private void EnterWorld(int world)
        {
            World = world;
            var map = MapOf(world);
            _visited.Clear();
            CurrentNodeId = map.EntryId;
            _visited.Add(CurrentNodeId);
            _route.Add(CurrentNodeId);
            CurrentNodeCompleted = true;
            _events.Add(new WorldEntered(world, map.EntryId));
        }

        /// <summary>Changes CRP by an amount from a named source (PRD 3.8.6); the value is clamped (PRD 3.8.1).</summary>
        public void ChangeCrp(int amount, string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("A CRP change names its source.", nameof(source));
            }

            Stats.Crp += amount;
            _events.Add(new CrpChanged(amount, source, Stats.Crp));
        }

        /// <summary>
        /// Acquires an Imprint rolled from the pool of the tier with the run's own stream
        /// (PRD 3.9.3): there is no slot limit; its acquisition effects change the stats now
        /// and its battle effects join every battle from here on; a stackable Imprint rolled
        /// again registers its effects again, a non-stackable one is held but acts once.
        /// </summary>
        public ImprintDefinition AcquireImprint(ImprintTier tier)
        {
            RequireInProgress();
            var pool = Content.Imprints.OfTier(tier);
            if (pool.Count == 0)
            {
                throw new InvalidOperationException($"The Imprint pool has no {tier} Imprint.");
            }

            var imprint = pool[_imprintRng.NextInt(0, pool.Count)];
            return Acquire(imprint);
        }

        /// <summary>Acquires a specific Imprint (an event's or a relationship's grant, PRD 3.10.6); the same rules as a rolled one.</summary>
        public ImprintDefinition AcquireImprint(string imprintId)
        {
            RequireInProgress();
            var imprint = Content.FindImprint(imprintId) ?? throw new ArgumentException($"Unknown Imprint '{imprintId}'.", nameof(imprintId));
            return Acquire(imprint);
        }

        private ImprintDefinition Acquire(ImprintDefinition imprint)
        {
            bool held = _imprints.Contains(imprint.Id);
            _imprints.Add(imprint.Id);
            if (imprint.Stackable || !held)
            {
                Effects.Add(imprint.Id, imprint.Effects, fireAcquired: true);
            }

            return imprint;
        }

        /// <summary>The enemy the current battle node rolled (PRD 3.2.8–3.2.10); throws when the node is not a battle or its enemy is missing from the content.</summary>
        public EnemyDefinition CurrentNodeEnemy
        {
            get
            {
                var node = CurrentNode;
                if (!node.IsBattle)
                {
                    throw new InvalidOperationException($"Node {node.Id} is a {node.Type}, not a battle.");
                }

                return (node.EnemyId is null ? null : Content.FindEnemy(node.EnemyId))
                    ?? throw new InvalidOperationException($"Node {node.Id} has no {NodeTypes.TierOf(node.Type)} enemy in the content.");
            }
        }

        /// <summary>Starts the current battle node's battle at the World's balance (PRD 3.2.8–3.2.10); winning it completes the node.</summary>
        public Battle StartNodeBattle()
        {
            return StartNodeBattle(EncounterBalance.ForWorld(World));
        }

        public Battle StartNodeBattle(EncounterBalance balance)
        {
            RequireNodeBattle();
            var battle = StartBattle(CurrentNodeEnemy, balance);
            _nodeBattle = true;
            return battle;
        }

        /// <summary>Starts the current battle node's battle with a fixed enemy HP, for fixtures and tests that pin the number.</summary>
        public Battle StartNodeBattle(int enemyHp)
        {
            RequireNodeBattle();
            var battle = StartBattle(CurrentNodeEnemy, enemyHp);
            _nodeBattle = true;
            return battle;
        }

        private void RequireNodeBattle()
        {
            RequireBattleFree();
            if (!CurrentNode.IsBattle || CurrentNodeCompleted)
            {
                throw new InvalidOperationException($"Node {CurrentNodeId} has no battle to start.");
            }
        }

        /// <summary>Starts a battle against the enemy at the run's World balance, reading cards from the loadout (PRD 3.5.1) with the Imprint and Charm effects attached (P18.4).</summary>
        public Battle StartBattle(EnemyDefinition enemy, EncounterBalance balance)
        {
            RequireBattleFree();
            return Begin(new Battle(Stats, enemy, balance, Loadout, NextBattleRng()));
        }

        /// <summary>Starts a battle with a fixed enemy HP in World 1, for fixtures and tests that pin the number.</summary>
        public Battle StartBattle(EnemyDefinition enemy, int enemyHp)
        {
            RequireBattleFree();
            return Begin(new Battle(Stats, enemy, enemyHp, Loadout, NextBattleRng()));
        }

        private Rng NextBattleRng()
        {
            _battlesStarted++;
            return Fork("battle-" + _battlesStarted);
        }

        private Battle Begin(Battle battle)
        {
            CurrentBattle = battle;
            _nodeBattle = false;
            Effects.AttachTo(battle);
            return battle;
        }

        /// <summary>
        /// Settles the battle the run started once it has ended: run-long modifiers are carried
        /// forward (PRD 3.9.8), the CRP changes it made join the run's stream with their source
        /// (PRD 3.8.6), Unstable cards count the battle (PRD 3.4.16), a won battle node is
        /// completed (PRD 3.2.8–3.2.10), and a death ends the run (PRD 3.3.9.3). Returns the
        /// cards the Binder destroyed.
        /// </summary>
        public IReadOnlyList<CardInstance> SettleBattle(Battle battle)
        {
            if (battle is null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (!ReferenceEquals(battle, CurrentBattle))
            {
                throw new InvalidOperationException("Only the battle this run started can be settled.");
            }

            if (battle.Outcome is null)
            {
                throw new InvalidOperationException("The battle has not ended.");
            }

            CurrentBattle = null;
            bool nodeBattle = _nodeBattle;
            _nodeBattle = false;
            Effects.Collect(battle);
            _battles.Add(BattleRecord.From(battle, _battles.Count == 0 ? null : _battles[_battles.Count - 1].EnemyId));
            foreach (var battleEvent in battle.Events)
            {
                if (battleEvent is StatChanged changed && changed.Stat == Chiki.Sim.Effects.RunStat.Crp)
                {
                    _events.Add(new CrpChanged(changed.Delta, changed.OwnerId ?? "battle", changed.Total));
                }
            }

            var destroyed = Binder.BattleEnded();
            if (battle.Outcome == BattleOutcome.Died)
            {
                End(RunStatus.Died);
            }
            else if (nodeBattle && !CurrentNodeCompleted && WonBy(battle))
            {
                OpenReward();
            }

            return destroyed;
        }

        /// <summary>Changes Essence by an amount from a named source (PRD 3.7.1); the value never goes below zero.</summary>
        public void ChangeEssence(int amount, string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("An Essence change names its source.", nameof(source));
            }

            Stats.Essence += amount;
            _events.Add(new EssenceChanged(amount, source, Stats.Essence));
        }

        /// <summary>Whether the battle's stream ended it with a win (PRD 3.3.9.2): the reward flow keys off that event.</summary>
        private static bool WonBy(Battle battle)
        {
            foreach (var battleEvent in battle.Events)
            {
                if (battleEvent is BattleEnded ended)
                {
                    return ended.Outcome == BattleOutcome.Won;
                }
            }

            return false;
        }

        /// <summary>
        /// The reward flow of a won battle node (PRD 3.2.8–3.2.10, 3.7.2–3.7.4): the Essence
        /// income of the tier and World is paid now (PRD 3.7.5), an Elite or Boss grants its
        /// Imprint now (PRD 3.9.3) and a Boss counts toward Charm unlocks (PRD 3.9.10), and the
        /// tier's cards are offered; the node completes when the player picks or skips
        /// (PRD 3.3.9.2). Runs once per won battle, from the settle.
        /// </summary>
        private void OpenReward()
        {
            var node = CurrentNode;
            var tier = NodeTypes.TierOf(node.Type);
            var offer = Rewards.Roll(_rewardRng, tier, Content.Cards.Values, World, node.Id);
            ChangeEssence(offer.Essence, EssenceSources.BattleReward);
            if (offer.ImprintTier is ImprintTier imprintTier)
            {
                offer.ImprintId = AcquireImprint(imprintTier).Id;
            }

            if (node.Type == NodeType.Boss)
            {
                _events.Add(new BossDefeated(World, node.Id, node.EnemyId ?? ""));
            }

            PendingReward = offer;
            var ids = new List<string>(offer.Cards.Count);
            foreach (var card in offer.Cards)
            {
                ids.Add(card.Id);
            }

            _events.Add(new RewardOffered(World, node.Id, offer.Tier, ids, offer.ImprintId));
        }

        /// <summary>Takes one card of the open offer into the Binder (PRD 3.7.2, 3.4.12) and resolves the offer, completing the node.</summary>
        public CardInstance PickReward(CardDefinition card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var offer = RequireOpenOffer();
            if (!offer.Offers(card))
            {
                throw new ArgumentException($"Card '{card.Id}' is not in the offer.", nameof(card));
            }

            var instance = Binder.Add(card);
            ResolveReward(offer, card);
            return instance;
        }

        /// <summary>Declines the open offer (PRD 3.7.2): no card is taken, the Essence stays, and the node completes.</summary>
        public void SkipReward()
        {
            ResolveReward(RequireOpenOffer(), null);
        }

        private RewardOffer RequireOpenOffer()
        {
            RequireInProgress();
            return PendingReward ?? throw new InvalidOperationException("No reward offer is open.");
        }

        private void ResolveReward(RewardOffer offer, CardDefinition? picked)
        {
            offer.Resolve(picked);
            PendingReward = null;
            _events.Add(new RewardResolved(World, offer.NodeId, picked?.Id));
            if (!CurrentNodeCompleted)
            {
                CompleteNode();
            }
        }

        /// <summary>
        /// Ends the run (PRD 3.9.11): the status is final and everything run-scoped is discarded
        /// (PRD 3.9.1, 3.5.4, 3.9.3): the Binder and its loadout, the Imprints, the effects and
        /// the until-reset modifiers. The stats stay readable for the run-end summary.
        /// </summary>
        public void End(RunStatus outcome)
        {
            if (outcome == RunStatus.InProgress)
            {
                throw new ArgumentException("A run ends by victory, death or abandonment.", nameof(outcome));
            }

            RequireInProgress();
            Status = outcome;
            CurrentBattle = null;
            _nodeBattle = false;
            PendingReward = null;
            Binder.Discard();
            _imprints.Clear();
            Effects.Clear();
            _events.Add(new RunEnded(outcome));
        }

        private void RequireInProgress()
        {
            if (IsOver)
            {
                throw new InvalidOperationException($"The run has ended ({Status}).");
            }
        }

        private void RequireBattleFree()
        {
            RequireInProgress();
            if (CurrentBattle != null)
            {
                throw new InvalidOperationException("The previous battle has not been settled.");
            }
        }
    }
}
