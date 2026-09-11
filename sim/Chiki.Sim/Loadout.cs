using System;
using System.Collections.Generic;
using System.Text;

namespace Chiki.Sim
{
    /// <summary>The outcome of placing a card in a loadout slot (PRD 3.5.2).</summary>
    public enum Placement
    {
        Accepted,

        /// <summary>The slot's key belongs to another Category (PRD 3.4.1).</summary>
        WrongCategory,

        /// <summary>The instance already sits in another slot; a Binder card occupies at most one (PRD 3.5.2).</summary>
        AlreadySlotted,

        /// <summary>The instance is not in the Binder the loadout is built from (PRD 3.5.5).</summary>
        NotInBinder,
    }

    /// <summary>A battle was requested with empty slots; <see cref="EmptySlots"/> lists them (PRD 3.5.5).</summary>
    public sealed class LoadoutIncompleteException : ArgumentException
    {
        public IReadOnlyList<Slot> EmptySlots { get; }

        public LoadoutIncompleteException(IReadOnlyList<Slot> emptySlots)
            : base("A loadout with an empty slot cannot enter battle. Empty: " + Loadout.Describe(emptySlots) + ".")
        {
            EmptySlots = emptySlots ?? throw new ArgumentNullException(nameof(emptySlots));
        }
    }

    /// <summary>
    /// The Battle Loadout (PRD 3.5.1, 4.6): exactly sixteen slots, one per key on each of the two
    /// lines, each holding at most one <see cref="CardInstance"/> of the slot's Category
    /// (PRD 3.5.2). Everything slotted is visible at all times; there is no draw pile, no hand
    /// and no discard anywhere in the simulation. A full loadout holds 2 Ability, 2 Left Attack,
    /// 2 Right Attack and 2 Defense cards per line because those are the keys (PRD 3.4.1).
    /// </summary>
    public sealed class Loadout
    {
        public const int SlotCount = Slot.LineCount * 8;

        private readonly Dictionary<Slot, CardInstance> _cards = new Dictionary<Slot, CardInstance>();

        /// <summary>All sixteen slots, line by line in key order (PRD 3.3.2.1).</summary>
        public IReadOnlyList<Slot> Slots => Slot.All;

        /// <summary>The card in a slot; null when the slot is empty.</summary>
        public CardInstance? this[Slot slot]
        {
            get
            {
                if (slot is null)
                {
                    throw new ArgumentNullException(nameof(slot));
                }

                return _cards.TryGetValue(slot, out var card) ? card : null;
            }
        }

        /// <summary>The card in the slot at a line (0 or 1) and key; null when empty.</summary>
        public CardInstance? CardAt(int line, SlotKey key)
        {
            return this[new Slot(line, key)];
        }

        /// <summary>The slotted cards in slot order.</summary>
        public IReadOnlyList<CardInstance> Cards
        {
            get
            {
                var cards = new List<CardInstance>();
                foreach (var slot in Slot.All)
                {
                    if (_cards.TryGetValue(slot, out var card))
                    {
                        cards.Add(card);
                    }
                }

                return cards;
            }
        }

        /// <summary>The slots holding nothing, in slot order (PRD 3.5.5, 3.5.10).</summary>
        public IReadOnlyList<Slot> EmptySlots
        {
            get
            {
                var empty = new List<Slot>();
                foreach (var slot in Slot.All)
                {
                    if (!_cards.ContainsKey(slot))
                    {
                        empty.Add(slot);
                    }
                }

                return empty;
            }
        }

        /// <summary>Whether every slot holds a card: the only loadout that may enter battle (PRD 3.5.5).</summary>
        public bool IsComplete => _cards.Count == SlotCount;

        /// <summary>How many cards of a Category a line holds; 2 each on a complete loadout (PRD 3.5.2).</summary>
        public int Count(int line, CardCategory category)
        {
            int count = 0;
            foreach (var pair in _cards)
            {
                if (pair.Key.Line == line && pair.Value.Definition.Category == category)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>The slot an instance sits in; null when it is not slotted.</summary>
        public Slot? SlotOf(CardInstance card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            foreach (var pair in _cards)
            {
                if (ReferenceEquals(pair.Value, card))
                {
                    return pair.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Puts a card in a slot (PRD 3.5.2, 3.5.5): refused when the slot's key is not the card's
        /// Category's or the instance already sits elsewhere; a card already in the slot is
        /// replaced and becomes unslotted. Placing a card where it already is changes nothing.
        /// </summary>
        public Placement Assign(Slot slot, CardInstance card)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!card.Definition.Category.Allows(slot.Key))
            {
                return Placement.WrongCategory;
            }

            var current = SlotOf(card);
            if (current != null)
            {
                return current == slot ? Placement.Accepted : Placement.AlreadySlotted;
            }

            _cards[slot] = card;
            return Placement.Accepted;
        }

        /// <summary>Empties a slot, returning the card it held; null when it was already empty (PRD 3.5.5).</summary>
        public CardInstance? Clear(Slot slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            if (_cards.TryGetValue(slot, out var card))
            {
                _cards.Remove(slot);
                return card;
            }

            return null;
        }

        /// <summary>Removes an instance from whichever slot it sits in, leaving the slot empty (PRD 3.5.10); false when it was not slotted.</summary>
        public bool Unslot(CardInstance card)
        {
            var slot = SlotOf(card);
            if (slot is null)
            {
                return false;
            }

            _cards.Remove(slot);
            return true;
        }

        /// <summary>Throws <see cref="LoadoutIncompleteException"/> naming the empty slots unless the loadout is complete (PRD 3.5.5).</summary>
        public void RequireComplete()
        {
            var empty = EmptySlots;
            if (empty.Count > 0)
            {
                throw new LoadoutIncompleteException(empty);
            }
        }

        /// <summary>Slots as the player reads them, line 1 or 2 and the key: "(2, I)".</summary>
        public static string Describe(IReadOnlyList<Slot> slots)
        {
            if (slots is null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            var text = new StringBuilder();
            for (int i = 0; i < slots.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }

                text.Append('(').Append(slots[i].Line + 1).Append(", ").Append(slots[i].Key).Append(')');
            }

            return text.ToString();
        }
    }
}
