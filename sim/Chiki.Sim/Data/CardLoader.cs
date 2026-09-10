using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Reads a <see cref="CardSet"/> from JSON (PRD 4.4): { id, cards: [ card ] } where a card is
    /// { id, name, category, rarity, class, value, cooldownBeats, effects?, specialRules?,
    /// upgradeStep?, lifespan?, flavorText?, unlockSource } and an effect is { trigger,
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
                UnlockSourceFromId(json["unlockSource"].AsString()));
        }

        public static EffectDefinition EffectFromJson(JsonValue json)
        {
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
                TriggerFromId(json["trigger"].AsString()),
                ModifierFromId(json["modifier"].AsString()),
                json.Optional("amount")?.AsInt() ?? 0,
                ConditionFromId(json.Optional("condition")?.AsString() ?? "none"),
                TargetFromId(json.Optional("target")?.AsString() ?? "enemy"),
                status,
                statId is null ? (RunStat?)null : StatFromId(statId),
                valueId is null ? (EffectValue?)null : ValueFromId(valueId),
                LifetimeFromId(lifetimeId ?? "instant"),
                json.Optional("beats")?.AsInt() ?? 0);
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
                case "beat-started": return EffectTrigger.BeatStarted;
                case "input-judged": return EffectTrigger.InputJudged;
                case "damage-dealt": return EffectTrigger.DamageDealt;
                case "damage-taken": return EffectTrigger.DamageTaken;
                case "block-gained": return EffectTrigger.BlockGained;
                case "status-applied": return EffectTrigger.StatusApplied;
                case "battle-ended": return EffectTrigger.BattleEnded;
                default: throw new JsonException($"Unknown effect trigger '{id}'.");
            }
        }

        public static EffectCondition ConditionFromId(string id)
        {
            switch (id)
            {
                case "none": return EffectCondition.None;
                case "on-perfect": return EffectCondition.OnPerfect;
                case "if-kills": return EffectCondition.IfKills;
                case "if-enemy-attacking": return EffectCondition.IfEnemyAttacking;
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
                default: throw new JsonException($"Unknown effect value '{id}'.");
            }
        }

        public static EffectLifetime LifetimeFromId(string id)
        {
            switch (id)
            {
                case "instant": return EffectLifetime.Instant;
                case "beats": return EffectLifetime.Beats;
                case "battle": return EffectLifetime.Battle;
                case "run": return EffectLifetime.Run;
                default: throw new JsonException($"Unknown effect lifetime '{id}'.");
            }
        }
    }
}
