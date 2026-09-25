#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>One ability or trait as the enemy card shows it: its icon and its name (PRD 3.6.26).</summary>
    public sealed class EnemyPowerView
    {
        public EnemyPowerView(string kind, string id, Image icon, UnityEngine.UI.Text name)
        {
            Kind = kind;
            Id = id;
            Icon = icon;
            Name = name;
        }

        /// <summary>The catalogue kind the icon is looked up under: <c>ability</c> or <c>trait</c>.</summary>
        public string Kind { get; }

        public string Id { get; }

        public Image Icon { get; }

        public UnityEngine.UI.Text Name { get; }
    }

    /// <summary>
    /// The enemy card (PRD 3.6.26): portrait, name, BPM from the track's starting tempo, each
    /// ability and trait as icon and name, the quote line and a badge. The badge reads New while
    /// the profile has never finished a battle against the enemy, otherwise its role icon and
    /// role name (P8.2). The full card sits in the pre-battle panel; the compact one, without the
    /// quote, sits beside the Binder's slots while the loadout is edited (PRD 3.5.8, P8.5).
    /// Every name comes from the enemy definition or the string table, every image from the
    /// visual catalogue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyCard : MonoBehaviour
    {
        public const string PortraitKind = "portrait";
        public const string AbilityKind = "ability";
        public const string TraitKind = "trait";
        public const string RoleKind = "role";
        public const string UiKind = "ui";

        /// <summary>The 9-slice plate the New badge is drawn on (P8.2).</summary>
        public const string BadgeNewId = "badge-new";

        public static readonly Vector2 FullSize = new Vector2(1140f, 480f);
        public static readonly Vector2 CompactSize = new Vector2(800f, 250f);

        private static readonly Color BadgeInk = new Color(0.1f, 0.08f, 0.12f, 1f);

        private readonly List<EnemyPowerView> _powers = new List<EnemyPowerView>();

        public EnemyDefinition Enemy { get; private set; } = null!;

        public bool Compact { get; private set; }

        public Image Portrait { get; private set; } = null!;

        public UnityEngine.UI.Text NameText { get; private set; } = null!;

        public UnityEngine.UI.Text BpmText { get; private set; } = null!;

        /// <summary>The quote line; null on the compact card.</summary>
        public UnityEngine.UI.Text? QuoteText { get; private set; }

        /// <summary>Every ability, then every trait, in the order the definition lists them.</summary>
        public IReadOnlyList<EnemyPowerView> Powers => _powers;

        /// <summary>Whether the badge reads New: the profile has never fought this enemy (P8.2).</summary>
        public bool BadgeIsNew { get; private set; }

        /// <summary>The badge's plate: the New plate, or the role icon once the enemy has been fought.</summary>
        public Image Badge { get; private set; } = null!;

        public UnityEngine.UI.Text BadgeLabel { get; private set; } = null!;

        /// <summary>
        /// Builds the card under a parent at a position. <paramref name="fought"/> is whether the
        /// profile's <c>enemiesFought</c> holds the enemy (P8.1).
        /// </summary>
        public static EnemyCard Build(Transform parent, EnemyDefinition enemy, bool fought, bool compact, Vector2 anchoredPosition)
        {
            if (enemy is null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            var size = compact ? CompactSize : FullSize;
            var root = HudFactory.Rect(compact ? "EnemyCardCompact" : "EnemyCard", parent, anchoredPosition, size);
            var card = root.gameObject.AddComponent<EnemyCard>();
            card.Enemy = enemy;
            card.Compact = compact;
            card.BadgeIsNew = !fought;
            var catalogue = VisualCatalogue.Active;

            // The portrait on the left, square, the badge under it.
            float portraitSize = compact ? 190f : 340f;
            float left = -size.x / 2f + portraitSize / 2f + 20f;
            float portraitY = compact ? 20f : 50f;
            card.Portrait = HudFactory.Image("Portrait", root, Color.white, new Vector2(left, portraitY), new Vector2(portraitSize, portraitSize), catalogue.Sprite(PortraitKind, PortraitSubject(enemy)));
            card.Portrait.preserveAspect = true;
            card.BuildBadge(root, catalogue, new Vector2(left, portraitY - portraitSize / 2f - (compact ? 16f : 34f)), compact);

            // The column to the portrait's right: name, BPM, powers, quote.
            float columnLeft = left + portraitSize / 2f + 30f;
            float columnWidth = size.x / 2f - columnLeft - 10f;
            float columnCentre = columnLeft + columnWidth / 2f;
            float top = size.y / 2f;

            card.NameText = ScreenFactory.Label("Name", root, enemy.Name, compact ? 40 : 56, new Vector2(columnCentre, top - (compact ? 30f : 44f)), new Vector2(columnWidth, compact ? 50f : 70f), TextAnchor.MiddleLeft);
            card.BpmText = ScreenFactory.Label("Bpm", root, Strings.Format("enemycard.bpm", Bpm(enemy)), compact ? 24 : 30, new Vector2(columnCentre, top - (compact ? 72f : 104f)), new Vector2(columnWidth, 40f), TextAnchor.MiddleLeft, ScreenFactory.MutedText);

            var powers = new List<(string Kind, string Id)>();
            foreach (var ability in enemy.Abilities)
            {
                powers.Add((AbilityKind, EnemyLoader.AbilityToId(ability)));
            }

            foreach (var trait in enemy.Traits)
            {
                powers.Add((TraitKind, EnemyLoader.TraitToId(trait)));
            }

            float iconSize = compact ? 40f : 48f;
            for (int i = 0; i < powers.Count; i++)
            {
                Vector2 at;
                float nameWidth;
                if (compact)
                {
                    // Two to a row, so four powers fit beside the slots.
                    float half = columnWidth / 2f;
                    at = new Vector2(columnLeft + (i % 2) * half, top - 116f - (i / 2) * 50f);
                    nameWidth = half - iconSize - 16f;
                }
                else
                {
                    at = new Vector2(columnLeft, top - 168f - i * 56f);
                    nameWidth = columnWidth - iconSize - 16f;
                }

                card.AddPower(root, catalogue, powers[i].Kind, powers[i].Id, at, iconSize, nameWidth, compact ? 22 : 30);
            }

            if (!compact)
            {
                float quoteY = top - 168f - Math.Max(powers.Count, 1) * 56f - 30f;
                card.QuoteText = ScreenFactory.Label("Quote", root, Strings.Format("enemycard.quote", enemy.QuoteLine ?? ""), 28, new Vector2(columnCentre, quoteY), new Vector2(columnWidth, 80f), TextAnchor.UpperLeft, ScreenFactory.Accent);
                card.QuoteText.fontStyle = FontStyle.Italic;
                card.QuoteText.gameObject.SetActive(!string.IsNullOrEmpty(enemy.QuoteLine));
            }

            return card;
        }

        /// <summary>The starting tempo of the enemy's own track (PRD 3.6.28), which the card shows as its BPM.</summary>
        public static int Bpm(EnemyDefinition enemy)
        {
            return enemy.Track.Tempo.StartBpm;
        }

        /// <summary>
        /// The catalogue subject of an enemy's portrait: its portrait id without the kind and
        /// content prefixes, so <c>portrait-enemy-ren</c> resolves to <c>spr_portrait_ren_static_01</c>
        /// (P8.2). An enemy without a portrait id falls back to its own id's subject.
        /// </summary>
        public static string PortraitSubject(EnemyDefinition enemy)
        {
            string id = string.IsNullOrWhiteSpace(enemy.PortraitId) ? enemy.Id : enemy.PortraitId!;
            id = StripPrefix(id, PortraitKind + "-");
            return StripPrefix(id, "enemy-");
        }

        /// <summary>The player-facing name of an ability, trait or role, from the string table (PRD 3.12.7).</summary>
        public static string PowerName(string kind, string id)
        {
            return Strings.Get(kind + "." + id + ".name");
        }

        private void BuildBadge(Transform root, VisualCatalogue catalogue, Vector2 at, bool compact)
        {
            var size = compact ? new Vector2(180f, 40f) : new Vector2(220f, 56f);
            int fontSize = compact ? 22 : 28;
            if (BadgeIsNew)
            {
                Badge = HudFactory.Image("Badge", root, Color.white, at, size, catalogue.Sprite(UiKind, BadgeNewId));
                BadgeLabel = HudFactory.StretchedText("Label", Badge.transform, fontSize, BadgeInk, TextAnchor.MiddleCenter);
                BadgeLabel.fontStyle = FontStyle.Bold;
                BadgeLabel.text = Strings.Get("enemycard.new");
                return;
            }

            // Fought before: the role icon beside the role name.
            string role = EnemyLoader.RoleToId(Enemy.Role);
            var row = HudFactory.Rect("Badge", root, at, size);
            Badge = HudFactory.Image("Icon", row, Color.white, new Vector2(-size.x / 2f + size.y / 2f, 0f), new Vector2(size.y, size.y), catalogue.Sprite(RoleKind, role));
            Badge.preserveAspect = true;
            BadgeLabel = HudFactory.Text("Label", row, fontSize, ScreenFactory.TextColor, TextAnchor.MiddleLeft);
            BadgeLabel.rectTransform.anchoredPosition = new Vector2(size.y / 2f + 6f, 0f);
            BadgeLabel.rectTransform.sizeDelta = new Vector2(size.x - size.y - 12f, size.y);
            BadgeLabel.text = PowerName(RoleKind, role);
        }

        private void AddPower(Transform root, VisualCatalogue catalogue, string kind, string id, Vector2 leftCentre, float iconSize, float nameWidth, int fontSize)
        {
            var icon = HudFactory.Image("Power " + id, root, Color.white, new Vector2(leftCentre.x + iconSize / 2f, leftCentre.y), new Vector2(iconSize, iconSize), catalogue.Sprite(kind, id));
            icon.preserveAspect = true;
            var name = HudFactory.Text("Name", icon.transform, fontSize, ScreenFactory.TextColor, TextAnchor.MiddleLeft);
            name.rectTransform.anchoredPosition = new Vector2(iconSize / 2f + 12f + nameWidth / 2f, 0f);
            name.rectTransform.sizeDelta = new Vector2(nameWidth, iconSize);
            name.text = PowerName(kind, id);
            _powers.Add(new EnemyPowerView(kind, id, icon, name));
        }

        private static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix, StringComparison.Ordinal) ? text.Substring(prefix.Length) : text;
        }
    }
}
