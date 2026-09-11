#nullable enable
using System.Collections;
using System.Linq;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    public class Presenter
    {
        private const float Tolerance = 0.5f;

        private static GameObject MakeCamera(string name)
        {
            var host = new GameObject(name);
            host.transform.position = new Vector3(0f, 0f, -10f);
            host.AddComponent<Camera>();
            return host;
        }

        private static IEnumerator WaitUntilRenderedAt(RhythmLineView line, int audioTimeMs, float timeoutSeconds = 30f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while ((!line.HasRendered || line.RenderedAtMs < audioTimeMs) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator rhythm_line_present_and_scrolling()
        {
            var rig = ClientTestContent.ScheduledRig("presenter-line", Beats.ToQuarterBeats(60));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var line = hud.RhythmLine;

            yield return WaitUntilRenderedAt(line, 200);
            int startMs = line.RenderedAtMs;
            int beat = Mathf.FloorToInt(line.ScrollBeats) + 3;
            var marker = line.BeatMarkerFor(beat);
            Assert.That(marker, Is.Not.Null, $"no marker shows beat {beat}");
            float startX = line.ViewportX(marker!.Rect);

            yield return WaitUntilRenderedAt(line, startMs + 1000);
            int endMs = line.RenderedAtMs;

            Assert.That(line.gameObject.activeInHierarchy, Is.True, "the Rhythm Line is not active");
            Assert.That(endMs - startMs, Is.GreaterThanOrEqualTo(1000), "two beats did not elapse");
            Assert.That(line.BeatMarkerFor(beat), Is.SameAs(marker), $"the marker for beat {beat} was reassigned or hidden");
            float moved = startX - line.ViewportX(marker.Rect);
            float expected = (endMs - startMs) / 500f * line.BeatWidth;
            Assert.That(moved, Is.EqualTo(expected).Within(Tolerance), "the marker did not scroll with the beat clock");
            Assert.That(moved, Is.GreaterThanOrEqualTo(2f * line.BeatWidth - Tolerance), "the marker moved less than two beat-widths");
            Assert.That(line.BeatMarkers.Count(m => m.Visible), Is.GreaterThanOrEqualTo(line.HorizonBeats), "beat markers are missing");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator cooldown_countdown_and_thin_inactive_line()
        {
            var card = new CardDefinition("card-cd-3", "Three", CardCategory.LeftAttack, 10, 3);
            var rig = ClientTestContent.ScheduledRig("presenter-cooldown", Beats.ToQuarterBeats(1)); // attack at beat 1 = 500 ms
            var hud = BattleHud.Build(rig.Driver, null, null, _ => card);
            var slots = hud.Slots;
            yield return rig.WaitUntilAudioMs(500);

            var result = rig.Driver.PressAt(ClientTestContent.SlotE, card, 500);
            Assert.That(result.Accepted, Is.True, "the press was not accepted");
            Assert.That(rig.Driver.Battle!.CooldownOf(ClientTestContent.SlotE), Is.EqualTo(3));

            var widget = slots.Widget(ClientTestContent.SlotE);
            Assert.That(widget.CountdownText, Is.EqualTo("3"));
            Assert.That(widget.OverlayShown, Is.True, "the cooldown overlay is not shown");
            Assert.That(slots.Widget(ClientTestContent.SlotR).OverlayShown, Is.False, "a slot not on cooldown shows the overlay");
            Assert.That(slots.ActiveLine, Is.EqualTo(0));
            Assert.That(slots.RowWidth(1), Is.LessThan(slots.RowWidth(0)), "the inactive line is not narrower");
            Assert.That(slots.Rows[0].gameObject.activeInHierarchy, Is.True);
            Assert.That(slots.Rows[1].gameObject.activeInHierarchy, Is.True);
            Assert.That(slots.Widgets.Count, Is.EqualTo(16));

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator grade_cue_and_flash()
        {
            var rig = ClientTestContent.ScheduledRig("presenter-cue", Beats.ToQuarterBeats(60));
            var hud = BattleHud.Build(rig.Driver, null, null, _ => ClientTestContent.LeftAttack10);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(0);

            hud.Feedback.OnBattleEvent(battle, new InputJudged(0, 0, ClientTestContent.SlotE, ClientTestContent.LeftAttack10.Id, Judgment.Perfect, 0, false));

            Assert.That(hud.Cues.LastPlayed, Is.EqualTo(Judgment.Perfect));
            Assert.That(hud.Cues.LastClip, Is.SameAs(hud.Cues.ClipFor(Judgment.Perfect)), "the Perfect cue is not what played");
            Assert.That(hud.Cues.PlayCount, Is.EqualTo(1));
            Assert.That(hud.Cues.Source, Is.Not.SameAs(rig.Source), "cues play through the track's source");
            Assert.That(hud.Slots.Widget(ClientTestContent.SlotE).IsFlashing, Is.True, "the pressed key did not flash");
            Assert.That(hud.Slots.Widget(ClientTestContent.SlotR).IsFlashing, Is.False, "an unpressed key flashed");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator glow_during_open_window()
        {
            // The only action is on beat 2 (1000 ms); its window is 875-1125 ms.
            var rig = ClientTestContent.ScheduledRig("presenter-glow", Beats.ToQuarterBeats(2));
            var hud = BattleHud.Build(rig.Driver, null, null, slot => slot.Key == SlotKey.E || slot.Key == SlotKey.R ? ClientTestContent.LeftAttack10 : null);
            var battle = rig.Driver.Battle!;
            var pending = battle.PendingAction;
            var slotE = hud.Slots.Widget(ClientTestContent.SlotE);
            var slotQ = hud.Slots.Widget(new Slot(0, SlotKey.Q));

            yield return rig.WaitUntilAudioMs(pending.OpenMs - 200);
            yield return null;
            Assert.That(slotE.IsGlowing, Is.False, "a card glowed before the window opened");

            yield return rig.WaitUntilAudioMs(pending.OpenMs + 20);
            yield return null;
            Assert.That(rig.Clock.NowMs, Is.LessThan(pending.CloseMs), "the window had already closed when checked");
            Assert.That(slotE.IsGlowing, Is.True, "a playable card did not glow while the window was open");
            Assert.That(hud.Slots.Widget(ClientTestContent.SlotELine2).IsGlowing, Is.True, "line 2's card did not glow");
            Assert.That(slotQ.IsGlowing, Is.False, "an empty slot glowed");
            Assert.That(hud.RhythmLine.WindowOpen, Is.True, "the Rhythm Line does not show the window open");
            Assert.That(hud.RhythmLine.ActionMarkers.Single().Highlighted, Is.True, "the incoming action is not highlighted");

            yield return rig.WaitUntilAudioMs(pending.CloseMs + 20);
            yield return null;
            Assert.That(slotE.IsGlowing, Is.False, "the glow stayed on after the window closed");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator shake_on_heavy_hit()
        {
            var camera = MakeCamera("presenter-camera");
            var rig = ClientTestContent.ScheduledRig("presenter-shake", Beats.ToQuarterBeats(60));
            var hud = BattleHud.Build(rig.Driver, camera.GetComponent<Camera>(), null, null);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(0);
            Assert.That(hud.Shake, Is.Not.Null, "the HUD has no camera shake");

            hud.Feedback.OnBattleEvent(battle, new DamageTaken(0, 0, 5, 0));
            Assert.That(hud.Shake!.IsShaking, Is.False, "a light hit shook the camera");
            Assert.That(hud.Shake.TriggerCount, Is.EqualTo(0));

            hud.Feedback.OnBattleEvent(battle, new DamageTaken(0, 0, 20, 0));
            Assert.That(hud.Shake.IsShaking, Is.True, "a heavy hit did not shake the camera");
            Assert.That(hud.Shake.TriggerCount, Is.EqualTo(1));

            Object.Destroy(hud.gameObject);
            rig.Destroy();
            Object.Destroy(camera);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator status_icon_with_tooltip()
        {
            var rig = ClientTestContent.ScheduledRig("presenter-status", Beats.ToQuarterBeats(60));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(0);

            // Bleed lasts 8 beats and ticks at the end of beats 0 and 1, so at beat 2 it has 6 left.
            battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed, 2);
            yield return rig.WaitUntilAudioMs(1100);
            Assert.That(battle.EnemyStatuses.RemainingBeats(StatusKind.Bleed), Is.EqualTo(6), "the fixture's Bleed is not at 6 beats left");
            Assert.That(rig.Driver.Battle!.CurrentBeat, Is.EqualTo(2));

            var enemyBar = hud.Statuses.Enemy;
            var icon = enemyBar.Icons.Single(i => i.Kind == StatusKind.Bleed);
            icon.ShowTooltip();

            Assert.That(icon.gameObject.activeInHierarchy, Is.True);
            Assert.That(icon.StacksText, Is.EqualTo("2"));
            Assert.That(icon.transform.position.y, Is.GreaterThan(enemyBar.Bar.position.y), "the icon does not sit above the enemy bar");
            Assert.That(icon.TooltipShown, Is.True);
            Assert.That(icon.TooltipText, Does.Contain("Bleed"));
            Assert.That(icon.TooltipText, Does.Contain("6"));
            Assert.That(icon.TooltipText, Does.Contain(Strings.Get("status.bleed.effect")));
            Assert.That(hud.Statuses.Player.Icons, Is.Empty, "the player shows a status it does not have");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator telegraph_drawn_with_countdown()
        {
            // AttackLeft at beat 4; the line is read during beat 1 (500-625 ms is its first quarter).
            var rig = ClientTestContent.ScheduledRig("presenter-telegraph", Beats.ToQuarterBeats(4));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var line = hud.RhythmLine;

            yield return WaitUntilRenderedAt(line, 500);
            Assert.That(line.RenderedAtMs, Is.LessThan(625), "the frame after beat 1 came too late to read the first quarter");
            Assert.That(rig.Driver.Battle!.CurrentBeat, Is.EqualTo(1));

            var marker = line.ActionMarkers.Single();
            Assert.That(marker.Visible, Is.True);
            Assert.That(marker.Kind, Is.EqualTo(EnemyActionKind.AttackLeft));
            Assert.That(marker.KindText, Is.EqualTo(Strings.Get("action.left")));
            Assert.That(marker.Beat, Is.EqualTo(4f));
            Assert.That(marker.Rect.anchoredPosition.x, Is.EqualTo(4f * line.BeatWidth).Within(0.01f), "the marker is not at beat 4 on the line");
            Assert.That(marker.CountdownText, Is.EqualTo("3"));
            Assert.That(marker.RemainingBeats, Is.EqualTo(3));

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }
    }
}
