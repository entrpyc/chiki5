using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>The sources an Essence change names besides a card, Imprint or Charm id (PRD 3.7.1).</summary>
    public static class EssenceSources
    {
        /// <summary>Income rolled for a won battle (PRD 3.7.5).</summary>
        public const string BattleReward = "battle reward";
    }

    /// <summary>
    /// A battle node's reward offer (PRD 3.7.2): the cards the player may choose one of, and
    /// the Essence already paid into the stats when the offer opened (PRD 3.7.5). The offer
    /// stays open until the player picks one card or skips; the run cannot move on before
    /// (PRD 3.3.9.2).
    /// </summary>
    public sealed class RewardOffer
    {
        public EncounterTier Tier { get; }

        /// <summary>The node whose battle paid the reward.</summary>
        public string NodeId { get; }

        /// <summary>The distinct cards offered, in roll order; the player takes at most one.</summary>
        public IReadOnlyList<CardDefinition> Cards { get; }

        /// <summary>The Essence income rolled for the win (PRD 3.7.5), already added to the run stats.</summary>
        public int Essence { get; }

        /// <summary>Whether the player has picked or skipped.</summary>
        public bool IsResolved { get; private set; }

        /// <summary>The card picked; null while open or after a skip.</summary>
        public CardDefinition? Picked { get; private set; }

        public RewardOffer(EncounterTier tier, string nodeId, IReadOnlyList<CardDefinition> cards, int essence)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException("An offer names its node.", nameof(nodeId));
            }

            if (essence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(essence), "Essence income must not be negative.");
            }

            Tier = tier;
            NodeId = nodeId;
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
            Essence = essence;
        }

        public bool Offers(CardDefinition card)
        {
            foreach (var offered in Cards)
            {
                if (offered.Id == card.Id)
                {
                    return true;
                }
            }

            return false;
        }

        internal void Resolve(CardDefinition? picked)
        {
            if (IsResolved)
            {
                throw new InvalidOperationException("The offer is already resolved.");
            }

            IsResolved = true;
            Picked = picked;
        }
    }

    /// <summary>
    /// The rules of battle rewards (PRD 3.7.2, 3.7.5, 3.4.14, 3.4.15): which loaded cards a
    /// reward may offer, how many and of which rarities, and the Essence income band per tier
    /// and World. Every roll draws only from the generator it is handed (PRD 3.2.4).
    /// </summary>
    public static class Rewards
    {
        /// <summary>The rarities a Normal battle reward offers (PRD 3.7.2).</summary>
        public static readonly IReadOnlyList<CardRarity> NormalRarities = new[] { CardRarity.Common, CardRarity.Uncommon };

        /// <summary>
        /// The reward pool: every card of class Normal (PRD 3.4.14). Event and Unstable cards
        /// come from Events and Sacrifice only and are never offered (PRD 3.4.15, 3.4.16).
        /// </summary>
        public static IReadOnlyList<CardDefinition> Pool(IEnumerable<CardDefinition> cards)
        {
            return Pool(cards, null);
        }

        /// <summary>The reward pool narrowed to the given rarities; null keeps every rarity.</summary>
        public static IReadOnlyList<CardDefinition> Pool(IEnumerable<CardDefinition> cards, IReadOnlyCollection<CardRarity>? rarities)
        {
            if (cards is null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var pool = new List<CardDefinition>();
            foreach (var card in cards)
            {
                if (card.Class == CardClass.Normal && (rarities is null || Contains(rarities, card.Rarity)))
                {
                    pool.Add(card);
                }
            }

            return pool;
        }

        /// <summary>Essence income for a won battle of the tier in the World, rolled inside the band of PRD 3.7.5.</summary>
        public static int RollEssence(Rng rng, EncounterTier tier, int world)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (world < 1 || world > Tuning.WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(world), $"World is 1 to {Tuning.WorldCount}.");
            }

            int min = Tuning.EssenceIncomeMin[(int)tier][world - 1];
            int max = Tuning.EssenceIncomeMax[(int)tier][world - 1];
            return rng.NextInt(min, max + 1);
        }

        /// <summary>
        /// Up to <paramref name="count"/> distinct cards drawn from the pool without
        /// replacement, in draw order; fewer when the pool is smaller.
        /// </summary>
        public static IReadOnlyList<CardDefinition> RollCards(Rng rng, IReadOnlyList<CardDefinition> pool, int count)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (pool is null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count must not be negative.");
            }

            var remaining = new List<CardDefinition>(pool);
            var drawn = new List<CardDefinition>();
            while (drawn.Count < count && remaining.Count > 0)
            {
                int index = rng.NextInt(0, remaining.Count);
                drawn.Add(remaining[index]);
                remaining.RemoveAt(index);
            }

            return drawn;
        }

        /// <summary>
        /// A Normal battle's offer (PRD 3.7.2): the Essence income of the World (PRD 3.7.5) and
        /// three distinct Common or Uncommon cards from the pool of the loaded cards (PRD 3.4.14).
        /// </summary>
        public static RewardOffer RollNormal(Rng rng, IEnumerable<CardDefinition> cards, int world, string nodeId)
        {
            int essence = RollEssence(rng, EncounterTier.Normal, world);
            var offered = RollCards(rng, Pool(cards, NormalRarities), Tuning.NormalRewardCardChoices);
            return new RewardOffer(EncounterTier.Normal, nodeId, offered, essence);
        }

        private static bool Contains(IReadOnlyCollection<CardRarity> rarities, CardRarity rarity)
        {
            foreach (var allowed in rarities)
            {
                if (allowed == rarity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
