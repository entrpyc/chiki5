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
    /// A battle node's reward offer (PRD 3.7.2–3.7.4): the cards the player may choose one of,
    /// the Essence already paid into the stats when the offer opened (PRD 3.7.5) and, for an
    /// Elite or Boss, the Imprint already acquired (PRD 3.9.3). The offer stays open until the
    /// player picks one card or skips; the run cannot move on before (PRD 3.3.9.2).
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

        /// <summary>The tier of the Imprint an Elite or Boss win grants (PRD 3.7.3, 3.7.4); null for a Normal win.</summary>
        public ImprintTier? ImprintTier { get; }

        /// <summary>The id of the Imprint acquired when the offer opened; null for a Normal win.</summary>
        public string? ImprintId { get; internal set; }

        /// <summary>Whether the player has picked or skipped.</summary>
        public bool IsResolved { get; private set; }

        /// <summary>The card picked; null while open or after a skip.</summary>
        public CardDefinition? Picked { get; private set; }

        public RewardOffer(EncounterTier tier, string nodeId, IReadOnlyList<CardDefinition> cards, int essence, ImprintTier? imprintTier = null)
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
            ImprintTier = imprintTier;
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
    /// The rules of battle rewards (PRD 3.7.2–3.7.5, 3.4.14, 3.4.15, 3.9.3): which loaded cards
    /// a reward may offer, how many and of which rarities per tier, the Imprint tier an Elite or
    /// Boss grants, and the Essence income band per tier and World. Every roll draws only from
    /// the generator it is handed (PRD 3.2.4).
    /// </summary>
    public static class Rewards
    {
        /// <summary>The rarities a Normal battle reward offers (PRD 3.7.2).</summary>
        public static readonly IReadOnlyList<CardRarity> NormalRarities = new[] { CardRarity.Common, CardRarity.Uncommon };

        /// <summary>The rarities an Elite or Boss reward offers (PRD 3.7.3, 3.7.4).</summary>
        public static readonly IReadOnlyList<CardRarity> HighRarities = new[] { CardRarity.Rare, CardRarity.Legendary };

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

        /// <summary>The rarities a reward of the tier offers.</summary>
        public static IReadOnlyList<CardRarity> RaritiesFor(EncounterTier tier)
        {
            return tier == EncounterTier.Normal ? NormalRarities : HighRarities;
        }

        /// <summary>How many cards a reward of the tier offers, of which the player takes one (PRD 3.7.2–3.7.4).</summary>
        public static int CardChoicesFor(EncounterTier tier)
        {
            switch (tier)
            {
                case EncounterTier.Normal: return Tuning.NormalRewardCardChoices;
                case EncounterTier.Elite: return Tuning.EliteRewardCardChoices;
                case EncounterTier.Boss: return Tuning.BossRewardCardChoices;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown tier.");
            }
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

        /// <summary>The tier of a dropped Imprint, rolled from the tier table (PRD 3.9.3): Common, Uncommon or Rare at the odds in <see cref="Tuning.ImprintTierOddsPercent"/>.</summary>
        public static ImprintTier RollImprintTier(Rng rng)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            int roll = rng.NextInt(0, 100);
            int threshold = 0;
            for (int i = 0; i < Tuning.ImprintTierOddsPercent.Length; i++)
            {
                threshold += Tuning.ImprintTierOddsPercent[i];
                if (roll < threshold)
                {
                    return (ImprintTier)i;
                }
            }

            return ImprintTier.Rare;
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
        /// The offer of a won battle of the tier (PRD 3.7.2–3.7.4): the Essence income of the
        /// tier and World (PRD 3.7.5), the tier's count of distinct cards of its rarities from
        /// the pool of the loaded cards (PRD 3.4.14), and for an Elite or Boss the tier of the
        /// Imprint it grants (PRD 3.9.3). The rolls are drawn in that order.
        /// </summary>
        public static RewardOffer Roll(Rng rng, EncounterTier tier, IEnumerable<CardDefinition> cards, int world, string nodeId)
        {
            int essence = RollEssence(rng, tier, world);
            var offered = RollCards(rng, Pool(cards, RaritiesFor(tier)), CardChoicesFor(tier));
            ImprintTier? imprintTier = tier == EncounterTier.Normal ? (ImprintTier?)null : RollImprintTier(rng);
            return new RewardOffer(tier, nodeId, offered, essence, imprintTier);
        }

        /// <summary>A Normal battle's offer (PRD 3.7.2): three distinct Common or Uncommon cards plus the World's Essence income.</summary>
        public static RewardOffer RollNormal(Rng rng, IEnumerable<CardDefinition> cards, int world, string nodeId)
        {
            return Roll(rng, EncounterTier.Normal, cards, world, nodeId);
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
