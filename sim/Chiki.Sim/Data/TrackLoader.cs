using System;
using System.Collections.Generic;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="Track"/> from its JSON sidecar (PRD 4.14): id, world, tier,
    /// lengthBeats, offsetMs and tempo { bpm, changes: [ { beat, bpm } ] }.
    /// </summary>
    public static class TrackLoader
    {
        public static Track FromJson(string json)
        {
            var root = JsonValue.Parse(json);
            var tempoJson = root["tempo"];

            var changes = new List<TempoChange>();
            var changesJson = tempoJson.Optional("changes");
            if (changesJson != null)
            {
                foreach (var change in changesJson.Items)
                {
                    changes.Add(new TempoChange(change["beat"].AsInt(), change["bpm"].AsInt()));
                }
            }

            var tempo = new TempoMap(tempoJson["bpm"].AsInt(), changes);
            var offset = root.Optional("offsetMs");

            return new Track(
                root["id"].AsString(),
                root["world"].AsInt(),
                TierFromId(root["tier"].AsString()),
                root["lengthBeats"].AsInt(),
                offset?.AsInt() ?? 0,
                tempo);
        }

        public static EncounterTier TierFromId(string id)
        {
            switch (id)
            {
                case "normal": return EncounterTier.Normal;
                case "elite": return EncounterTier.Elite;
                case "boss": return EncounterTier.Boss;
                default: throw new JsonException($"Unknown encounter tier '{id}'.");
            }
        }

        public static string TierToId(EncounterTier tier)
        {
            switch (tier)
            {
                case EncounterTier.Normal: return "normal";
                case EncounterTier.Elite: return "elite";
                case EncounterTier.Boss: return "boss";
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }
    }
}
