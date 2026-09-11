using System;
using System.Collections.Generic;
using System.IO;

namespace Chiki.Sim.Data
{
    /// <summary>A save written by a newer build than this one; it is left untouched (P22.3).</summary>
    public sealed class SaveVersionException : Exception
    {
        public SaveVersionException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Writes a <see cref="Run"/> to JSON and reads it back (PRD 4.2, 3.1.5). The document
    /// carries <c>schemaVersion</c>; an older version is brought up to date by the migration
    /// table before it is read (P22.3), and a newer version than this build knows is refused
    /// before anything is touched. Card instances name their definition by id, so reading needs
    /// the definitions the run draws on. Battle state is never written (PRD 3.1.6).
    /// </summary>
    public static class RunSerializer
    {
        /// <summary>Version 2 added the route, the battle records and the open reward offer (P22.3).</summary>
        public const int SchemaVersion = 2;

        /// <summary>One step per past version: brings a document of that version to the next one by adding the fields it lacks with their defaults.</summary>
        private static readonly Dictionary<int, Action<JsonValue>> Migrations = new Dictionary<int, Action<JsonValue>>
        {
            [1] = root =>
            {
                root.Set("route", JsonValue.EmptyArray());
                root.Set("battles", JsonValue.EmptyArray());
                root.Set("pendingReward", JsonValue.Null);
            },
        };

        public static string ToJson(Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var w = new JsonWriter();
            w.BeginObject();
            w.Member("schemaVersion", SchemaVersion);
            w.Member("seed", run.Seed);
            w.Member("world", run.World);
            w.Member("currentNode", run.CurrentNodeId);
            w.Member("status", StatusToId(run.Status));
            w.Member("battlesStarted", run.BattlesStarted);
            w.Member("visited", run.Visited);
            w.Member("currentNodeCompleted", run.CurrentNodeCompleted);
            w.Member("route", run.Route);
            w.Name("stats").BeginObject();
            w.Member("maxArd", run.Stats.MaxArd);
            w.Member("ard", run.Stats.Ard);
            w.Member("baseDmg", run.Stats.BaseDmg);
            w.Member("essence", run.Stats.Essence);
            w.Member("crp", run.Stats.Crp);
            w.EndObject();
            w.Member("charms", run.Charms);
            w.Member("imprints", run.Imprints);
            w.Member("armorUpgrades", run.ArmorUpgrades);
            w.Member("difficultyModifiers", run.DifficultyModifiers);
            w.Member("assist", run.Assist);
            w.Name("binder").BeginObject();
            w.Member("nextId", run.Binder.NextId);
            w.Name("cards").BeginArray();
            foreach (var card in run.Binder.Cards)
            {
                w.BeginObject();
                w.Member("id", card.Id);
                w.Member("definition", card.Definition.Id);
                w.Member("upgraded", card.Upgraded);
                w.Member("trait", card.TraitId);
                w.Member("battlesRemaining", card.BattlesRemaining);
                w.Member("shopPrice", card.ShopPrice);
                w.EndObject();
            }

            w.EndArray();
            w.EndObject();
            w.Name("loadout").BeginArray();
            foreach (var slot in run.Loadout.Slots)
            {
                var card = run.Loadout[slot];
                if (card != null)
                {
                    w.BeginObject();
                    w.Member("line", slot.Line);
                    w.Member("key", KeyToId(slot.Key));
                    w.Member("card", card.Id);
                    w.EndObject();
                }
            }

            w.EndArray();
            w.Name("battles").BeginArray();
            foreach (var battle in run.Battles)
            {
                WriteBattle(w, battle);
            }

            w.EndArray();
            w.Name("pendingReward");
            if (run.PendingReward is RewardOffer offer)
            {
                w.BeginObject();
                w.Member("tier", TierToId(offer.Tier));
                w.Member("node", offer.NodeId);
                var cardIds = new List<string>();
                foreach (var card in offer.Cards)
                {
                    cardIds.Add(card.Id);
                }

                w.Member("cards", cardIds);
                w.Member("essence", offer.Essence);
                w.Member("imprintTier", offer.ImprintTier is ImprintTier tier ? ImprintTierToId(tier) : null);
                w.Member("imprintId", offer.ImprintId);
                w.EndObject();
            }
            else
            {
                w.Null();
            }

            w.EndObject();
            return w.ToString();
        }

        /// <summary>A battle record as the save and the run log write it (PRD 3.15.1, 3.15.2).</summary>
        public static void WriteBattle(JsonWriter w, BattleRecord battle)
        {
            w.BeginObject();
            w.Member("enemy", battle.EnemyId);
            w.Member("durationBeats", battle.DurationBeats);
            w.Member("durationSeconds", battle.DurationSeconds);
            w.Member("durationMs", battle.DurationMs);
            w.Name("judgments").BeginObject();
            w.Member("perfect", battle.Perfects);
            w.Member("good", battle.Goods);
            w.Member("miss", battle.Misses);
            w.Member("noInput", battle.NoInputs);
            w.EndObject();
            w.Member("damageTaken", battle.DamageTaken);
            w.Member("signaturesFired", battle.SignaturesFired);
            w.Name("cardsPerSlot").BeginObject();
            foreach (var pair in battle.CardsPerSlot)
            {
                w.Member(pair.Key, pair.Value);
            }

            w.EndObject();
            w.Member("outcome", OutcomeToId(battle.Outcome));
            w.Member("sameEnemyAsPrevious", battle.SameEnemyAsPrevious);
            w.EndObject();
        }

        public static BattleRecord ReadBattle(JsonValue json)
        {
            var judgments = json["judgments"];
            var perSlot = new Dictionary<string, int>(StringComparer.Ordinal);
            var slots = json["cardsPerSlot"];
            foreach (var name in slots.MemberNames)
            {
                perSlot[name] = slots[name].AsInt();
            }

            return new BattleRecord(
                json["enemy"].AsString(),
                json["durationBeats"].AsInt(),
                json["durationMs"].AsInt(),
                judgments["perfect"].AsInt(),
                judgments["good"].AsInt(),
                judgments["miss"].AsInt(),
                judgments["noInput"].AsInt(),
                json["damageTaken"].AsInt(),
                json["signaturesFired"].AsInt(),
                perSlot,
                OutcomeFromId(json["outcome"].AsString()),
                json["sameEnemyAsPrevious"].AsBool());
        }

        /// <summary>
        /// The document brought up to this build's schema: each past version's step runs in
        /// order and the version is restamped. A newer document is refused and returned
        /// nowhere; a current one comes back unchanged in content.
        /// </summary>
        public static string Migrate(string json)
        {
            var root = JsonValue.Parse(json);
            MigrateInPlace(root);
            return root.ToJson();
        }

        private static void MigrateInPlace(JsonValue root)
        {
            int version = root["schemaVersion"].AsInt();
            if (version > SchemaVersion)
            {
                throw new SaveVersionException($"The run was saved by a newer build (schema {version}, this build reads up to {SchemaVersion}).");
            }

            for (int from = version; from < SchemaVersion; from++)
            {
                if (!Migrations.TryGetValue(from, out var step))
                {
                    throw new SaveVersionException($"No migration from run schema {from} to {from + 1}.");
                }

                step(root);
            }

            root.Set("schemaVersion", JsonValue.Of(SchemaVersion));
        }

        /// <summary>Reads a run whose card instances draw on the starter set alone and that holds no Charm or Imprint content.</summary>
        public static Run FromJson(string json, CardSet starter)
        {
            return FromJson(json, new RunContent(starter ?? throw new ArgumentNullException(nameof(starter))));
        }

        /// <summary>Reads the run file at the path against the content it draws on; a file of a newer schema is refused and left untouched.</summary>
        public static Run FromFile(string path, RunContent content)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A path is required.", nameof(path));
            }

            return FromJson(File.ReadAllText(path), content);
        }

        /// <summary>Reads a run against the content it draws on, migrating an older document first; an unknown card definition id is refused.</summary>
        public static Run FromJson(string json, RunContent content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var definitions = content.Cards;

            var root = JsonValue.Parse(json);
            MigrateInPlace(root);

            var statsJson = root["stats"];
            var stats = new RunStats(
                statsJson["maxArd"].AsInt(),
                statsJson["ard"].AsInt(),
                statsJson["baseDmg"].AsInt(),
                statsJson["essence"].AsInt(),
                statsJson["crp"].AsInt());

            var binderJson = root["binder"];
            var cards = new List<CardInstance>();
            foreach (var cardJson in binderJson["cards"].Items)
            {
                string definitionId = cardJson["definition"].AsString();
                if (!definitions.TryGetValue(definitionId, out var definition))
                {
                    throw new JsonException($"The run names an unknown card definition '{definitionId}'.");
                }

                cards.Add(CardInstance.Restore(
                    cardJson["id"].AsInt(),
                    definition,
                    cardJson.Optional("upgraded")?.AsBool() ?? false,
                    cardJson.Optional("trait")?.AsString(),
                    cardJson.Optional("battlesRemaining")?.AsInt(),
                    cardJson.Optional("shopPrice")?.AsInt()));
            }

            var binder = Binder.Restore(cards, binderJson["nextId"].AsInt());
            foreach (var slotJson in root["loadout"].Items)
            {
                var slot = new Slot(slotJson["line"].AsInt(), KeyFromId(slotJson["key"].AsString()));
                int cardId = slotJson["card"].AsInt();
                var card = binder.Find(cardId) ?? throw new JsonException($"Slot {Loadout.Describe(new[] { slot })} names an instance {cardId} outside the Binder.");
                var placement = binder.Assign(slot, card);
                if (placement != Placement.Accepted)
                {
                    throw new JsonException($"Slot {Loadout.Describe(new[] { slot })} cannot hold instance {cardId}: {placement}.");
                }
            }

            var battles = new List<BattleRecord>();
            foreach (var battleJson in root["battles"].Items)
            {
                battles.Add(ReadBattle(battleJson));
            }

            RewardOffer? pendingReward = null;
            if (root.Optional("pendingReward") is JsonValue offerJson)
            {
                var offered = new List<CardDefinition>();
                foreach (var idJson in offerJson["cards"].Items)
                {
                    string id = idJson.AsString();
                    offered.Add(content.FindCard(id) ?? throw new JsonException($"The reward offer names an unknown card definition '{id}'."));
                }

                var imprintTierJson = offerJson.Optional("imprintTier");
                pendingReward = new RewardOffer(
                    TierFromId(offerJson["tier"].AsString()),
                    offerJson["node"].AsString(),
                    offered,
                    offerJson["essence"].AsInt(),
                    imprintTierJson is null ? (ImprintTier?)null : ImprintTierFromId(imprintTierJson.AsString()))
                {
                    ImprintId = offerJson.Optional("imprintId")?.AsString(),
                };
            }

            return new Run(
                root["seed"].AsString(),
                stats,
                Strings(root["charms"]),
                Strings(root["imprints"]),
                NullableStrings(root["armorUpgrades"]),
                binder,
                Strings(root["difficultyModifiers"]),
                root["assist"].AsBool(),
                content,
                root["world"].AsInt(),
                root.Optional("currentNode")?.AsString(),
                StatusFromId(root["status"].AsString()),
                root.Optional("battlesStarted")?.AsInt() ?? 0,
                root.Optional("visited") is JsonValue visited ? Strings(visited) : null,
                root.Optional("currentNodeCompleted")?.AsBool() ?? true,
                battles,
                Strings(root["route"]),
                pendingReward);
        }

        public static string StatusToId(RunStatus status)
        {
            switch (status)
            {
                case RunStatus.InProgress: return "in-progress";
                case RunStatus.Won: return "won";
                case RunStatus.Died: return "died";
                case RunStatus.Abandoned: return "abandoned";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown run status.");
            }
        }

        public static RunStatus StatusFromId(string id)
        {
            switch (id)
            {
                case "in-progress": return RunStatus.InProgress;
                case "won": return RunStatus.Won;
                case "died": return RunStatus.Died;
                case "abandoned": return RunStatus.Abandoned;
                default: throw new JsonException($"Unknown run status '{id}'.");
            }
        }

        public static string OutcomeToId(BattleOutcome outcome)
        {
            return outcome.ToString().ToLowerInvariant();
        }

        public static BattleOutcome OutcomeFromId(string id)
        {
            return Enum.TryParse<BattleOutcome>(id, true, out var outcome) ? outcome : throw new JsonException($"Unknown battle outcome '{id}'.");
        }

        public static string TierToId(EncounterTier tier)
        {
            return tier.ToString().ToLowerInvariant();
        }

        public static EncounterTier TierFromId(string id)
        {
            return Enum.TryParse<EncounterTier>(id, true, out var tier) ? tier : throw new JsonException($"Unknown encounter tier '{id}'.");
        }

        public static string ImprintTierToId(ImprintTier tier)
        {
            return tier.ToString().ToLowerInvariant();
        }

        public static ImprintTier ImprintTierFromId(string id)
        {
            return Enum.TryParse<ImprintTier>(id, true, out var tier) ? tier : throw new JsonException($"Unknown Imprint tier '{id}'.");
        }

        public static string KeyToId(SlotKey key)
        {
            return key.ToString().ToLowerInvariant();
        }

        public static SlotKey KeyFromId(string id)
        {
            switch (id)
            {
                case "q": return SlotKey.Q;
                case "w": return SlotKey.W;
                case "e": return SlotKey.E;
                case "r": return SlotKey.R;
                case "u": return SlotKey.U;
                case "i": return SlotKey.I;
                case "o": return SlotKey.O;
                case "p": return SlotKey.P;
                default: throw new JsonException($"Unknown slot key '{id}'.");
            }
        }

        private static List<string> Strings(JsonValue array)
        {
            var list = new List<string>();
            foreach (var item in array.Items)
            {
                list.Add(item.AsString());
            }

            return list;
        }

        private static List<string?> NullableStrings(JsonValue array)
        {
            var list = new List<string?>();
            foreach (var item in array.Items)
            {
                list.Add(item.IsNull ? null : item.AsString());
            }

            return list;
        }
    }
}
