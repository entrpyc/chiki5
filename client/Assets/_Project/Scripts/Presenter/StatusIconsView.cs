#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Driver;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// One side's bar, the enemy's HP or the player's ARD, with its status icons sitting above
    /// it (PRD 3.3.7.1). Each status kind on that side is one icon with its total stacks and the
    /// longest time left; a status that lasts until consumed shows no countdown. The bar is a
    /// frame with a horizontal filled image inside it, and Block (PRD 3.3.4.2) is its own icon
    /// and value beside the bar, never a suffix on the bar's text (P4.4).
    /// </summary>
    public sealed class SideBarView
    {
        public const float BarHeight = 32f;
        public const float IconSize = 44f;
        public const float IconGap = 6f;

        /// <summary>The kind a standing ability's badge is catalogued under (P2.4).</summary>
        public const string AbilityKind = "ability";

        /// <summary>The kind the bar frame, the two fills and the Block icon are catalogued under (P4.4).</summary>
        public const string UiKind = "ui";

        public const string FrameId = "bar-frame";
        public const string EnemyFillId = "bar-fill-enemy";
        public const string ArdFillId = "bar-fill-ard";
        public const string BlockId = "block";

        /// <summary>How far inside the frame the fill sits on every side, so the frame's rim stays visible (P4.4).</summary>
        public const float FillInset = 4f;

        /// <summary>The Block icon's drawn size beside the bar (P4.4).</summary>
        public const float BlockIconSize = 34f;

        private static readonly Color BarBackground = new Color(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color EnemyFill = new Color(0.8f, 0.2f, 0.25f, 1f);
        private static readonly Color PlayerFill = new Color(0.25f, 0.65f, 0.9f, 1f);
        private static readonly Color BlockTint = new Color(0.6f, 0.72f, 0.88f, 1f);

        private readonly float _width;
        private readonly Image _frame;
        private readonly Image _fill;
        private readonly Image _ability;
        private readonly Image _blockIcon;
        private readonly UnityEngine.UI.Text _blockValue;
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

        /// <summary>The badge of a standing ability on this side, Iron Veil among them (PRD 3.6.9); hidden while none stands.</summary>
        public Image AbilityBadge => _ability;

        /// <summary>The frame the bar is drawn in (P4.4).</summary>
        public Image Frame => _frame;

        /// <summary>The bar's fill, a horizontally filled image (P4.4).</summary>
        public Image FillImage => _fill;

        /// <summary>How full the bar is, 0 at empty and 1 at full (P4.4).</summary>
        public float Fill => _fill.fillAmount;

        /// <summary>The Block icon beside the bar (PRD 3.3.4.2); hidden while the side holds no Block.</summary>
        public Image BlockIcon => _blockIcon;

        /// <summary>The Block value beside its icon; empty while the side holds no Block.</summary>
        public string BlockText => _blockValue.text;

        public bool BlockShown => _blockIcon.gameObject.activeSelf;

        /// <summary>The content id the badge shows, or null while no ability stands.</summary>
        public string? AbilityShown { get; private set; }

        public string LabelText => _label.text;

        internal SideBarView(StatusTarget side, RectTransform parent, Vector2 anchoredPosition, float width)
        {
            Side = side;
            _width = width;
            Root = HudFactory.Rect(side + "Bar", parent, anchoredPosition, new Vector2(width, BarHeight + IconGap + IconSize));

            var framePiece = new LinePiece(UiKind, FrameId, BarBackground);
            _frame = HudFactory.Image("Bar", Root, framePiece.Tint, Vector2.zero, new Vector2(width, BarHeight), framePiece.Sprite);
            Bar = _frame.rectTransform;

            bool enemy = side == StatusTarget.Enemy;
            var fillPiece = new LinePiece(UiKind, enemy ? EnemyFillId : ArdFillId, enemy ? EnemyFill : PlayerFill);
            _fill = HudFactory.StretchedImage("Fill", Bar, fillPiece.Tint, fillPiece.Sprite);
            _fill.rectTransform.offsetMin = new Vector2(FillInset, FillInset);
            _fill.rectTransform.offsetMax = new Vector2(-FillInset, -FillInset);
            _fill.type = UnityEngine.UI.Image.Type.Filled;
            _fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            _fill.fillAmount = 1f;

            _label = HudFactory.StretchedText("Label", Bar, 18, Color.white, TextAnchor.MiddleCenter);

            var blockPiece = new LinePiece(UiKind, BlockId, BlockTint);
            _blockIcon = HudFactory.Image(
                "Block",
                Root,
                blockPiece.Tint,
                new Vector2(width / 2f + IconGap + BlockIconSize / 2f, -BarHeight / 2f - IconGap),
                new Vector2(BlockIconSize, BlockIconSize),
                blockPiece.Sprite);
            _blockValue = HudFactory.StretchedText("BlockValue", _blockIcon.rectTransform, 20, Color.white, TextAnchor.MiddleCenter);
            _blockIcon.gameObject.SetActive(false);

            IconsRow = HudFactory.Rect("Statuses", Root, new Vector2(0f, BarHeight / 2f + IconGap + IconSize / 2f), new Vector2(width, IconSize));

            _ability = HudFactory.Image("Ability", IconsRow, Color.white, new Vector2(width / 2f - IconSize / 2f, 0f), new Vector2(IconSize, IconSize));
            _ability.gameObject.SetActive(false);
        }

        /// <summary>Shows a standing ability's badge at the end of this side's icon row, or hides it when the id is null (P2.4).</summary>
        public void ShowAbility(string? abilityId)
        {
            if (AbilityShown != abilityId)
            {
                AbilityShown = abilityId;
                if (abilityId != null)
                {
                    HudFactory.SetSprite(_ability, VisualCatalogue.Active.Sprite(AbilityKind, abilityId));
                }
            }

            bool shown = abilityId != null;
            if (_ability.gameObject.activeSelf != shown)
            {
                _ability.gameObject.SetActive(shown);
            }
        }

        public void SetBar(int value, int max, string text)
        {
            _fill.fillAmount = max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;
            _label.text = text;
        }

        /// <summary>Shows this side's Block beside the bar as its icon and value, or hides both at zero (P4.4).</summary>
        public void SetBlock(int block)
        {
            _blockValue.text = block > 0 ? Strings.Format("bar.block", block) : string.Empty;
            if (_blockIcon.gameObject.activeSelf != block > 0)
            {
                _blockIcon.gameObject.SetActive(block > 0);
            }
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

        private int? _veilModifierId;

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

        /// <summary>
        /// Refreshes both bars after every event, and follows the stream for the modifiers that
        /// veil the enemy (PRD 3.6.9): the ability's badge joins the enemy's icons while one
        /// stands and leaves when that same modifier expires.
        /// </summary>
        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            if (battleEvent is ModifierActivated activated && EnemyVeil.Veils(activated))
            {
                _veilModifierId = activated.ModifierId;
                Enemy.ShowAbility(EnemyVeil.AbilityId(battle));
            }
            else if (battleEvent is ModifierExpired expired && _veilModifierId == expired.ModifierId)
            {
                _veilModifierId = null;
                Enemy.ShowAbility(null);
            }

            Refresh(battle);
        }

        public void Refresh(Sim.Battle battle)
        {
            Enemy.SetBar(battle.EnemyHp, battle.EnemyMaxHp, Strings.Format("bar.enemy_hp", battle.EnemyHp, battle.EnemyMaxHp));
            Enemy.SetBlock(battle.EnemyBlock);
            Enemy.SetStatuses(battle.EnemyStatuses);
            Player.SetBar(battle.Stats.Ard, battle.Stats.MaxArd, Strings.Format("bar.ard", battle.Stats.Ard, battle.Stats.MaxArd));
            Player.SetBlock(battle.Block);
            Player.SetStatuses(battle.PlayerStatuses);
        }
    }
}
