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

    /// <summary>
    /// One run (PRD 4.2, 3.2.2): its seed, World and node, run-wide stats, the Charms equipped
    /// pre-run, the Imprints held, the armor upgrade slots, the Binder with its loadout, the
    /// difficulty modifiers and Assist flag, and its status. Created through
    /// <see cref="RunSetup.Start"/>; restored from a save through
    /// <see cref="Data.RunSerializer"/>. Every subsystem's random stream is a fork of the seed
    /// (PRD 3.2.4, 6.8). The run starts and settles its battles, attaching the effects of its
    /// Imprints and Charms to each (P18.3, P18.4), and ends by victory, death or abandonment,
    /// discarding everything run-scoped (PRD 3.9.1). The map graphs join in P19.1.
    /// </summary>
    public sealed class Run
    {
        private readonly List<string> _imprints;
        private readonly string?[] _armorUpgrades;
        private readonly Rng _imprintRng;
        private int _battlesStarted;

        /// <summary>The run seed, custom or generated (PRD 3.2.4); shown on the map and the run-end screen.</summary>
        public string Seed { get; }

        /// <summary>The World being played, 1 to <see cref="Tuning.WorldCount"/> (PRD 3.2.1).</summary>
        public int World { get; private set; }

        /// <summary>The node the player stands on; null until the map places them (P19.4).</summary>
        public string? CurrentNodeId { get; private set; }

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

        /// <summary>Battles started this run, the label of each battle's random stream.</summary>
        public int BattlesStarted => _battlesStarted;

        /// <summary>The root generator of the seed (PRD 3.2.4); subsystems draw from <see cref="Fork"/>, never from this directly.</summary>
        public Rng Rng { get; }

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
            int battlesStarted = 0)
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
            World = world;
            CurrentNodeId = currentNodeId;
            Status = status;
            _battlesStarted = battlesStarted;
            Rng = new Rng(seed);
            _imprintRng = Fork("imprints");
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
            Effects.AttachTo(battle);
            return battle;
        }

        /// <summary>
        /// Settles the battle the run started once it has ended: run-long modifiers are carried
        /// forward (PRD 3.9.8), Unstable cards count the battle (PRD 3.4.16), and a death ends
        /// the run (PRD 3.3.9.3). Returns the cards the Binder destroyed.
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
            Effects.Collect(battle);
            var destroyed = Binder.BattleEnded();
            if (battle.Outcome == BattleOutcome.Died)
            {
                End(RunStatus.Died);
            }

            return destroyed;
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
            Binder.Discard();
            _imprints.Clear();
            Effects.Clear();
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
