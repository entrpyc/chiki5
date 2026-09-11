# Chiki — Implementation plan

| PRD       | docs/project/prd.md |
| :-------- | :------------------ |
| Requested | Replace every placeholder graphic and sound in the client with real assets, and list in every phase and item the assets the operator must supply (operator, 2026-09-11) |
| Scope     | 25 requirements and 6 groundwork pieces in 11 phases, 50 items |
| Tests run | `dotnet test sim/Chiki.sln` for the simulation; `powershell -File tools/run-client-tests.ps1` for the client, EditMode then PlayMode once P1.1 is done |

**Completion rule.** An item is done when every test it lists is green in the suite. A requirement is done when every item that cites it is done. A phase is done when every item in it is done and the whole suite is green. Nothing is marked done on any other evidence.

**Operator assets gate items.** Every item with an *Assets* line cannot go green until the operator has put those files in the repository; its tests read the shipped catalogue, not generated stand-ins. Phase 1 needs nothing from the operator.

**Asset conventions.** Sprites are PNG, 8-bit RGBA, straight alpha, drawn at 1080p reference size and imported at 100 pixels per unit with bilinear filtering (docs/project/unity-setup.md). Names follow `spr_<kind>_<subject>_<variant>_<nn>.png`: *kind* is one of enemy, player, vfx, bg, card, portrait, status, node, action, category, ability, trait, role, charm, imprint, ui, logo; *subject* is the content id without its kind prefix (`ren` for `enemy-ren`, `keen-edge` for `imprint-keen-edge`); *variant* is the clip or state (`idle`, `attack-left`, `static`, `pressed`); *nn* counts frames from 01. Files go under `client/Assets/_Project/Art/Shared/<subject>/` or `Art/World1/<subject>/`. Character and effect clips come with a `<kind>_<subject>.clips.json` beside the frames giving each clip's length in beats, whether it loops, and its strike frame. Each 9-slice sprite states its border in its item. Sound effects are WAV, 48 kHz, 16-bit, mono, named `sfx_<subject>_<variant>.wav`, under `client/Assets/_Project/Audio/`; music is OGG Vorbis, 48 kHz, stereo, named `mus_w<world>_<subject>.ogg`, with its sidecar under `data/tracks/`. No asset contains player-facing lettering; all text comes from the string table. Git LFS already tracks png, psd, wav, ogg and aseprite.

## Scope

### Requested

| PRD # | Requirement | Phase |
| :-- | :-- | :-- |
| 3.3.1.1 | The Rhythm Line showing telegraphed actions is visible for the whole battle | 2 |
| 3.6.3 | Upcoming actions on the Rhythm Line: what, on which beat, how many beats remain | 2 |
| 3.3.8.1 | Audio cue per grade, key-press flash, glow on playable cards, line highlights, shake and effects on heavy hits | 2, 3, 4 |
| 3.6.9 | Iron Veil is shown by an icon and a darker Rhythm Line | 2 |
| 3.4.2 | Categories are colour-coded and icon-coded: Attack red, Defense blue, Ability green | 3 |
| 3.3.5.4 | A cooling slot shows a dim overlay and a numeric or radial beat countdown; inactive line thinner | 3 |
| 3.3.5.3 | A cooling slot pressed gives disabled feedback, sound and flash | 3 |
| 3.3.7.1 | Status icons sit above the HP and ARD bars with tooltips | 4 |
| 3.8.1 | CRP always visible on the map and during battle | 4 |
| 3.14.1 | Stylised, readable combat visuals; the cute exterior (Lulu) | 5, 6, 10 |
| 3.4.7 | Card anatomy: name, Category, Rarity, value, cooldown, statuses, special rules, flavour | 7 |
| 3.5.5 | Before every battle the player may open the Binder, preview cards and rebuild the loadout | 7 |
| 3.7.2 | Normal battle reward: choose 1 card of 3 offered | 7 |
| 3.6.26 | The enemy card shows portrait, name, BPM, powers, quote, New or Type badge, Binder and Fight | 8 |
| 3.2.16 | The map shows player icon, connections, node type icons, continent name, stats, Charms, Imprints, Binder, settings | 9 |
| 3.9.11 | The run-end screen shows outcome, seed, stats and every unlock | 10 |
| 3.12.1 | The latency calibration screen runs an audio and video offset test | 10 |
| 3.6.28 | Every enemy is bound to exactly one track of its own | 11 |
| 3.6.32 | Chart and track loop together seamlessly; the music never stops | 11 |
| 3.3.1.6 | The music is never desynchronised, stretched, paused or interrupted | 11 |
| 4.14 | Music track: World, tier, tempo map with offset, beat map, length in beats | 1 |
| 6.2 | 60 fps at 1080p on a 2019 integrated-graphics laptop, no audio dropouts | 11 |

### Blockers pulled in

| PRD # | Requirement | Blocks | Phase |
| :-- | :-- | :-- | :-- |
| 3.5.8 | The upcoming enemy's card is visible while the loadout is edited | 3.6.26 | 8 |
| 3.5.11 | The Binder opens from the map to review cards, Traits and Unstable lifespans without editing | 3.2.16 | 9 |
| 4.11 | Trait definition: name, effect, source | 3.5.11 | 9 |
| — | Client test runner runs the EditMode suite as well as PlayMode | all | 1 |
| — | Sprite importer for the `spr_` naming convention, with clip sidecars | all | 1 |
| — | Visual catalogue: content id to sprite or clip, with a recorded fallback | all | 1 |
| — | Audio catalogue: sound-effect and track ids to clips, with generated fallback | 3.3.5.3, 3.3.8.1, 3.12.1, 4.14 | 1 |
| — | BeatAnimator: sprite clips stepped from the beat clock, strike frame on the beat | 3.3.8.1, 3.14.1 | 1 |
| — | Profile records the enemies it has fought, for the New badge | 3.6.26 | 8 |

### Left out

| PRD # | Requirement | Why |
| :-- | :-- | :-- |
| 3.14.7 | The player's appearance advances a corruption stage at each CRP threshold, beast form at 100 | Blocked by 3.8.4, whose band effects are content not yet written in decisions.md, and by 3.8.5, the beast rampage; the operator chose to leave it out. Lulu is drawn in one appearance under 3.14.1. |
| 3.2.15 | Each World's biome art, decorations, palette, soundtrack, BPM theme and enemy pool, props placed procedurally | Needs World 2 and 3 enemy pools, charts and tracks, which do not exist; the operator chose to leave it out. One arena background and one map backdrop serve every World. |
| 3.14.2 | Each World is a continent with its own fiction, biome, soundtrack and enemy pool | Same chain as 3.2.15; left out with it. |

### Already in place

| PRD # | Evidence | Tested |
| :-- | :-- | :-- |
| 3.3.1.1 | `RhythmLineView` scrolls on the beat clock, drawn in flat colours | yes, `Client.Presenter › rhythm_line_present_and_scrolling` |
| 3.6.3 | Telegraph markers with kind label and countdown, flat coloured squares | yes, `Client.Presenter › telegraph_drawn_with_countdown` |
| 3.3.5.4 | Cooldown overlay with a numeric countdown; inactive line at 70% | yes, `Client.Presenter › cooldown_countdown_and_thin_inactive_line` |
| 3.3.5.3 | A cooling press is refused and emits `SlotDisabled` | yes, `Sim.Cooldown › disabled_press_is_not_a_miss` |
| 3.3.8.1 | Generated sine cues, a white flash, a solid glow and camera shake | yes, `Client.Presenter › grade_cue_and_flash`, `glow_during_open_window`, `shake_on_heavy_hit` |
| 3.3.7.1 | Status squares with stacks and tooltips; HP and ARD bars | yes, `Client.Presenter › status_icon_with_tooltip` |
| 3.6.9 | Iron Veil reduces damage by 80% for 5 beats | yes, `Sim.Powers › iron_veil_reduces_80_for_5_beats` |
| 3.8.1 | CRP text on the map and in battle with a change label | yes, `Client.Crp › visible_on_map_and_battle_with_change_label` |
| 3.4.7 | Card anatomy fields in the definition schema | yes, `Sim.Cards › cooldown_must_be_2_to_6` |
| 3.5.5 | Binder editing screen with an empty-slot guard, text only | yes, `Client.Loadout › cannot_confirm_with_empty_slot` |
| 3.7.2 | One of three cards offered on a Normal win | yes, `Sim.Rewards › pick_one_of_three_into_binder` |
| 3.6.26 | Pre-battle panel with enemy name, Fight and Edit buttons (partly built) | yes, `Client.Loadout › enter_starts_battle_with_kept_loadout` |
| 3.2.16 | Map with player marker, connections, typed labels, ARD, CRP, Essence, seed, settings (partly built) | yes, `Client.Map › select_neighbour_moves` |
| 3.9.11 | Run-end screen with outcome, seed, stats and unlock ids | yes, `Client.RunEnd › death_shows_summary_and_returns` |
| 3.12.1 | Calibration screen, stored offset, offered first and reachable from the map | yes, `Client.Calibration › median_offset_stored` |
| 3.6.28 | Each enemy's battle runs on its own track's beat map | yes, `Sim.Enemies › battle_tempo_from_enemy_track` |
| 3.6.32 | Chart and track loop together | yes, `Sim.Chart › loops_from_start_seamlessly` |
| 3.3.1.6 | Continuous playback through Stun and Signature, on the generated click track | yes, `Client.Clock › playback_continuous_through_stun_and_signature` |
| 3.3.1.9 | Tempo map drives the beat clock | yes, `Sim.Track › tempo_change_shifts_later_beats` |
| 4.14 | Track with offset and beat map | yes, `Sim.Track › offset_shifts_all_beats` |
| 6.2 | 60 fps battle on placeholder visuals | yes, `Client.Perf › battle_scene_60fps_no_audio_dropouts` |
| — | `BeatClock` on `AudioSettings.dspTime`; string table in `data/strings/en.json`; `HudFactory` and `ScreenFactory` build every image and text; profile migrations in `ProfileStore.Migrate`; Git LFS for png, wav and ogg | yes, the Client and Sim suites above |

## Order

Nothing can be drawn until the client can load it, so Phase 1 is pure groundwork: the runner learns EditMode (import settings and catalogue completeness need `UnityEditor`), then the sprite importer, the visual and audio catalogues that sit on it, the BeatAnimator that plays imported clips, and the recorded-track loader that 4.14 needs. Every later phase needs the visual catalogue; the stage and heavy-hit effect also need the BeatAnimator.

After that the order follows what the player reads first in a fight. The Rhythm Line (3.3.1.1, 3.6.3, 3.6.9) comes before the slots (3.4.2, 3.3.5.4, 3.3.5.3) because the telegraph is what a press answers; feedback and the bars (3.3.8.1, 3.3.7.1, 3.8.1) come after both because they react to presses on slots. Characters (3.14.1) follow the HUD, split so Lulu and Ren, the tutorial enemy, land first and the rest of the cast second. Cards (3.4.7, 3.5.5, 3.7.2) come before the enemy card (3.6.26, 3.5.8) because the enemy card sits beside the Binder's faces while editing, and the map (3.2.16) comes after both because its read-only Binder (3.5.11, which needs 4.11) reuses those faces. The UI skin sweep of 3.14.1 comes after every screen exists, since its test asks every screen to be free of fallbacks. Music (3.6.28, 3.6.32, 3.3.1.6) is last but one only because nothing else waits on it; the frame-rate measurement (6.2) closes the plan because it must run with every shipped asset. Ties within a phase were broken behaviour before content: the component that draws a thing precedes the item that fills it with the operator's art.

## Phases

### Phase 1 — Assets have somewhere to land

*The client imports sprites and sprite clips by name, looks up art and sound by content id with placeholders as the fallback, animates clips from the beat clock, and plays a recorded track when one exists. Done when every P1 test is green and the suite passes.*

**Operator supplies:** nothing. Every Phase 1 test generates its own sprites and clips.

#### P1.1 Client runner runs EditMode and PlayMode
- PRD: — (groundwork for every item from P1.2 to P11.4)
- Does: `tools/run-client-tests.ps1` runs the EditMode suite, then the PlayMode suite, in two batch-mode invocations; writes `results-editmode.xml` and `results-playmode.xml`; prints one summary line per platform; and exits non-zero when either platform fails or writes no results. Asset checks that need `UnityEditor` live in `Chiki.Client.Tests.EditMode`, which gains a reference to `Chiki.Client.Editor`.
- Assets: none.
- Needs: —
- Test (integration): `Client.Pipeline › editmode_suite_runs` — given the EditMode assembly, when the runner executes, then this test is reported passed in `results-editmode.xml` and `results-playmode.xml` is still written.

#### P1.2 Sprite importer and clip sidecar
- PRD: — (groundwork for P1.3, P1.5 and every item with sprite assets)
- Does: an `AssetPostprocessor` in `Chiki.Client.Editor` imports every PNG under `client/Assets/_Project/Art/` named by the asset conventions as a single sprite at 100 pixels per unit, bilinear, no mipmaps, pivot at bottom centre for kinds enemy, player and vfx and centred otherwise, and records each sprite's opaque bounds. A PNG under `Art/` whose name does not match, or whose kind is not listed, fails the import with an error naming the file. For enemy, player and vfx, frames sharing kind, subject and variant become one `SpriteClip` asset, in `nn` order, with length in beats, loop flag and strike frame read from the sidecar; a clip with no sidecar entry, or a strike frame past its last frame, fails the import. Sprites pack into one Sprite Atlas per subject at 2048 px, 4096 when the frames do not fit. Assumption: this settles the unity-setup contradiction between one atlas per enemy and one per World in favour of one per subject.
- Assets: none.
- Needs: P1.1
- Test (integration): `Client.Importer › sequence_becomes_clip` — given generated `spr_enemy_test_attack-left_01.png` to `_03.png` and a sidecar giving attack-left 1 beat, no loop, strike frame 2, when imported, then one clip exists with 3 frames in order, 100 pixels per unit, bilinear filtering, bottom-centre pivot and strike frame 2.
- Test (integration): `Client.Importer › misnamed_file_rejected` — given `spr_enemy_test.png` under `Art/`, when imported, then an import error names the file and no clip or catalogue entry is created for it.

#### P1.3 Visual catalogue
- PRD: — (groundwork for P2.1 to P10.4)
- Does: a `VisualCatalogue` ScriptableObject at `client/Assets/_Project/Data/VisualCatalogue.asset` maps a kind and a content id to a `Sprite`, or to a `SpriteClip` for enemy, player and vfx. A lookup of an unknown id returns the fallback sprite, or a one-frame fallback clip, and records `kind/id` in `Missing`. The menu command Chiki › Rebuild Visual Catalogue fills it from every imported `spr_` asset, so the operator never edits it by hand. Boot loads it once and hands it to `BattleHud`, `ScreenFactory` and `HudFactory`; `HudFactory.Image` takes an optional sprite and uses Sliced mode when the sprite has a border. A built-in 4 × 4 white sprite, made by the implementer, serves plain colour fills such as scrims and is never counted as missing.
- Assets: none.
- Needs: P1.2
- Test (integration): `Client.Catalogue › lookup_returns_sprite_or_fallback` — given a catalogue holding a test sprite as status `bleed`, when `bleed` and `nothing` are looked up, then `bleed` returns the test sprite, `nothing` returns the fallback, and `Missing` lists `status/nothing`.
- Test (integration): `Client.Catalogue › rebuild_collects_imported_sprites` — given imported test files `spr_status_bleed_static_01.png` and `spr_node_elite_static_01.png`, when the catalogue is rebuilt, then both resolve by kind and id.
- Test (integration): `Client.Presenter › hud_reads_catalogue_from_boot` — given Boot started with a catalogue whose status `bleed` is a test sprite, when the battle HUD shows Bleed on the enemy, then the status icon carries that sprite.

#### P1.4 Audio catalogue
- PRD: — (groundwork for P1.6, P3.4, P4.1, P10.4)
- Does: an `AudioCatalogue` ScriptableObject beside the visual catalogue maps sound-effect ids (`cue-perfect`, `cue-good`, `cue-miss`, `press-disabled`, `click-beat`, `click-accent`) and track ids to `AudioClip`s, rebuilt by the same menu command from files named by the asset conventions. Sound effects import as Decompress On Load so they play without decode latency; tracks import as Compressed In Memory with Preload Audio Data on, so scheduling never waits on disk. `JudgmentCues`, `Metronome` and `PlaceholderAudio` ask the catalogue first and fall back to today's generated tones only for ids it lacks.
- Assets: none.
- Needs: P1.1
- Test (integration): `Client.Catalogue › audio_lookup_or_generated_fallback` — given a catalogue holding a test clip for `cue-perfect` only, when a Perfect and a Good are judged, then the Perfect plays the test clip and the Good plays the generated tone.
- Test (integration): `Client.Catalogue › metronome_uses_catalogue_click` — given a catalogue holding a test clip for `click-beat`, when the metronome is on for 4 beats, then 4 plays of that clip are scheduled at the beat map's times.

#### P1.5 BeatAnimator
- PRD: — (groundwork for P4.2, P5.1, P5.2)
- Does: a `BeatAnimator` component shows a `SpriteClip` on a `SpriteRenderer` or uGUI `Image`, picking the frame from the `BeatClock`'s audio time only, never `Time.time`, `Time.deltaTime` or Animator time. It runs at 8 frames per second at 120 BPM, scaled by the BPM in force ÷ 120, so a tempo change retimes the clip at the change. Looping clips wrap on whole beats. `PlayStrikeAt(clip, audioMs)` starts a one-shot clip early enough that its strike frame is on screen at `audioMs`, then returns to the idle loop.
- Assets: none.
- Needs: P1.2
- Test (integration): `Client.BeatAnimator › frame_rate_scales_with_bpm` — given an 8-frame looping clip on a track at 120 BPM changing to 180 BPM at beat 8, when one second of audio time elapses before and after the change, then 8 and then 12 frames advance.
- Test (integration): `Client.BeatAnimator › strike_frame_lands_on_beat` — given a 6-frame attack clip with strike frame 4 requested to strike at beat 8, when audio time reaches beat 8, then frame 4 is on screen, within one frame.
- Test (integration): `Client.BeatAnimator › follows_audio_not_frame_time` — given the frame rate capped at 20 fps, when 2 seconds of audio time elapse, then the frame on screen equals the frame computed from audio time.

#### P1.6 Recorded track loader
- PRD: 4.14
- Does: `BattleScene` and `CalibrationScreen` resolve a track id through the audio catalogue and schedule the recorded clip on the `BeatClock`; the generated click track plays only when the catalogue has no clip for the id, with a warning naming it. The sidecar's offset puts beat 0 that many milliseconds into the clip and the tempo map places every later beat (3.3.1.9).
- Assets: none.
- Needs: P1.4
- Test (integration): `Client.Audio › recorded_clip_preferred_over_click_track` — given a catalogue holding a test clip for `track-fixture-ren`, when a Ren battle starts, then the clip scheduled on the beat clock is that clip and not a generated click track.
- Test (measurement): `Client.Audio › sidecar_offset_places_beat_zero` — given a test clip with one click at 37 ms and a sidecar offset of 37, when an action charted at beat 0 is answered at the click's audio time, then it is judged Perfect with a timing error under 2 ms.

### Phase 2 — The Rhythm Line reads at a glance

*The strip the player reads the whole fight from is drawn art: background, ticks, playhead, Judgment Window, five telegraph icons, their open-window glow, and Iron Veil's darker line. Done when every P2 test is green and the suite passes.*

**Operator supplies:** the Rhythm Line kit of seven sprites, five telegraph icons, a wind-up bar, a telegraph glow, a dark line background and the Iron Veil icon. Fourteen sprites in all, every one listed in the items below.

#### P2.1 Rhythm Line art
- PRD: 3.3.1.1
- Does: `RhythmLineView` draws its background, beat ticks, quarter ticks, playhead and Judgment Window band, idle and open, from the catalogue. Geometry stays as built: a 1600 × 180 strip, 120 px per beat, 8 beats ahead and 3 behind, playhead at 30% from the left. Beat numbers stay text.
- Assets, in `Art/Shared/rhythmline/`:
  - `spr_ui_rhythmline_bg_01.png`, 9-slice, drawn at 400 × 180, 32 px left and right borders
  - `spr_ui_rhythmline_beat_01.png`, 4 × 126
  - `spr_ui_rhythmline_quarter_01.png`, 2 × 54
  - `spr_ui_rhythmline_playhead_01.png`, 8 × 180
  - `spr_ui_rhythmline_window_01.png` and `spr_ui_rhythmline_window-open_01.png`, 9-slice, 64 × 144, 16 px left and right borders
- Needs: P1.3
- Test (integration): `Client.Catalogue › rhythm_line_kit_complete` — given the shipped catalogue, when the six Rhythm Line sprites are read, then each exists at its stated size and the three 9-slice sprites have left and right borders set.
- Test (integration): `Client.Presenter › rhythm_line_drawn_from_catalogue` — given a running battle with the shipped catalogue, when two beats render, then the background, every visible tick, the playhead and the window band carry catalogue sprites and `Missing` is empty.

#### P2.2 Telegraph icons
- PRD: 3.6.3
- Does: each action marker shows its kind's icon instead of a coloured square, at 80 × 80, with the kind label and beats-remaining count as text; the Charge wind-up bar uses the wind-up sprite sliced along its length. Imminent and highlighted markers keep today's scale changes.
- Assets, in `Art/Shared/telegraph/`:
  - `spr_action_attack-left_static_01.png`, `spr_action_attack-right_static_01.png`, `spr_action_defend_static_01.png`, `spr_action_buff_static_01.png`, `spr_action_charge_static_01.png`, 80 × 80 each, told apart by shape alone
  - `spr_ui_windup-bar_static_01.png`, 9-slice, 64 × 6, 3 px left and right borders
  - The placeholder attack-right is blue, the Defense category's colour; choose telegraph colours that do not collide with the category colours of 3.4.2.
- Needs: P1.3
- Test (integration): `Client.Catalogue › telegraph_icons_complete` — given the shipped catalogue, when the five action kinds are looked up, then each has an 80 × 80 icon and no two icons are the same image.
- Test (integration): `Client.Presenter › telegraph_shows_kind_icon` — given AttackLeft at beat 4 and a Charge at beat 8 with a 3-beat wind-up, when the battle is at beat 1, then the beat-4 marker shows the attack-left icon with count 3, and the beat-8 marker shows the charge icon with a wind-up bar spanning beats 5 to 8.

#### P2.3 Telegraph highlight
- PRD: 3.3.8.1
- Does: while an action's Judgment Window is open, its marker shows the glow sprite behind the icon, tinted by kind in code; the glow leaves when the window closes.
- Assets: `spr_ui_telegraph-glow_static_01.png`, 120 × 120, soft white glow on transparent.
- Needs: P2.2
- Test (integration): `Client.Presenter › open_window_glows_telegraph` — given AttackLeft at beat 4 and the shipped catalogue, when audio time enters and then leaves beat 4's window, then the marker shows the catalogue glow inside the window and hides it after.

#### P2.4 Iron Veil darkens the line
- PRD: 3.6.9
- Does: a `ModifierActivated` event from Iron Veil swaps the Rhythm Line to the dark background and shows the Iron Veil icon at the enemy bar; the matching `ModifierExpired` restores both. Later abilities that darken the line reuse the same dark background.
- Assets: `spr_ui_rhythmline_bg-dark_01.png`, same size and borders as the background in P2.1; `spr_ability_iron-veil_static_01.png`, 64 × 64.
- Needs: P2.1
- Test (integration): `Client.Presenter › iron_veil_darkens_line_for_five_beats` — given an enemy with Iron Veil whose buff lands at beat 2, when beats 2 to 8 render, then the line shows the dark background and the Iron Veil icon until the veil expires 5 beats later, and the normal background with no icon after.

### Phase 3 — Slots read by colour and shape

*The sixteen slots carry their category's frame and icon, a radial cooldown sweep, a drawn glow and flash, and a distinct disabled flash and sound. Done when every P3 test is green and the suite passes.*

**Operator supplies:** three category slot frames, three category icons, a cooldown sweep sprite, a glow, a press flash, a disabled flash, and one disabled-press sound. Ten sprites and one WAV.

#### P3.1 Category frames and icons
- PRD: 3.4.2
- Does: every slot frame uses its category's frame, Attack red for E, R, U and I, Defense blue for O and P, Ability green for Q and W, with the category icon in the top-right corner; the icon always appears with the colour, never instead of it. Empty slots show the frame at half opacity. The inactive line stays at 70% scale.
- Assets, in `Art/Shared/slots/`:
  - `spr_ui_slot-frame_attack_01.png`, `spr_ui_slot-frame_defense_01.png`, `spr_ui_slot-frame_ability_01.png`, 9-slice, drawn at 160 × 120, 16 px borders, red, blue and green
  - `spr_category_attack_static_01.png`, `spr_category_defense_static_01.png`, `spr_category_ability_static_01.png`, 64 × 64, three silhouettes that stay distinct in greyscale
- Needs: P1.3
- Test (integration): `Client.Catalogue › category_art_complete` — given the shipped catalogue, when the three categories are read, then each has a frame with borders set and a 64 × 64 icon, and no two icons are the same image.
- Test (integration): `Client.Presenter › slot_frames_follow_category` — given the starter loadout, when the battle HUD renders, then on both lines Q and W carry the ability frame and icon, E, R, U and I the attack frame and icon, and O and P the defense frame and icon.

#### P3.2 Radial cooldown
- PRD: 3.3.5.4
- Does: the cooldown overlay becomes a radial sweep, `Image.Filled` Radial 360 clockwise from the top, whose fill is the fraction of cooldown left, read from audio time so it moves smoothly between beats; the beat count stays as text on top.
- Assets: `spr_ui_slot-cooldown_static_01.png`, 128 × 128, a white rounded square matching the slot frame's corners; the client dims and fills it.
- Needs: P3.1
- Test (integration): `Client.Presenter › cooldown_sweeps_with_beats` — given a 3-beat cooldown started at beat 1, when audio time is at beat 1, beat 2 and halfway through beat 3, then the fill is 1, 2/3 and 1/6 within 0.02, the count reads 3, 2 and 1, and the overlay carries the catalogue sprite.

#### P3.3 Glow and press flash
- PRD: 3.3.8.1
- Does: the open-window glow and the key-press flash use their sprites; the flash fades from full to nothing over half a beat of audio time, as today.
- Assets: `spr_ui_slot-glow_static_01.png`, 9-slice, 172 × 132, 22 px borders, soft outer glow; `spr_ui_slot-flash_static_01.png`, 9-slice, 160 × 120, 16 px borders, white.
- Needs: P3.1
- Test (integration): `Client.Presenter › glow_and_flash_drawn_from_catalogue` — given the window at beat 4 and a press on E at beat 4, when rendered, then the playable slots' glow and E's flash carry the catalogue sprites, and E's flash alpha is 0 half a beat later.

#### P3.4 Disabled press
- PRD: 3.3.5.3
- Does: a `SlotDisabled` event plays `press-disabled` from the audio catalogue and shows the disabled flash on that slot for half a beat; no judgment cue plays and the ordinary press flash does not show.
- Assets: `spr_ui_slot-disabled_static_01.png`, 9-slice, 160 × 120, 16 px borders; `sfx_press_disabled.wav`, under 150 ms, clearly unlike the three judgment cues.
- Needs: P3.1, P1.4
- Test (integration): `Client.Presenter › disabled_press_sound_and_flash` — given the shipped catalogues and E cooling with 2 beats left, when E is pressed, then the shipped `press-disabled` clip plays once, E shows the disabled flash and not the press flash, and no judgment cue plays.

### Phase 4 — Feedback you hear and feel

*Judgments sound like the game, heavy hits burst, and statuses, bars, Block and CRP are drawn. Done when every P4 test is green and the suite passes.*

**Operator supplies:** three judgment cue recordings, a four-frame heavy-hit burst with its clip sidecar, six status icons, a tooltip panel, a bar frame, two bar fills, a Block icon and a CRP icon. Sixteen sprites, one sidecar and three WAVs.

#### P4.1 Recorded judgment cues
- PRD: 3.3.8.1
- Does: `JudgmentCues` plays the recorded Perfect, Good and Miss clips from the audio catalogue through its own source.
- Assets: `sfx_cue_perfect.wav`, `sfx_cue_good.wav`, `sfx_cue_miss.wav`, each under 150 ms with its attack inside the first 5 ms, so the cue sits on the beat.
- Needs: P1.4
- Test (measurement): `Client.Catalogue › judgment_cues_complete_and_tight` — given the shipped audio catalogue, when the three cues are read, then each exists, lasts under 150 ms, imports as Decompress On Load, and has its first sample above half its peak within 5 ms of the start.
- Test (integration): `Client.Presenter › judgment_cues_play_recordings` — given the shipped catalogue, when a Perfect, a Good and a Miss are judged, then the three recorded clips play in that order and no generated tone plays.

#### P4.2 Heavy-hit effect
- PRD: 3.3.8.1
- Does: a `DamageTaken` of 15 or more plays the heavy-hit clip over the player through `BeatAnimator`, alongside today's camera shake; smaller hits play neither.
- Assets: `spr_vfx_hit-heavy_burst_01.png` to `_04.png`, 256 × 256, one beat long; `vfx_hit-heavy.clips.json` giving burst 1 beat, no loop, strike frame 1.
- Needs: P1.5, P1.3
- Test (integration): `Client.Presenter › heavy_hit_plays_effect` — given the shipped catalogue, when DamageTaken 20 is handled, then the heavy-hit clip starts over the player and the shake triggers; when DamageTaken 5 is handled, neither happens.

#### P4.3 Status icons and tooltip
- PRD: 3.3.7.1
- Does: status icons use their art, 44 px on screen, with the stack count as text; the tooltip panel uses the tooltip sprite.
- Assets, in `Art/Shared/status/`: `spr_status_scar_static_01.png`, `spr_status_weak_static_01.png`, `spr_status_stun_static_01.png`, `spr_status_bleed_static_01.png`, `spr_status_thorns_static_01.png`, `spr_status_disarmed_static_01.png`, 64 × 64 each, told apart by shape; `spr_ui_tooltip_static_01.png`, 9-slice, 260 × 70, 12 px borders.
- Needs: P1.3
- Test (integration): `Client.Catalogue › status_icons_complete` — given the shipped catalogue, when the six statuses are looked up, then each has a 64 × 64 icon and no two are the same image.
- Test (integration): `Client.Presenter › status_icon_drawn_from_catalogue` — given Bleed 2 on the enemy with 6 beats left, when rendered, then the icon carries the bleed sprite with "2" and its tooltip panel carries the tooltip sprite.

#### P4.4 Bars and Block
- PRD: 3.3.7.1
- Does: the enemy HP and player ARD bars use a frame and a horizontal filled image each; Block shows as its icon and value beside the bar instead of a text suffix.
- Assets: `spr_ui_bar_frame_01.png`, 9-slice, 560 × 32, 8 px borders; `spr_ui_bar_fill-enemy_01.png` and `spr_ui_bar_fill-ard_01.png`, 552 × 24; `spr_ui_block_static_01.png`, 64 × 64.
- Needs: P1.3
- Test (integration): `Client.Presenter › bars_fill_and_block_icon` — given enemy HP 60 of 120 and ARD 300 of 300 with Block 10, when rendered, then the enemy fill is 0.5, the ARD fill is 1, the Block icon shows with 10, and every bar image carries a catalogue sprite.

#### P4.5 CRP badge
- PRD: 3.8.1
- Does: the CRP value on the map and in battle sits beside the CRP icon; the change label still shows source and amount.
- Assets: `spr_ui_crp_static_01.png`, 64 × 64.
- Needs: P1.3
- Test (integration): `Client.Crp › crp_badge_on_map_and_battle` — given CRP 7 and the shipped catalogue, when the map and then a battle render, then both show the CRP icon beside 7.

### Phase 5 — Fighters on the stage

*Lulu and Ren stand on a drawn arena and move on the beat, each strike landing on its action's beat. Done when every P5 test is green and the suite passes.*

**Operator supplies:** the arena background, Lulu's seven clips with sidecar, and Ren's seven clips with sidecar. About 80 frames, one background and two sidecars.

#### P5.1 Enemy on stage
- PRD: 3.14.1
- Does: the battle scene places the enemy on the right of the stage, facing left, feet on the floor line, behind the HUD. `BeatAnimator` loops idle; each charted action plays its clip with the strike frame on the action's beat: attack left, attack right, defend, and for a Charge the wind-up loop through its wind-up beats, then the attack-left clip on the Charge's beat. `DamageDealt` to the enemy plays hit; a won battle plays death and holds its last frame. Assumption: a Charge resolves on the attack-left clip, because the seven-clip set in unity-setup has no release clip.
- Assets: none; the test generates its clips.
- Needs: P1.5, P1.3
- Test (integration): `Client.Stage › enemy_clips_follow_chart` — given a test enemy with all seven clips and a chart of AttackLeft at beat 4, Defend at beat 6 and a Charge at beat 10 with a 3-beat wind-up, when the battle runs, then idle shows at beat 1, the attack-left strike frame at beat 4 within one frame, defend at beat 6, the charge loop during beats 7 to 10, the attack-left strike at beat 10, hit after the player's damage, and the held death frame when HP reaches 0.

#### P5.2 Player on stage
- PRD: 3.14.1
- Does: Lulu stands on the left, facing right, on the same floor line. An attack press plays attack-left or attack-right by the slot's side, a Defense press plays defend, an Ability press plays ability, each with its strike frame on the answered action's beat; `DamageTaken` plays hit; a lost battle plays death. Assumption: the player's clips are idle, attack-left, attack-right, defend, ability, hit and death, the enemy set with ability in place of charge; unity-setup names only the enemy set.
- Assets: none; the test generates its clips.
- Needs: P1.5, P1.3
- Test (integration): `Client.Stage › player_clips_follow_presses` — given presses on E, O and Q answering actions at beats 4, 6 and 8, when they land, then attack-left, defend and ability play with their strike frames on beats 4, 6 and 8, and a DamageTaken plays hit.

#### P5.3 Arena background
- PRD: 3.14.1
- Does: one arena background sits behind the stage in every World; the camera shows its central 1920 × 1080 at 16:9 and more of its sides on ultrawide, never the clear colour.
- Assets: `spr_bg_arena_static_01.png`, 2560 × 1080; the central 1920 × 1080 is the 16:9 frame, the floor line sits 200 px above the bottom edge, and the outer 320 px on each side holds nothing essential, no characters and no lettering.
- Needs: P1.3
- Test (integration): `Client.Stage › arena_fills_16_9_and_ultrawide` — given the shipped background, when a battle renders at 1920 × 1080 and at 2560 × 1080, then the background covers every pixel of the frame both times.

#### P5.4 Lulu's art
- PRD: 3.14.1
- Does: Lulu's seven clips are imported and catalogued as player `lulu`, and the battle uses them.
- Assets, in `Art/Shared/lulu/`:
  - `spr_player_lulu_idle_01.png` to `_08.png`, 2 beats, looping
  - `attack-left`, `attack-right` and `ability`, 4 to 8 frames each, each with a strike frame
  - `defend`, 4 to 6 frames; `hit`, 3 to 4 frames; `death`, 6 to 8 frames
  - every frame a 512 × 512 canvas, Lulu 360 to 440 px tall, feet at bottom centre, facing right; the cute exterior of 3.14.1
  - `player_lulu.clips.json` with each clip's beats, loop flag and strike frame
- Needs: P5.2
- Test (integration): `Client.Catalogue › lulu_art_complete` — given the shipped catalogue, when Lulu's clips are read, then all seven exist, idle has 8 frames looping over 2 beats, every other clip has 3 to 8 frames, the three striking clips have a strike frame, and the first idle frame's opaque height is 360 to 440 px.

#### P5.5 Ren's art
- PRD: 3.14.1
- Does: Ren's seven clips are imported and catalogued as enemy `ren`, and Ren's battles use them. Ren is the tutorial enemy: Normal tier, Tank, slow rhythm, Iron Veil and Guard, "Count with me. One... two... and hold."
- Assets, in `Art/World1/ren/`:
  - `spr_enemy_ren_idle_01.png` to `_08.png`, 2 beats, looping
  - `attack-left` and `attack-right`, 4 to 8 frames each with a strike frame; `defend`, 4 to 6 frames
  - `charge`, 4 frames, 1 beat, looping; `hit`, 3 to 4 frames; `death`, 6 to 8 frames
  - every frame a 512 × 512 canvas, Ren 360 to 440 px tall, feet at bottom centre, facing left
  - `enemy_ren.clips.json` with each clip's beats, loop flag and strike frame
- Needs: P5.1
- Test (integration): `Client.Catalogue › enemy_ren_art_complete` — given the shipped catalogue, when Ren's clips are read, then all seven exist, idle has 8 frames looping over 2 beats, charge has 4 frames looping over 1 beat, the others have 3 to 8 frames, both attacks have a strike frame, and the first idle frame's opaque height is 360 to 440 px.

### Phase 6 — The whole cast

*Every enemy the game can roll is drawn and animated. Done when every P6 test is green and the suite passes.*

**Operator supplies:** Kess, Vey, Orm and Malk, seven clips each with a sidecar, on the same spec as Ren. About 160 frames and four sidecars.

#### P6.1 Kess's art
- PRD: 3.14.1
- Does: Kess's seven clips are catalogued as enemy `kess`. Kess: Normal tier, Aggressor, fast rhythm, Rising Tempo, "Every hit I land is the next one's warm-up."
- Assets: in `Art/World1/kess/`, `spr_enemy_kess_<clip>_<nn>.png` for idle (8 frames), attack-left, attack-right, defend, charge (4 frames), hit and death, on the canvas, height and facing rules of P5.5; `enemy_kess.clips.json`.
- Needs: P5.1
- Test (integration): `Client.Catalogue › enemy_kess_art_complete` — given the shipped catalogue, when Kess's clips are read, then they meet every condition of `enemy_ren_art_complete`.

#### P6.2 Vey's art
- PRD: 3.14.1
- Does: Vey's seven clips are catalogued as enemy `vey`. Vey: Normal tier, Mentalist, fast rhythm, Charge / Buff, applies Bleed, "Small cuts. Many beats. Do the sum."
- Assets: in `Art/World1/vey/`, `spr_enemy_vey_<clip>_<nn>.png` for the seven clips on the rules of P5.5; `enemy_vey.clips.json`.
- Needs: P5.1
- Test (integration): `Client.Catalogue › enemy_vey_art_complete` — given the shipped catalogue, when Vey's clips are read, then they meet every condition of `enemy_ren_art_complete`.

#### P6.3 Orm's art
- PRD: 3.14.1
- Does: Orm's seven clips are catalogued as enemy `orm`. Orm: Elite tier, Tank, slow rhythm, Iron Veil, Stoneform and Guard, applies Weak, "Stone does not hurry. Stone does not miss."
- Assets: in `Art/World1/orm/`, `spr_enemy_orm_<clip>_<nn>.png` for the seven clips on the rules of P5.5; `enemy_orm.clips.json`.
- Needs: P5.1
- Test (integration): `Client.Catalogue › enemy_orm_art_complete` — given the shipped catalogue, when Orm's clips are read, then they meet every condition of `enemy_ren_art_complete`.

#### P6.4 Malk's art
- PRD: 3.14.1
- Does: Malk's seven clips are catalogued as enemy `malk`. Malk: Boss tier, Aggressor, fast rhythm, Rising Tempo and Charge / Buff, Stoneform and Guard, applies Bleed and Weak. Boss phase clips wait for multi-phase bosses, which are not built.
- Assets: in `Art/World1/malk/`, `spr_enemy_malk_<clip>_<nn>.png` for the seven clips on the rules of P5.5; `enemy_malk.clips.json`.
- Needs: P5.1
- Test (integration): `Client.Catalogue › enemy_malk_art_complete` — given the shipped catalogue, when Malk's clips are read, then they meet every condition of `enemy_ren_art_complete`.

### Phase 7 — Cards look like cards

*A card face shows the whole anatomy over its illustration, and the Binder and the reward panel show faces instead of text. Done when every P7 test is green and the suite passes.*

**Operator supplies:** three category card frames, four rarity treatments and twenty card illustrations. Twenty-seven sprites.

#### P7.1 Card face
- PRD: 3.4.7
- Does: a `CardFace` prefab renders the anatomy: illustration, the category frame of 3.4.2, a rarity treatment, name, value, cooldown in beats, the icons of statuses it applies, special rules and flavour text when present. It comes in a full size, 256 × 360, for previews and offers, and a compact size, 100 × 140, showing illustration, frame, rarity and name, for Binder slots. All text comes from the card definition or the string table.
- Assets, in `Art/Shared/cards/`:
  - `spr_ui_card-frame_attack_01.png`, `spr_ui_card-frame_defense_01.png`, `spr_ui_card-frame_ability_01.png`, 512 × 720, with a transparent illustration window and plain areas for name, value, cooldown and rules text
  - `spr_ui_card-rarity_common_01.png`, `_uncommon_01`, `_rare_01`, `_legendary_01`, 512 × 720 overlays, border and gem only, transparent elsewhere
- Needs: P1.3, P3.1, P4.3
- Test (integration): `Client.Catalogue › card_frames_complete` — given the shipped catalogue, when frames and rarity treatments are read, then three frames and four treatments exist at 512 × 720.
- Test (integration): `Client.Cards › card_face_shows_anatomy` — given `card-rend` with a test illustration, when a full face renders, then it shows the illustration, the attack frame, the Common treatment, "Rend", 10, 3 beats, the Bleed icon and "Applies 1 Bleed." with no flavour line; and `card-jab` shows its flavour line.

#### P7.2 Starter card illustrations
- PRD: 3.4.7
- Does: every card in `data/sets/starter.json` has an illustration in the catalogue.
- Assets, in `Art/Shared/cards/`:
  - twenty `spr_card_<name>_static_01.png` at 512 × 720 for jab, cleave, rend, cross, hook, fang, guard, brace, ember-mark, hollow-cut, dull-edge, spark and starter-1 to starter-8
  - keep the subject inside the central 512 × 400 so the compact face still reads
  - the eight Starter N cards carry placeholder names and numbers; rename and redesign them in `data/sets/starter.json` before commissioning their art
- Needs: P7.1
- Test (integration): `Client.Catalogue › starter_card_art_complete` — given the shipped catalogue and `data/sets/starter.json`, when every card id is looked up, then each has a 512 × 720 illustration and `Missing` stays empty.

#### P7.3 Binder previews card faces
- PRD: 3.5.5
- Does: the Binder screen shows its 16 slots as compact faces, replaces the text list of owned cards with a scrolling column of compact faces, and shows the highlighted card as a full face preview. Keys, Confirm and Back behave as today.
- Assets: none beyond P7.1 and P7.2.
- Needs: P7.1
- Test (integration): `Client.Loadout › binder_previews_card_faces` — given the starter Binder, when the Binder opens, then 16 compact faces fill the slots, every owned card appears as a compact face, and moving the selection shows that card as the full face preview.

#### P7.4 Reward offers as card faces
- PRD: 3.7.2
- Does: the reward panel offers its three cards as three full faces side by side, chosen with Left, Right and Enter or with keys 1 to 3; Skip stays on Esc.
- Assets: none beyond P7.1 and P7.2.
- Needs: P7.1
- Test (integration): `Client.Reward › three_offers_as_card_faces` — given a Normal win offering three cards, when the reward panel renders, then three full faces show those cards, and choosing the second puts it in the Binder.

### Phase 8 — The enemy card before the fight

*Before a fight, and while the loadout is edited, the player sees who they face: portrait, name, BPM, powers, quote and a New or role badge. Done when every P8 test is green and the suite passes.*

**Operator supplies:** five enemy portraits, a badge plate, four power icons and three role icons. Thirteen sprites.

#### P8.1 Fought enemies on the profile
- PRD: — (groundwork for P8.2)
- Does: the profile gains `enemiesFought`, the ids of enemies it has finished a battle against, won or lost, written when the battle ends (3.1.8). The profile schema moves from 2 to 3 with a migration that adds an empty list. PRD 4.1 does not list this field; it exists only for the New badge of 3.6.26.
- Assets: none.
- Needs: —
- Test (integration): `Client.Profile › fought_enemies_recorded_and_migrated` — given a schema-2 profile file, when loaded, then its version is 3 and `enemiesFought` is empty; when a battle against Ren ends and the profile is reloaded, then it holds `enemy-ren` once.

#### P8.2 Enemy card
- PRD: 3.6.26
- Does: the pre-battle panel becomes the enemy card: portrait, name, BPM from the track's starting tempo, each ability and trait as icon and name, the quote line, a badge, and the Edit (Binder) and Fight buttons. The badge reads New while the enemy is absent from `enemiesFought`, otherwise the role icon and role name. Assumption: "New or Type" means New for a never-fought enemy and its role after; the PRD defines it no further. Ability, trait and role names come from the string table. A portrait id such as `portrait-enemy-ren` resolves to `spr_portrait_ren_static_01.png`.
- Assets: `spr_ui_badge_new_01.png`, 9-slice, 160 × 48, 16 px borders, no lettering.
- Needs: P8.1, P1.3
- Test (integration): `Client.EnemyCard › shows_every_field` — given Ren on a profile that has never fought Ren, when the pre-battle panel opens, then it shows Ren's portrait, "Ren", 120 BPM, Iron Veil and Guard each with its icon, the quote, the New badge, Edit and Fight; after one battle against Ren, the badge shows the Tank icon and name instead.

#### P8.3 Enemy portraits
- PRD: 3.6.26
- Does: every enemy's portrait id resolves to a portrait in the catalogue.
- Assets: `spr_portrait_ren_static_01.png`, `spr_portrait_kess_static_01.png`, `spr_portrait_vey_static_01.png`, `spr_portrait_orm_static_01.png`, `spr_portrait_malk_static_01.png`, 512 × 512, head and shoulders, facing left, transparent background.
- Needs: P8.2
- Test (integration): `Client.Catalogue › enemy_portraits_complete` — given the shipped catalogue and `data/enemies/fixtures.json`, when every enemy's portrait id is looked up, then each resolves to a 512 × 512 sprite.

#### P8.4 Power and role icons
- PRD: 3.6.26
- Does: every ability and trait an enemy carries, and every role, has an icon for the enemy card.
- Assets: `spr_ability_rising-tempo_static_01.png`, `spr_ability_charge-buff_static_01.png`, `spr_trait_guard_static_01.png`, `spr_trait_stoneform_static_01.png`, `spr_role_aggressor_static_01.png`, `spr_role_tank_static_01.png`, `spr_role_mentalist_static_01.png`, 64 × 64 each. Iron Veil's icon comes from P2.4.
- Needs: P8.2, P2.4
- Test (integration): `Client.Catalogue › power_and_role_icons_complete` — given the shipped catalogue, when every ability and trait carried by an enemy in `data/enemies/fixtures.json` and the three roles are looked up, then each has a 64 × 64 icon.

#### P8.5 Enemy card while editing
- PRD: 3.5.8
- Does: opening the Binder from the pre-battle panel shows the same enemy card, compact, beside the slots: portrait, name, BPM, powers and badge.
- Assets: none beyond P8.3 and P8.4.
- Needs: P8.2, P7.3
- Test (integration): `Client.Loadout › enemy_card_visible_while_editing` — given a battle node with Kess, when Edit opens the Binder, then Kess's enemy card is visible with its portrait and Rising Tempo.

### Phase 9 — The map is a place

*The map is drawn: node icons, marker, paths and ground, a full header with the continent name, the equipped Charms and Imprints, and a Binder to review without editing. Done when every P9 test is green and the suite passes.*

**Operator supplies:** seven node icons, a player marker, two path sprites, a map backdrop, two Charm icons, six Imprint icons, and three continent names in the string table. Nineteen sprites and three strings.

#### P9.1 Trait definition
- PRD: 4.11
- Does: a `TraitDefinition` record in the simulation holds id, name, one effect through the effect framework, and a source of Pool or Relationship (3.10.6); a loader reads `data/traits/*.json` and validation rejects a Trait without an effect or with an unknown source. Two fixture Traits ship in `data/traits/fixtures.json`, and `CardInstance.TraitId` resolves against them. Applying Traits at the Forge (3.4.19) stays out of scope; enemy traits (3.6.19 to 3.6.25) keep their current enum.
- Assets: none.
- Needs: —
- Test (unit): `Sim.Traits › fixtures_load_and_validate` — given `data/traits/fixtures.json`, when loaded, then two Traits load, each effect registers with the framework, and a Trait with no effect fails validation.

#### P9.2 Read-only Binder from the map
- PRD: 3.5.11
- Does: a Binder button on the map opens the Binder in review mode: slots and owned cards as faces, each card's Trait name when it has one, and each Unstable card's remaining battles (3.4.16). Placing and clearing are refused, Confirm is absent, and Back returns to the map at the same node.
- Assets: none beyond Phase 7.
- Needs: P9.1, P7.3
- Test (integration): `Client.Loadout › map_binder_is_read_only` — given a run on the map holding an Unstable card with 2 battles left and a card restored with a fixture Trait, when the Binder button is pressed, then both show as faces with "2 battles" and the Trait's name, a placement is refused, and Back returns to the same node.

#### P9.3 Nodes, marker, paths and ground
- PRD: 3.2.16
- Does: each map node shows its type icon with its label below; the player marker and every connection use their sprites, walked connections the walked variant; and a backdrop fills the screen behind the board, the same in every World.
- Assets, in `Art/Shared/map/`:
  - `spr_node_normal-battle_static_01.png`, `spr_node_elite_static_01.png`, `spr_node_boss_static_01.png`, `spr_node_shop_static_01.png`, `spr_node_event_static_01.png`, `spr_node_blacksmith_static_01.png`, `spr_node_forge_static_01.png`, 64 × 64, told apart by shape
  - `spr_ui_map-marker_static_01.png`, 64 × 64
  - `spr_ui_map-path_static_01.png` and `spr_ui_map-path_walked_01.png`, 64 × 8, tileable left to right
  - `spr_bg_map_static_01.png`, 2560 × 1080, on the frame rules of the arena in P5.3
- Needs: P1.3
- Test (integration): `Client.Catalogue › node_icons_complete` — given the shipped catalogue, when the seven node types are looked up, then each has a 64 × 64 icon and no two are the same image.
- Test (integration): `Client.Map › map_drawn_from_catalogue` — given a generated World 1 map, when rendered, then every node carries its type's icon, the marker and every connection carry catalogue sprites, the backdrop covers the screen, and `Missing` is empty.

#### P9.4 Map header
- PRD: 3.2.16
- Does: the header shows the current World's continent name from the string table, ARD, CRP with its badge, Base DMG, Essence, the seed, a Binder button and Settings. Assumption: continent names are the strings `world.1.name` to `world.3.name`, written by the operator; the PRD names no continents.
- Assets: three continent names in `data/strings/en.json` under `world.1.name`, `world.2.name` and `world.3.name`.
- Needs: P9.3, P9.2, P4.5
- Test (integration): `Client.Map › header_shows_every_stat` — given World 1 with ARD 250 of 300, CRP 7, Base DMG 2, Essence 40 and seed "chiki-1", when the map renders, then the header shows the World 1 continent name and each value, and the Binder and Settings buttons are present.

#### P9.5 Charms and Imprints on the map
- PRD: 3.2.16
- Does: a row of equipped Charm icons and held Imprint icons sits under the header, each with its name and effect in a tooltip.
- Assets: `spr_charm_clean-victory_static_01.png`, `spr_charm_momentum-plate_static_01.png`, and `spr_imprint_<name>_static_01.png` for keen-edge, thick-hide, bramble-skin, quick-guard, war-drum and stone-heart, 64 × 64 each.
- Needs: P9.3
- Test (integration): `Client.Catalogue › charm_and_imprint_icons_complete` — given the shipped catalogue, when every Charm in `data/charms/fixtures.json` and every Imprint in `data/imprints/fixtures.json` is looked up, then each has a 64 × 64 icon.
- Test (integration): `Client.Map › charms_and_imprints_shown` — given Clean Victory equipped and Keen Edge and Thick Hide held, when the map renders, then one Charm icon and two Imprint icons show from the catalogue, each with its name in its tooltip.

### Phase 10 — Menus, screens and type

*Every screen wears the game's skin and font, the title has a logo, the run-end screen has banners and icons, and calibration uses a drawn marker and recorded clicks. Done when every P10 test is green and the suite passes.*

**Operator supplies:** a licensed font family, four button states, a panel, a backdrop, a toggle box and check, the logo, a title background, three outcome plates, a calibration marker and two click samples. Fourteen sprites, one font family and two WAVs.

#### P10.1 UI skin and font
- PRD: 3.14.1
- Does: `ScreenFactory` and `HudFactory` build every button, panel, toggle and backdrop from the skin sprites and every text in the shipped font. Buttons use Sprite Swap with normal, highlighted, pressed and disabled sprites. Plain colour fills such as scrims use the built-in white sprite.
- Assets:
  - a font family in `client/Assets/_Project/UI/Fonts/`, regular and bold, TTF or OTF, covering Latin-1 and Latin Extended-A (the ö in Björn, the en dash), licensed for embedding in a commercial game
  - `spr_ui_button_normal_01.png`, `_highlighted_01`, `_pressed_01`, `_disabled_01`, 9-slice, 420 × 96, 24 px borders
  - `spr_ui_panel_static_01.png`, 9-slice, 512 × 512, 32 px borders
  - `spr_ui_backdrop_static_01.png`, 2560 × 1080
  - `spr_ui_toggle_box_01.png`, 44 × 44, and `spr_ui_toggle_check_01.png`, 26 × 26
- Needs: P7.4, P8.5, P9.4, P9.5
- Test (integration): `Client.Screens › every_screen_uses_skin_and_font` — given the shipped catalogue, when the pre-run, map, Binder, pre-battle, reward, stop, run-end, settings and calibration screens open in turn, then every text uses the shipped font, every button has all four state sprites, and `Missing` is empty.

#### P10.2 Title logo
- PRD: 3.14.1
- Does: the pre-run screen shows the logo over the title background in place of the title text.
- Assets: `spr_logo_chiki_static_01.png`, about 1200 × 400, transparent; `spr_bg_title_static_01.png`, 2560 × 1080.
- Needs: P10.1
- Test (integration): `Client.Screens › prerun_shows_logo` — given the shipped catalogue, when the pre-run screen opens, then the logo and title background show from the catalogue and Start Run and Settings still work.

#### P10.3 Run-end banners and unlock icons
- PRD: 3.9.11
- Does: the run-end screen shows the outcome word on its plate, Won, Died or Abandoned, and each unlock as its icon or compact card face with its name, never its raw id.
- Assets: `spr_ui_outcome_won_01.png`, `spr_ui_outcome_died_01.png`, `spr_ui_outcome_abandoned_01.png`, 1200 × 240 plates with no lettering.
- Needs: P10.1, P9.5, P7.1
- Test (integration): `Client.RunEnd › banner_and_unlock_icons` — given a run that ends by death after unlocking Clean Victory, when the screen renders, then the Died plate shows with "Died", and Clean Victory appears with its icon and name and not as `charm-clean-victory`.

#### P10.4 Calibration marker and clicks
- PRD: 3.12.1
- Does: the calibration beat marker uses its sprite and still pulses from audio time. The calibration click track is built from the recorded click samples at the beat map's times, accenting every fourth beat, and the metronome plays the same click through P1.4.
- Assets: `spr_ui_calibration-marker_static_01.png`, 140 × 140; `sfx_click_beat.wav` and `sfx_click_accent.wav`, under 20 ms, attack inside the first millisecond.
- Needs: P10.1, P1.4
- Test (integration): `Client.Calibration › marker_and_clicks_recorded` — given the shipped catalogues, when calibration runs for 8 beats, then the marker carries the calibration sprite, and 8 clicks sit at the beat map's times using the recorded samples, the accent on the first and fifth.

### Phase 11 — Recorded music, and the frame rate holds

*Every enemy fights to its own recording, the recordings loop without a seam and are never interrupted, and the fully dressed game still holds 60 fps. Done when every P11 test is green and the suite passes.*

**Operator supplies:** five recorded tracks, five rewritten sidecars and five charts re-authored for the recordings.

#### P11.1 Recorded tracks for the cast
- PRD: 3.6.28
- Does: each enemy in `data/enemies/fixtures.json` fights to its own recorded track. Before any shipped chart or sidecar changes, the simulation tests that load fixture enemies, charts and tracks through `TestContent` switch to frozen copies under `sim/Chiki.Sim.Tests/fixtures/`, so recorded music never changes a rules test. Track and chart ids stay as they are, and chart validation (3.6.31) keeps rejecting an action outside the new length.
- Assets, for each of Ren, Kess, Vey, Orm and Malk:
  - `client/Assets/_Project/Audio/mus_w1_<name>.ogg`, OGG Vorbis, 48 kHz, stereo, cut to loop from its first sample
  - its sidecar `data/tracks/fixture-<name>.json` rewritten for the recording: offset in ms to the first beat, length in beats, starting BPM and every BPM change at its beat
  - its chart `data/charts/chart-enemy-<name>.json` re-authored for the recording, every action on a beat or quarter beat inside the new length; chart density sets enemy HP through 3.7.15, so a busier chart makes a longer fight
- Needs: P1.6
- Test (unit): `Sim.Fixtures › rules_tests_use_frozen_fixtures` — given the frozen copies, when `TestContent` loads the fixture enemies, then it reads them from `sim/Chiki.Sim.Tests/fixtures/` and not from `data/`.
- Test (integration): `Client.Catalogue › cast_tracks_complete` — given the shipped audio catalogue, when every enemy's track id is looked up, then each has a recorded clip whose length matches its sidecar's offset plus its length in beats at its tempo map, within 10 ms.

#### P11.2 Recorded tracks loop seamlessly
- PRD: 3.6.32
- Does: every recording's length in samples equals its sidecar's offset plus its beat-map length, so the chart and the music wrap together at `TrackLooped` with no gap and no overlap.
- Assets: none beyond P11.1.
- Needs: P11.1
- Test (measurement): `Client.Audio › recorded_tracks_loop_seamlessly` — given each shipped track, when its sample count is compared with its beat map, then they agree within one sample; and when Ren's battle plays past the loop point, then the first action of the second lap is judged against its second-lap time within 2 ms.

#### P11.3 Recorded music never interrupted
- PRD: 3.3.1.6
- Does: a battle on a recorded track keeps the audio source advancing without a pause, seek or pitch change through a Stun, a Signature and a loop.
- Assets: none beyond P11.1.
- Needs: P11.1
- Test (measurement): `Client.Clock › recorded_track_continuous_through_stun_signature_and_loop` — given Ren's recorded track with a player Stun, a Signature and a loop inside 40 seconds, when the battle runs, then the source's sample position advances every frame by the audio time elapsed, within one buffer, and its pitch stays 1.

#### P11.4 Frame rate with the shipped art
- PRD: 6.2
- Does: repeats the placeholder build's frame-rate measurement on the heaviest shipped fight, Malk's, with every shipped sprite, clip and track loaded.
- Assets: none beyond the earlier phases.
- Needs: P6.4, P5.3, P5.4, P10.1, P11.1
- Test (measurement): `Client.Perf › malk_battle_60fps_with_shipped_art` — given Malk's battle at 1920 × 1080 with the shipped catalogues, when 30 seconds run, then the 1% low frame time is under 16.7ms and zero audio underruns are recorded.
