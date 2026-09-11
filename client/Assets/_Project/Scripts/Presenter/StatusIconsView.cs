#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Driver;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One side's bar, the enemy's HP or the player's ARD, with its status icons sitting above
    /// it (PRD 3.3.7.1). Each status kind on that side is one icon with its total stacks and the
    /// longest time left; a status that lasts until consumed shows no countdown.
    /// </summary>
    public sealed class SideBarView
    {
        public const float BarHeight = 32f;
        public const float IconSize = 44f;
        public const float IconGap = 6f;

        private static readonly Color BarBackground = new Color(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color EnemyFill = new Color(0.8f, 0.2f, 0.25f, 1f);
        private static readonly Color PlayerFill = new Color(0.25f, 0.65f, 0.9f, 1f);

        private readonly float _width;
        private readonly Image _fill;
        private readonly UnityEngine.UI.Text _label;
        private readonly List<StatusIconWidget> _pool = new List<StatusIconWidget>();
        private readonly List<StatusIconWidget> _shown = new List<StatusIconWidget>();

        public StatusTarget Side { get; }

        public RectTransform Root { get; }

        public RectTransform Bar { get; }

        /// <summary>The row the icons sit in, above the bar.</summary>
        public RectTransform IconsRow { get; }

        /// <summary>The icons on screen, one per status kind on this side, in kind order.</summary>
        public IReadOnlyList<StatusIconWidget> Icons => _shown;

        public string LabelText => _label.text;

        internal SideBarView(StatusTarget side, RectTransform parent, Vector2 anchoredPosition, float width)
        {
            Side = side;
            _width = width;
            Root = HudFactory.Rect(side + "Bar", parent, anchoredPosition, new Vector2(width, BarHeight + IconGap + IconSize));

            var background = HudFactory.Image("Bar", Root, BarBackground, Vector2.zero, new Vector2(width, BarHeight));
            Bar = background.rectTransform;

            _fill = HudFactory.Image("Fill", Bar, side == StatusTarget.Enemy ? EnemyFill : PlayerFill);
            _fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _fill.rectTransform.anchoredPosition = Vector2.zero;
            _fill.rectTransform.sizeDelta = new Vector2(width, 0f);

            _label = HudFactory.StretchedText("Label", Bar, 18, Color.white, TextAnchor.MiddleCenter);

            IconsRow = HudFactory.Rect("Statuses", Root, new Vector2(0f, BarHeight / 2f + IconGap + IconSize / 2f), new Vector2(width, IconSize));
        }

        public void SetBar(int value, int max, string text)
        {
            float fraction = max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;
            _fill.rectTransform.sizeDelta = new Vector2(_width * fraction, 0f);
            _label.text = text;
        }

        /// <summary>Shows one icon per status kind on the side: total stacks and the longest time left (PRD 3.3.7.1).</summary>
        public void SetStatuses(StatusSet statuses)
        {
            if (statuses is null)
            {
                throw new ArgumentNullException(nameof(statuses));
            }

            _shown.Clear();
            int next = 0;
            foreach (StatusKind kind in Enum.GetValues(typeof(StatusKind)))
            {
                if (!statuses.Has(kind))
                {
                    continue;
                }

                int? remaining = LongestRemaining(statuses, kind);
                if (next >= _pool.Count)
                {
                    _pool.Add(StatusIconWidget.Create(IconsRow, IconSize));
                }

                var icon = _pool[next];
                icon.Set(kind, statuses.Stacks(kind), remaining);
                icon.Rect.anchoredPosition = new Vector2(-_width / 2f + IconSize / 2f + next * (IconSize + IconGap), 0f);
                if (!icon.gameObject.activeSelf)
                {
                    icon.gameObject.SetActive(true);
                }

                _shown.Add(icon);
                next++;
            }

            for (int i = next; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf)
                {
                    _pool[i].HideTooltip();
                    _pool[i].gameObject.SetActive(false);
                }
            }
        }

        private static int? LongestRemaining(StatusSet statuses, StatusKind kind)
        {
            int? longest = null;
            foreach (var instance in statuses.Instances)
            {
                if (instance.Kind == kind && instance.RemainingBeats.HasValue)
                {
                    longest = longest.HasValue ? Math.Max(longest.Value, instance.RemainingBeats.Value) : instance.RemainingBeats.Value;
                }
            }

            return longest;
        }
    }

    /// <summary>
    /// The two bars and their status icons (PRD 3.3.7.1): the enemy's HP with its Block and
    /// statuses, the player's ARD with Block and statuses, each refreshed from the battle's state
    /// after every event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusIconsView : MonoBehaviour, IBattlePresenter
    {
        public const float BarWidth = 560f;

        public SideBarView Player { get; private set; } = null!;

        public SideBarView Enemy { get; private set; } = null!;

        public static StatusIconsView Build(BattleDriver driver, RectTransform parent, Vector2 enemyPosition, Vector2 playerPosition)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var root = HudFactory.Rect("Bars", parent);
            var view = root.gameObject.AddComponent<StatusIconsView>();
            view.Enemy = new SideBarView(StatusTarget.Enemy, root, enemyPosition, BarWidth);
            view.Player = new SideBarView(StatusTarget.Player, root, playerPosition, BarWidth);
            if (driver.Battle != null)
            {
                view.Refresh(driver.Battle);
            }

            driver.AttachPresenter(view);
            return view;
        }

        public SideBarView Side(StatusTarget target)
        {
            return target == StatusTarget.Enemy ? Enemy : Player;
        }

        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            Refresh(battle);
        }

        public void Refresh(Sim.Battle battle)
        {
            Enemy.SetBar(battle.EnemyHp, battle.EnemyMaxHp, WithBlock(Strings.Format("bar.enemy_hp", battle.EnemyHp, battle.EnemyMaxHp), battle.EnemyBlock));
            Enemy.SetStatuses(battle.EnemyStatuses);
            Player.SetBar(battle.Stats.Ard, battle.Stats.MaxArd, WithBlock(Strings.Format("bar.ard", battle.Stats.Ard, battle.Stats.MaxArd), battle.Block));
            Player.SetStatuses(battle.PlayerStatuses);
        }

        private static string WithBlock(string text, int block)
        {
            return block > 0 ? text + "  " + Strings.Format("bar.block", block) : text;
        }
    }
}
