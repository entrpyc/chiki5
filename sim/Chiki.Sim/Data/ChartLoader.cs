using System;
using System.Collections.Generic;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="Chart"/> from JSON (PRD 4.16, 3.6.31): id, enemy, track and actions
    /// [ { kind, position, defenseLevel?, windUpBeats? } ] with positions in quarter beats.
    /// The chart's track must already be loaded, since length and actions per minute derive
    /// from it.
    /// </summary>
    public static class ChartLoader
    {
        public static Chart FromJson(string json, Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var root = JsonValue.Parse(json);
            string trackId = root["track"].AsString();
            if (trackId != track.Id)
            {
                throw new JsonException($"Chart names track '{trackId}' but was given '{track.Id}'.");
            }

            var actions = new List<EnemyAction>();
            foreach (var actionJson in root["actions"].Items)
            {
                actions.Add(new EnemyAction(
                    KindFromId(actionJson["kind"].AsString()),
                    actionJson["position"].AsInt(),
                    actionJson.Optional("defenseLevel")?.AsInt() ?? 0,
                    actionJson.Optional("windUpBeats")?.AsInt() ?? 0));
            }

            return new Chart(root["id"].AsString(), root["enemy"].AsString(), track, actions);
        }

        public static EnemyActionKind KindFromId(string id)
        {
            switch (id)
            {
                case "attack-left": return EnemyActionKind.AttackLeft;
                case "attack-right": return EnemyActionKind.AttackRight;
                case "defend": return EnemyActionKind.Defend;
                case "buff": return EnemyActionKind.Buff;
                case "charge": return EnemyActionKind.Charge;
                default: throw new JsonException($"Unknown enemy action kind '{id}'.");
            }
        }

        public static string KindToId(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.AttackLeft: return "attack-left";
                case EnemyActionKind.AttackRight: return "attack-right";
                case EnemyActionKind.Defend: return "defend";
                case EnemyActionKind.Buff: return "buff";
                case EnemyActionKind.Charge: return "charge";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
