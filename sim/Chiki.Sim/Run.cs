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
    /// (PRD 3.2.4, 6.8). The map graphs join in P19.1.
    /// </summary>
    public sealed class Run
    {
        private readonly List<string> _imprints;
        private readonly string?[] _armorUpgrades;

        /// <summary>The run seed, custom or generated (PRD 3.2.4); shown on the map and the run-end screen.</summary>
        public string Seed { get; }

        /// <summary>The World being played, 1 to <see cref="Tuning.WorldCount"/> (PRD 3.2.1).</summary>
        public int World { get; private set; }

        /// <summary>The node the player stands on; null until the map places them (P19.4).</summary>
        public string? CurrentNodeId { get; private set; }

        public RunStats Stats { get; }

        /// <summary>The ids of the 0–2 Charms equipped pre-run, fixed for the run (PRD 3.9.6).</summary>
        public IReadOnlyList<string> Charms { get; }

        /// <summary>The ids of the Imprints held, in acquisition order (PRD 3.9.3); filled by P18.3.</summary>
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

        /// <summary>The root generator of the seed (PRD 3.2.4); subsystems draw from <see cref="Fork"/>, never from this directly.</summary>
        public Rng Rng { get; }

        /// <summary>A run as a save recorded it, or as <see cref="RunSetup.Start"/> builds it.</summary>
        public Run(
            string seed,
            RunStats stats,
            IReadOnlyList<string> charms,
            IReadOnlyList<string> imprints,
            IReadOnlyList<string?> armorUpgrades,
            Binder binder,
            IReadOnlyList<string> difficultyModifiers,
            bool assist,
            int world = 1,
            string? currentNodeId = null,
            RunStatus status = RunStatus.InProgress)
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

            Seed = seed;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
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
            Rng = new Rng(seed);
        }

        /// <summary>An independent generator for a subsystem ("map", "shop", "battle"), the same for the same seed and label (PRD 3.2.4).</summary>
        public Rng Fork(string label)
        {
            return Rng.Fork(label);
        }
    }
}
