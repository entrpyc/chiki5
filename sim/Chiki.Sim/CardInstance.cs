using System;

namespace Chiki.Sim
{
    /// <summary>
    /// One owned copy of a card (PRD 4.5): the definition it references, whether it has been
    /// upgraded (PRD 3.4.18), its Trait slot (PRD 3.4.19; always empty in this plan), the battles
    /// an Unstable card has left (PRD 3.4.16) and its shop price while in a shop (PRD 3.7.7;
    /// unset in this plan). Two instances of one definition are distinct objects with their own
    /// state; <see cref="Id"/> is assigned by whoever owns the instance (the Binder, P16.1).
    /// </summary>
    public sealed class CardInstance
    {
        public int Id { get; }

        public CardDefinition Definition { get; }

        public bool Upgraded { get; private set; }

        /// <summary>The Trait on this card, at most one (PRD 3.4.19); null until the Forge plan builds Traits.</summary>
        public string? TraitId { get; private set; }

        /// <summary>Battles left before an Unstable card is destroyed (PRD 3.4.16); null for other classes.</summary>
        public int? BattlesRemaining { get; private set; }

        /// <summary>The price rolled while the card sits in a shop (PRD 3.7.7, 3.7.8); null elsewhere.</summary>
        public int? ShopPrice { get; private set; }

        /// <summary>The value the card plays with: its definition's, plus the upgrade step once upgraded (PRD 3.4.18).</summary>
        public int Value => Definition.Value + (Upgraded ? Definition.UpgradeStep : 0);

        /// <summary>An Unstable card at 0 battles remaining is destroyed (PRD 3.4.16).</summary>
        public bool IsExpired => BattlesRemaining == 0;

        public CardInstance(int id, CardDefinition definition)
        {
            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            BattlesRemaining = definition.Class == CardClass.Unstable ? definition.Lifespan : null;
        }

        /// <summary>An instance as a saved run recorded it (PRD 4.5), with every tracked field restored.</summary>
        public static CardInstance Restore(int id, CardDefinition definition, bool upgraded, string? traitId, int? battlesRemaining, int? shopPrice)
        {
            var instance = new CardInstance(id, definition);
            if ((definition.Class == CardClass.Unstable) != (battlesRemaining != null))
            {
                throw new ArgumentException("Exactly an Unstable card has battles remaining.", nameof(battlesRemaining));
            }

            if (battlesRemaining < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(battlesRemaining), "Battles remaining must not be negative.");
            }

            instance.Upgraded = upgraded;
            instance.TraitId = traitId;
            instance.BattlesRemaining = battlesRemaining;
            instance.SetShopPrice(shopPrice);
            return instance;
        }

        /// <summary>Counts one battle the card was in the Binder for (PRD 3.4.16); returns true when that used up its lifespan.</summary>
        public bool CountBattle()
        {
            if (BattlesRemaining is null || BattlesRemaining.Value == 0)
            {
                return false;
            }

            BattlesRemaining--;
            return BattlesRemaining == 0;
        }

        /// <summary>Applies the one upgrade a card may take per run (PRD 3.4.18); returns false when already upgraded.</summary>
        public bool Upgrade()
        {
            if (Upgraded)
            {
                return false;
            }

            Upgraded = true;
            return true;
        }

        public void SetShopPrice(int? price)
        {
            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), "A price must not be negative.");
            }

            ShopPrice = price;
        }
    }
}
