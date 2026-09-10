using System;
using System.Collections.Generic;
using System.Linq;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="Chart"/> from JSON (PRD 4.16, 3.6.31): id, enemy, track and actions
    /// [ { kind, position, defenseLevel?, windUpBeats?, applies? } ] with positions authored in
    /// beats with up to three decimals (2.25 is the first quarter after beat 2) and applies as
    /// [ { status, stacks?, value? } ], the statuses the action lands on the player
    /// (PRD 3.3.4.6). <see cref="Read"/> gives the chart as authored, <see cref="ChartValidator"/>
    /// checks it against its track, and <see cref="FromJson"/> does both and builds the chart,
    /// refusing one that breaks a rule. The chart's track must already be loaded, since length
    /// and actions per minute derive from it.
    /// </summary>
    public static class ChartLoader
    {
        public static StatusKind StatusFromId(string id)
        {
            switch (id)
            {
                case "scar": return StatusKind.Scar;
                case "weak": return StatusKind.Weak;
                case "stun": return StatusKind.Stun;
                case "bleed": return StatusKind.Bleed;
                case "thorns": return StatusKind.Thorns;
                case "disarmed": return StatusKind.Disarmed;
                default: throw new JsonException($"Unknown status '{id}'.");
            }
        }

        public static string StatusToId(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Scar: return "scar";
                case StatusKind.Weak: return "weak";
                case StatusKind.Stun: return "stun";
                case StatusKind.Bleed: return "bleed";
                case StatusKind.Thorns: return "thorns";
                case StatusKind.Disarmed: return "disarmed";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static StatusApplication ApplicationFromJson(JsonValue json)
        {
            return new StatusApplication(
                StatusFromId(json["status"].AsString()),
                json.Optional("stacks")?.AsInt() ?? 1,
                json.Optional("value")?.AsInt() ?? 0);
        }

        /// <summary>The chart as authored, before validation.</summary>
        public static ChartDocument Read(string json)
        {
            var root = JsonValue.Parse(json);
            var actions = new List<ChartActionDocument>();
            foreach (var actionJson in root["actions"].Items)
            {
                var applies = new List<StatusApplication>();
                var appliesJson = actionJson.Optional("applies");
                if (appliesJson != null)
                {
                    foreach (var applicationJson in appliesJson.Items)
                    {
                        applies.Add(ApplicationFromJson(applicationJson));
                    }
                }

                actions.Add(new ChartActionDocument(
                    actionJson["kind"].AsString(),
                    actionJson["position"].AsThousandths(),
                    actionJson.Optional("defenseLevel")?.AsInt() ?? 0,
                    actionJson.Optional("windUpBeats")?.AsInt() ?? 0,
                    applies));
            }

            return new ChartDocument(root["id"].AsString(), root["enemy"].AsString(), root["track"].AsString(), actions);
        }

        /// <summary>Reads, validates against the track (PRD 3.6.31) and builds; a chart with violations is refused.</summary>
        public static Chart FromJson(string json, Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var document = Read(json);
            if (document.TrackId != track.Id)
            {
                throw new JsonException($"Chart names track '{document.TrackId}' but was given '{track.Id}'.");
            }

            return Build(document, track);
        }

        /// <summary>Builds a validated document into a chart on its track; a document with violations is refused.</summary>
        public static Chart Build(ChartDocument document, Track track)
        {
            var violations = ChartValidator.Validate(document, track);
            if (violations.Count > 0)
            {
                throw new JsonException("Chart breaks a rule: " + string.Join("; ", violations.Select(v => v.ToString())));
            }

            var actions = new List<EnemyAction>();
            foreach (var action in document.Actions)
            {
                actions.Add(new EnemyAction(
                    KindFromId(action.KindId),
                    action.PositionQb,
                    action.DefenseLevel,
                    action.WindUpBeats,
                    action.Applies));
            }

            return new Chart(document.Id, document.EnemyId, track, actions);
        }

        public static EnemyActionKind KindFromId(string id)
        {
            switch (id)
            {
                case ChartValidator.KindAttackLeft: return EnemyActionKind.AttackLeft;
                case ChartValidator.KindAttackRight: return EnemyActionKind.AttackRight;
                case ChartValidator.KindDefend: return EnemyActionKind.Defend;
                case ChartValidator.KindBuff: return EnemyActionKind.Buff;
                case ChartValidator.KindCharge: return EnemyActionKind.Charge;
                default: throw new JsonException($"Unknown enemy action kind '{id}'.");
            }
        }

        public static string KindToId(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.AttackLeft: return ChartValidator.KindAttackLeft;
                case EnemyActionKind.AttackRight: return ChartValidator.KindAttackRight;
                case EnemyActionKind.Defend: return ChartValidator.KindDefend;
                case EnemyActionKind.Buff: return ChartValidator.KindBuff;
                case EnemyActionKind.Charge: return ChartValidator.KindCharge;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
