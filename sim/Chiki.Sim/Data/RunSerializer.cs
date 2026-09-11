using System;
using System.Collections.Generic;

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
    /// carries <c>schemaVersion</c> so a later build can migrate it (P22.3); a newer version
    /// than this build knows is refused. Card instances name their definition by id, so reading
    /// needs the definitions the run draws on.
    /// </summary>
    public static class RunSerializer
    {
        public const int SchemaVersion = 1;

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
            w.EndObject();
            return w.ToString();
        }

        /// <summary>Reads a run whose card instances draw on the given set's definitions.</summary>
        public static Run FromJson(string json, CardSet definitions)
        {
            if (definitions is null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var byId = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            foreach (var card in definitions.Cards)
            {
                byId[card.Id] = card;
            }

            return FromJson(json, byId);
        }

        public static Run FromJson(string json, IReadOnlyDictionary<string, CardDefinition> definitions)
        {
            if (definitions is null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var root = JsonValue.Parse(json);
            int version = root["schemaVersion"].AsInt();
            if (version > SchemaVersion)
            {
                throw new SaveVersionException($"The run was saved by a newer build (schema {version}, this build reads up to {SchemaVersion}).");
            }

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

            return new Run(
                root["seed"].AsString(),
                stats,
                Strings(root["charms"]),
                Strings(root["imprints"]),
                NullableStrings(root["armorUpgrades"]),
                binder,
                Strings(root["difficultyModifiers"]),
                root["assist"].AsBool(),
                root["world"].AsInt(),
                root.Optional("currentNode")?.AsString(),
                StatusFromId(root["status"].AsString()));
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
