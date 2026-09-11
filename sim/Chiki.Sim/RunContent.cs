using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>
    /// The content a run draws on: the starter set that fills its first Binder (PRD 3.5.3),
    /// every other card set its instances may name, the Charm table (PRD 4.9) and the Imprint
    /// pool (PRD 4.10). Loaded once at start and shared by every run; the run holds ids and
    /// resolves them here.
    /// </summary>
    public sealed class RunContent
    {
        private readonly Dictionary<string, CardDefinition> _cards = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CharmDefinition> _charms = new Dictionary<string, CharmDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, ImprintDefinition> _imprints = new Dictionary<string, ImprintDefinition>(StringComparer.Ordinal);

        public CardSet Starter { get; }

        public CharmSet Charms { get; }

        public ImprintSet Imprints { get; }

        /// <summary>Every card definition by id, from the starter set and the other sets.</summary>
        public IReadOnlyDictionary<string, CardDefinition> Cards => _cards;

        public RunContent(CardSet starter, CharmSet? charms = null, ImprintSet? imprints = null, IReadOnlyList<CardSet>? otherSets = null)
        {
            Starter = starter ?? throw new ArgumentNullException(nameof(starter));
            Charms = charms ?? new CharmSet("none", Array.Empty<CharmDefinition>());
            Imprints = imprints ?? new ImprintSet("none", Array.Empty<ImprintDefinition>());

            AddCards(starter);
            if (otherSets != null)
            {
                foreach (var set in otherSets)
                {
                    AddCards(set);
                }
            }

            foreach (var charm in Charms.Charms)
            {
                _charms[charm.Id] = charm;
            }

            foreach (var imprint in Imprints.Imprints)
            {
                _imprints[imprint.Id] = imprint;
            }
        }

        public CardDefinition? FindCard(string id)
        {
            return _cards.TryGetValue(id, out var card) ? card : null;
        }

        public CharmDefinition? FindCharm(string id)
        {
            return _charms.TryGetValue(id, out var charm) ? charm : null;
        }

        public ImprintDefinition? FindImprint(string id)
        {
            return _imprints.TryGetValue(id, out var imprint) ? imprint : null;
        }

        private void AddCards(CardSet set)
        {
            if (set is null)
            {
                throw new ArgumentException("A card set must not be null.");
            }

            foreach (var card in set.Cards)
            {
                _cards[card.Id] = card;
            }
        }
    }
}
