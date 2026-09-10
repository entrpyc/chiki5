using System;

namespace Chiki.Sim
{
    /// <summary>The one Rarity every card has (PRD 3.4.4); it fixes the card's power band.</summary>
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    /// <summary>An inclusive band a value must fall in; <see cref="Max"/> is null when the band is open at the top.</summary>
    public readonly struct ValueBand
    {
        public int Min { get; }

        public int? Max { get; }

        public ValueBand(int min, int? max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(int value)
        {
            return value >= Min && (Max is null || value <= Max.Value);
        }

        public override string ToString()
        {
            return Max is null ? $"{Min}+" : $"{Min}–{Max}";
        }
    }

    /// <summary>The scaling bands of PRD 3.4.4: damage for attack cards, Block for Defense cards.</summary>
    public static class RarityBands
    {
        public static ValueBand Damage(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new ValueBand(Tuning.CommonDamageMin, Tuning.CommonDamageMax);
                case CardRarity.Uncommon: return new ValueBand(Tuning.UncommonDamageMin, Tuning.UncommonDamageMax);
                case CardRarity.Rare: return new ValueBand(Tuning.RareDamageMin, Tuning.RareDamageMax);
                case CardRarity.Legendary: return new ValueBand(Tuning.LegendaryDamageMin, Tuning.LegendaryDamageMax);
                default: throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity.");
            }
        }

        public static ValueBand Block(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new ValueBand(Tuning.CommonBlockMin, Tuning.CommonBlockMax);
                case CardRarity.Uncommon: return new ValueBand(Tuning.UncommonBlockMin, Tuning.UncommonBlockMax);
                case CardRarity.Rare: return new ValueBand(Tuning.RareBlockMin, Tuning.RareBlockMax);
                case CardRarity.Legendary: return new ValueBand(Tuning.LegendaryBlockMin, null);
                default: throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown rarity.");
            }
        }
    }
}
