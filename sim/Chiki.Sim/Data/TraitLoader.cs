using System.Collections.Generic;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="TraitSet"/> from JSON (PRD 4.11): { id, traits: [ trait ] } where a
    /// trait is { id, name, effect, source? } and the effect is a card effect entry
    /// (<see cref="CardLoader.EffectFromJson"/>) with its trigger stated. A trait with no effect,
    /// or with a source other than "pool" or "relationship", fails the load.
    /// </summary>
    public static class TraitLoader
    {
        public static TraitSet SetFromJson(string json)
        {
            var root = JsonValue.Parse(json);
            var traits = new List<TraitDefinition>();
            foreach (var traitJson in root["traits"].Items)
            {
                traits.Add(TraitFromJson(traitJson));
            }

            return new TraitSet(root["id"].AsString(), traits);
        }

        public static TraitDefinition TraitFromJson(JsonValue json)
        {
            string id = json["id"].AsString();
            var effectJson = json.Optional("effect") ?? throw new JsonException($"Trait '{id}' has no effect; a Trait has exactly one (PRD 4.11).");
            return new TraitDefinition(
                id,
                json["name"].AsString(),
                CardLoader.EffectFromJson(effectJson),
                SourceFromId(json.Optional("source")?.AsString() ?? "pool"));
        }

        public static TraitSource SourceFromId(string id)
        {
            switch (id)
            {
                case "pool": return TraitSource.Pool;
                case "relationship": return TraitSource.Relationship;
                default: throw new JsonException($"Unknown Trait source '{id}'.");
            }
        }
    }
}
