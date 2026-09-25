#nullable enable
using System.Collections;
using System.Linq;
using Chiki.Client.Driver;
using Chiki.Client.Presenter;
using Chiki.Client.Visuals;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>
    /// The fighters and the arena they stand on (P5.1 to P5.3). The stage keeps every clip it
    /// started as a cue at the audio time it began, so these tests ask what was on screen on a
    /// beat rather than sampling whichever frame happened to follow it.
    /// </summary>
    public class Stage
    {
        private const string TestSubject = "client-test";

        private VisualCatalogue _catalogue = null!;
        private GameObject? _camera;
        private StageView? _stage;
        private Rig? _rig;

        [SetUp]
        public void SetUp()
        {
            _catalogue = ScriptableObject.CreateInstance<VisualCatalogue>();
            VisualCatalogue.Use(_catalogue);
        }

        [TearDown]
        public void TearDown()
        {
            if (_stage != null)
            {
                Object.Destroy(_stage.gameObject);
                _stage = null;
            }

            if (_camera != null)
            {
                Object.Destroy(_camera);
                _camera = null;
            }

            _rig?.Destroy();
            _rig = null;
            ClientTestContent.ClearCatalogues();
        }

        /// <summary>The audio time of a whole beat on the battle's track.</summary>
        private static int At(Chiki.Sim.Battle battle, int beat)
        {
            return battle.BeatMap.TimeAtBeat(beat);
        }

        /// <summary>
        /// P5.1: the enemy's clips follow the chart. Three actions — an attack on beat 4, a
        /// Defend on beat 6 and a Charge landing on beat 10 after a three-beat wind-up — then two
        /// more attacks the player answers, the second of which kills.
        /// </summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator enemy_clips_follow_chart()
        {
            ClientTestContent.PutFighterClips(_catalogue, StageView.EnemyKind, TestSubject);

            var track = ClientTestContent.FixtureTrack();
            var chart = ClientTestContent.Chart(
                track,
                new EnemyAction(EnemyActionKind.AttackLeft, Beats.ToQuarterBeats(4)),
                new EnemyAction(EnemyActionKind.Defend, Beats.ToQuarterBeats(6)),
                new EnemyAction(EnemyActionKind.Charge, Beats.ToQuarterBeats(7), windUpBeats: 3),
                new EnemyAction(EnemyActionKind.AttackLeft, Beats.ToQuarterBeats(12)),
                new EnemyAction(EnemyActionKind.AttackLeft, Beats.ToQuarterBeats(14)));

            _rig = ClientTestContent.ScheduledRig("stage-enemy", chart, ClientTestContent.EnemyWith(chart), enemyHp: 15);
            var battle = _rig.Driver.Battle!;
            _stage = StageView.Build(_rig.Driver);
            var enemy = _stage.Enemy;

            Assert.That(enemy.Subject, Is.EqualTo(TestSubject), "the stage did not take the enemy's subject from its content id");
            Assert.That(enemy.FacesLeft, Is.True, "the enemy does not face left");
            Assert.That(enemy.transform.position.x, Is.GreaterThan(0f), "the enemy does not stand on the right of the stage");
            Assert.That(enemy.transform.position.y, Is.EqualTo(StageView.FloorY).Within(0.001f), "the enemy's feet are not on the floor line");

            // Two Perfect presses of a Left Attack: 10 damage each against an AttackLeft, so the
            // first leaves the enemy at 5 and the second kills it.
            _rig.Driver.Script(new[]
            {
                new ScriptedInput(At(battle, 12), ClientTestContent.SlotE, ClientTestContent.LeftAttack10),
                new ScriptedInput(At(battle, 14), ClientTestContent.SlotR, ClientTestContent.LeftAttack10),
            });

            yield return _rig.WaitUntilAudioMs(At(battle, 16), 40f);

            Assert.That(enemy.VariantAt(At(battle, 1)), Is.EqualTo(FighterClips.Idle), "the enemy is not idling on beat 1");

            Assert.That(enemy.VariantAt(At(battle, 4)), Is.EqualTo(FighterClips.AttackLeft), "the attack on beat 4 did not play attack-left");
            Assert.That(enemy.FrameAt(At(battle, 4)), Is.EqualTo(enemy.Clip(FighterClips.AttackLeft).StrikeFrame ?? 0), "the attack-left strike frame is not on beat 4");

            Assert.That(enemy.VariantAt(At(battle, 6)), Is.EqualTo(FighterClips.Defend), "the Defend on beat 6 did not play defend");
            Assert.That(enemy.FrameAt(At(battle, 6)), Is.EqualTo(1), "defend did not start on beat 6");

            foreach (int beat in new[] { 7, 8, 9 })
            {
                Assert.That(enemy.VariantAt(At(battle, beat)), Is.EqualTo(FighterClips.Charge), "the Charge wind-up is not looping on beat " + beat);
            }

            Assert.That(enemy.VariantAt(At(battle, 10)), Is.EqualTo(FighterClips.AttackLeft), "the Charge did not resolve on attack-left");
            Assert.That(enemy.FrameAt(At(battle, 10)), Is.EqualTo(enemy.Clip(FighterClips.AttackLeft).StrikeFrame ?? 0), "the Charge's strike frame is not on beat 10");

            Assert.That(battle.Events.OfType<DamageDealt>().Count(), Is.EqualTo(2), "the two answered attacks did not both damage the enemy");
            Assert.That(enemy.VariantAt(At(battle, 12)), Is.EqualTo(FighterClips.Hit), "the enemy did not play hit when it took damage");

            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won), "the enemy did not reach 0 HP");
            Assert.That(enemy.Holding, Is.True, "the enemy is not holding a last frame");
            Assert.That(enemy.VariantAt(At(battle, 15)), Is.EqualTo(FighterClips.Death), "the won battle did not play death");
            Assert.That(
                enemy.FrameAt(At(battle, 20)),
                Is.EqualTo(enemy.Clip(FighterClips.Death).FrameCount),
                "death did not hold its last frame");
        }

        /// <summary>P5.2: Lulu answers on the beat, by the Category the pressed slot's key fixes.</summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator player_clips_follow_presses()
        {
            ClientTestContent.PutFighterClips(_catalogue, StageView.EnemyKind, TestSubject);
            ClientTestContent.PutFighterClips(_catalogue, StageView.PlayerKind, StageView.PlayerSubject);

            var track = ClientTestContent.FixtureTrack();
            var chart = ClientTestContent.Chart(
                track,
                Beats.ToQuarterBeats(4),
                Beats.ToQuarterBeats(6),
                Beats.ToQuarterBeats(8),
                Beats.ToQuarterBeats(10));

            _rig = ClientTestContent.ScheduledRig("stage-player", chart, ClientTestContent.EnemyWith(chart));
            var battle = _rig.Driver.Battle!;
            _stage = StageView.Build(_rig.Driver);
            var player = _stage.Player;

            Assert.That(player.Subject, Is.EqualTo(StageView.PlayerSubject), "the player on the stage is not Lulu");
            Assert.That(player.FacesLeft, Is.False, "the player does not face right");
            Assert.That(player.transform.position.x, Is.LessThan(0f), "the player does not stand on the left of the stage");
            Assert.That(player.transform.position.y, Is.EqualTo(StageView.FloorY).Within(0.001f), "the player's feet are not on the floor line");

            var defense = new CardDefinition("card-guard-8", "Guard 8", CardCategory.Defense, 2, Tuning.CooldownMinBeats);
            var ability = new CardDefinition("card-focus-6", "Focus 6", CardCategory.Ability, 6, Tuning.CooldownMinBeats);
            _rig.Driver.Script(new[]
            {
                new ScriptedInput(At(battle, 4), new Slot(0, SlotKey.E), ClientTestContent.LeftAttack10),
                new ScriptedInput(At(battle, 6), new Slot(0, SlotKey.O), defense),
                new ScriptedInput(At(battle, 8), new Slot(0, SlotKey.Q), ability),
            });

            // Beat 10 is left unanswered, so the enemy's attack lands and the player takes damage.
            yield return _rig.WaitUntilAudioMs(At(battle, 11), 40f);

            Assert.That(battle.JudgmentLog.Take(3).Select(e => e.Grade), Has.All.EqualTo(Judgment.Perfect), "a scripted press was not Perfect");

            Assert.That(player.VariantAt(At(battle, 4)), Is.EqualTo(FighterClips.AttackLeft), "the press on E did not play attack-left");
            Assert.That(player.FrameAt(At(battle, 4)), Is.EqualTo(player.Clip(FighterClips.AttackLeft).StrikeFrame ?? 0), "attack-left's strike frame is not on beat 4");

            Assert.That(player.VariantAt(At(battle, 6)), Is.EqualTo(FighterClips.Defend), "the press on O did not play defend");
            Assert.That(player.FrameAt(At(battle, 6)), Is.EqualTo(1), "defend did not start on beat 6");

            Assert.That(player.VariantAt(At(battle, 8)), Is.EqualTo(FighterClips.Ability), "the press on Q did not play ability");
            Assert.That(player.FrameAt(At(battle, 8)), Is.EqualTo(player.Clip(FighterClips.Ability).StrikeFrame ?? 0), "ability's strike frame is not on beat 8");

            Assert.That(battle.Events.OfType<DamageTaken>().Any(e => e.Amount > 0), Is.True, "the unanswered action dealt no damage");
            Assert.That(player.VariantAt(At(battle, 10)), Is.EqualTo(FighterClips.Hit), "the player did not play hit when damage came in");
        }

        /// <summary>P5.3: the shipped arena covers the frame at 16:9 and at 21:9.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator arena_fills_16_9_and_ultrawide()
        {
            var shipped = ClientTestContent.ShippedVisuals();
            _catalogue = shipped;

            _rig = ClientTestContent.ScheduledRig("stage-arena", Beats.ToQuarterBeats(4));
            _camera = new GameObject("stage-camera");
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            var camera = _camera.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = BattleHud.CanvasHeight / BattleHud.PixelsPerUnit / 2f;

            _stage = StageView.Build(_rig.Driver);
            yield return null;

            Assert.That(shipped.Has(StageView.BackgroundKind, StageView.BackgroundId), Is.True, "the arena is not in the shipped catalogue");
            Assert.That(_stage.Background.sprite!.rect.size, Is.EqualTo(new Vector2(2560f, 1080f)), "the arena is not 2560 by 1080");

            camera.aspect = 1920f / 1080f;
            Assert.That(_stage.Covers(camera), Is.True, "the arena leaves the clear colour showing at 16:9");

            camera.aspect = 2560f / 1080f;
            Assert.That(_stage.Covers(camera), Is.True, "the arena leaves the clear colour showing on ultrawide");

            Assert.That(
                shipped.Missing.Where(id => id.StartsWith(StageView.BackgroundKind + "/")),
                Is.Empty,
                "the arena fell back: " + string.Join(", ", shipped.Missing));
        }
    }
}
