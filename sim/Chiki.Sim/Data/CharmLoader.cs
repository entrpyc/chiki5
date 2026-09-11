using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="CharmSet"/> from JSON (PRD 4.9): { id, charms: [ charm ] } where a
    /// charm is { id, name, rarity, trigger, condition?, effects: [ effect ], unlock: { fact,
    /// amount?, subject? }, endingAltering?, runCap? }. An effect is a card effect entry
    /// (<see cref="CardLoader.EffectFromJson"/>) that inherits the Charm's trigger and condition
    /// when it states none.
    /// </summary>
    public static class CharmLoader
    {
        public static CharmSet SetFromJson(string json)
        {
            var root = JsonValue.Parse(json);
            var charms = new List<CharmDefinition>();
            foreach (var charmJson in root["charms"].Items)
            {
                charms.Add(CharmFromJson(charmJson));
            }

            return new CharmSet(root["id"].AsString(), charms);
        }

        public static CharmDefinition CharmFromJson(JsonValue json)
        {
            var trigger = new CharmTrigger(
                CardLoader.TriggerFromId(json["trigger"].AsString()),
                CardLoader.ConditionFromId(json.Optional("condition")?.AsString() ?? "none"));

            var effects = new List<EffectDefinition>();
            foreach (var effectJson in json["effects"].Items)
            {
                effects.Add(CardLoader.EffectFromJson(effectJson, trigger.Event, trigger.Condition));
            }

            return new CharmDefinition(
                json["id"].AsString(),
                json["name"].AsString(),
                RarityFromId(json["rarity"].AsString()),
                trigger,
                effects,
                UnlockFromJson(json["unlock"]),
                json.Optional("endingAltering")?.AsBool() ?? false,
                json.Optional("runCap")?.AsInt());
        }

        public static UnlockCondition UnlockFromJson(JsonValue json)
        {
            return new UnlockCondition(
                FactFromId(json["fact"].AsString()),
                json.Optional("amount")?.AsInt() ?? 1,
                json.Optional("subject")?.AsString());
        }

        public static CharmRarity RarityFromId(string id)
        {
            switch (id)
            {
                case "common": return CharmRarity.Common;
                case "uncommon": return CharmRarity.Uncommon;
                case "rare": return CharmRarity.Rare;
                case "legendary": return CharmRarity.Legendary;
                default: throw new JsonException($"Unknown Charm rarity '{id}'.");
            }
        }

        public static UnlockFact FactFromId(string id)
        {
            switch (id)
            {
                case "bosses-defeated": return UnlockFact.BossesDefeated;
                case "milestone": return UnlockFact.Milestone;
                case "challenge": return UnlockFact.Challenge;
                case "relationship-level": return UnlockFact.RelationshipLevel;
                default: throw new JsonException($"Unknown unlock fact '{id}'.");
            }
        }
    }
}
