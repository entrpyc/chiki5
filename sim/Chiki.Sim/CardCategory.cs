using System;
using System.Collections.Generic;

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
        /// <summary>The Category whose cards a key holds: Q/W Ability, E/R Left Attack, U/I Right Attack, O/P Defense.</summary>
        public static CardCategory ForKey(SlotKey key)
        {
            switch (key)
            {
                case SlotKey.Q:
                case SlotKey.W:
                    return CardCategory.Ability;
                case SlotKey.E:
                case SlotKey.R:
                    return CardCategory.LeftAttack;
                case SlotKey.U:
                case SlotKey.I:
                    return CardCategory.RightAttack;
                case SlotKey.O:
                case SlotKey.P:
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

        /// <summary>The slots a Category's cards may sit in: its two keys on both lines (PRD 3.4.1).</summary>
        public static IReadOnlyList<Slot> SlotsFor(CardCategory category)
        {
            var slots = new List<Slot>();
            foreach (var slot in Slot.All)
            {
                if (category.Allows(slot.Key))
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        /// <summary>Left Attack and Right Attack are the attack Categories (PRD 3.4.3).</summary>
        public static bool IsAttack(this CardCategory category)
        {
            return category == CardCategory.LeftAttack || category == CardCategory.RightAttack;
        }
    }
}
