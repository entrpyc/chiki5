using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="CardSet"/> from JSON (PRD 4.4): { id, cards: [ card ] } where a card is
    /// { id, name, category, rarity, class, value, cooldownBeats, effects?, specialRules?,
    /// upgradeStep?, lifespan?, flavorText?, unlockSource, scaling? } and an effect is { trigger,
    /// modifier, amount?, condition?, target?, status?, stacks?, statusValue?, stat?, value?,
    /// lifetime?, beats? }. Ids are lowercase kebab-case. Loading checks shape only; the rules
    /// of PRD 3.4 are <see cref="CardValidator"/>'s.
    /// </summary>
    public static class CardLoader
    {
        public static CardSet SetFromJson(string json)
        {
            var root = JsonValue.Parse(json);
            var cards = new List<CardDefinition>();
            foreach (var cardJson in root["cards"].Items)
            {
                cards.Add(CardFromJson(cardJson));
            }

            return new CardSet(root["id"].AsString(), cards);
        }

        public static CardDefinition CardFromJson(JsonValue json)
        {
            var effects = new List<EffectDefinition>();
            var effectsJson = json.Optional("effects");
            if (effectsJson != null)
            {
                foreach (var effectJson in effectsJson.Items)
                {
                    effects.Add(EffectFromJson(effectJson));
                }
            }

            return new CardDefinition(
                json["id"].AsString(),
                json["name"].AsString(),
                CategoryFromId(json["category"].AsString()),
                json["value"].AsInt(),
                json["cooldownBeats"].AsInt(),
                RarityFromId(json["rarity"].AsString()),
                ClassFromId(json["class"].AsString()),
                effects,
                json.Optional("specialRules")?.AsString(),
                json.Optional("upgradeStep")?.AsInt() ?? 0,
                json.Optional("lifespan")?.AsInt(),
                json.Optional("flavorText")?.AsString(),
                UnlockSourceFromId(json["unlockSource"].AsString()),
                ScalingFromJson(json.Optional("scaling")));
        }

        /// <summary>{ source, amount, per?, status? } (PRD 3.4.9, 3.8.8); null when absent.</summary>
        public static ValueScaling? ScalingFromJson(JsonValue? json)
        {
            if (json is null)
            {
                return null;
            }

            var statusId = json.Optional("status")?.AsString();
            return new ValueScaling(
                ScalingSourceFromId(json["source"].AsString()),
                json["amount"].AsInt(),
                json.Optional("per")?.AsInt() ?? 1,
                statusId is null ? (StatusKind?)null : ChartLoader.StatusFromId(statusId));
        }

        public static ScalingSource ScalingSourceFromId(string id)
        {
            switch (id)
            {
                case "block": return ScalingSource.Block;
                case "ard": return ScalingSource.Ard;
                case "base-dmg": return ScalingSource.BaseDmg;
                case "essence": return ScalingSource.Essence;
                case "crp": return ScalingSource.Crp;
                case "status": return ScalingSource.Status;
                default: throw new JsonException($"Unknown scaling source '{id}'.");
            }
        }

        /// <summary>One effect entry; <paramref name="defaultTrigger"/> and <paramref name="defaultCondition"/> stand in when the entry states none, as a Charm's effects inherit the Charm's (PRD 4.9).</summary>
        public static EffectDefinition EffectFromJson(JsonValue json, EffectTrigger? defaultTrigger = null, EffectCondition? defaultCondition = null)
        {
            var triggerId = json.Optional("trigger")?.AsString();
            var trigger = triggerId != null ? TriggerFromId(triggerId)
                : defaultTrigger ?? throw new JsonException("Missing required field 'trigger'.");
            var conditionId = json.Optional("condition")?.AsString();
            var condition = conditionId != null ? ConditionFromId(conditionId) : defaultCondition ?? EffectCondition.None;
            var statusId = json.Optional("status")?.AsString();
            StatusApplication? status = statusId is null
                ? null
                : new StatusApplication(
                    ChartLoader.StatusFromId(statusId),
                    json.Optional("stacks")?.AsInt() ?? 1,
                    json.Optional("statusValue")?.AsInt() ?? 0);
            var statId = json.Optional("stat")?.AsString();
            var valueId = json.Optional("value")?.AsString();
            var lifetimeId = json.Optional("lifetime")?.AsString();

            return new EffectDefinition(
                trigger,
                ModifierFromId(json["modifier"].AsString()),
                json.Optional("amount")?.AsInt() ?? 0,
                condition,
                TargetFromId(json.Optional("target")?.AsString() ?? "enemy"),
                status,
                statId is null ? (RunStat?)null : StatFromId(statId),
                valueId is null ? (EffectValue?)null : ValueFromId(valueId),
                LifetimeFromId(lifetimeId ?? "instant"),
                json.Optional("beats")?.AsInt() ?? 0,
                json.Optional("conditionAmount")?.AsInt() ?? 0);
        }

        public static CardCategory CategoryFromId(string id)
        {
            switch (id)
            {
                case "ability": return CardCategory.Ability;
                case "left-attack": return CardCategory.LeftAttack;
                case "right-attack": return CardCategory.RightAttack;
                case "defense": return CardCategory.Defense;
                default: throw new JsonException($"Unknown card category '{id}'.");
            }
        }

        public static string CategoryToId(CardCategory category)
        {
            switch (category)
            {
                case CardCategory.Ability: return "ability";
                case CardCategory.LeftAttack: return "left-attack";
                case CardCategory.RightAttack: return "right-attack";
                case CardCategory.Defense: return "defense";
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        public static CardRarity RarityFromId(string id)
        {
            switch (id)
            {
                case "common": return CardRarity.Common;
                case "uncommon": return CardRarity.Uncommon;
                case "rare": return CardRarity.Rare;
                case "legendary": return CardRarity.Legendary;
                default: throw new JsonException($"Unknown rarity '{id}'.");
            }
        }

        public static CardClass ClassFromId(string id)
        {
            switch (id)
            {
                case "normal": return CardClass.Normal;
                case "event": return CardClass.Event;
                case "unstable": return CardClass.Unstable;
                default: throw new JsonException($"Unknown card class '{id}'.");
            }
        }

        public static UnlockSource UnlockSourceFromId(string id)
        {
            switch (id)
            {
                case "starter": return UnlockSource.Starter;
                case "pool": return UnlockSource.Pool;
                case "boss": return UnlockSource.Boss;
                case "relationship": return UnlockSource.Relationship;
                default: throw new JsonException($"Unknown unlock source '{id}'.");
            }
        }

        public static EffectTrigger TriggerFromId(string id)
        {
            switch (id)
            {
                case "on-play": return EffectTrigger.OnPlay;
                case "passive": return EffectTrigger.Passive;
                case "battle-started": return EffectTrigger.BattleStarted;
                case "beat-started": return EffectTrigger.BeatStarted;
                case "beat-ended": return EffectTrigger.BeatEnded;
                case "input-judged": return EffectTrigger.InputJudged;
                case "action-resolved": return EffectTrigger.ActionResolved;
                case "damage-dealt": return EffectTrigger.DamageDealt;
                case "damage-taken": return EffectTrigger.DamageTaken;
                case "block-gained": return EffectTrigger.BlockGained;
                case "status-applied": return EffectTrigger.StatusApplied;
                case "battle-ended": return EffectTrigger.BattleEnded;
                case "acquired": return EffectTrigger.Acquired;
                default: throw new JsonException($"Unknown effect trigger '{id}'.");
            }
        }

        public static EffectCondition ConditionFromId(string id)
        {
            switch (id)
            {
                case "none": return EffectCondition.None;
                case "on-perfect": return EffectCondition.OnPerfect;
                case "on-good": return EffectCondition.OnGood;
                case "on-miss": return EffectCondition.OnMiss;
                case "if-no-input": return EffectCondition.IfNoInput;
                case "if-kills": return EffectCondition.IfKills;
                case "if-enemy-attacking": return EffectCondition.IfEnemyAttacking;
                case "if-damage-landed": return EffectCondition.IfDamageLanded;
                case "if-buff-action": return EffectCondition.IfBuffAction;
                case "if-enemy-quiet-beats": return EffectCondition.IfEnemyQuietBeats;
                case "if-perfect-defense": return EffectCondition.IfPerfectDefense;
                default: throw new JsonException($"Unknown effect condition '{id}'.");
            }
        }

        public static EffectModifier ModifierFromId(string id)
        {
            switch (id)
            {
                case "deal-damage": return EffectModifier.DealDamage;
                case "deal-true-damage": return EffectModifier.DealTrueDamage;
                case "gain-block": return EffectModifier.GainBlock;
                case "apply-status": return EffectModifier.ApplyStatus;
                case "change-stat": return EffectModifier.ChangeStat;
                case "add-value": return EffectModifier.AddValue;
                case "multiply-value": return EffectModifier.MultiplyValue;
                default: throw new JsonException($"Unknown effect modifier '{id}'.");
            }
        }

        public static StatusTarget TargetFromId(string id)
        {
            switch (id)
            {
                case "player": return StatusTarget.Player;
                case "enemy": return StatusTarget.Enemy;
                default: throw new JsonException($"Unknown effect target '{id}'.");
            }
        }

        public static RunStat StatFromId(string id)
        {
            switch (id)
            {
                case "ard": return RunStat.Ard;
                case "base-dmg": return RunStat.BaseDmg;
                case "essence": return RunStat.Essence;
                case "crp": return RunStat.Crp;
                case "max-ard": return RunStat.MaxArd;
                default: throw new JsonException($"Unknown run stat '{id}'.");
            }
        }

        public static EffectValue ValueFromId(string id)
        {
            switch (id)
            {
                case "card-value": return EffectValue.CardValue;
                case "damage-dealt": return EffectValue.DamageDealt;
                case "damage-taken": return EffectValue.DamageTaken;
                case "enemy-damage": return EffectValue.EnemyDamage;
                case "enemy-damage-taken": return EffectValue.EnemyDamageTaken;
                default: throw new JsonException($"Unknown effect value '{id}'.");
            }
        }

        public static EffectLifetime LifetimeFromId(string id)
        {
            switch (id)
            {
                case "instant": return EffectLifetime.Instant;
                case "beats": return EffectLifetime.Beats;
                case "consumed": return EffectLifetime.Consumed;
                case "battle": return EffectLifetime.Battle;
                case "run": return EffectLifetime.Run;
                default: throw new JsonException($"Unknown effect lifetime '{id}'.");
            }
        }
    }
}
