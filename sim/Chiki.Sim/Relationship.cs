using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>The five NPCs who recur across runs (PRD 3.10.1): main characters level 1–10, secondary 1–5 (PRD 3.10.3).</summary>
    public sealed record Npc(string Id, string Name, bool Main)
    {
        public int MaxLevel => Main ? Tuning.MainNpcMaxLevel : Tuning.SecondaryNpcMaxLevel;

        public static readonly Npc Fisherman = new Npc("fisherman", "the Fisherman", true);
        public static readonly Npc FlowerGirl = new Npc("flower-girl", "the Flower Girl", true);
        public static readonly Npc Gambler = new Npc("gambler", "the Gambler", true);
        public static readonly Npc Bjorn = new Npc("bjorn", "Björn the Blacksmith", false);
        public static readonly Npc Gero = new Npc("gero", "Gero the Shopkeeper", false);

        /// <summary>All five, in the PRD's order.</summary>
        public static readonly IReadOnlyList<Npc> All = new[] { Fisherman, FlowerGirl, Gambler, Bjorn, Gero };

        public static Npc? Find(string id)
        {
            foreach (var npc in All)
            {
                if (npc.Id == id)
                {
                    return npc;
                }
            }

            return null;
        }
    }

    /// <summary>Where RP came from (PRD 3.10.4).</summary>
    public enum RpSource
    {
        Dialogue,
        Minigame,
        Sacrifice,
        Interaction,
    }

    public static class RpSources
    {
        public static string ToId(RpSource source)
        {
            switch (source)
            {
                case RpSource.Dialogue: return "dialogue";
                case RpSource.Minigame: return "minigame";
                case RpSource.Sacrifice: return "sacrifice";
                case RpSource.Interaction: return "interaction";
                default: throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown RP source.");
            }
        }

        public static RpSource FromId(string id)
        {
            switch (id)
            {
                case "dialogue": return RpSource.Dialogue;
                case "minigame": return RpSource.Minigame;
                case "sacrifice": return RpSource.Sacrifice;
                case "interaction": return RpSource.Interaction;
                default: throw new ArgumentException($"Unknown RP source '{id}'.", nameof(id));
            }
        }
    }

    /// <summary>
    /// The relationship with one NPC (PRD 4.12, 3.10.3–3.10.6): its level, the RP toward the
    /// next level, the unlocks its levels granted and the source of the last gain. Level N to
    /// N+1 costs N+1 RP (PRD 3.10.5); levels cap at the NPC's maximum, where further RP is
    /// kept toward nothing. The profile owns one per NPC and it persists across runs.
    /// </summary>
    public sealed class Relationship
    {
        private readonly List<string> _unlocksGranted;

        public Npc Npc { get; }

        public int Level { get; private set; }

        /// <summary>RP toward the next level (PRD 3.10.4); stays below the cost while below the cap.</summary>
        public int RpTowardNext { get; private set; }

        /// <summary>The unlock ids the levels reached have granted, one per level (PRD 3.10.6); filled by the content that grants them.</summary>
        public IReadOnlyList<string> UnlocksGranted => _unlocksGranted;

        /// <summary>Where the last RP came from; null before any gain.</summary>
        public RpSource? LastSource { get; private set; }

        /// <summary>The RP the next level costs (PRD 3.10.5); null at the cap.</summary>
        public int? NextLevelCost => Level >= Npc.MaxLevel ? (int?)null : Level + 1;

        public bool AtCap => Level >= Npc.MaxLevel;

        public Relationship(Npc npc) : this(npc, Tuning.NpcStartLevel, 0, Array.Empty<string>(), null)
        {
        }

        /// <summary>A relationship as the profile recorded it.</summary>
        public Relationship(Npc npc, int level, int rpTowardNext, IReadOnlyList<string> unlocksGranted, RpSource? lastSource)
        {
            Npc = npc ?? throw new ArgumentNullException(nameof(npc));
            if (level < Tuning.NpcStartLevel || level > npc.MaxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), $"{npc.Name} has levels {Tuning.NpcStartLevel} to {npc.MaxLevel}.");
            }

            if (rpTowardNext < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rpTowardNext), "RP must not be negative.");
            }

            Level = level;
            RpTowardNext = rpTowardNext;
            _unlocksGranted = new List<string>(unlocksGranted ?? throw new ArgumentNullException(nameof(unlocksGranted)));
            LastSource = lastSource;
        }

        /// <summary>Adds RP from a source (PRD 3.10.4) and levels up as often as the cost is met (PRD 3.10.5); returns the levels gained.</summary>
        public int GrantRp(int amount, RpSource source)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "RP granted must not be negative.");
            }

            LastSource = source;
            RpTowardNext += amount;
            int gained = 0;
            while (NextLevelCost is int cost && RpTowardNext >= cost)
            {
                RpTowardNext -= cost;
                Level++;
                gained++;
            }

            return gained;
        }

        /// <summary>Records the unlock a level granted (PRD 3.10.6).</summary>
        public void RecordUnlock(string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId))
            {
                throw new ArgumentException("An unlock id is required.", nameof(unlockId));
            }

            _unlocksGranted.Add(unlockId);
        }
    }

    /// <summary>One RP grant as recorded (PRD 3.10.4): the NPC, the amount and its source.</summary>
    public sealed record RpGrant(string NpcId, int Amount, RpSource Source);

    /// <summary>
    /// The profile's relationships (PRD 3.10.3): one per NPC, all five present from the first
    /// launch at level 1 with 0 RP, and the record of every grant. <see cref="GrantRp"/> is the
    /// one entry point for RP; no in-scope feature calls it in play yet (P18.8).
    /// </summary>
    public sealed class Relationships
    {
        private readonly Dictionary<string, Relationship> _byNpc = new Dictionary<string, Relationship>(StringComparer.Ordinal);
        private readonly List<RpGrant> _grants = new List<RpGrant>();

        /// <summary>All five relationships in the PRD's order.</summary>
        public IReadOnlyList<Relationship> All
        {
            get
            {
                var all = new List<Relationship>();
                foreach (var npc in Npc.All)
                {
                    all.Add(_byNpc[npc.Id]);
                }

                return all;
            }
        }

        /// <summary>Every grant recorded, in order.</summary>
        public IReadOnlyList<RpGrant> Grants => _grants;

        /// <summary>A fresh profile's relationships: every NPC at level 1 with 0 RP.</summary>
        public Relationships()
        {
            foreach (var npc in Npc.All)
            {
                _byNpc[npc.Id] = new Relationship(npc);
            }
        }

        /// <summary>Relationships as the profile recorded them; an NPC missing from the record starts fresh.</summary>
        public Relationships(IEnumerable<Relationship> recorded) : this()
        {
            if (recorded is null)
            {
                throw new ArgumentNullException(nameof(recorded));
            }

            foreach (var relationship in recorded)
            {
                _byNpc[relationship.Npc.Id] = relationship;
            }
        }

        public Relationship this[Npc npc] => _byNpc[(npc ?? throw new ArgumentNullException(nameof(npc))).Id];

        public Relationship Of(string npcId)
        {
            return _byNpc.TryGetValue(npcId, out var relationship)
                ? relationship
                : throw new ArgumentException($"Unknown NPC '{npcId}'.", nameof(npcId));
        }

        /// <summary>Grants RP to an NPC from a source and records it (PRD 3.10.4); returns the levels gained.</summary>
        public int GrantRp(Npc npc, int amount, RpSource source)
        {
            var relationship = this[npc];
            _grants.Add(new RpGrant(npc.Id, amount, source));
            return relationship.GrantRp(amount, source);
        }

        public int GrantRp(string npcId, int amount, RpSource source)
        {
            return GrantRp(Npc.Find(npcId) ?? throw new ArgumentException($"Unknown NPC '{npcId}'.", nameof(npcId)), amount, source);
        }
    }
}
