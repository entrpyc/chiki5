#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using Chiki.Client.Flow;
using Chiki.Client.Presenter;
using Chiki.Client.Profiles;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    public class Presenter
    {
        private const float Tolerance = 0.5f;

        [TearDown]
        public void TearDown()
        {
            ClientTestContent.ClearCatalogues();
        }

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

        /// <summary>P1.3: the catalogue Boot hands over is the one the HUD draws from.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator hud_reads_catalogue_from_boot()
        {
            var catalogue = ScriptableObject.CreateInstance<VisualCatalogue>();
            var bleed = ClientTestContent.TestSprite("spr_status_bleed_static_01");
            catalogue.Put("status", "bleed", bleed);

            string root = Path.Combine(Application.temporaryCachePath, "boot-catalogue-" + System.Guid.NewGuid().ToString("N"));
            var store = new ProfileStore(root);
            var profile = store.Create("catalogue");
            profile.Calibrated = true;
            store.Save(profile);

            var bootHost = new GameObject("Boot");
            var boot = bootHost.AddComponent<BootScene>();
            boot.ProfileName = "catalogue";
            boot.Visuals = catalogue;
            boot.Begin(store);
            Assert.That(boot.Started, Is.True);
            Assert.That(VisualCatalogue.Active, Is.SameAs(catalogue), "Boot did not hand its catalogue to the presenters");

            var rig = ClientTestContent.ScheduledRig("presenter-catalogue", Beats.ToQuarterBeats(60));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var battle = rig.Driver.Battle!;
            yield return rig.WaitUntilAudioMs(0);

            battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed, 2);
            yield return rig.WaitUntilAudioMs(200);

            var icon = hud.Statuses.Enemy.Icons.Single(i => i.Kind == StatusKind.Bleed);
            Assert.That(icon.Sprite, Is.SameAs(bleed), "the status icon does not carry the catalogue's sprite");
            Assert.That(catalogue.Missing, Does.Not.Contain("status/bleed"));

            Object.Destroy(hud.gameObject);
            Object.Destroy(bootHost);
            rig.Destroy();
            ClientTestContent.ClearCatalogues();
            Directory.Delete(root, true);
        }

        /// <summary>P2.1: every piece of the line is the shipped art, not a coloured rectangle.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator rhythm_line_drawn_from_catalogue()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var rig = ClientTestContent.ScheduledRig("presenter-line-art", Beats.ToQuarterBeats(4));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var line = hud.RhythmLine;

            // Two beats of the fixture track at 120 BPM.
            yield return WaitUntilRenderedAt(line, 1000);

            var background = catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.BackgroundId);
            var beatTick = catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.BeatTickId);
            var quarterTick = catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.QuarterTickId);
            var playhead = catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.PlayheadId);
            var window = catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.WindowId);

            Assert.That(line.Background.sprite, Is.SameAs(background), "the line's background is not the catalogue's");
            Assert.That(line.Playhead.sprite, Is.SameAs(playhead), "the playhead is not the catalogue's");
            Assert.That(line.Window.gameObject.activeInHierarchy, Is.True, "the Judgment Window band is not shown");
            Assert.That(line.Window.sprite, Is.SameAs(window), "the window band is not the catalogue's");

            var visible = line.BeatMarkers.Where(m => m.Visible).ToList();
            Assert.That(visible, Is.Not.Empty, "no beat marker is visible");
            foreach (var marker in visible)
            {
                Assert.That(marker.Line.sprite, Is.SameAs(beatTick), "beat " + marker.Beat + "'s tick is not the catalogue's");
                foreach (var quarter in marker.Quarters)
                {
                    Assert.That(quarter.sprite, Is.SameAs(quarterTick), "a quarter tick of beat " + marker.Beat + " is not the catalogue's");
                }
            }

            Assert.That(catalogue.Missing, Is.Empty, "the line fell back for: " + string.Join(", ", catalogue.Missing));

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        /// <summary>P2.2: a marker carries its kind's icon, and a Charge its wind-up bar.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator telegraph_shows_kind_icon()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var track = ClientTestContent.FixtureTrack();
            var chart = ClientTestContent.Chart(
                track,
                new EnemyAction(EnemyActionKind.AttackLeft, Beats.ToQuarterBeats(4)),
                new EnemyAction(EnemyActionKind.Charge, Beats.ToQuarterBeats(5), windUpBeats: 3));
            var rig = ClientTestContent.ScheduledRig("presenter-icons", chart, ClientTestContent.EnemyWith(chart));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var line = hud.RhythmLine;

            // The line is read during beat 1, whose first quarter runs 500-625 ms.
            yield return WaitUntilRenderedAt(line, 500);
            Assert.That(line.RenderedAtMs, Is.LessThan(625), "the frame after beat 1 came too late to read the first quarter");

            Assert.That(line.ActionMarkers, Has.Count.EqualTo(2), "both telegraphs are not on the line");
            var attack = line.ActionMarkers[0];
            var charge = line.ActionMarkers[1];

            Assert.That(attack.Beat, Is.EqualTo(4f));
            Assert.That(attack.Icon.sprite, Is.SameAs(catalogue.Sprite(ActionMarker.ActionKindName, ChartLoader.KindToId(EnemyActionKind.AttackLeft))), "the beat-4 marker does not show the attack-left icon");
            Assert.That(attack.CountdownText, Is.EqualTo("3"));
            Assert.That(attack.WindUpShown, Is.False, "an attack shows a wind-up bar");

            Assert.That(charge.Beat, Is.EqualTo(8f), "the Charge does not land on beat 8");
            Assert.That(charge.Icon.sprite, Is.SameAs(catalogue.Sprite(ActionMarker.ActionKindName, ChartLoader.KindToId(EnemyActionKind.Charge))), "the beat-8 marker does not show the charge icon");
            Assert.That(charge.WindUpShown, Is.True, "the Charge shows no wind-up bar");
            Assert.That(charge.WindUp.sprite, Is.SameAs(catalogue.Sprite(RhythmLineView.UiKind, ActionMarker.WindUpBarId)), "the wind-up bar is not the catalogue's");

            float barRight = charge.Rect.anchoredPosition.x - ActionMarker.IconSize / 2f;
            float barLeft = barRight - charge.WindUp.rectTransform.sizeDelta.x;
            Assert.That(barLeft, Is.EqualTo(5f * line.BeatWidth).Within(Tolerance), "the wind-up bar does not start at beat 5");
            Assert.That(barRight, Is.LessThan(8f * line.BeatWidth), "the wind-up bar does not run into beat 8");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        /// <summary>P2.3: the open window puts the catalogue's glow behind the marker's icon.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator open_window_glows_telegraph()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var rig = ClientTestContent.ScheduledRig("presenter-telegraph-glow", Beats.ToQuarterBeats(4));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var line = hud.RhythmLine;
            var pending = rig.Driver.Battle!.PendingAction;

            yield return rig.WaitUntilAudioMs(pending.OpenMs - 200);
            yield return null;
            var marker = line.ActionMarkers.Single();
            Assert.That(marker.GlowShown, Is.False, "the marker glowed before its window opened");

            yield return rig.WaitUntilAudioMs(pending.OpenMs + 20);
            yield return null;
            Assert.That(rig.Clock.NowMs, Is.LessThan(pending.CloseMs), "the window had already closed when checked");
            Assert.That(marker.GlowShown, Is.True, "the marker does not glow inside its window");
            Assert.That(marker.Glow.sprite, Is.SameAs(catalogue.Sprite(RhythmLineView.UiKind, ActionMarker.GlowId)), "the glow is not the catalogue's");

            yield return rig.WaitUntilAudioMs(pending.CloseMs + 60);
            yield return null;
            Assert.That(marker.GlowShown, Is.False, "the glow stayed on after the window closed");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }

        /// <summary>P2.4: Iron Veil darkens the line and badges the enemy bar for as long as it stands.</summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator iron_veil_darkens_line_for_five_beats()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var track = ClientTestContent.FixtureTrack();
            var chart = ClientTestContent.Chart(track, new EnemyAction(EnemyActionKind.Buff, Beats.ToQuarterBeats(2)));
            var rig = ClientTestContent.ScheduledRig("presenter-veil", chart, ClientTestContent.EnemyWith(chart, EnemyAbility.IronVeil));
            var hud = BattleHud.Build(rig.Driver, null, null, null);
            var line = hud.RhythmLine;
            var battle = rig.Driver.Battle!;
            var enemyBar = hud.Statuses.Enemy;

            Assert.That(line.Veiled, Is.False, "the line was dark before the Buff landed");

            yield return rig.WaitUntilAudioMs(track.BeatMap.TimeAtBeat(3));
            yield return null;
            Assert.That(battle.IronVeilActive, Is.True, "the Buff on beat 2 did not raise the veil");
            Assert.That(line.Veiled, Is.True, "the line did not go dark under the veil");
            Assert.That(line.Background.sprite, Is.SameAs(catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.DarkBackgroundId)), "the line is not showing the dark background");
            Assert.That(enemyBar.AbilityShown, Is.EqualTo(EnemyLoader.AbilityToId(EnemyAbility.IronVeil)), "the enemy bar does not show the Iron Veil icon");
            Assert.That(enemyBar.AbilityBadge.sprite, Is.SameAs(catalogue.Sprite(SideBarView.AbilityKind, EnemyLoader.AbilityToId(EnemyAbility.IronVeil))), "the badge is not the catalogue's Iron Veil icon");

            yield return rig.WaitUntilAudioMs(track.BeatMap.TimeAtBeat(6));
            yield return null;
            Assert.That(line.Veiled, Is.True, "the line lightened before the veil's five beats were up");

            yield return rig.WaitUntilAudioMs(track.BeatMap.TimeAtBeat(8));
            yield return null;
            Assert.That(battle.IronVeilActive, Is.False, "the veil outlasted its five beats");
            Assert.That(line.Veiled, Is.False, "the line stayed dark after the veil expired");
            Assert.That(line.Background.sprite, Is.SameAs(catalogue.Sprite(RhythmLineView.UiKind, RhythmLineView.BackgroundId)), "the line did not return to the normal background");
            Assert.That(enemyBar.AbilityShown, Is.Null, "the Iron Veil icon stayed after the veil expired");
            Assert.That(enemyBar.AbilityBadge.gameObject.activeInHierarchy, Is.False, "the badge is still on screen");

            var activated = battle.Events.OfType<ModifierActivated>().Single(e => e.Value == EffectValue.EnemyDamageTaken);
            Assert.That(activated.Beats, Is.EqualTo(Tuning.IronVeilBeats), "the veil that darkened the line does not last five beats");

            Object.Destroy(hud.gameObject);
            rig.Destroy();
        }
    }
}
