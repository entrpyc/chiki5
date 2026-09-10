using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>One broken rule on one card: the card, the PRD number of the rule and what is wrong.</summary>
    public sealed record CardViolation(string CardId, string Rule, string Message)
    {
        public override string ToString()
        {
            return $"{CardId}: {Message} ({Rule})";
        }
    }

    /// <summary>
    /// The rules every card must obey (PRD 3.4.21), each a build-time failure on the shipped
    /// sets: anatomy and cooldown (PRD 3.4.7, 3.3.5.1), rarity bands (PRD 3.4.4), effect
    /// compatibility (PRD 3.4.8) and class (PRD 3.4.13). Every violation is returned, not just
    /// the first.
    /// </summary>
    public static class CardValidator
    {
        public const string RuleAnatomy = "3.4.7";
        public const string RuleRarityBand = "3.4.4";
        public const string RuleEffectCompatibility = "3.4.8";
        public const string RuleClass = "3.4.13";
        public const string RuleUniqueId = "4.4";

        /// <summary>Validates a whole set: every card's rules plus unique ids.</summary>
        public static IReadOnlyList<CardViolation> Validate(CardSet set)
        {
            if (set is null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            var violations = new List<CardViolation>();
            var seen = new HashSet<string>();
            foreach (var card in set.Cards)
            {
                if (!seen.Add(card.Id))
                {
                    violations.Add(new CardViolation(card.Id, RuleUniqueId, "duplicate card id"));
                }

                violations.AddRange(Validate(card));
            }

            return violations;
        }

        public static IReadOnlyList<CardViolation> Validate(CardDefinition card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var violations = new List<CardViolation>();
            ValidateAnatomy(card, violations);
            ValidateBand(card, violations);
            ValidateEffects(card, violations);
            ValidateClass(card, violations);
            return violations;
        }

        /// <summary>PRD 3.4.7: name required; cooldown 2–6 beats (PRD 3.3.5.1).</summary>
        private static void ValidateAnatomy(CardDefinition card, List<CardViolation> violations)
        {
            if (string.IsNullOrWhiteSpace(card.Name))
            {
                violations.Add(new CardViolation(card.Id, RuleAnatomy, "name is required"));
            }

            if (card.CooldownBeats < Tuning.CooldownMinBeats || card.CooldownBeats > Tuning.CooldownMaxBeats)
            {
                violations.Add(new CardViolation(
                    card.Id,
                    RuleAnatomy,
                    $"cooldown {card.CooldownBeats} is outside {Tuning.CooldownMinBeats}–{Tuning.CooldownMaxBeats} beats"));
            }
        }

        /// <summary>PRD 3.4.4: an attack's damage and a Defense card's Block sit in the rarity's band.</summary>
        private static void ValidateBand(CardDefinition card, List<CardViolation> violations)
        {
            if (card.Category.IsAttack())
            {
                var band = RarityBands.Damage(card.Rarity);
                if (!band.Contains(card.Value))
                {
                    violations.Add(new CardViolation(card.Id, RuleRarityBand, $"{card.Rarity} attack damage {card.Value} is outside the band {band}"));
                }
            }
            else if (card.Category == CardCategory.Defense)
            {
                var band = RarityBands.Block(card.Rarity);
                if (!band.Contains(card.Value))
                {
                    violations.Add(new CardViolation(card.Id, RuleRarityBand, $"{card.Rarity} Defense Block {card.Value} is outside the band {band}"));
                }
            }
        }

        /// <summary>PRD 3.4.8: an effect appears only on the categories, and at or above the rarity, the table gives.</summary>
        private static void ValidateEffects(CardDefinition card, List<CardViolation> violations)
        {
            foreach (var effect in card.Effects)
            {
                var row = RowOf(effect);
                if (row is null)
                {
                    continue;
                }

                var minimum = EffectCompatibility.MinimumRarity(row.Value, card.Category);
                if (minimum is null)
                {
                    violations.Add(new CardViolation(card.Id, RuleEffectCompatibility, $"{EffectCompatibility.NameOf(row.Value)} is never allowed on a {card.Category} card"));
                }
                else if (card.Rarity < minimum.Value)
                {
                    violations.Add(new CardViolation(card.Id, RuleEffectCompatibility, $"{EffectCompatibility.NameOf(row.Value)} on a {card.Category} card needs {minimum.Value} or above, not {card.Rarity}"));
                }
            }
        }

        /// <summary>PRD 3.4.13–3.4.16: Unstable cards have a lifespan of at least one battle; the other classes none.</summary>
        private static void ValidateClass(CardDefinition card, List<CardViolation> violations)
        {
            if (card.Class == CardClass.Unstable)
            {
                if (card.Lifespan is null || card.Lifespan.Value < 1)
                {
                    violations.Add(new CardViolation(card.Id, RuleClass, "an Unstable card needs a lifespan of at least 1 battle"));
                }
            }
            else if (card.Lifespan != null)
            {
                violations.Add(new CardViolation(card.Id, RuleClass, $"a {card.Class} card has no lifespan"));
            }
        }

        /// <summary>The row of the PRD 3.4.8 table an effect falls under, or null when the table does not restrict it.</summary>
        private static CardEffectKind? RowOf(EffectDefinition effect)
        {
            switch (effect.Modifier)
            {
                case EffectModifier.ApplyStatus:
                    switch (effect.Status!.Kind)
                    {
                        case StatusKind.Bleed: return CardEffectKind.Bleed;
                        case StatusKind.Weak: return CardEffectKind.Weak;
                        case StatusKind.Scar: return CardEffectKind.Scar;
                        case StatusKind.Thorns: return CardEffectKind.Thorns;
                        case StatusKind.Stun: return CardEffectKind.Stun;
                        case StatusKind.Disarmed: return CardEffectKind.Disarmed;
                        default: throw new ArgumentOutOfRangeException(nameof(effect), effect.Status.Kind, "Unknown status.");
                    }

                case EffectModifier.GainBlock:
                    return effect.Target == StatusTarget.Player ? CardEffectKind.Block : (CardEffectKind?)null;
                case EffectModifier.ChangeStat:
                    return effect.Stat == RunStat.Ard && effect.Amount > 0 ? CardEffectKind.Repair : (CardEffectKind?)null;
                case EffectModifier.DealTrueDamage:
                    return CardEffectKind.TrueDmg;
                default:
                    return null;
            }
        }
    }

    /// <summary>The rows of the effect compatibility table (PRD 3.4.8).</summary>
    public enum CardEffectKind
    {
        Bleed,
        Weak,
        Scar,
        Thorns,
        Stun,
        Block,
        Disarmed,
        Repair,
        TrueDmg,
    }

    /// <summary>The one table of PRD 3.4.8: effect × category → minimum rarity, or null where the effect never appears.</summary>
    public static class EffectCompatibility
    {
        public static CardRarity? MinimumRarity(CardEffectKind effect, CardCategory category)
        {
            int column = category.IsAttack() ? 0 : category == CardCategory.Defense ? 1 : 2;
            return Table(effect)[column];
        }

        public static string NameOf(CardEffectKind effect)
        {
            switch (effect)
            {
                case CardEffectKind.Block: return "Block";
                case CardEffectKind.Repair: return "Repair";
                case CardEffectKind.TrueDmg: return "True DMG";
                default: return effect.ToString();
            }
        }

        private static CardRarity?[] Table(CardEffectKind effect)
        {
            // Columns: Attack, Defense, Ability.
            switch (effect)
            {
                case CardEffectKind.Bleed: return new CardRarity?[] { CardRarity.Common, null, CardRarity.Common };
                case CardEffectKind.Weak: return new CardRarity?[] { CardRarity.Common, null, CardRarity.Common };
                case CardEffectKind.Scar: return new CardRarity?[] { CardRarity.Common, null, CardRarity.Common };
                case CardEffectKind.Thorns: return new CardRarity?[] { null, CardRarity.Uncommon, CardRarity.Uncommon };
                case CardEffectKind.Stun: return new CardRarity?[] { CardRarity.Rare, null, CardRarity.Rare };
                case CardEffectKind.Block: return new CardRarity?[] { CardRarity.Common, null, null };
                case CardEffectKind.Disarmed: return new CardRarity?[] { CardRarity.Uncommon, null, CardRarity.Uncommon };
                case CardEffectKind.Repair: return new CardRarity?[] { null, CardRarity.Uncommon, CardRarity.Uncommon };
                case CardEffectKind.TrueDmg: return new CardRarity?[] { CardRarity.Rare, null, null };
                default: throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown effect kind.");
            }
        }
    }
}
