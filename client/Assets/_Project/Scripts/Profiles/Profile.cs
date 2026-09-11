#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Profiles
{
    /// <summary>
    /// One named profile (PRD 3.1.2, 4.1): everything permanent belongs to it and nothing is
    /// shared between profiles. <see cref="ProfileStore"/> writes it to JSON through the backing
    /// fields; the file carries a schema version so a later build can migrate it (P22.3). The
    /// containers are filled by later items: meta progression by P17.3, relationships by P18.6,
    /// the run in progress by P22.1, the run history by the run-end flow.
    /// </summary>
    [Serializable]
    public sealed class Profile
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string name = "";
        [SerializeField] private string created = "";
        [SerializeField] private string lastPlayed = "";
        [SerializeField] private MetaProgression meta = new MetaProgression();
        [SerializeField] private List<Relationship> relationships = new List<Relationship>();
        [SerializeField] private ProfileSettings settings = new ProfileSettings();
        [SerializeField] private int calibrationOffsetMs;
        [SerializeField] private bool calibrated;
        [SerializeField] private bool tutorialCompleted;
        [SerializeField] private string runInProgress = "";
        [SerializeField] private List<RunHistoryEntry> runHistory = new List<RunHistoryEntry>();
        [SerializeField] private string runLogFolder = "";

        /// <summary>The version of the file layout this profile was written with.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Unique on the install (PRD 3.1.1); also the profile's folder name.</summary>
        public string Name
        {
            get => name;
            internal set => name = value;
        }

        /// <summary>ISO 8601 UTC timestamp of creation (PRD 4.1).</summary>
        public string Created
        {
            get => created;
            internal set => created = value;
        }

        /// <summary>ISO 8601 UTC timestamp of the last launch with this profile (PRD 4.1).</summary>
        public string LastPlayed
        {
            get => lastPlayed;
            set => lastPlayed = value;
        }

        /// <summary>Charm, card, Imprint-pool, difficulty-modifier and cosmetic unlocks (PRD 3.9.2); filled by P17.3.</summary>
        public MetaProgression Meta => meta;

        /// <summary>One entry per NPC (PRD 4.12), all five present from creation (P18.6); the rules live in <see cref="Chiki.Sim.Relationship"/>.</summary>
        public List<Relationship> Relationships => relationships;

        /// <summary>The record for one NPC; the store guarantees all five exist.</summary>
        public Relationship RelationshipWith(Npc npc)
        {
            if (npc is null)
            {
                throw new ArgumentNullException(nameof(npc));
            }

            EnsureRelationships();
            return relationships.Find(r => r.NpcId == npc.Id)!;
        }

        /// <summary>Adds a fresh level-1 record for every NPC the profile lacks (PRD 3.10.3); a file from before P18.6 gains them on load.</summary>
        public void EnsureRelationships()
        {
            foreach (var npc in Npc.All)
            {
                if (!relationships.Exists(r => r.NpcId == npc.Id))
                {
                    relationships.Add(Relationship.FromSim(new Chiki.Sim.Relationship(npc)));
                }
            }
        }

        /// <summary>Grants RP to an NPC through the simulation's rules (PRD 3.10.4, 3.10.5) and records the result; returns the levels gained. The caller saves the profile.</summary>
        public int GrantRp(Npc npc, int amount, RpSource source)
        {
            var record = RelationshipWith(npc);
            var relationship = record.ToSim();
            int gained = relationship.GrantRp(amount, source);
            record.CopyFrom(relationship);
            return gained;
        }

        /// <summary>Settings (PRD 3.12.2 to 3.12.6).</summary>
        public ProfileSettings Settings => settings;

        /// <summary>The latency offset measured by the calibration screen, in milliseconds (PRD 3.12.1); positive when the player's taps land late.</summary>
        public int CalibrationOffsetMs
        {
            get => calibrationOffsetMs;
            set => calibrationOffsetMs = value;
        }

        /// <summary>Whether the calibration screen has run to its end on this profile; false forces it before the first battle (PRD 3.12.1).</summary>
        public bool Calibrated
        {
            get => calibrated;
            set => calibrated = value;
        }

        /// <summary>Tutorial completion (PRD 3.13.3).</summary>
        public bool TutorialCompleted
        {
            get => tutorialCompleted;
            set => tutorialCompleted = value;
        }

        /// <summary>The serialised run in progress, or null when none (PRD 3.1.5); written by P22.1.</summary>
        public string? RunInProgress
        {
            get => string.IsNullOrEmpty(runInProgress) ? null : runInProgress;
            set => runInProgress = value ?? "";
        }

        /// <summary>Outcomes and seeds of past runs (PRD 3.9.11).</summary>
        public List<RunHistoryEntry> RunHistory => runHistory;

        /// <summary>The folder the profile's run logs are written to (PRD 3.15.1); set by the store beside the profile file.</summary>
        public string RunLogFolder
        {
            get => runLogFolder;
            internal set => runLogFolder = value;
        }
    }

    /// <summary>The meta progression container (PRD 3.9.2): sets of ids, kept as lists for the file.</summary>
    [Serializable]
    public sealed class MetaProgression
    {
        [SerializeField] private List<string> charmUnlocks = new List<string>();
        [SerializeField] private List<string> cardUnlocks = new List<string>();
        [SerializeField] private List<string> imprintPoolUnlocks = new List<string>();
        [SerializeField] private List<string> difficultyModifiers = new List<string>();
        [SerializeField] private List<string> cosmetics = new List<string>();

        public List<string> CharmUnlocks => charmUnlocks;
        public List<string> CardUnlocks => cardUnlocks;
        public List<string> ImprintPoolUnlocks => imprintPoolUnlocks;
        public List<string> DifficultyModifiers => difficultyModifiers;
        public List<string> Cosmetics => cosmetics;

        /// <summary>Grants a Charm permanently (PRD 3.9.5); false when it was already unlocked.</summary>
        public bool UnlockCharm(string charmId) => Unlock(charmUnlocks, charmId);

        public bool HasCharm(string charmId) => charmUnlocks.Contains(charmId);

        /// <summary>Adds a card to the Global Binder (PRD 3.5.9); false when it was already there.</summary>
        public bool UnlockCard(string cardId) => Unlock(cardUnlocks, cardId);

        public bool HasCard(string cardId) => cardUnlocks.Contains(cardId);

        /// <summary>Adds an Imprint to the pool (PRD 3.9.2); false when it was already there.</summary>
        public bool UnlockImprint(string imprintId) => Unlock(imprintPoolUnlocks, imprintId);

        public bool HasImprint(string imprintId) => imprintPoolUnlocks.Contains(imprintId);

        private static bool Unlock(List<string> ids, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An id is required.", nameof(id));
            }

            if (ids.Contains(id))
            {
                return false;
            }

            ids.Add(id);
            return true;
        }
    }

    /// <summary>The relationship with one NPC as the file records it (PRD 4.12): level, RP toward the next level, the unlocks granted and the last source. Rules live in <see cref="Chiki.Sim.Relationship"/>.</summary>
    [Serializable]
    public sealed class Relationship
    {
        [SerializeField] private string npcId = "";
        [SerializeField] private int level = Tuning.NpcStartLevel;
        [SerializeField] private int points;
        [SerializeField] private List<string> unlocksGranted = new List<string>();
        [SerializeField] private string lastSource = "";

        public Relationship()
        {
        }

        public Relationship(string npcId, int level, int points)
        {
            this.npcId = npcId;
            this.level = level;
            this.points = points;
        }

        public static Relationship FromSim(Chiki.Sim.Relationship relationship)
        {
            var record = new Relationship(relationship.Npc.Id, relationship.Level, relationship.RpTowardNext);
            record.CopyFrom(relationship);
            return record;
        }

        /// <summary>The record as a simulation relationship with the rules attached; an unknown NPC id is refused.</summary>
        public Chiki.Sim.Relationship ToSim()
        {
            var npc = Npc.Find(npcId) ?? throw new InvalidOperationException("Unknown NPC '" + npcId + "'.");
            return new Chiki.Sim.Relationship(npc, level, points, unlocksGranted, string.IsNullOrEmpty(lastSource) ? (RpSource?)null : RpSources.FromId(lastSource));
        }

        public void CopyFrom(Chiki.Sim.Relationship relationship)
        {
            npcId = relationship.Npc.Id;
            level = relationship.Level;
            points = relationship.RpTowardNext;
            unlocksGranted = new List<string>(relationship.UnlocksGranted);
            lastSource = relationship.LastSource is RpSource source ? RpSources.ToId(source) : "";
        }

        public string NpcId => npcId;

        public List<string> UnlocksGranted => unlocksGranted;

        /// <summary>The id of the last RP source (PRD 3.10.4); empty before any gain.</summary>
        public string LastSource => lastSource;

        public int Level
        {
            get => level;
            set => level = value;
        }

        public int Points
        {
            get => points;
            set => points = value;
        }
    }

    /// <summary>The settings a profile owns (PRD 3.12.2 to 3.12.6). Volumes are whole percentages.</summary>
    [Serializable]
    public sealed class ProfileSettings
    {
        [SerializeField] private bool metronomeOn;
        [SerializeField] private int musicVolume = 100;
        [SerializeField] private int sfxVolume = 100;
        [SerializeField] private int metronomeVolume = 100;
        [SerializeField] private bool assistMode;
        [SerializeField] private bool fullscreen = true;
        [SerializeField] private bool verticalSync = true;

        /// <summary>The metronome toggle (PRD 3.12.2).</summary>
        public bool MetronomeOn
        {
            get => metronomeOn;
            set => metronomeOn = value;
        }

        public int MusicVolume
        {
            get => musicVolume;
            set => musicVolume = value;
        }

        public int SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = value;
        }

        public int MetronomeVolume
        {
            get => metronomeVolume;
            set => metronomeVolume = value;
        }

        public bool AssistMode
        {
            get => assistMode;
            set => assistMode = value;
        }

        public bool Fullscreen
        {
            get => fullscreen;
            set => fullscreen = value;
        }

        public bool VerticalSync
        {
            get => verticalSync;
            set => verticalSync = value;
        }
    }

    /// <summary>One past run (PRD 3.9.11): its seed, how it ended and when.</summary>
    [Serializable]
    public sealed class RunHistoryEntry
    {
        [SerializeField] private string seed = "";
        [SerializeField] private string outcome = "";
        [SerializeField] private string endedAt = "";

        public RunHistoryEntry()
        {
        }

        public RunHistoryEntry(string seed, string outcome, string endedAt)
        {
            this.seed = seed;
            this.outcome = outcome;
            this.endedAt = endedAt;
        }

        public string Seed => seed;
        public string Outcome => outcome;
        public string EndedAt => endedAt;
    }
}
