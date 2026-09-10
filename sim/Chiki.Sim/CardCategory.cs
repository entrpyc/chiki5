using System;

namespace Chiki.Sim
{
    /// <summary>The one Category every card belongs to (PRD 3.4.1); it fixes the card's legal slots.</summary>
    public enum CardCategory
    {
        Ability,
        LeftAttack,
        RightAttack,
        Defense,
    }

    /// <summary>The fixed mapping between Categories and slot keys (PRD 3.4.1, 3.3.2.1).</summary>
    public static class CardCategories
    {
        /// <summary>The Category whose cards a key holds: A/S Ability, D/F Left Attack, J/K Right Attack, L/; Defense.</summary>
        public static CardCategory ForKey(SlotKey key)
        {
            switch (key)
            {
                case SlotKey.A:
                case SlotKey.S:
                    return CardCategory.Ability;
                case SlotKey.D:
                case SlotKey.F:
                    return CardCategory.LeftAttack;
                case SlotKey.J:
                case SlotKey.K:
                    return CardCategory.RightAttack;
                case SlotKey.L:
                case SlotKey.Semicolon:
                    return CardCategory.Defense;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown slot key.");
            }
        }

        /// <summary>Whether a card of this Category may sit in a slot with the given key.</summary>
        public static bool Allows(this CardCategory category, SlotKey key)
        {
            return ForKey(key) == category;
        }

        /// <summary>Left Attack and Right Attack are the attack Categories (PRD 3.4.3).</summary>
        public static bool IsAttack(this CardCategory category)
        {
            return category == CardCategory.LeftAttack || category == CardCategory.RightAttack;
        }
    }
}
