using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>
    /// The Run Binder (PRD 3.5.3, 3.5.4): every card instance the player owns this run, and the
    /// <see cref="Loadout"/> built from it. The Binder assigns instance ids, is the only way a
    /// card enters or leaves the run, and counts Unstable lifespans down at the end of every
    /// battle it holds them for (PRD 3.4.16). A card destroyed by lifespan leaves its slot empty
    /// (PRD 3.5.10). Discarding the Binder at run end is the run's job (P18.2).
    /// </summary>
    public sealed class Binder
    {
        private readonly List<CardInstance> _cards = new List<CardInstance>();
        private int _nextId = 1;

        /// <summary>Every instance in the Binder, in acquisition order.</summary>
        public IReadOnlyList<CardInstance> Cards => _cards;

        /// <summary>The sixteen-slot loadout built from this Binder (PRD 3.5.1).</summary>
        public Loadout Loadout { get; } = new Loadout();

        /// <summary>The instances not sitting in any slot, in acquisition order (PRD 3.5.5).</summary>
        public IReadOnlyList<CardInstance> Unslotted
        {
            get
            {
                var unslotted = new List<CardInstance>();
                foreach (var card in _cards)
                {
                    if (Loadout.SlotOf(card) is null)
                    {
                        unslotted.Add(card);
                    }
                }

                return unslotted;
            }
        }

        /// <summary>The starter Binder: one instance of every card in the starter set (PRD 3.5.3).</summary>
        public static Binder Starter(CardSet starterSet)
        {
            if (starterSet is null)
            {
                throw new ArgumentNullException(nameof(starterSet));
            }

            var binder = new Binder();
            foreach (var card in starterSet.Cards)
            {
                binder.Add(card);
            }

            return binder;
        }

        /// <summary>Acquires a card: a new instance of the definition enters the Binder (PRD 3.4.12, 3.5.4).</summary>
        public CardInstance Add(CardDefinition definition)
        {
            var instance = new CardInstance(_nextId++, definition);
            _cards.Add(instance);
            return instance;
        }

        public bool Contains(CardInstance card)
        {
            return card != null && _cards.Contains(card);
        }

        /// <summary>The instance with the given id; null when none.</summary>
        public CardInstance? Find(int id)
        {
            foreach (var card in _cards)
            {
                if (card.Id == id)
                {
                    return card;
                }
            }

            return null;
        }

        /// <summary>Destroys an instance: it leaves the Binder and its slot, if any, is left empty (PRD 3.5.10); false when it was not in the Binder.</summary>
        public bool Remove(CardInstance card)
        {
            if (!Contains(card))
            {
                return false;
            }

            Loadout.Unslot(card);
            _cards.Remove(card);
            return true;
        }

        /// <summary>Places a Binder card in a slot (PRD 3.5.5); a card not in this Binder is refused.</summary>
        public Placement Assign(Slot slot, CardInstance card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!Contains(card))
            {
                return Placement.NotInBinder;
            }

            return Loadout.Assign(slot, card);
        }

        /// <summary>Empties a slot, returning the card it held (PRD 3.5.5).</summary>
        public CardInstance? Clear(Slot slot)
        {
            return Loadout.Clear(slot);
        }

        /// <summary>
        /// Fills every empty slot, in slot order, with the first unslotted Binder card of the
        /// slot's Category, in acquisition order (PRD 3.5.3); slots already holding a card keep
        /// it. Returns the slots still empty because the Binder ran out of that Category.
        /// </summary>
        public IReadOnlyList<Slot> AutoFill()
        {
            foreach (var slot in Loadout.EmptySlots)
            {
                foreach (var card in _cards)
                {
                    if (card.Definition.Category.Allows(slot.Key) && Loadout.SlotOf(card) is null)
                    {
                        Loadout.Assign(slot, card);
                        break;
                    }
                }
            }

            return Loadout.EmptySlots;
        }

        /// <summary>
        /// A battle ended with these cards in the Binder: every Unstable card counts one battle
        /// (PRD 3.4.16), slotted or not, and those at 0 are destroyed and removed from the
        /// Binder and the loadout. Returns the destroyed instances in Binder order.
        /// </summary>
        public IReadOnlyList<CardInstance> BattleEnded()
        {
            var destroyed = new List<CardInstance>();
            foreach (var card in _cards)
            {
                if (card.CountBattle())
                {
                    destroyed.Add(card);
                }
            }

            foreach (var card in destroyed)
            {
                Remove(card);
            }

            return destroyed;
        }
    }
}
