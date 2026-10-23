#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Chiki.Client.Driver;
using Chiki.Client.Screens;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>What the top bar shows of the run: the equipped Charms, the held Imprints and the content that names them.</summary>
    public sealed record HeldItems(IReadOnlyList<string> Charms, IReadOnlyList<string> Imprints, RunContent? Content)
    {
        public static readonly HeldItems None = new HeldItems(new string[0], new string[0], null);
    }

    /// <summary>
    /// The bar along the top of the battle (operator layout, 2026-10-23): the two Charm slots in
    /// its framed squares (PRD 3.9.6), the held Imprints left to right through both dark strips
    /// (PRD 3.9.3), and beside the four icons on its right the run's Essence, ARD, CRP and Base
    /// DMG (PRD 3.8.1, 3.2.16). Every position is measured from the bar art, which is authored
    /// at 4K and drawn at half size.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopBarView : MonoBehaviour, IBattlePresenter
    {
        public const string UiKind = "ui";
        public const string BarId = "top-bar";

        /// <summary>The bar art, 3840 by 250, at half size.</summary>
        public static readonly Vector2 BarSize = new Vector2(1920f, 125f);

        /// <summary>The two Charm squares' centres from the bar's centre (at 4K: 1803 and 2009 from the left edge).</summary>
        public static readonly float[] CharmSlotX = { -58.5f, 44.5f };

        public const float CharmIconSize = 70f;

        /// <summary>The dark strips either side of the squares (at 4K: 1196–1712 and 2099–2667), where the Imprints run left to right.</summary>
        public static readonly (float Left, float Right)[] ImprintStrips = { (-362f, -104f), (89.5f, 373.5f) };

        public const float ImprintSize = 40f;
        public const float ImprintGap = 8f;

        /// <summary>The four stat icons' centres (at 4K: 2773, 3044, 3317, 3585): Essence, ARD, CRP, Base DMG.</summary>
        public static readonly float[] StatIconX = { 426.5f, 562f, 698.5f, 832.5f };

        /// <summary>A stat's number starts this far right of its icon's centre and may run this wide.</summary>
        public const float StatTextOffset = 34f;
        public const float StatTextWidth = 95f;

        private static readonly Color CharmPlaceholder = new Color(0.88f, 0.77f, 0.47f, 1f);
        private static readonly Color ImprintPlaceholder = new Color(0.6f, 0.75f, 0.9f, 1f);

        private readonly List<HeldIcon> _charmIcons = new List<HeldIcon>();
        private readonly List<HeldIcon> _imprintIcons = new List<HeldIcon>();
        private UnityEngine.UI.Text _essence = null!;
        private UnityEngine.UI.Text _ard = null!;
        private UnityEngine.UI.Text _crp = null!;
        private UnityEngine.UI.Text _baseDmg = null!;

        public RectTransform Rect { get; private set; } = null!;

        public Image Bar { get; private set; } = null!;

        public IReadOnlyList<HeldIcon> CharmIcons => _charmIcons;

        public IReadOnlyList<HeldIcon> ImprintIcons => _imprintIcons;

        public string EssenceText => _essence.text;

        public string ArdText => _ard.text;

        public string CrpText => _crp.text;

        public string BaseDmgText => _baseDmg.text;

        public static TopBarView Build(BattleDriver driver, RectTransform parent, Vector2 position, HeldItems held)
        {
            if (driver == null)
            {
                throw new System.ArgumentNullException(nameof(driver));
            }

            held ??= HeldItems.None;
            var root = HudFactory.Rect("TopBar", parent, position, BarSize);
            var view = root.gameObject.AddComponent<TopBarView>();
            view.Rect = root;

            var barPiece = new LinePiece(UiKind, BarId, new Color(0.08f, 0.08f, 0.1f, 0.95f));
            view.Bar = HudFactory.StretchedImage("Bar", root, barPiece.Tint, barPiece.Sprite);

            view.DrawHeld(held);

            view._essence = view.Stat("Essence", 0);
            view._ard = view.Stat("Ard", 1);
            view._crp = view.Stat("Crp", 2);
            view._baseDmg = view.Stat("BaseDmg", 3);

            if (driver.Battle != null)
            {
                view.Refresh(driver.Battle);
            }

            driver.AttachPresenter(view);
            return view;
        }

        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            Refresh(battle);
        }

        /// <summary>Reads the four numbers from the run's stats as the battle holds them.</summary>
        public void Refresh(Sim.Battle battle)
        {
            var stats = battle.Stats;
            _essence.text = stats.Essence.ToString(CultureInfo.InvariantCulture);
            _ard.text = stats.Ard.ToString(CultureInfo.InvariantCulture);
            _crp.text = stats.Crp.ToString(CultureInfo.InvariantCulture);
            _baseDmg.text = stats.BaseDmg.ToString(CultureInfo.InvariantCulture);
        }

        private UnityEngine.UI.Text Stat(string name, int index)
        {
            var text = HudFactory.Text(name, Rect, 22, Color.white, TextAnchor.MiddleLeft, bold: true);
            text.rectTransform.anchoredPosition = new Vector2(StatIconX[index] + StatTextOffset + StatTextWidth / 2f, 0f);
            text.rectTransform.sizeDelta = new Vector2(StatTextWidth, 40f);
            return text;
        }

        /// <summary>
        /// The Charms into their squares in order, then the Imprints left to right through the
        /// strips, a stackable one held more than once shown once with its copy count. Tooltips
        /// name each one and its effect, as on the map (P9.5).
        /// </summary>
        private void DrawHeld(HeldItems held)
        {
            for (int i = 0; i < held.Charms.Count && i < CharmSlotX.Length; i++)
            {
                string id = held.Charms[i];
                var charm = held.Content?.FindCharm(id);
                _charmIcons.Add(HeldIcon.Create(Rect, HeldIcon.CharmKind, id, Tooltip(HeldIcon.CharmKind, id, charm?.Name ?? id), 1, new Vector2(CharmSlotX[i], 0f), CharmIconSize, CharmPlaceholder));
            }

            var positions = ImprintPositions().GetEnumerator();
            foreach (var group in held.Imprints.GroupBy(id => id))
            {
                if (!positions.MoveNext())
                {
                    break;
                }

                var imprint = held.Content?.FindImprint(group.Key);
                _imprintIcons.Add(HeldIcon.Create(Rect, HeldIcon.ImprintKind, group.Key, Tooltip(HeldIcon.ImprintKind, group.Key, imprint?.Name ?? group.Key), group.Count(), new Vector2(positions.Current, 0f), ImprintSize, ImprintPlaceholder));
            }
        }

        /// <summary>Every Imprint centre the strips offer, left to right, the left strip first.</summary>
        public static IEnumerable<float> ImprintPositions()
        {
            foreach (var strip in ImprintStrips)
            {
                for (float x = strip.Left + ImprintSize / 2f; x + ImprintSize / 2f <= strip.Right; x += ImprintSize + ImprintGap)
                {
                    yield return x;
                }
            }
        }

        private static string Tooltip(string kind, string id, string name)
        {
            return Strings.Format("map.held_tooltip", name, Strings.Get(kind + ".effect." + id));
        }
    }
}
