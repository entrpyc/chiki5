using System;
using System.Collections.Generic;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads an <see cref="EnemySet"/> from JSON (PRD 4.7): { id, enemies: [ enemy ] } where an
    /// enemy is { id, name, tier, role, profile?, intendedSeconds, track, chart, damagePerHit,
    /// abilities?, traits?, statusesUsed?, portrait?, quote?, phases? }. The charts an enemy
    /// names must already be loaded and are passed in by id. Loading checks shape only; the
    /// rules of PRD 3.6 are <see cref="EnemyValidator"/>'s.
    /// </summary>
    public static class EnemyLoader
    {
        public static EnemySet SetFromJson(string json, IReadOnlyDictionary<string, Chart> charts)
        {
            var root = JsonValue.Parse(json);
            var enemies = new List<EnemyDefinition>();
            foreach (var enemyJson in root["enemies"].Items)
            {
                enemies.Add(EnemyFromJson(enemyJson, charts));
            }

            return new EnemySet(root["id"].AsString(), enemies);
        }

        public static EnemyDefinition EnemyFromJson(JsonValue json, IReadOnlyDictionary<string, Chart> charts)
        {
            if (charts is null)
            {
                throw new ArgumentNullException(nameof(charts));
            }

            string chartId = json["chart"].AsString();
            if (!charts.TryGetValue(chartId, out var chart))
            {
                throw new JsonException($"Chart '{chartId}' is not loaded.");
            }

            var profileId = json.Optional("profile")?.AsString();

            return new EnemyDefinition(
                json["id"].AsString(),
                json["name"].AsString(),
                TrackLoader.TierFromId(json["tier"].AsString()),
                RoleFromId(json["role"].AsString()),
                profileId is null ? (RhythmProfile?)null : ProfileFromId(profileId),
                json["intendedSeconds"].AsInt(),
                chart,
                json["damagePerHit"].AsInt(),
                Strings(json.Optional("abilities"), AbilityFromId),
                Strings(json.Optional("traits"), TraitFromId),
                Strings(json.Optional("statusesUsed"), ChartLoader.StatusFromId),
                json.Optional("portrait")?.AsString(),
                json.Optional("quote")?.AsString(),
                Strings(json.Optional("phases"), s => s),
                json["track"].AsString());
        }

        private static List<T> Strings<T>(JsonValue? array, Func<string, T> convert)
        {
            var list = new List<T>();
            if (array != null)
            {
                foreach (var item in array.Items)
                {
                    list.Add(convert(item.AsString()));
                }
            }

            return list;
        }

        public static EnemyRole RoleFromId(string id)
        {
            switch (id)
            {
                case "aggressor": return EnemyRole.Aggressor;
                case "tank": return EnemyRole.Tank;
                case "mentalist": return EnemyRole.Mentalist;
                default: throw new JsonException($"Unknown enemy role '{id}'.");
            }
        }

        public static string RoleToId(EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Aggressor: return "aggressor";
                case EnemyRole.Tank: return "tank";
                case EnemyRole.Mentalist: return "mentalist";
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        public static RhythmProfile ProfileFromId(string id)
        {
            switch (id)
            {
                case "fast": return RhythmProfile.Fast;
                case "slow": return RhythmProfile.Slow;
                default: throw new JsonException($"Unknown rhythm profile '{id}'.");
            }
        }

        public static string ProfileToId(RhythmProfile profile)
        {
            switch (profile)
            {
                case RhythmProfile.Fast: return "fast";
                case RhythmProfile.Slow: return "slow";
                default: throw new ArgumentOutOfRangeException(nameof(profile));
            }
        }

        public static EnemyAbility AbilityFromId(string id)
        {
            switch (id)
            {
                case "rising-tempo": return EnemyAbility.RisingTempo;
                case "misstep-pain": return EnemyAbility.MisstepPain;
                case "counterblade": return EnemyAbility.Counterblade;
                case "pressure": return EnemyAbility.Pressure;
                case "iron-veil": return EnemyAbility.IronVeil;
                case "backflash-barrier": return EnemyAbility.BackflashBarrier;
                case "fake-move": return EnemyAbility.FakeMove;
                case "heavy-hand": return EnemyAbility.HeavyHand;
                case "beat-rush": return EnemyAbility.BeatRush;
                case "chain-breaker": return EnemyAbility.ChainBreaker;
                case "double-step": return EnemyAbility.DoubleStep;
                case "charge-buff": return EnemyAbility.ChargeBuff;
                case "frenzy-mode": return EnemyAbility.FrenzyMode;
                case "corruption-aegis": return EnemyAbility.CorruptionAegis;
                default: throw new JsonException($"Unknown enemy ability '{id}'.");
            }
        }

        public static string AbilityToId(EnemyAbility ability)
        {
            switch (ability)
            {
                case EnemyAbility.RisingTempo: return "rising-tempo";
                case EnemyAbility.MisstepPain: return "misstep-pain";
                case EnemyAbility.Counterblade: return "counterblade";
                case EnemyAbility.Pressure: return "pressure";
                case EnemyAbility.IronVeil: return "iron-veil";
                case EnemyAbility.BackflashBarrier: return "backflash-barrier";
                case EnemyAbility.FakeMove: return "fake-move";
                case EnemyAbility.HeavyHand: return "heavy-hand";
                case EnemyAbility.BeatRush: return "beat-rush";
                case EnemyAbility.ChainBreaker: return "chain-breaker";
                case EnemyAbility.DoubleStep: return "double-step";
                case EnemyAbility.ChargeBuff: return "charge-buff";
                case EnemyAbility.FrenzyMode: return "frenzy-mode";
                case EnemyAbility.CorruptionAegis: return "corruption-aegis";
                default: throw new ArgumentOutOfRangeException(nameof(ability));
            }
        }

        public static EnemyTrait TraitFromId(string id)
        {
            switch (id)
            {
                case "accuracy-bet": return EnemyTrait.AccuracyBet;
                case "stoneform": return EnemyTrait.Stoneform;
                case "absorb-shell": return EnemyTrait.AbsorbShell;
                case "pure-heart": return EnemyTrait.PureHeart;
                case "blood-leech": return EnemyTrait.BloodLeech;
                case "thorns-shell": return EnemyTrait.ThornsShell;
                case "guard": return EnemyTrait.Guard;
                default: throw new JsonException($"Unknown enemy trait '{id}'.");
            }
        }

        public static string TraitToId(EnemyTrait trait)
        {
            switch (trait)
            {
                case EnemyTrait.AccuracyBet: return "accuracy-bet";
                case EnemyTrait.Stoneform: return "stoneform";
                case EnemyTrait.AbsorbShell: return "absorb-shell";
                case EnemyTrait.PureHeart: return "pure-heart";
                case EnemyTrait.BloodLeech: return "blood-leech";
                case EnemyTrait.ThornsShell: return "thorns-shell";
                case EnemyTrait.Guard: return "guard";
                default: throw new ArgumentOutOfRangeException(nameof(trait));
            }
        }
    }
}
