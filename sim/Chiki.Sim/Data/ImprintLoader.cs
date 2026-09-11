using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads an <see cref="ImprintSet"/> from JSON (PRD 4.10): { id, imprints: [ imprint ] }
    /// where an imprint is { id, name, tier, effects: [ effect ], stackable?, source? } and an
    /// effect is a card effect entry (<see cref="CardLoader.EffectFromJson"/>) with its trigger
    /// stated: "acquired" for a change made once when the Imprint is gained, or a battle event.
    /// </summary>
    public static class ImprintLoader
    {
        public static ImprintSet SetFromJson(string json)
        {
            var root = JsonValue.Parse(json);
            var imprints = new List<ImprintDefinition>();
            foreach (var imprintJson in root["imprints"].Items)
            {
                imprints.Add(ImprintFromJson(imprintJson));
            }

            return new ImprintSet(root["id"].AsString(), imprints);
        }

        public static ImprintDefinition ImprintFromJson(JsonValue json)
        {
            var effects = new List<EffectDefinition>();
            foreach (var effectJson in json["effects"].Items)
            {
                effects.Add(CardLoader.EffectFromJson(effectJson));
            }

            return new ImprintDefinition(
                json["id"].AsString(),
                json["name"].AsString(),
                TierFromId(json["tier"].AsString()),
                effects,
                json.Optional("stackable")?.AsBool() ?? false,
                SourceFromId(json.Optional("source")?.AsString() ?? "pool"));
        }

        public static ImprintTier TierFromId(string id)
        {
            switch (id)
            {
                case "common": return ImprintTier.Common;
                case "uncommon": return ImprintTier.Uncommon;
                case "rare": return ImprintTier.Rare;
                default: throw new JsonException($"Unknown Imprint tier '{id}'.");
            }
        }

        public static ImprintSource SourceFromId(string id)
        {
            switch (id)
            {
                case "pool": return ImprintSource.Pool;
                case "relationship": return ImprintSource.Relationship;
                default: throw new JsonException($"Unknown Imprint source '{id}'.");
            }
        }
    }
}
