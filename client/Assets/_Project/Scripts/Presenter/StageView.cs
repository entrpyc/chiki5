#nullable enable
using System;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The stage the battle is fought on (PRD 3.14.1, P5.1 to P5.3): the arena behind everything,
    /// the enemy standing on the right facing left and the player on the left facing right, both
    /// with their feet on the arena's floor line and both behind the HUD.
    ///
    /// The enemy is driven by the chart, not by the stream: every render looks at the actions
    /// still to come and starts each one's clip early enough that its strike frame lands on the
    /// action's beat, with a Charge's wind-up loop covering the beats between its telegraph and
    /// its landing (PRD 3.6.16). The stream drives only what the chart cannot know — a hit, a
    /// press and a death. Nothing here reads <c>Time.time</c>: every clip is placed and stepped
    /// from the beat map.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageView : MonoBehaviour, IBattlePresenter
    {
        /// <summary>The arena's kind and id in the visual catalogue (P5.3).</summary>
        public const string BackgroundKind = "bg";
        public const string BackgroundId = "arena";

        /// <summary>The kinds the two fighters' clips are catalogued under.</summary>
        public const string PlayerKind = "player";
        public const string EnemyKind = "enemy";

        /// <summary>The player character of the demo (PRD 3.14.1).</summary>
        public const string PlayerSubject = "lulu";

        /// <summary>
        /// The arena's floor line in world units: the background is 1080 px tall at 100 pixels
        /// per unit and its floor line sits 200 px above its bottom edge (P5.3).
        /// </summary>
        public const float FloorY = -1080f / 2f / 100f + 200f / 100f;

        /// <summary>How far from the centre each fighter stands, clear of the bars and the slot rows.</summary>
        public const float StandX = 6.2f;

        /// <summary>The sorting orders that put the arena behind the fighters and both behind the HUD.</summary>
        public const int BackgroundOrder = -200;
        public const int FighterOrder = -100;

        /// <summary>How far ahead the stage looks for actions to start a clip for.</summary>
        public const int HorizonBeats = 4;

        private BattleDriver? _driver;
        private Sim.Battle? _battle;
        private int _nextActionIndex;
        private int _windingUpIndex = -1;

        public SpriteRenderer Background { get; private set; } = null!;

        public FighterView Enemy { get; private set; } = null!;

        public FighterView Player { get; private set; } = null!;

        /// <summary>The audio time of the last render.</summary>
        public int RenderedAtMs { get; private set; }

        public bool HasRendered { get; private set; }

        /// <summary>The subject a content id names: the id without its kind prefix, as the sprite conventions spell it.</summary>
        public static string SubjectOf(string kind, string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
            {
                return "";
            }

            string prefix = kind + "-";
            return contentId.StartsWith(prefix, StringComparison.Ordinal) ? contentId.Substring(prefix.Length) : contentId;
        }

        /// <summary>Builds the stage under the battle's transform and attaches it to the driver's stream.</summary>
        public static StageView Build(BattleDriver driver, Transform? parent = null)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var root = new GameObject("Stage");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            var stage = root.AddComponent<StageView>();
            stage._driver = driver;
            stage._battle = driver.Battle;

            var backdrop = new GameObject("Arena");
            backdrop.transform.SetParent(root.transform, false);
            stage.Background = backdrop.AddComponent<SpriteRenderer>();
            stage.Background.sprite = VisualCatalogue.Active.Sprite(BackgroundKind, BackgroundId);
            stage.Background.sortingOrder = BackgroundOrder;

            var clock = driver.Clock;
            var map = stage._battle?.BeatMap;
            string enemySubject = SubjectOf(EnemyKind, stage._battle?.Enemy.Id ?? "");
            stage.Enemy = FighterView.Build(root.transform, "Enemy", EnemyKind, enemySubject, new Vector2(StandX, FloorY), true, clock, map, FighterOrder);
            stage.Player = FighterView.Build(root.transform, "Player", PlayerKind, PlayerSubject, new Vector2(-StandX, FloorY), false, clock, map, FighterOrder);

            driver.AttachPresenter(stage);
            if (stage._battle != null)
            {
                stage.Render(Math.Max(0, stage._battle.CurrentTimeMs));
            }

            return stage;
        }

        /// <summary>
        /// Whether the arena covers everything a camera frames. The background is wider than the
        /// 16:9 frame so an ultrawide window shows more of its sides and never the clear colour
        /// (P5.3).
        /// </summary>
        public bool Covers(Camera camera)
        {
            if (camera == null || Background.sprite == null)
            {
                return false;
            }

            const float slack = 0.001f;
            var bounds = Background.bounds;
            var eye = camera.transform.position;
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            return bounds.min.x <= eye.x - halfWidth + slack
                && bounds.max.x >= eye.x + halfWidth - slack
                && bounds.min.y <= eye.y - halfHeight + slack
                && bounds.max.y >= eye.y + halfHeight - slack;
        }

        /// <summary>
        /// Follows the stream for what the chart cannot know: damage either way is a hit, a
        /// judged press is the player's answer to the action it was graded against, and the end
        /// of the battle lays the loser down on a held last frame.
        /// </summary>
        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            _battle = battle;
            switch (battleEvent)
            {
                case DamageDealt dealt when dealt.Amount > 0:
                    Enemy.Play(FighterClips.Hit, TimeAtQb(battle, dealt.PositionQb));
                    break;
                case DamageTaken taken when taken.Amount > 0:
                    Player.Play(FighterClips.Hit, TimeAtQb(battle, taken.PositionQb));
                    break;
                case InputJudged judged:
                    Player.PlayStrikeAt(FighterClips.ForSlot(judged.Slot), judged.PositionQb);
                    break;
                case BattleEnded ended:
                    var loser = ended.Outcome == BattleOutcome.Won ? Enemy : Player;
                    loser.Play(FighterClips.Death, TimeAtQb(battle, ended.PositionQb), hold: true);
                    break;
            }
        }

        /// <summary>Starts the clips the chart calls for by an audio time and renders both fighters at it.</summary>
        public void Render(int audioTimeMs)
        {
            ScheduleEnemy(audioTimeMs);
            Enemy.Render(audioTimeMs);
            Player.Render(audioTimeMs);
            RenderedAtMs = audioTimeMs;
            HasRendered = true;
        }

        /// <summary>
        /// Starts the enemy's clip for every charted action whose first frame is due: a Charge's
        /// wind-up loop when its telegraph begins (PRD 3.6.16), and the action's own clip
        /// <c>strike - 1</c> quarter beats before it lands, so the strike frame is the one on
        /// screen on the beat.
        /// </summary>
        private void ScheduleEnemy(int audioTimeMs)
        {
            var battle = _battle;
            if (battle == null || Enemy.Holding)
            {
                return;
            }

            foreach (var upcoming in battle.UpcomingActions(HorizonBeats))
            {
                if (upcoming.Index < _nextActionIndex)
                {
                    continue;
                }

                string? variant = FighterClips.ForAction(upcoming.Action.Kind);
                if (variant == null)
                {
                    _nextActionIndex = upcoming.Index + 1;
                    continue;
                }

                if (upcoming.Action.Kind == EnemyActionKind.Charge
                    && upcoming.Index != _windingUpIndex
                    && audioTimeMs >= TimeAtQb(battle, upcoming.TelegraphQb))
                {
                    _windingUpIndex = upcoming.Index;
                    Enemy.Play(FighterClips.Charge, TimeAtQb(battle, upcoming.TelegraphQb));
                }

                var clip = Enemy.Clip(variant);
                int startQb = Math.Max(0, upcoming.PositionQb - ((clip.StrikeFrame ?? 1) - 1));
                if (audioTimeMs < TimeAtQb(battle, startQb))
                {
                    break;
                }

                Enemy.PlayStrikeAt(variant, upcoming.PositionQb);
                _nextActionIndex = upcoming.Index + 1;
            }
        }

        private static int TimeAtQb(Sim.Battle battle, int positionQb)
        {
            return battle.BeatMap.TimeAtQb(Math.Max(0, positionQb));
        }

        private void LateUpdate()
        {
            // A Unity null check, not a C# one: the driver may already have been destroyed.
            var driver = _driver;
            if (driver == null || _battle == null)
            {
                return;
            }

            var clock = driver.Clock;
            if (clock == null || !clock.IsScheduled)
            {
                return;
            }

            Render(Math.Max(0, clock.NowMs));
        }
    }
}
