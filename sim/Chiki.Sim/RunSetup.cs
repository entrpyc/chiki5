using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>The outcome of equipping a Charm pre-run (PRD 3.9.6).</summary>
    public enum EquipResult
    {
        Equipped,

        /// <summary>Both Charm slots are taken.</summary>
        SlotsFull,

        /// <summary>The Charm is not in the profile's unlocked set (PRD 3.9.5).</summary>
        NotUnlocked,

        AlreadyEquipped,

        /// <summary>The run has started; the choice is final (PRD 3.9.6).</summary>
        RunStarted,
    }

    /// <summary>
    /// The pre-run choices (PRD 3.2.2, 3.9.6, 3.2.4): 0–2 Charms from the profile's unlocked
    /// set and an optional custom seed. <see cref="Start"/> builds the <see cref="Run"/> in its
    /// start state and fixes the choices; the setup refuses every change afterwards.
    /// </summary>
    public sealed class RunSetup
    {
        private const string SeedAlphabet = "abcdefghjkmnpqrstuvwxyz23456789";
        private const int SeedGroupLength = 4;

        private readonly HashSet<string> _unlocked;
        private readonly List<string> _charms = new List<string>();

        /// <summary>The Charm ids the profile has unlocked (PRD 3.9.2).</summary>
        public IReadOnlyCollection<string> Unlocked => _unlocked;

        /// <summary>The Charms equipped so far, in slot order.</summary>
        public IReadOnlyList<string> Charms => _charms;

        public bool Started { get; private set; }

        public RunSetup(IEnumerable<string> unlockedCharmIds)
        {
            if (unlockedCharmIds is null)
            {
                throw new ArgumentNullException(nameof(unlockedCharmIds));
            }

            _unlocked = new HashSet<string>(unlockedCharmIds, StringComparer.Ordinal);
        }

        /// <summary>Equips an unlocked Charm into the next free slot (PRD 3.9.6).</summary>
        public EquipResult Equip(string charmId)
        {
            if (string.IsNullOrWhiteSpace(charmId))
            {
                throw new ArgumentException("A Charm id is required.", nameof(charmId));
            }

            if (Started)
            {
                return EquipResult.RunStarted;
            }

            if (!_unlocked.Contains(charmId))
            {
                return EquipResult.NotUnlocked;
            }

            if (_charms.Contains(charmId))
            {
                return EquipResult.AlreadyEquipped;
            }

            if (_charms.Count >= Tuning.CharmSlots)
            {
                return EquipResult.SlotsFull;
            }

            _charms.Add(charmId);
            return EquipResult.Equipped;
        }

        /// <summary>Removes an equipped Charm; false when it was not equipped or the run has started.</summary>
        public bool Unequip(string charmId)
        {
            return !Started && _charms.Remove(charmId);
        }

        /// <summary>
        /// Starts the run (PRD 3.2.2): the starter Binder auto-filled into both lines, the
        /// equipped Charms, zero Imprints, ARD at maximum, Base DMG 0, Essence 0, CRP 0. A blank
        /// <paramref name="seed"/> is replaced by one generated from <paramref name="entropy"/>,
        /// which the caller takes from outside the simulation (PRD 3.2.4).
        /// </summary>
        public Run Start(CardSet starterSet, string? seed = null, ulong entropy = 0)
        {
            return Start(new RunContent(starterSet ?? throw new ArgumentNullException(nameof(starterSet))), seed, entropy);
        }

        /// <summary>Starts the run on the full content it draws on: starter set, Charm table and Imprint pool (see the starter-set overload).</summary>
        public Run Start(RunContent content, string? seed = null, ulong entropy = 0)
        {
            if (Started)
            {
                throw new InvalidOperationException("The run has already started.");
            }

            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var starterSet = content.Starter;
            var binder = Binder.Starter(starterSet);
            var stillEmpty = binder.AutoFill();
            if (stillEmpty.Count > 0)
            {
                throw new ArgumentException("The starter set cannot fill both lines; empty: " + Loadout.Describe(stillEmpty) + ".", nameof(starterSet));
            }

            string runSeed = string.IsNullOrWhiteSpace(seed) ? GenerateSeed(entropy) : seed!.Trim();
            Started = true;
            return new Run(
                runSeed,
                new RunStats(),
                _charms,
                Array.Empty<string>(),
                new string?[Tuning.ArmorUpgradeSlots],
                binder,
                Array.Empty<string>(),
                assist: false,
                content: content);
        }

        /// <summary>A shareable seed of two four-character groups drawn from the entropy (PRD 3.2.4); the same entropy gives the same seed.</summary>
        public static string GenerateSeed(ulong entropy)
        {
            var rng = new Rng(entropy);
            var chars = new char[SeedGroupLength * 2 + 1];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = i == SeedGroupLength ? '-' : SeedAlphabet[rng.NextInt(0, SeedAlphabet.Length)];
            }

            return new string(chars);
        }
    }
}
