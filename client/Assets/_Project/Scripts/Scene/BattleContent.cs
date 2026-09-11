#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Content;
using Chiki.Sim;
using Chiki.Sim.Data;

namespace Chiki.Client.Scene
{
    /// <summary>
    /// The JSON content under <c>data/</c> loaded for a battle: every track, every chart on its
    /// track, the fixture enemies and the starter card set. Definitions never reference art.
    /// </summary>
    public sealed class BattleContent
    {
        private readonly Dictionary<string, Track> _tracks;
        private readonly Dictionary<string, Chart> _charts;
        private readonly Dictionary<string, EnemyDefinition> _enemies;

        public IReadOnlyDictionary<string, Track> Tracks => _tracks;

        public IReadOnlyDictionary<string, Chart> Charts => _charts;

        public IReadOnlyDictionary<string, EnemyDefinition> Enemies => _enemies;

        public CardSet Cards { get; }

        private BattleContent(Dictionary<string, Track> tracks, Dictionary<string, Chart> charts, Dictionary<string, EnemyDefinition> enemies, CardSet cards)
        {
            _tracks = tracks;
            _charts = charts;
            _enemies = enemies;
            Cards = cards;
        }

        public EnemyDefinition Enemy(string id)
        {
            return _enemies.TryGetValue(id, out var enemy) ? enemy : throw new KeyNotFoundException("No enemy '" + id + "' in data/enemies.");
        }

        /// <summary>Reads data/tracks, data/charts, data/enemies/fixtures.json and data/sets/starter.json.</summary>
        public static BattleContent LoadFixtures()
        {
            var tracks = new Dictionary<string, Track>();
            foreach (var path in Directory.GetFiles(Path.Combine(ContentFiles.DataRoot, "tracks"), "*.json"))
            {
                var track = TrackLoader.FromJson(File.ReadAllText(path));
                tracks[track.Id] = track;
            }

            var charts = new Dictionary<string, Chart>();
            foreach (var path in Directory.GetFiles(Path.Combine(ContentFiles.DataRoot, "charts"), "*.json"))
            {
                var document = ChartLoader.Read(File.ReadAllText(path));
                if (!tracks.TryGetValue(document.TrackId, out var track))
                {
                    throw new InvalidDataException("Chart '" + document.Id + "' names track '" + document.TrackId + "', which data/tracks does not hold.");
                }

                var chart = ChartLoader.Build(document, track);
                charts[chart.Id] = chart;
            }

            var enemies = new Dictionary<string, EnemyDefinition>();
            foreach (var enemy in EnemyLoader.SetFromJson(ContentFiles.ReadText("enemies/fixtures.json"), charts).Enemies)
            {
                enemies[enemy.Id] = enemy;
            }

            var cards = CardLoader.SetFromJson(ContentFiles.ReadText("sets/starter.json"));
            return new BattleContent(tracks, charts, enemies, cards);
        }

        /// <summary>
        /// Fills the sixteen slots from a card set, each slot with a card of its key's Category
        /// (PRD 3.4.1), cycling through the set's cards of that Category in order. The Loadout
        /// itself is a later plan item; this is the fixture the presenters run against.
        /// </summary>
        public static IReadOnlyDictionary<Slot, CardDefinition> FillSlots(CardSet set)
        {
            if (set is null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            var byCategory = new Dictionary<CardCategory, List<CardDefinition>>();
            foreach (var card in set.Cards)
            {
                if (!byCategory.TryGetValue(card.Category, out var list))
                {
                    list = new List<CardDefinition>();
                    byCategory[card.Category] = list;
                }

                list.Add(card);
            }

            var next = new Dictionary<CardCategory, int>();
            var slots = new Dictionary<Slot, CardDefinition>();
            foreach (var slot in Slot.All)
            {
                var category = CardCategories.ForKey(slot.Key);
                if (!byCategory.TryGetValue(category, out var cards) || cards.Count == 0)
                {
                    continue;
                }

                next.TryGetValue(category, out int index);
                slots[slot] = cards[index % cards.Count];
                next[category] = index + 1;
            }

            return slots;
        }
    }
}
