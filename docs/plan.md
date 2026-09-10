# Chiki — Implementation plan

| PRD       | docs/project/prd.md |
| :-------- | :------------------ |
| Requested | Stage 1–4 foundation: 3.2.3; 3.3.1.3, 3.3.1.4, 3.3.1.7, 3.3.1.8, 3.3.1.9; 3.3.3.1–3.3.3.3; 3.3.4.1–3.3.4.8; 3.3.5.1, 3.3.5.3; 3.3.6.1–3.3.6.3; 3.3.7.1–3.3.7.8; 3.3.9.2–3.3.9.5; 4.8; 3.4.1, 3.4.4, 3.4.7, 3.4.8, 3.4.9, 3.4.10, 3.4.13–3.4.16, 3.4.21; 4.4, 4.5; 3.6.1–3.6.4, 3.6.28, 3.6.29, 3.6.31, 3.6.32, 4.7, 4.16; 3.7.15–3.7.17; 3.6.5, 3.6.6, 3.6.8, 3.6.9, 3.6.16, 3.6.20, 3.6.25; 3.9.8; 3.3.1.1, 3.3.1.5, 3.3.1.6, 6.1, 6.2; 3.3.2.1, 3.3.2.2, 3.3.2.3, 3.3.2.5, 3.3.2.6; 3.12.1, 3.3.8.2; 3.3.5.4, 3.3.8.1; 3.2.1, 3.2.2, 3.9.1, 3.9.11; 3.2.5–3.2.10; 3.8.1, 3.8.2, 3.8.6; 3.5.1–3.5.6; 3.7.2, 3.7.5; 3.1.5, 3.1.6, 3.1.8, 4.2, 3.1.2; 3.15.1, 3.15.2 |
| Scope     | 126 requirements in 23 phases, 149 items |
| Tests run | `dotnet test sim/Chiki.sln` for the simulation; `Unity -batchmode -nographics -projectPath client -runTests -testPlatform PlayMode -testResults results.xml` for the client |

**Completion rule.** An item is done when every test it lists is green in the suite. A requirement is done when every item that cites it is done. A phase is done when every item in it is done and the whole suite is green. Nothing is marked done on any other evidence.

Technology, settled by the operator for this plan: Unity (C#) for the client, with the rules simulation in a plain .NET class library `sim/Chiki.Sim` tested by NUnit in `sim/Chiki.Sim.Tests`, and client tests in the Unity Test Framework under `client/Assets/Tests`. The simulation never references `UnityEngine`; the client references the simulation.

## Scope

### Requested

| PRD # | Requirement | Phase |
| :-- | :-- | :-- |
| 3.2.3 | Run-wide stats: ARD baseline 300, Base DMG +1 = +1 attack damage, Essence, CRP; ARD carries between battles | 1, 3 |
| 3.3.1.1 | Battle synchronised to the track's BPM; Rhythm Line visible throughout | 12, 14 |
| 3.3.1.3 | Exactly one card or nothing per enemy action; nothing between actions | 2 |
| 3.3.1.4 | All combat durations in beats | 2 |
| 3.3.1.5 | Timing windows scale with BPM | 2 |
| 3.3.1.6 | Music never desynced, stretched, paused or interrupted by a mechanic | 12 |
| 3.3.1.7 | Every encounter is one player against one enemy | 2 |
| 3.3.1.8 | Every action opportunity is a charted enemy action, on beats or quarter beats | 2 |
| 3.3.1.9 | Tracks carry a tempo map the beat clock follows | 2 |
| 3.3.2.1 | Fixed key layout A/S, D/F, J/K, L/; across two lines | 13 |
| 3.3.2.2 | Line-switch key changes the active line; both lines visible | 13 |
| 3.3.2.3 | Space plus slot key sends to the Signature Chain; Space does nothing else | 13 |
| 3.3.2.5 | Keys bound by physical position; labels show the real character | 13 |
| 3.3.2.6 | Line switch on Left Shift: key-down, any beat, ungraded, no cooldown | 13 |
| 3.3.3.1 | Perfect / Good / Miss grading of pressed inputs | 2 |
| 3.3.3.2 | No input: no card, no cooldown, no judgment; enemy resolves at full value | 4 |
| 3.3.3.3 | Stun when an enemy action arrives is treated as no input | 5 |
| 3.3.4.1 | Timing governs output and incoming; category governs only what the card does | 3 |
| 3.3.4.2 | Incoming damage formula with IncomingMult 0% / 50% / 100% / 100% and Block first | 3 |
| 3.3.4.3 | Player effect formula with JudgmentMult 100% / 50% / 0% and Base DMG | 3 |
| 3.3.4.4 | Efficacy matrix | 3 |
| 3.3.4.5 | Displayed values rounded to the nearest whole number | 3 |
| 3.3.4.6 | Timing mitigates damage only; statuses land regardless | 6 |
| 3.3.4.7 | True DMG ignores Block and reductions | 6 |
| 3.3.4.8 | The nine edge cases, each a unit test | 6 |
| 3.3.5.1 | Any press starts its slot cooldown; 16 independent slots; 2–6 beats | 4 |
| 3.3.5.3 | A slot on cooldown cannot be played; disabled feedback; not a Miss | 4 |
| 3.3.5.4 | Cooldown overlay and countdown; both lines visible, inactive thinner | 14 |
| 3.3.6.1 | Banking into three Signature slots; incoming per timing | 4 |
| 3.3.6.2 | Three banked triggers the Signature: 30 damage, chain empties | 4 |
| 3.3.6.3 | A Missed send still banks and starts the cooldown | 4 |
| 3.3.7.1 | Status rules: beats, resolve after actions, additive stacks, refresh resets, icons with tooltips | 5, 14 |
| 3.3.7.2 | Same-beat status priority: Stun, multipliers, Reflect/Thorns, DoT | 5 |
| 3.3.7.3 | Scar | 5 |
| 3.3.7.4 | Weak | 5 |
| 3.3.7.5 | Stun | 5 |
| 3.3.7.6 | Bleed | 5 |
| 3.3.7.7 | Thorns | 5 |
| 3.3.8.1 | Feedback: judgment audio cue, key flash, card glow, Rhythm Line highlights, shake | 14 |
| 3.3.8.2 | Judgment applies the calibration offset; metronome toggle | 15 |
| 3.3.9.2 | Battle won at enemy HP 0; node reward flow runs | 3, 20 |
| 3.3.9.3 | Player dies at ARD 0; run ends | 3, 18 |
| 3.3.9.4 | Perfect Defense recorded per battle | 4 |
| 3.3.9.5 | Block and statuses cleared at battle end | 6 |
| 4.8 | Battle entity: state, judgment log, outcome | 2 |
| 3.4.1 | One Category per card; legal slots | 7 |
| 3.4.4 | Four Rarities with damage and Block bands | 7 |
| 3.4.7 | Card anatomy including cooldown and optional flavor text | 7 |
| 3.4.8 | Effect ↔ category ↔ minimum rarity compatibility | 7 |
| 3.4.9 | Four mechanic kinds: direct, scaling, status, reaction | 8 |
| 3.4.10 | Cards define CardValue and modifiers only, never the formula | 7 |
| 3.4.13 | Exactly one class per card | 7 |
| 3.4.14 | Normal class: standard pool | 20 |
| 3.4.15 | Event class: never in shops or battle rewards | 20 |
| 3.4.16 | Unstable class: lifespan in battles, then destroyed | 16 |
| 3.4.21 | Every card in the table complies or fails validation | 7 |
| 4.4 | Card definition entity | 7 |
| 4.5 | Card instance entity | 8 |
| 3.6.1 | One role per enemy with HP multipliers 0.8× / 1.2× / 1.0× | 9 |
| 3.6.2 | One rhythm profile per enemy: Fast or Slow | 9 |
| 3.6.3 | Upcoming actions shown: what, which beat, beats remaining | 9, 14 |
| 3.6.4 | Tier capability budget enforced | 9 |
| 3.6.28 | Enemy bound to its own track and a chart written for it | 9 |
| 3.6.29 | Enemy HP from formula × role; damage within band | 10 |
| 3.6.31 | Chart: finite ordered actions at quarter-beat positions, validated | 9 |
| 3.6.32 | Chart and track loop together from the start | 9 |
| 4.7 | Enemy definition entity referencing its chart | 9 |
| 4.16 | Chart entity | 2 |
| 3.7.15 | Enemy HP formula from intended duration, with worked check | 9 |
| 3.7.16 | Enemy damage bands per role and tier, +15% per World | 10 |
| 3.7.17 | Mistake budget 10–20 / 6–10 / 4–7 | 10 |
| 3.6.5 | A01 Rising Tempo | 11 |
| 3.6.6 | A02 Misstep Pain | 11 |
| 3.6.8 | A04 Pressure | 11 |
| 3.6.9 | A05 Iron Veil | 11 |
| 3.6.16 | A13 Charge / Buff | 11 |
| 3.6.20 | T02 Stoneform | 11 |
| 3.6.25 | T07 Guard | 11 |
| 3.9.8 | Charm triggers on battle facts; "until reset" lasts to the next run | 18 |
| 6.1 | Timing accuracy: graded against the audio clock, within 2 ms | 12 |
| 6.2 | 60 fps at 1080p; audio never drops with a frame | 14 |
| 3.12.1 | Calibration screen: offset test, stored on the profile, offered first, reachable always | 15 |
| 3.2.1 | Three Worlds in order, each ending at its Boss; World 3 Boss wins | 19 |
| 3.2.2 | Run start state | 17 |
| 3.9.1 | Run progression resets on run end | 18 |
| 3.9.11 | Run-end screen; profile updated; return to pre-run | 23 |
| 3.2.5 | Graph shape rules | 19 |
| 3.2.6 | 55–70 nodes, ~15 per traversal, type distribution | 19 |
| 3.2.7 | Forward-only movement; a transition adds CRP and saves | 19 |
| 3.2.8 | Normal battle node | 20 |
| 3.2.9 | Elite node | 21 |
| 3.2.10 | Boss node | 21 |
| 3.8.1 | CRP 0–100, clamped, visible on map and in battle | 17, 23 |
| 3.8.2 | +1 CRP per node transition | 19 |
| 3.8.6 | Every CRP change shows source and amount | 19 |
| 3.5.1 | Loadout of exactly 16 visible cards, no draw or hand | 16 |
| 3.5.2 | 2/2/2/2 per line; category-legal slots; one slot per card | 16 |
| 3.5.3 | Starter Binder fills both lines | 16 |
| 3.5.4 | Binder persists for the run and is discarded at its end | 18 |
| 3.5.5 | Rebuild the loadout before a battle; no empty slot enters | 16, 23 |
| 3.5.6 | Keep previous loadout by default; one input to enter | 23 |
| 3.7.2 | Normal reward: 1 of 3 cards plus Essence | 20 |
| 3.7.5 | Essence income bands | 20 |
| 3.1.5 | One run in progress; autosave at transitions and on return to map; resume at node | 22 |
| 3.1.6 | A battle is never saved partway | 22 |
| 3.1.8 | Meta unlocks and RP written the moment they are earned | 22 |
| 4.2 | Run entity | 17, 22 |
| 3.1.2 | A profile owns its progression, settings, calibration, run and logs | 15 |
| 3.15.1 | Run log file per run | 22 |
| 3.15.2 | Repeated-enemy flag in the log | 22 |

### Blockers pulled in

| PRD # | Requirement | Blocks | Phase |
| :-- | :-- | :-- | :-- |
| — | .NET simulation library and NUnit test project, `dotnet test` green | all | 1 |
| — | Integer-only rules arithmetic and an explicitly passed seeded PRNG | all | 1 |
| 6.7 | Simulation separable from rendering; every rule a unit test | all | 1 |
| 4.14 | Music track entity: tempo map and derived beat map | 3.3.1.1, 3.6.28, 3.12.1 | 2 |
| — | Trigger, condition and modifier core shared by cards, enemy powers and Charms | 3.4.9, 3.6.5, 3.6.6, 3.6.8, 3.6.9, 3.6.16, 3.6.20, 3.6.25, 3.9.8 | 8 |
| 3.8.8 | Cards may scale with CRP, stated on the card | 3.4.9 | 8 |
| — | Starter fixture content: 20 valid card definitions and one track | 3.5.3, 3.7.2 | 8 |
| 3.3.9.1 | Encounter tiers with intended durations | 3.2.8, 3.2.9, 3.2.10, 3.6.4, 3.7.5, 3.7.16, 3.7.17 | 9 |
| — | Fixture enemies: one per role, one per tier | 3.6.29, 3.7.17, 3.2.8 | 10 |
| — | Unity project referencing the simulation, Unity Test Framework running from the CLI | 3.3.1.1, 3.3.2.1, 3.3.5.4, 3.3.8.1, 3.12.1, 6.1, 6.2 | 12 |
| — | Battle harness bridging the audio clock and input events into the simulation | 3.3.1.1, 3.3.2.1, 3.3.8.2, 6.1 | 12 |
| 4.10 | Imprint entity with fixture Imprints | 3.9.3, 3.7.3, 3.7.4 | 16 |
| 4.9 | Charm entity with fixture Charms | 3.2.2, 3.9.8, 3.1.8, 3.9.10 | 16 |
| 3.2.4 | Run seed: custom seed accepted; same seed, same run | 3.2.5, 3.2.6, 3.9.11, 3.15.1 | 17, 19 |
| 3.9.2 | Meta progression persists on the profile | 3.2.2, 3.1.8, 3.9.8 | 17 |
| 3.9.6 | Equip 0–2 Charms pre-run | 3.2.2, 3.9.8 | 17 |
| 3.9.3 | Imprints acquired in a run, rolled by tier, unlimited, lost at run end | 3.2.9, 3.2.10 | 18 |
| 4.12 | NPC relationship entity | 3.1.8, 3.1.2 | 18 |
| 3.10.3 | RP per NPC persists; levels 1–10 main, 1–5 secondary | 3.1.8 | 18 |
| 3.10.4 | RP is gained and recorded | 3.1.8 | 18 |
| 3.7.3 | Elite reward | 3.2.9 | 21 |
| 3.7.4 | Boss reward | 3.2.10 | 21 |
| 3.9.10 | Boss defeat counts toward Charm unlocks | 3.2.10 | 21 |
| — | Minimal map view: nodes, connections, move, enter battle | 3.5.6, 3.9.11, 3.8.1 | 23 |

### Left out

| PRD # | Requirement | Why |
| :-- | :-- | :-- |
| 3.3.7.8 | Disarmed disables Trait effects | Needs Traits (3.4.19, 4.11), which the operator chose not to pull in; deferred to the Forge plan |

### Already in place

| PRD # | Evidence | Tested |
| :-- | :-- | :-- |
| — | Nothing is built. The repository holds design documents and data tables only; `data/cards.csv`, `data/enemies.csv` and `data/imprints.csv` are mostly TBD and are replaced by test fixtures in this plan | no |

## Order

Everything depends on the simulation library existing and being testable, so Phase 1 is groundwork plus 6.7. Combat rules form one chain: the track, its tempo map and the chart that supplies every enemy action (4.14, 3.3.1.9, 4.16, 3.3.1.8), then judgment (3.3.3.1, 4.8), before resolution (3.3.4.2, 3.3.4.3, 3.3.4.4) before cooldowns and the Signature Chain (3.3.5.1, 3.3.6.1), which in turn are needed to state what "no input" leaves untouched (3.3.3.2). Statuses (3.3.7.2) come after resolution because their priority order is defined against it, and the edge-case table (3.3.4.8) closes the chain because its rows exercise every earlier rule at once. True DMG (3.3.4.7) was moved next to the edge cases to keep Phase 3 to eight items.

Cards and enemies become data only after the rules they are validated against exist: the card schema (4.4, 3.4.4, 3.4.8) before the effect framework (3.4.9), the enemy schema and tiers (4.7, 3.3.9.1) before the HP formula (3.7.15) and the damage bands (3.7.16), and the seven exercisers (3.6.5 onward) last in the headless half because they need everything above. The client half starts with the audio clock (3.3.1.1, 6.1) because the input layer (3.3.2.1) and every presenter (3.3.5.4, 3.3.8.1) stamp against it; calibration (3.12.1) needs a profile to store its offset, so the profile record (3.1.2) sits in the same phase. The run half orders create before read before end: Binder and loadout (3.5.1) before the run entity (4.2, 3.2.2), before run end (3.9.1, 3.5.4), before the map (3.2.5), before battle nodes and rewards (3.2.8, 3.7.2), before Elite and Boss (3.2.9, 3.2.10), before save and resume (3.1.5), and the on-screen loop (3.5.6, 3.9.11) last because it only presents what the phases before it produce.

Ties broken: 3.2.3 is delivered twice, the stat holder in Phase 1 and the Base DMG rule in Phase 3, because the second half is a formula fact. 3.3.9.3 is delivered as a battle outcome in Phase 3 and as a run outcome in Phase 18. 3.1.8 sits with save (Phase 22) rather than with run end because its test is about durability across a crash. Non-battle node types (Shop, Event, Blacksmith, Forge) are generated by 3.2.6 with their correct distribution but resolve as empty stops until their features are planned.

## Phases

### ✅ Phase 1 — The simulation exists and is testable

*Delivers a `dotnet test` run that proves the rules library boots, uses integer arithmetic and a seeded PRNG, references no engine, and holds the four run stats. Done when every P1 test is green and the suite passes.*

#### ✅ P1.1 Simulation library and test project

- PRD: — (groundwork for every later item)
- Does: Creates `sim/Chiki.sln` with `Chiki.Sim` (netstandard2.1 class library, so Unity can reference it) and `Chiki.Sim.Tests` (NUnit, net8.0). Adds a smoke test and a CI script that runs `dotnet test sim/Chiki.sln`.
- Needs: —
- Test (unit): `Sim.Smoke › library_loads` — given the test project, when it references `Chiki.Sim`, then a type from the library instantiates and the test passes.

#### ✅ P1.2 Integer rules arithmetic and seeded PRNG

- PRD: — (groundwork for P1.4, P2.1, P3.1, P19.1)
- Does: All rule math uses integers or fixed-point (thousandths) with a single rounding helper; a `Rng` type wraps a seeded xorshift-class generator, is passed explicitly, and exposes `NextInt(min, max)` and `Fork(label)` so subsystems get independent, reproducible streams.
- Needs: P1.1
- Test (unit): `Sim.Rng › same_seed_same_sequence` — given two `Rng` instances with seed 42, when 1000 values are drawn from each, then the sequences are identical.
- Test (unit): `Sim.Rng › fork_is_independent` — given one `Rng`, when `Fork("map")` and `Fork("shop")` are drawn from, then neither draw changes the other's next value.

#### ✅ P1.3 No rendering dependency

- PRD: 6.7
- Does: `Chiki.Sim` references only the base class library; a test inspects its referenced assemblies. Every rule item in this plan adds its test to this project, which is what the rest of 6.7 means.
- Needs: P1.1
- Test (unit): `Sim.Architecture › sim_references_no_engine` — given the compiled `Chiki.Sim` assembly, when its referenced assembly names are listed, then none starts with `UnityEngine` or `UnityEditor`.

#### ✅ P1.4 Run stats holder

- PRD: 3.2.3
- Does: A `RunStats` type with ARD current and maximum (maximum defaults to 300, current starts at maximum and is clamped to 0..maximum), Base DMG (starts 0), Essence (starts 0, never below 0) and CRP (starts 0, delegated to P17.6 for clamping). Raising maximum ARD raises current by the same amount; the holder persists across battle instances. Assumption: a battle context is the `Battle` aggregate, which is constructed with a reference to the run's `RunStats` and never copies it.
- Needs: P1.1
- Test (unit): `Sim.RunStats › ard_defaults_to_300` — given a new `RunStats`, when read, then max ARD is 300 and current ARD is 300.
- Test (unit): `Sim.RunStats › ard_carries_between_battles` — given ARD set to 120, when two successive battle contexts are created from the same stats, then the second sees 120.
- Test (unit): `Sim.RunStats › raising_max_ard_adds_to_current` — given ARD 250 of 300, when max ARD is raised by 20, then current is 270 and max 320.

### ✅ Phase 2 — Beats and judgment

*Delivers a headless battle that ticks in beats from a track's tempo map, takes its action opportunities from a chart, grades a pressed input, and records everything in a judgment log. Done when every P2 test is green and the suite passes.*

#### ✅ P2.1 Music track and beat map

- PRD: 4.14
- Does: A `Track` definition with id, World, encounter tier, length in beats, start offset in milliseconds and a tempo map (starting BPM plus BPM changes at beat positions, P2.9). A `BeatMap` derived from it gives the audio time of any position in quarter beats; all times are integer milliseconds.
- Needs: P1.2
- Test (unit): `Sim.Track › beat_times_from_bpm` — given BPM 120 and offset 0, when beat 4 is queried, then its time is 2000 ms.
- Test (unit): `Sim.Track › offset_shifts_all_beats` — given BPM 120 and offset 250, when beat 0 and beat 1 are queried, then they are 250 ms and 750 ms.

#### P2.2 Withdrawn

- PRD: — (withdrawn: the every-second-beat rule left the PRD with 3.3.1.2)
- Does: Withdrawn.
- Needs: —
- Test (unit): withdrawn.

#### ✅ P2.3 One card per enemy action

- PRD: 3.3.1.3
- Does: For each enemy action the first slot press inside its Judgment Window is accepted; any second press for the same action is rejected with an "action already answered" result that consumes nothing; a press when no enemy action's window is open is rejected the same way. Taking no action is legal.
- Needs: P2.11
- Test (unit): `Sim.Beats › second_press_same_action_rejected` — given an enemy action with one accepted press, when another slot is pressed inside the same window, then the result is rejected and no card is consumed.
- Test (unit): `Sim.Beats › press_between_actions_rejected` — given no enemy action within any window, when a slot is pressed, then it is rejected and no judgment is recorded.

#### ✅ P2.4 Durations in beats

- PRD: 3.3.1.4
- Does: Every duration API (status, cooldown, effect) accepts an integer beat count only; the battle advances them by one per beat tick; no API takes seconds.
- Needs: P2.1
- Test (unit): `Sim.Beats › durations_tick_per_beat` — given a timed effect of 3 beats, when 3 beat ticks pass, then it has expired, and after 2 it has not.

#### ✅ P2.5 One enemy per battle

- PRD: 3.3.1.7
- Does: A battle is constructed from exactly one enemy definition and one player; constructing with zero or more than one enemy fails. Assumption: until P16 the player is the run's `RunStats`.
- Needs: P2.1
- Test (unit): `Sim.Battle › exactly_one_enemy` — given battle construction, when called with two enemies, then it throws; with one, it succeeds.

#### ✅ P2.6 Judgment grades

- PRD: 3.3.3.1
- Does: A pressed input carries its audio time; the battle compares it with the charted position of the nearest enemy action (P2.11) and returns Perfect inside the inner window, Good inside the outer window, Miss outside both. Window widths at BPM 120 are Perfect ±40 ms and Good ±90 ms (tuning constants, one place), narrower than a quarter beat so adjacent actions never share a window. A Miss is only ever the grade of a pressed input. Assumption: the Judgment Window in which a press is accepted at all is plus or minus a quarter beat around the action, clipped at the midpoint to a neighbouring action; a press inside it but outside the Good window is the Miss grade, and a press outside it is rejected (P2.3).
- Needs: P2.11
- Test (unit): `Sim.Judgment › perfect_good_miss_by_offset` — given BPM 120, when inputs arrive at +20, +60 and +120 ms from the window centre, then the grades are Perfect, Good and Miss.
- Test (unit): `Sim.Judgment › miss_only_from_press` — given an enemy action with no press, when its window closes, then no judgment of any grade is recorded.

#### ✅ P2.7 Windows scale with BPM

- PRD: 3.3.1.5
- Does: Window widths are stored as a fraction of a beat, so at BPM 240 they are half the milliseconds of BPM 120.
- Needs: P2.6
- Test (unit): `Sim.Judgment › windows_scale_with_bpm` — given BPM 240, when an input arrives +30 ms from centre, then it is Good, whereas at BPM 120 the same offset is Perfect.

#### ✅ P2.8 Battle state and event stream

- PRD: 4.8
- Does: A `Battle` aggregate holding enemy, track, tier, beat clock, player Block, statuses on both sides, Signature Chain contents, a judgment log with one entry per enemy action (grade or no-input, slot, card), damage taken, Perfect Defense flag and outcome. Every state change appends a typed event (BeatStarted, InputJudged, DamageDealt, DamageTaken, StatusApplied, CardBanked, SignatureFired, BattleEnded) to an ordered stream that subscribers read; later items add event types as they need them. Statuses are added to the aggregate by P5.1 and the DamageTaken amount by P3.1.
- Needs: P2.6
- Test (unit): `Sim.Battle › judgment_log_one_entry_per_enemy_action` — given a chart with 5 actions and presses answering the first and third, when the battle ends, then the log has 5 entries, two graded and three no-input.
- Test (unit): `Sim.Battle › events_are_ordered` — given the same battle, when the stream is read, then events are in position order and each InputJudged precedes that action's DamageTaken.

#### ✅ P2.9 Tempo map

- PRD: 3.3.1.9
- Does: The beat map follows the track's tempo map: a BPM change at beat n applies from beat n onward; quarter-beat times are computed piecewise; the judgment windows (P2.6) use the BPM in force at the action's position.
- Needs: P2.1
- Test (unit): `Sim.Track › tempo_change_shifts_later_beats` — given BPM 120 changing to 240 at beat 8, when beat 12 is queried, then its time is 5000 ms (8 beats at 500 ms plus 4 at 250 ms).
- Test (unit): `Sim.Track › quarter_beats_between_beats` — given BPM 120, when position 2.75 beats is queried, then its time is 1375 ms.

#### ✅ P2.10 Chart record

- PRD: 4.16
- Does: A `Chart` record with enemy id, track id and an ordered list of actions, each a kind (AttackLeft, AttackRight, Defend with level, Buff, Charge with wind-up) at a position in quarter beats; length in beats is derived from the track and actions per minute is computed from count and length. Assumption: the loader takes the already-loaded `Track` the chart names; actions per minute is kept exact in thousandths for 3.7.15 and its whole-number form drops the fraction (30 actions in 16 s is 112).
- Needs: P2.1
- Test (unit): `Sim.Chart › loads_and_derives` — given a JSON chart of 30 actions on a 32-beat track at BPM 120, when loaded, then the actions round-trip in order, length is 32 beats and actions per minute is 112 (30 actions in 16 seconds, rounded).

#### ✅ P2.11 Enemy actions are the action opportunities

- PRD: 3.3.1.8
- Does: The battle's action opportunities are exactly the chart's actions: each opens a Judgment Window centred on its charted time; no opportunity exists anywhere else; a chart position on a quarter beat is honoured.
- Needs: P2.10, P2.1
- Test (unit): `Sim.Beats › opportunities_equal_chart_actions` — given a chart with actions at 1, 2.5 and 4.25 beats, when the battle enumerates its opportunities, then there are exactly three, at those times, and none on beats 3 or 5.

### ✅ Phase 3 — Damage resolves

*Delivers the full resolution of one enemy action: incoming damage by timing, player effect by timing and category, and the two ways a battle ends. Done when every P3 test is green and the suite passes.*

#### ✅ P3.1 Incoming damage formula

- PRD: 3.3.4.2
- Does: When the enemy attacks on a beat, `Incoming = EnemyDMG × IncomingMult × StatusMults − Block` with IncomingMult 0% Perfect, 50% Good, 100% Miss, 100% no input; Block absorbs first and is reduced; the remainder reduces ARD; result is never negative.
- Needs: P2.8, P1.4
- Test (unit): `Sim.Resolve › incoming_by_judgment` — given enemy DMG 20 and no Block, when the judgment is Perfect, Good, Miss and no-input, then ARD loss is 0, 10, 20 and 20.
- Test (unit): `Sim.Resolve › block_absorbs_first` — given Block 6 and Good against DMG 20, when resolved, then Block is 0 and ARD loss is 4.

#### ✅ P3.2 Player effect formula

- PRD: 3.3.4.3
- Does: `Effect = CardValue × JudgmentMult × (1 + BaseDMG%) × StatusMults` with JudgmentMult 100% Perfect, 50% Good, 0% Miss; for Defense cards the value is Block gained. The Base DMG term is realised so that the rule in 3.2.3 holds (P3.3). Assumption: until P7.1 the card is a minimal `CardDefinition` (id, name, Category, CardValue), and until the Loadout exists (P16) `Battle.Press` takes the card the slot holds from the caller.
- Needs: P2.8, P1.4
- Test (unit): `Sim.Resolve › effect_by_judgment` — given a 10-damage attack card, when Perfect, Good and Miss, then damage is 10, 5 and 0.
- Test (unit): `Sim.Resolve › defense_block_by_judgment` — given an 8-Block Defense card, when Good, then Block gained is 4.

#### ✅ P3.3 Base DMG adds one per point

- PRD: 3.2.3
- Does: Each +1 Base DMG adds exactly +1 to every attack card's Perfect output before multipliers; Defense and Ability values are unaffected. Assumption: the Good case is 13 × 50% = 6.5, which P3.6 rounds up to 7; the earlier figure of 6 was an arithmetic slip.
- Needs: P3.2
- Test (unit): `Sim.Resolve › base_dmg_adds_flat` — given Base DMG 3 and a 10-damage attack, when Perfect, then damage is 13; when Good, 7 (six and a half rounds up per P3.6).
- Test (unit): `Sim.Resolve › base_dmg_ignores_defense` — given Base DMG 3 and an 8-Block card, when Perfect, then Block gained is 8.

#### ✅ P3.4 Efficacy matrix

- PRD: 3.3.4.4
- Does: Player effect is applied by the matrix: against a Left or Right attack a correct-side attack deals full damage, a wrong-side attack deals 0, Defense gains Block, Ability resolves, Signature send banks; against Defends an attack is reduced by the enemy's defense level; against Buffs or idle any attack deals full damage. Assumption: against a Defend both attack sides are reduced by the same defense level; a Charge wind-up (3.6.16) resolves as the Buff row; an Ability has nothing to resolve until P8.1 attaches effects.
- Needs: P3.2
- Test (unit): `Sim.Resolve › wrong_side_whiffs` — given the enemy attacks Left, when the player plays a Right Attack with Perfect, then damage dealt is 0.
- Test (unit): `Sim.Resolve › no_wrong_side_on_buff` — given the enemy's charted action is a Buff, when the player plays a Right Attack with Perfect, then full damage is dealt.
- Test (unit): `Sim.Resolve › attack_into_defend_reduced` — given the enemy Defends with defense level 50%, when a 10-damage correct attack lands Perfect, then damage dealt is 5.

#### ✅ P3.5 Timing governs incoming, category governs efficacy

- PRD: 3.3.4.1
- Does: The resolution order is fixed: grade, incoming damage, player effect. Incoming damage never reads the card's category. Assumption: because incoming resolves before the effect, Block gained on an action absorbs from the next enemy action on, not the one it answered.
- Needs: P3.1, P3.4
- Test (unit): `Sim.Resolve › incoming_independent_of_category` — given the enemy attacks for 20, when the player plays Defense, wrong-side Attack, Ability and Signature send each with Perfect, then ARD loss is 0 in all four cases; with Good, 10 in all four.

#### ✅ P3.6 Rounding

- PRD: 3.3.4.5
- Does: Damage and Block are computed in thousandths and rounded to the nearest whole number once, at display and application, with halves rounding up.
- Needs: P3.2
- Test (unit): `Sim.Resolve › rounds_to_nearest` — given a 7-damage card at Good, when applied, then damage is 4 (three and a half rounds up); given 9 at Good, 5 (four and a half rounds up).

#### ✅ P3.7 Battle won at HP 0

- PRD: 3.3.9.2
- Does: When enemy HP reaches 0 the battle ends with outcome Won and emits BattleEnded; no further beats are processed. Assumption: until P10.2 derives it, enemy HP is a `Battle` constructor argument.
- Needs: P3.4
- Test (unit): `Sim.Battle › won_at_zero_hp` — given enemy HP 10, when a 10-damage Perfect lands, then the outcome is Won and the next beat tick is a no-op.

#### ✅ P3.8 Player dies at ARD 0

- PRD: 3.3.9.3
- Does: When ARD reaches 0 the battle ends with outcome Died and emits BattleEnded.
- Needs: P3.1
- Test (unit): `Sim.Battle › died_at_zero_ard` — given ARD 15 and an enemy attack of 20 with no input, when resolved, then ARD is 0 and the outcome is Died.

### ✅ Phase 4 — Cooldowns and the Signature Chain

*Delivers the two things a press can do besides playing a card: start a cooldown, or bank toward a Signature; and pins down what no input leaves untouched. Done when every P4 test is green and the suite passes.*

#### ✅ P4.1 Cooldown on any press

- PRD: 3.3.5.1
- Does: Sixteen slots (2 lines × 8 keys) each hold an independent cooldown counter. Any accepted press of a card starts that slot's cooldown at the card's cooldown value (2–6 beats), regardless of grade; it decrements one per beat tick from the press. Assumption: until P7.1 loads it from data, `CardDefinition` takes the cooldown as an optional constructor argument that defaults to the 2-beat minimum; the cooldown starts at the press, not at the window close, and emits CooldownStarted.
- Needs: P2.8, P2.6
- Test (unit): `Sim.Cooldown › missed_card_still_cools` — given a card with cooldown 4, when it is pressed with a Miss, then the slot reads 4 and after 4 ticks reads 0.
- Test (unit): `Sim.Cooldown › slots_independent_across_lines` — given key D on line 1 pressed, when key D on line 2 is read, then it is 0.

#### ✅ P4.2 Slot on cooldown cannot be played

- PRD: 3.3.5.3
- Does: A press on a slot with cooldown above 0 returns a Disabled result carrying the remaining beats, emits a SlotDisabled event for feedback, consumes no card, records no judgment, and does not use up the enemy action. Assumption: the cooldown check runs before the window check, so a cooling slot pressed between enemy actions still gives disabled feedback.
- Needs: P4.1
- Test (unit): `Sim.Cooldown › disabled_press_is_not_a_miss` — given a slot with 2 beats left, when it is pressed, then the result is Disabled, the judgment log has no entry, and another slot can still be played this beat.

#### ✅ P4.3 No input

- PRD: 3.3.3.2
- Does: An enemy action with no accepted press consumes no card, starts no cooldown, records a no-input log entry (not a judgment), fires no Miss-triggered effect, and resolves at IncomingMult 100%.
- Needs: P4.1, P3.1
- Test (unit): `Sim.Judgment › no_input_starts_nothing` — given an enemy action with no press, when its window closes, then all 16 cooldowns are unchanged and the log entry is NoInput.
- Test (unit): `Sim.Judgment › no_input_takes_full_damage` — given enemy DMG 12 and Block 0, when no input, then ARD loss is 12.

#### ✅ P4.4 Banking into the Signature Chain

- PRD: 3.3.6.1
- Does: A Signature-send press (Space plus slot) answering an enemy action moves that slot's card into the first empty of three chain slots instead of resolving its effect; the beat's incoming damage resolves by the press's grade as usual (P3.1). Sending with the chain full is rejected like a disabled press. Assumption: because the third bank fires at once (P4.5) a full chain is never observable, so the guard is defensive: outcome SignatureChainFull plus a SlotDisabled event with that reason.
- Needs: P3.5
- Test (unit): `Sim.Signature › send_banks_not_plays` — given an attack card sent with Perfect while the enemy attacks, when resolved, then enemy HP is unchanged, the chain holds 1 card, and ARD loss is 0.
- Test (unit): `Sim.Signature › good_send_takes_half` — given a send with Good against DMG 20, when resolved, then ARD loss is 10.

#### ✅ P4.5 Signature fires at three

- PRD: 3.3.6.2
- Does: When the third card is banked the Signature fires in the same beat: 30 damage to the enemy (a constant in one place), the chain empties, and a SignatureFired event is emitted. Assumption: the Signature damage is flat, untouched by grade, Base DMG or the efficacy matrix, and is applied through a DamageDealt event that follows SignatureFired.
- Needs: P4.4
- Test (unit): `Sim.Signature › third_bank_fires_30` — given two banked cards and enemy HP 100, when a third is banked, then enemy HP is 70 and the chain is empty.

#### ✅ P4.6 Missed send still banks and cools

- PRD: 3.3.6.3
- Does: A Signature send graded Miss still banks the card; every send starts the slot's cooldown per P4.1.
- Needs: P4.4, P4.1
- Test (unit): `Sim.Signature › missed_send_banks_and_cools` — given a send graded Miss on a card with cooldown 3, when resolved, then the chain holds 1 and the slot reads 3.

#### ✅ P4.7 Perfect Defense recorded

- PRD: 3.3.9.4
- Does: The battle tracks total damage taken to ARD; at BattleEnded, Perfect Defense is true if that total is 0 and is stored on the battle record and in the event.
- Needs: P3.7, P3.1
- Test (unit): `Sim.Battle › perfect_defense_when_no_ard_lost` — given a battle where every enemy attack met a Perfect, when it ends, then PerfectDefense is true; given one Good, false.

### ✅ Phase 5 — Statuses

*Delivers the status system and its five in-scope statuses in the fixed priority order. Done when every P5 test is green and the suite passes.*

#### ✅ P5.1 Status rules

- PRD: 3.3.7.1
- Does: Statuses on either side have a duration in beats, resolve after the beat's actions unless the status says otherwise, stack additively, and reapplication resets the timer to the new source's full duration. Icon rendering is P14.4. Assumption: until P8.6 attaches status effects to content, `Battle.ApplyStatus` is the public way a status lands and tests call it directly; timers tick at beat end, after damage over time; Weak's X values from several sources add; Stun does not stack (a second Stun before the first is consumed adds nothing).
- Needs: P2.4, P2.8
- Test (unit): `Sim.Status › stacks_add_and_refresh_resets` — given Bleed 2 stacks with 3 beats left, when Bleed 1 stack is applied, then stacks are 3 and duration is 8.
- Test (unit): `Sim.Status › resolves_after_actions` — given a Bleed on the enemy and a killing blow this beat, when the beat resolves, then the kill is recorded before the Bleed tick.

#### ✅ P5.2 Same-beat priority

- PRD: 3.3.7.2
- Does: When several statuses trigger on one beat they resolve Stun, then damage multipliers (Weak), then Reflect and Thorns, then damage over time (Bleed) at beat end; the order is one table in code. Assumption: the table also gives each status its moment in the beat (action arrives, attack landed, beat end) and the battle walks the table at every moment; each trigger emits a StatusTriggered event.
- Needs: P5.1
- Test (unit): `Sim.Status › priority_order` — given Stun, Weak, Thorns and Bleed all pending on one beat, when the beat resolves, then the event stream shows them in that order.

#### ✅ P5.3 Scar

- PRD: 3.3.7.3
- Does: Each Scar stack on the enemy adds a 2% chance, rolled from the battle's `Rng`, that a Perfect hit deals double damage; each stack lasts 10 beats. Assumption: `Battle` takes its `Rng` as a constructor argument; one roll of `NextInt(0, 1000)` per Perfect attack hit that deals damage while the enemy carries Scar, and no draw otherwise; every application of Scar keeps its own 10-beat clock.
- Needs: P5.1, P1.2
- Test (unit): `Sim.Status › scar_doubles_by_seeded_roll` — given 5 Scar stacks (10%) and a seed whose first roll is below 10, when a 10-damage Perfect lands, then damage is 20; with a seed rolling above, 10.
- Test (unit): `Sim.Status › scar_stack_lasts_10_beats` — given one stack, when 10 ticks pass, then the enemy has no Scar.

#### ✅ P5.4 Weak

- PRD: 3.3.7.4
- Does: Weak on a target reduces the damage it deals by the source's X% for 8 beats; on the enemy it reduces incoming damage to the player via StatusMults (P3.1); on the player it reduces card damage via StatusMults (P3.2).
- Needs: P5.2, P3.1, P3.2
- Test (unit): `Sim.Status › weak_reduces_enemy_damage` — given Weak 25% on the enemy and DMG 20, when the player takes a Miss, then ARD loss is 15.
- Test (unit): `Sim.Status › weak_expires_after_8` — given Weak applied, when 8 ticks pass, then it is gone.

#### ✅ P5.5 Stun on the enemy

- PRD: 3.3.7.5
- Does: A Stunned enemy skips its next action beat: the telegraphed action on that beat does not resolve and the status is consumed. Assumption: the skipped action still counts as the player's action opportunity, so an accepted press resolves its card against the charted row as usual, and the judgment log still gets its entry.
- Needs: P5.2
- Test (unit): `Sim.Status › stunned_enemy_skips_action` — given the enemy telegraphs an attack next beat, when Stun is applied and the beat resolves, then no DamageTaken is emitted and Stun is gone.

#### ✅ P5.6 Stun on the player

- PRD: 3.3.3.3
- Does: If the player is Stunned when an enemy action arrives, that action is treated exactly as no input (P4.3): presses are ignored, no cooldown starts, and the enemy resolves at full value. Assumption: an ignored press returns outcome PlayerStunned and emits SlotDisabled for feedback; the Stun is consumed when the action resolves.
- Needs: P5.5, P4.3
- Test (unit): `Sim.Status › stunned_player_action_is_no_input` — given the player Stunned and a press arriving, when the enemy action resolves, then the log entry is NoInput, cooldowns are unchanged, and full enemy damage is taken.

#### ✅ P5.7 Bleed

- PRD: 3.3.7.6
- Does: Bleed deals 1 damage per stack per beat at beat end for 8 beats; on the player it bypasses nothing but is not subject to timing mitigation. Assumption: at beat end the enemy's Bleed resolves before the player's, so a kill takes precedence over a death on the same beat; Bleed on the player is absorbed by Block first and counts toward damage taken (P4.7).
- Needs: P5.2
- Test (unit): `Sim.Status › bleed_ticks_per_stack` — given Bleed 3 on enemy HP 50, when 2 ticks pass, then HP is 44.

#### ✅ P5.8 Thorns

- PRD: 3.3.7.7
- Does: Thorns on the player makes the next enemy attack that lands take X damage, then one stack is consumed; stacks accumulate and last until consumed. Assumption: an attack lands when it resolves against the player, whatever the grade, and not when a Stunned enemy skips it; each source keeps its own X and the oldest stack is consumed first.
- Needs: P5.2
- Test (unit): `Sim.Status › thorns_consumed_by_next_attack` — given Thorns 5 twice (two stacks), when the enemy attacks twice, then the enemy takes 5 each time and Thorns is gone after the second.

### ✅ Phase 6 — Edge cases green

*Delivers the remaining resolution rules and the nine-row test table that proves the whole combat core at once. Done when every P6 test is green and the suite passes.*

#### ✅ P6.1 Statuses land regardless of judgment

- PRD: 3.3.4.6
- Does: An enemy action that applies a status applies it on Perfect, Good, Miss and no input alike; only its damage component is mitigated. A card effect or immunity flagged as blocking that status prevents it. Assumption: the statuses are content on the charted action (`applies` in the chart JSON), always target the player, and land right after the action's damage and before the player's effect; a Stunned enemy's skipped action lands none. Immunity is a per-side flag on the battle (`GrantImmunity`), the hook a blocking card effect uses until P8.1 drives it.
- Needs: P5.1, P3.5
- Test (unit): `Sim.Resolve › debuff_lands_on_perfect` — given the enemy attacks with Weak, when the player is Perfect, then ARD loss is 0 and the player has Weak.
- Test (unit): `Sim.Resolve › immunity_blocks_status` — given the player has Weak immunity, when the same action resolves, then no Weak is applied.

#### ✅ P6.2 Battle end clears Block and statuses

- PRD: 3.3.9.5
- Does: At BattleEnded the player's Block and all statuses on both sides are cleared; the next battle starts with Block 0 and no statuses. Assumption: the clearing emits one StatusRemoved per status and one BlockCleared per side that held Block, all before BattleEnded; the enemy's Block (P6.3) is cleared the same way.
- Needs: P3.7, P5.1
- Test (unit): `Sim.Battle › block_and_statuses_cleared_at_end` — given Block 12 and Bleed on the player at the killing blow, when the next battle starts from the same run stats, then Block is 0 and no status is present.

#### ✅ P6.3 True DMG

- PRD: 3.3.4.7
- Does: A True DMG effect reduces the target's HP or ARD directly, ignoring Block, Weak, Iron-Veil-style reductions and any StatusMults. Assumption: enemy Block (`GrantBlock`) and a timed Iron-Veil-style reduction (`ReduceEnemyDamageTaken`) are battle state from here on, so the test can set them and P11.4, P11.6 and P11.7 drive them through the framework; every damage to the enemy that is not True DMG is reduced first, then absorbed by its Block, then taken off HP. True DMG on the player still counts toward damage taken (3.3.9.4).
- Needs: P3.1, P5.4
- Test (unit): `Sim.Resolve › true_dmg_ignores_block_and_reductions` — given the enemy has 10 Block and an 80% damage reduction, when a 7 True DMG effect lands, then enemy HP drops by 7 and Block is unchanged.

#### ✅ P6.4 The nine edge cases

- PRD: 3.3.4.8
- Does: One parameterised test per row of the table in 3.3.4.8, each built from the public battle API, no internals. Assumption: "minus Block" is the Block held as the action arrives (P3.5), so the Good Defense row is tested holding Block 4 like its neighbours; an Ability's full resolution is shown by an AbilityResolved event carrying the JudgmentMult (100% on Perfect), which P8.1 applies to the card's declared effects.
- Needs: P4.3, P4.6, P5.6, P6.1
- Test (unit): `Sim.EdgeCases › perfect_wrong_side_while_attacked` — given the enemy attacks Left, when a Right Attack lands Perfect, then deal 0 and take 0.
- Test (unit): `Sim.EdgeCases › good_correct_counter` — given the enemy attacks Left with 20 and Block 4, when a 10 Left Attack is Good, then deal 5 and take 6.
- Test (unit): `Sim.EdgeCases › miss_any_card` — given the enemy attacks with 20 and Block 4, when any card is Missed, then deal 0, take 16, slot on cooldown.
- Test (unit): `Sim.EdgeCases › perfect_ability_while_attacked` — given the enemy attacks, when an Ability is Perfect, then its effect resolves fully and take 0.
- Test (unit): `Sim.EdgeCases › good_defense_while_attacked` — given the enemy attacks with 20 and an 8-Block card, when Good, then gain 4 Block and take 6.
- Test (unit): `Sim.EdgeCases › no_input_while_attacked` — given the enemy attacks with 20 and Block 4, when no input, then take 16, no cooldown, no Miss-triggered effect.
- Test (unit): `Sim.EdgeCases › perfect_signature_send_while_attacked` — given the enemy attacks, when a send is Perfect, then the card is banked and take 0.
- Test (unit): `Sim.EdgeCases › stun_when_enemy_action_arrives` — given the player Stunned, when the enemy action resolves, then it is treated as no input.
- Test (unit): `Sim.EdgeCases › debuff_on_perfect_beat` — given the enemy applies a non-damage debuff, when the player is Perfect, then the debuff lands.

### Phase 7 — Cards are data

*Delivers the card definition schema, its loader, and the validator that turns every card rule into a build-time failure. Done when every P7 test is green and the suite passes.*

#### P7.1 Card definition schema and loader

- PRD: 4.4
- Does: A `CardDefinition` record with id, name, Category, Rarity, class, damage-or-value, cooldown beats, effects (a list of typed effect entries), special rules, reaction conditions, upgrade step, Unstable lifespan (nullable), flavor text (optional), unlock source. Loaded from a JSON data file per set; `data/cards.csv` is migrated to this format by P8.8.
- Needs: P1.1
- Test (unit): `Sim.Cards › loads_definition_from_json` — given a JSON card entry with every field, when loaded, then every field round-trips.

#### P7.2 Category fixes legal slots

- PRD: 3.4.1
- Does: Category is one of Ability, LeftAttack, RightAttack, Defense and maps to its two keys per line (A/S, D/F, J/K, L/;); a card can be slotted only where its category allows.
- Needs: P7.1
- Test (unit): `Sim.Cards › category_to_slots` — given a Defense card, when its legal slots are listed, then they are L and ; on both lines and nothing else.

#### P7.3 Rarity bands

- PRD: 3.4.4
- Does: Rarity is one of Common, Uncommon, Rare, Legendary; the validator rejects an attack card whose damage or a defense card whose Block falls outside its band (Common 8–12 / 5–10, Uncommon 12–16 / 10–16, Rare 16–22 / 16–21, Legendary 22–26 / 22+).
- Needs: P7.1
- Test (unit): `Sim.Cards › rarity_band_enforced` — given a Common attack with damage 14, when validated, then it fails naming the band; with 12, it passes.

#### P7.4 Card anatomy

- PRD: 3.4.7
- Does: The validator requires name, Category, Rarity, value and a cooldown of 2–6 beats; status effects, special rules and flavor text are optional; flavor text has no rule beyond being a string.
- Needs: P7.1
- Test (unit): `Sim.Cards › cooldown_must_be_2_to_6` — given cooldown 7, when validated, then it fails; given 2 and 6, it passes.

#### P7.5 Effect compatibility

- PRD: 3.4.8
- Does: A table of effect × category → minimum rarity from 3.4.8 (Bleed, Weak, Scar, Thorns, Stun, Block-on-attack, Disarmed, Repair, True DMG); the validator rejects an effect on a category where it is not allowed or below its minimum rarity.
- Needs: P7.3
- Test (unit): `Sim.Cards › stun_needs_rare_attack` — given an Uncommon attack with Stun, when validated, then it fails; given Rare, it passes.
- Test (unit): `Sim.Cards › thorns_never_on_attack` — given an attack with Thorns of any rarity, when validated, then it fails.

#### P7.6 Exactly one class

- PRD: 3.4.13
- Does: Class is one of Normal, Event, Unstable and is required; Unstable requires a lifespan of at least 1 battle; the other classes must have none.
- Needs: P7.1
- Test (unit): `Sim.Cards › unstable_requires_lifespan` — given class Unstable with no lifespan, when validated, then it fails; given Normal with a lifespan, it fails too.

#### P7.7 Cards carry no formula

- PRD: 3.4.10
- Does: The definition has no field for multipliers, judgment factors or Base DMG handling; resolution reads only CardValue and effect modifiers and applies P3.2 itself.
- Needs: P7.1, P3.2
- Test (unit): `Sim.Cards › resolution_uses_cardvalue_only` — given a loaded 10-damage card, when played Good with Base DMG 2, then damage is 6, computed by the battle and not by the card.

#### P7.8 Validation gate

- PRD: 3.4.21
- Does: A `CardValidator` runs every rule in P7.2 to P7.6 over a whole card set and returns all violations; a test loads every shipped data set so an invalid card fails the suite, which is what "removed before it ships" means operationally.
- Needs: P7.2, P7.3, P7.4, P7.5, P7.6
- Test (unit): `Sim.Cards › shipped_sets_validate` — given every JSON card set under the data folder, when validated, then there are zero violations.
- Test (unit): `Sim.Cards › validator_reports_all_violations` — given a set with three broken cards, when validated, then three violations are returned, each naming the card and the rule.

### Phase 8 — Effects framework

*Delivers the one trigger-condition-modifier framework that cards, enemy powers and Charms all use, proven on the four card mechanic kinds. Done when every P8 test is green and the suite passes.*

#### P8.1 Trigger, condition and modifier core

- PRD: — (groundwork for P8.3 to P8.7, P11.1 to P11.7, P18.4)
- Does: An `Effect` is a trigger (an event type from the battle stream, or "on play"), an optional condition evaluated against battle state, and a modifier (deal damage, gain Block, apply status, change a stat, multiply a value) with a lifetime in beats, battles or "run". A registry attaches effects to an owner (card instance, enemy, Charm); the battle evaluates registered effects when their trigger event is appended.
- Needs: P2.8, P5.1
- Test (unit): `Sim.Effects › trigger_fires_on_event` — given an effect on DamageTaken that applies Thorns 2, when the player takes damage, then the player has Thorns 2.
- Test (unit): `Sim.Effects › condition_gates_trigger` — given the same effect with condition "grade is Perfect", when damage is taken on a Good, then nothing is applied.
- Test (unit): `Sim.Effects › lifetime_in_beats_expires` — given a 3-beat modifier, when 3 ticks pass, then it no longer applies.

#### P8.2 Card instance

- PRD: 4.5
- Does: A `CardInstance` references a definition and carries upgraded flag, Trait slot (empty in this plan), battles remaining for Unstable cards, and a shop price field (unset in this plan). Two instances of one definition are distinct.
- Needs: P7.1
- Test (unit): `Sim.Cards › instances_are_distinct` — given two instances of one definition, when one's battles-remaining is decremented, then the other is unchanged.

#### P8.3 Direct damage mechanic

- PRD: 3.4.9
- Does: A card whose only effect is "deal CardValue" resolves through P3.2 and the matrix.
- Needs: P8.1, P8.2
- Test (unit): `Sim.Mechanics › direct_damage` — given a direct 10-damage attack, when Perfect on the correct side, then the enemy loses 10.

#### P8.4 Scaling with player buffs

- PRD: 3.4.9
- Does: A card may declare its value as base plus a multiple of a player stat or buff stack (for example +2 per Block held); the value is computed at play time.
- Needs: P8.1, P8.2
- Test (unit): `Sim.Mechanics › scales_with_block` — given a card with value 6 plus 1 per 2 Block and the player holding 8 Block, when Perfect, then damage is 10.

#### P8.5 Scaling with CRP

- PRD: 3.8.8
- Does: A card may declare its value as base plus a multiple of the run's CRP, and the declaration is part of the card's text; nothing scales with CRP unless declared.
- Needs: P8.4, P1.4
- Test (unit): `Sim.Mechanics › scales_with_crp_when_declared` — given a card with value 8 plus 1 per 10 CRP and CRP 40, when Perfect, then damage is 12; a card without the declaration deals 8 at any CRP.

#### P8.6 Status application mechanic

- PRD: 3.4.9
- Does: A card effect may apply any in-scope status (Scar, Weak, Stun, Bleed, Thorns) with stacks and the status's duration; application follows P6.1 for enemy-sourced and P3.2 for the player's own judgment gating (a Missed card applies nothing).
- Needs: P8.1, P5.3, P5.4, P5.5, P5.7, P5.8
- Test (unit): `Sim.Mechanics › applies_bleed_on_perfect_not_miss` — given a card applying Bleed 2, when Perfect then the enemy has Bleed 2; when Miss, none.

#### P8.7 Reaction conditions

- PRD: 3.4.9
- Does: The three condition kinds from the PRD are available to cards: "on Perfect", "if this kills", "if the enemy is attacking this beat", each combinable with any modifier.
- Needs: P8.1, P3.7
- Test (unit): `Sim.Mechanics › on_perfect_bonus` — given a card with +5 damage on Perfect, when Perfect, then 15; when Good, 5.
- Test (unit): `Sim.Mechanics › if_kills_grants_block` — given a card granting 10 Block if it kills, when it reduces enemy HP to 0, then the player has 10 Block; when it does not kill, 0.
- Test (unit): `Sim.Mechanics › if_enemy_attacking` — given a card with double damage if the enemy attacks this beat, when the enemy attacks, then 20; when idle, 10.

#### P8.8 Starter fixture content

- PRD: — (groundwork for P16.3, P20.3)
- Does: A `data/sets/starter.json` with 20 cards (6 Ability, 5 Left Attack, 5 Right Attack, 4 Defense, all Common, Normal class, cooldowns 2–6, values inside bands) that passes P7.8, and a `data/tracks/fixture-120.json` track at BPM 120. The ten drafted rows of `data/cards.csv` are carried into it with concrete values; the rest are filler named `Starter <n>`.
- Needs: P7.8, P2.1
- Test (unit): `Sim.Fixtures › starter_set_validates_and_fills_two_lines` — given the starter set, when validated and grouped by category, then there are zero violations and at least 4 cards per category.

### Phase 9 — Enemies are data

*Delivers the enemy definition bound to its own track and chart, tiers, roles, profiles, the budget and chart validators, looping and the HP formula. Done when every P9 test is green and the suite passes.*

#### P9.1 Enemy definition

- PRD: 4.7
- Does: An `EnemyDefinition` record with id, name, tier, role, rhythm profile, track id, chart id (P2.10), damage per hit, abilities, traits, statuses used, portrait id, quote line, and an optional phase list for Bosses (field only in this plan). HP is not a field (P10.2).
- Needs: P1.1, P2.10
- Test (unit): `Sim.Enemies › loads_definition_with_chart_reference` — given a JSON enemy naming a track and a chart, when loaded, then every field round-trips and the chart resolves to a loaded `Chart`.

#### P9.2 Encounter tiers

- PRD: 3.3.9.1
- Does: Tier is one of Normal (intended 30–60 s), Elite (60–90 s), Boss (90–180 s); each definition carries an intended duration in seconds inside its tier's band, which the validator enforces; P9.8 reads it.
- Needs: P9.1
- Test (unit): `Sim.Enemies › intended_duration_inside_tier_band` — given a Normal enemy with 75 s, when validated, then it fails; with 45 s, it passes.

#### P9.3 Roles

- PRD: 3.6.1
- Does: Role is exactly one of Aggressor (HP 0.8×), Tank (1.2×), Mentalist (1.0×); the multiplier is exposed for P10.2.
- Needs: P9.1
- Test (unit): `Sim.Enemies › role_multiplier` — given each role, when its HP multiplier is read, then it is 800, 1200 and 1000 thousandths respectively.

#### P9.4 Rhythm profiles

- PRD: 3.6.2
- Does: Profile is exactly one of Fast or Slow; the validator requires it and rejects a definition with none.
- Needs: P9.1
- Test (unit): `Sim.Enemies › profile_required` — given a definition without a profile, when validated, then it fails.

#### P9.5 Upcoming actions exposed

- PRD: 3.6.3
- Does: The battle exposes, for any horizon, the enemy's upcoming actions as (action, target position, beats remaining) computed from the chart and the current position; display is P14.6.
- Needs: P9.1, P2.11
- Test (unit): `Sim.Enemies › upcoming_actions_with_countdown` — given a chart with AttackLeft at beat 4 and the battle at beat 1, when upcoming actions are queried, then the first entry is AttackLeft, position 4, 3 remaining.

#### P9.6 Tier budget

- PRD: 3.6.4
- Does: The validator enforces the table: Normal 1 ability, 0–1 traits, 0–1 statuses; Elite 1, 1–2, 1–2; Boss 1–2, 2, 1–2.
- Needs: P9.2
- Test (unit): `Sim.Enemies › normal_cannot_carry_two_abilities` — given a Normal enemy with 2 abilities, when validated, then it fails; a Boss with 2 passes.

#### P9.7 Track binding

- PRD: 3.6.28
- Does: An enemy's track id and chart id must resolve to a loaded track and a chart written for that same track; the validator rejects a chart whose track differs from the enemy's, and two enemies may not share a track. The battle's beat map comes from that track and the chart's positions land on it.
- Needs: P9.1, P2.9
- Test (unit): `Sim.Enemies › battle_tempo_from_enemy_track` — given an enemy bound to a BPM 140 track, when a battle starts, then its beat map is at 140 BPM and the chart's action at beat 4 lands at the map's beat 4 time.
- Test (unit): `Sim.Enemies › chart_must_match_track` — given an enemy whose chart names a different track, when validated, then it fails.

#### P9.8 HP formula

- PRD: 3.7.15
- Does: `EnemyHp(actionsPerMinute, attackRatio, avgCardDmg, judgmentMix, intendedSeconds)` implementing AAPM = ActionsPerMinute × AttackRatio, AvgAttackDMG = AvgCardDMG × (P% + G% × 0.5), DPM, DPS and HP = DPS × duration, in thousandths, rounded once; ActionsPerMinute comes from the enemy's chart (P2.10).
- Needs: P1.2, P2.10
- Test (unit): `Sim.Balance › worked_check_360` — given a chart at 60 actions per minute, AttackRatio 0.6, AvgAttackDMG 10 (all Perfect), 60 s, when computed, then base HP is 360.

#### P9.9 Chart validation

- PRD: 3.6.31
- Does: A `ChartValidator` rejects a chart with no actions, an action beyond the track's length, a position not on a quarter beat, an unknown action kind, or a Charge wind-up outside 3–5 beats; every shipped chart is validated in the suite.
- Needs: P2.10, P9.2
- Test (unit): `Sim.Chart › rejects_off_grid_and_out_of_range` — given a chart with an action at 2.3 beats and another at beat 40 on a 32-beat track, when validated, then two violations name those actions; a chart with actions on 2.25 and 31.75 passes.

#### P9.10 Chart and track loop together

- PRD: 3.6.32
- Does: When the battle position reaches the track's length, the chart restarts at its first action and the beat map continues from beat 0 of the next pass with no gap; the loop count is exposed for the presenter and the audio scheduler.
- Needs: P2.11, P9.7
- Test (unit): `Sim.Chart › loops_from_start_seamlessly` — given a 32-beat chart whose first action is at beat 1 and the battle unfinished at beat 32, when the position passes 32, then the next opportunity is at beat 33 (pass 2, beat 1) and the beat map time is continuous.

### Phase 10 — Enemy numbers hold

*Delivers the damage bands, the derived HP, the mistake-budget check and the fixture enemies every later phase fights. Done when every P10 test is green and the suite passes.*

#### P10.1 Damage bands and World scaling

- PRD: 3.7.16
- Does: A table of damage-per-hit bands per role × tier from 3.7.16; the validator rejects a World 1 definition outside its band; the battle raises a definition's damage by 15% per World above World 1, compounded and rounded once.
- Needs: P9.3, P9.2
- Test (unit): `Sim.Balance › band_enforced_for_world_1` — given a Normal Aggressor with damage 16, when validated, then it fails; with 15, it passes.
- Test (unit): `Sim.Balance › damage_rises_15_percent_per_world` — given damage 10 in World 1, when the same enemy appears in World 3, then its damage is 13 (10 raised 15% twice, rounded).

#### P10.2 HP and damage derived at battle start

- PRD: 3.6.29
- Does: At battle start enemy HP = P9.8 result for the World's AvgCardDMG (12, 14, 16 for Worlds 1–3, one constant table) and the enemy's intended duration, times the role multiplier; damage per hit comes from P10.1.
- Needs: P9.8, P9.3, P10.1
- Test (unit): `Sim.Balance › tank_hp_is_1_2x_formula` — given the worked-check inputs and a Tank, when a battle starts, then HP is 432; an Aggressor, 288.

#### P10.3 Mistake budget

- PRD: 3.7.17
- Does: A measurement over the fixture enemies (P10.4): a full-ARD player who takes only Misses survives at least 10 consecutive Misses against a Normal enemy at the top of its band, 6 against an Elite, 4 against a Boss.
- Needs: P10.2, P10.4, P3.1
- Test (measurement): `Sim.Balance › mistake_budget_lower_bounds` — given each fixture enemy at max band damage and ARD 300, when the player Misses 10, 6 and 4 times respectively, then ARD is above 0 in every case.

#### P10.4 Fixture enemies

- PRD: — (groundwork for P10.3, P11.1 to P11.7, P20.1, P21.1, P21.3)
- Does: `data/enemies/fixtures.json` with five definitions bound to the fixture track: Normal Aggressor (Fast), Normal Tank (Slow), Normal Mentalist (Fast), one Elite Tank, one Boss Aggressor, each on its own 32-beat fixture track at BPM 120 with an authored chart of 16–32 actions using quarter-beat positions at least once, and powers drawn only from the seven in-scope abilities and traits; Ren from `data/enemies.csv` becomes the Normal Tank rescaled to the formulas.
- Needs: P9.6, P9.9, P10.1
- Test (unit): `Sim.Fixtures › fixture_enemies_validate` — given the fixture set, when definitions and charts are validated, then there are zero violations, one definition per tier exists, and no two enemies share a track.

### Phase 11 — The framework runs enemy powers

*Delivers the seven exerciser abilities and traits through the shared framework, one per trigger shape. Done when every P11 test is green and the suite passes.*

#### P11.1 Rising Tempo

- PRD: 3.6.5
- Does: Passive: each time this enemy deals ARD damage, its Base DMG rises by 3 for the rest of the battle.
- Needs: P8.1, P3.1
- Test (unit): `Sim.Powers › rising_tempo_adds_3_per_hit` — given damage 10 and two landed hits, when the third attack resolves on a Miss, then ARD loss is 16.

#### P11.2 Misstep Pain

- PRD: 3.6.6
- Does: Passive: the player takes 5 damage on a Good and 10 on a Miss, applied after the beat's incoming damage; no-input beats are exempt (P4.3).
- Needs: P8.1, P4.3
- Test (unit): `Sim.Powers › misstep_pain_by_grade` — given three charted Buff actions, when the player answers Good, Miss and then takes no input, then ARD losses are 5, 10 and 0.

#### P11.3 Pressure

- PRD: 3.6.8
- Does: Passive: after an enemy action with no input, the enemy's next attack deals 2× damage; the multiplier is consumed by that attack.
- Needs: P8.1, P4.3
- Test (unit): `Sim.Powers › pressure_doubles_after_no_input` — given damage 10, when the player takes no input and then Misses the next attack, then that attack costs 20 and the one after costs 10.

#### P11.4 Iron Veil

- PRD: 3.6.9
- Does: Timed: for 5 beats from activation the enemy takes 80% less damage; an IronVeilActive flag is on the battle state for the presenter.
- Needs: P8.1, P3.2
- Test (unit): `Sim.Powers › iron_veil_reduces_80_for_5_beats` — given Iron Veil on beat 2, when a 10 Perfect lands on beat 4 and another on beat 8, then the enemy loses 2 and then 10.

#### P11.5 Charge / Buff

- PRD: 3.6.16
- Does: Telegraphed: a Charge action in the chart shows a wind-up of 3–5 beats on the upcoming-actions list (P9.5), then resolves as an empowered move at 2× its base damage; the validator (P9.9) rejects wind-ups outside 3–5.
- Needs: P8.1, P9.5
- Test (unit): `Sim.Powers › charge_telegraphs_then_hits_double` — given Charge(4) at beat 2 with damage 10, when upcoming actions are read at beat 2, then the hit shows at beat 6 with 4 remaining; when it lands on a Miss, ARD loss is 20.

#### P11.6 Stoneform

- PRD: 3.6.20
- Does: Conditional: after three consecutive beats in which the enemy took no damage, it gains 10 Block; the counter resets on any damage.
- Needs: P8.1
- Test (unit): `Sim.Powers › stoneform_after_three_quiet_beats` — given the enemy undamaged for beats 1–3, when beat 3 ends, then the enemy has 10 Block; when damaged on beat 2, none by beat 4.

#### P11.7 Guard

- PRD: 3.6.25
- Does: Battle-start: the enemy begins with 30 Block, which absorbs damage before HP.
- Needs: P8.1, P2.8
- Test (unit): `Sim.Powers › guard_starts_with_30_block` — given an enemy with Guard and HP 100, when a 40 Perfect lands, then Block is 0 and HP is 90.

### Phase 12 — The client hears the beat

*Delivers the Unity project, an audio-clock-driven beat source that drives the simulation, and the measurement that grading is independent of framerate. Done when every P12 test is green and the suite passes.*

#### P12.1 Unity project and test runner

- PRD: — (groundwork for P12.2 to P12.5, every P13, P14, P15 and P23 item)
- Does: `client/` Unity project (2022 LTS or later) referencing `Chiki.Sim` as a compiled assembly, with an assembly definition `Chiki.Client`, the Input System package, and Unity Test Framework edit-mode and play-mode assemblies under `client/Assets/Tests`. A CLI script runs the play-mode suite headless.
- Needs: P1.3
- Test (integration): `Client.Smoke › sim_available_in_client` — given the client test assembly, when a `Battle` from `Chiki.Sim` is constructed, then it works and the scene loads.

#### P12.2 Battle synchronised to the track

- PRD: 3.3.1.1
- Does: A `BeatClock` schedules the track with `AudioSettings.dspTime`, converts DSP time to the sim's millisecond beat map (P2.1), and ticks the simulation's beats from audio time, never from `Time.deltaTime`. The Rhythm Line view is P14.1.
- Needs: P12.1, P2.1
- Test (integration): `Client.Clock › beats_tick_on_dsp_time` — given the fixture track scheduled, when 4 seconds of DSP time elapse, then the simulation has received beats 0–7 at BPM 120 and each tick's audio time matches the beat map within 1 ms.

#### P12.3 Timing accuracy

- PRD: 6.1
- Does: Input events are stamped with audio time by converting the Input System event timestamp to DSP time at the moment the event was generated, not the frame it was read; grading uses that stamp.
- Needs: P12.2
- Test (measurement): `Client.Clock › grade_independent_of_framerate` — given an input generated at beat centre +30 ms, when the frame rate is capped at 30 fps and then 144 fps, then both runs grade Perfect and the stamped offset differs by less than 2 ms between them.

#### P12.4 Music never interrupted

- PRD: 3.3.1.6
- Does: No mechanic touches the audio source's pitch, playback position or pause state; enemy rhythm changes (Beat Rush, out of scope) and player Stun act only on the simulation.
- Needs: P12.2, P5.6
- Test (measurement): `Client.Clock › playback_continuous_through_stun_and_signature` — given a battle with a player Stun and a Signature firing, when 8 seconds elapse, then the audio source's pitch stayed at 1 and its DSP position advanced monotonically by 8 seconds within 5 ms.

#### P12.5 Battle harness

- PRD: — (groundwork for P13.1 to P13.5, P14.1 to P14.6, P15.4)
- Does: A `BattleDriver` that owns one `Battle`, subscribes the presenter to its event stream, forwards timestamped inputs, and can be driven in tests by a scripted input list with audio-time stamps.
- Needs: P12.2
- Test (integration): `Client.Harness › scripted_inputs_reach_sim` — given a script of three timestamped presses, when the driver runs them, then the simulation's judgment log holds three graded entries.

### Phase 13 — Keys

*Delivers the fixed keyboard layout, physical-position binding, the Left Shift line switch and the Space chord, all feeding the driver. Done when every P13 test is green and the suite passes.*

#### P13.1 Fixed layout

- PRD: 3.3.2.1
- Does: An `InputMap` binds A, S to Ability slots 1–2, D, F to Left Attack 1–2, J, K to Right Attack 1–2, L, ; to Defense 1–2 of the active line; the map is a constant with no rebinding path; a press produces a (line, slot) event with its audio-time stamp (P12.3).
- Needs: P12.5
- Test (integration): `Client.Input › eight_keys_map_to_slots` — given the map, when each of the eight keys is pressed, then the driver receives slot indices 0–7 on the active line and nothing for any other key.

#### P13.2 Physical-position binding

- PRD: 3.3.2.5
- Does: Keys are bound by Input System physical key (scancode) so the home-row shape holds on any layout; the slot label shows the character that key produces on the current layout via `Keyboard.current[key].displayName`.
- Needs: P13.1
- Test (integration): `Client.Input › labels_follow_layout` — given a simulated AZERTY layout, when labels are read, then the Ability-1 slot shows Q while the physical key is unchanged and still maps to Ability 1.

#### P13.3 Left Shift switches lines

- PRD: 3.3.2.6
- Does: Left Shift is the dedicated line-switch key: on key-down, at any time including between enemy actions, the active line toggles; the press is never graded, never a Miss, starts no cooldown, and is ignored while Space is held (it is never part of a chord).
- Needs: P13.1
- Test (integration): `Client.Input › shift_toggles_ungraded` — given a moment between enemy actions, when Left Shift is pressed, then the active line is 2, the judgment log is unchanged and no cooldown started.
- Test (integration): `Client.Input › shift_ignored_during_chord` — given Space held, when Left Shift is pressed, then the active line is unchanged.

#### P13.4 Active line routes presses

- PRD: 3.3.2.2
- Does: After a switch, slot presses route to the new active line's cards; both lines remain present in the loadout state at all times (rendering thinner is P14.2).
- Needs: P13.3
- Test (integration): `Client.Input › press_after_switch_hits_line_2` — given line 1 active and a switch, when D is pressed, then the card in Left-Attack-1 of line 2 is played.

#### P13.5 Space chord sends to the chain

- PRD: 3.3.2.3
- Does: A slot key pressed while Space is held produces a Signature-send event for that slot (P4.4); Space alone produces no event; releasing Space produces no event.
- Needs: P13.1
- Test (integration): `Client.Input › space_plus_key_sends` — given Space held, when D is pressed, then the driver receives a send for Left-Attack-1 and the chain holds 1 card.
- Test (integration): `Client.Input › space_alone_does_nothing` — given no other key, when Space is pressed and released, then no event reaches the driver.

### Phase 14 — A battle you can see

*Delivers the minimal combat presenter needed to judge feel: the Rhythm Line, both loadout lines with cooldowns, status icons, judgment feedback, and the framerate measurement. Done when every P14 test is green and the suite passes.*

#### P14.1 Rhythm Line view

- PRD: 3.3.1.1
- Does: A horizontal timeline scrolling with the beat clock, marking every beat and the quarter-beat grid, every charted enemy action, and the Judgment Window on the next enemy action; it is on screen for the whole battle.
- Needs: P12.2, P12.5
- Test (integration): `Client.Presenter › rhythm_line_present_and_scrolling` — given a running battle, when two beats elapse, then the Rhythm Line element is active and its beat markers have moved by two beat-widths.

#### P14.2 Cooldown overlay and two lines

- PRD: 3.3.5.4
- Does: Sixteen slot widgets in two rows; a slot on cooldown shows a dim overlay and the remaining beats as a number; the inactive row is rendered at reduced width; both rows are always visible.
- Needs: P12.5, P13.4
- Test (integration): `Client.Presenter › cooldown_countdown_and_thin_inactive_line` — given a slot with 3 beats left and line 1 active, when rendered, then the slot shows "3" with the overlay and line 2's row width is smaller than line 1's; both rows are active objects.

#### P14.3 Judgment feedback

- PRD: 3.3.8.1
- Does: On InputJudged play a per-grade audio cue and flash the pressed key; while a slot's window is open glow its card; highlight upcoming actions on the Rhythm Line; on DamageTaken above 15 shake the camera briefly.
- Needs: P14.1, P12.5
- Test (integration): `Client.Presenter › grade_cue_and_flash` — given a Perfect judgment event, when the presenter handles it, then the Perfect cue plays and the key widget's flash is triggered.
- Test (integration): `Client.Presenter › glow_during_open_window` — given the Judgment Window opens, when rendered, then the playable cards glow and stop glowing after it closes.
- Test (integration): `Client.Presenter › shake_on_heavy_hit` — given DamageTaken of 20, when handled, then the camera shake is triggered; given 5, it is not.

#### P14.4 Status icons

- PRD: 3.3.7.1
- Does: Statuses on each side render as icons above that side's HP or ARD bar with stack count and a tooltip on hover naming the status, its effect and remaining beats.
- Needs: P12.5, P5.1
- Test (integration): `Client.Presenter › status_icon_with_tooltip` — given Bleed 2 on the enemy with 6 beats left, when rendered, then an icon with "2" sits above the enemy bar and its tooltip contains "Bleed" and "6".

#### P14.5 Frame rate holds

- PRD: 6.2
- Does: The battle scene, with the fixture Boss and all presenters active, renders at 1080p; a measurement records average and 1% low frame time over 30 seconds on the build machine, and audio underruns are counted.
- Needs: P14.1, P14.2, P14.3, P14.4
- Test (measurement): `Client.Perf › battle_scene_60fps_no_audio_dropouts` — given the battle scene at 1080p, when 30 seconds run, then the 1% low frame time is under 16.7ms and zero audio underruns are recorded.

#### P14.6 Upcoming actions on the Rhythm Line

- PRD: 3.6.3
- Does: Every upcoming enemy action from P9.5 is drawn on the Rhythm Line at its beat with its kind (Left, Right, Defend, Buff, Charge wind-up) and a beats-remaining number.
- Needs: P14.1, P9.5
- Test (integration): `Client.Presenter › telegraph_drawn_with_countdown` — given AttackLeft at beat 4 and the battle at beat 1, when rendered, then a Left marker sits at beat 4 showing "3".

### Phase 15 — Profile and calibration

*Delivers a profile that owns settings and calibration, and the calibration screen that every feel test must run first. Done when every P15 test is green and the suite passes.*

#### P15.1 Profile record

- PRD: 3.1.2
- Does: A `Profile` file per named profile under the persistent data path holding: meta progression container (filled by P17.3), relationships container (P18.6), settings, calibration offset, tutorial-completed flag, run-in-progress slot (P22.1), run history and run-log folder path. A `ProfileStore` loads, saves and lists profiles; the picker UI is out of scope, so tests create profiles by name.
- Needs: P12.1
- Test (integration): `Client.Profile › profiles_share_nothing` — given profiles "A" and "B", when A's calibration offset is set to 80, then B's is still 0 after reload.

#### P15.2 Offset test and storage

- PRD: 3.12.1
- Does: A calibration screen plays a metronome at BPM 120 for 16 beats and shows a pulsing marker; the player taps Space on each beat; the median of tap-minus-beat offsets (audio time, P12.3) becomes the profile's offset. The screen states that Bluetooth audio adds 100–300 ms.
- Needs: P15.1, P12.3
- Test (integration): `Client.Calibration › median_offset_stored` — given 16 simulated taps each +60 ms late, when the test finishes, then the profile's offset is 60 and the screen showed the Bluetooth note.

#### P15.3 Offered first, reachable always

- PRD: 3.12.1
- Does: On a profile's first launch the calibration screen opens before any battle can start; afterwards it is reachable from the settings entry available on the map and in the pre-run screen.
- Needs: P15.2
- Test (integration): `Client.Calibration › first_launch_forces_calibration` — given a fresh profile, when the game enters the pre-run screen, then the calibration screen is open and the Start Run action is disabled until it closes.
- Test (integration): `Client.Calibration › reachable_from_map` — given a calibrated profile on the map, when Settings then Calibrate is chosen, then the calibration screen opens.

#### P15.4 Offset applied and metronome toggle

- PRD: 3.3.8.2
- Does: The driver subtracts the profile's offset from every input stamp before grading; a metronome toggle in settings plays a click on every beat from the beat clock.
- Needs: P15.2, P12.5
- Test (integration): `Client.Calibration › offset_shifts_grading` — given offset 60 and an input +60 ms late, when graded, then it is Perfect; with offset 0, Good.
- Test (integration): `Client.Calibration › metronome_toggle_clicks_on_beats` — given the toggle on, when 4 beats elapse, then 4 clicks were scheduled at the beat map's times.

### Phase 16 — Binder and loadout

*Delivers the Binder, the sixteen-slot loadout with its composition rules, Unstable lifespans, and the Imprint and Charm entities the run will hold. Done when every P16 test is green and the suite passes.*

#### P16.1 Loadout of sixteen

- PRD: 3.5.1
- Does: A `Loadout` with exactly 16 slots addressed by (line 1–2, key 0–7), each holding at most one `CardInstance`; there is no draw, discard or hand concept anywhere in the simulation; the battle reads cards from slots only.
- Needs: P8.2
- Test (unit): `Sim.Loadout › sixteen_slots_and_no_hidden_zone` — given a loadout, when its slots are enumerated, then there are 16, and the battle API exposes no draw or hand operation.

#### P16.2 Composition and legality

- PRD: 3.5.2
- Does: Each line must hold 2 Ability, 2 Left Attack, 2 Right Attack and 2 Defense cards in their category's keys (P7.2); placing a card in a slot of another category is rejected; one instance can occupy at most one slot.
- Needs: P16.1, P7.2
- Test (unit): `Sim.Loadout › category_slot_rejected` — given a Defense card, when placed in key D, then it is rejected; in key L, accepted.
- Test (unit): `Sim.Loadout › instance_in_one_slot_only` — given an instance already in line 1 key A, when placed in line 2 key S, then it is rejected.

#### P16.3 Starter Binder fills both lines

- PRD: 3.5.3
- Does: A `Binder` created from the starter set (P8.8) holds one instance per starter card; an auto-fill builds a legal 16-card loadout from it for both lines.
- Needs: P16.2, P8.8
- Test (unit): `Sim.Binder › starter_autofill_is_legal` — given the starter Binder, when auto-filled, then all 16 slots are filled and the composition check passes.

#### P16.4 Rebuild before a battle

- PRD: 3.5.5
- Does: Between battles the loadout is editable: any slot can be cleared or assigned from the Binder; entering a battle with any empty slot is rejected with the empty slots listed. The UI is P23.3.
- Needs: P16.2
- Test (unit): `Sim.Loadout › empty_slot_blocks_battle` — given a loadout with line 2 key K empty, when a battle is requested, then it is rejected naming (2, K); after filling it, accepted.

#### P16.5 Unstable lifespan

- PRD: 3.4.16
- Does: An Unstable instance's battles-remaining decrements at the end of every battle it spent in the Binder (slotted or not); at 0 it is destroyed and removed from Binder and loadout.
- Needs: P16.3, P8.2, P6.2
- Test (unit): `Sim.Binder › unstable_destroyed_after_lifespan` — given an Unstable card with lifespan 2 in the Binder, when two battles end, then it is absent from the Binder and its slot is empty.

#### P16.6 Imprint entity and fixtures

- PRD: 4.10
- Does: An `ImprintDefinition` with id, name, tier (Common, Uncommon, Rare), effect (P8.1 effect entries), stackable flag and source; a fixture set of 6 Imprints, two per tier, using only in-scope modifiers (for example +2 Base DMG, +10 max ARD, Thorns 2 at battle start).
- Needs: P8.1
- Test (unit): `Sim.Imprints › fixtures_load_by_tier` — given the fixture set, when loaded, then there are 2 per tier and every effect registers with the framework.

#### P16.7 Charm entity and fixtures

- PRD: 4.9
- Does: A `CharmDefinition` with id, name, rarity, trigger (a framework trigger with condition), effect, unlock condition (a predicate over profile facts such as bosses defeated) and ending-altering flag; fixtures: Clean Victory (Perfect Defense → +10 Essence) and Momentum Plate (Perfect Defense → +5 max ARD, cap +50 per run), both unlocked by "defeat 1 boss".
- Needs: P8.1
- Test (unit): `Sim.Charms › fixtures_load` — given the two fixtures, when loaded, then both triggers reference PerfectDefense and both unlock conditions read bosses defeated.

### Phase 17 — A run begins

*Delivers the run entity, its seed, the meta progression it draws on, Charm equipping and the start state. Done when every P17 test is green and the suite passes.*

#### P17.1 Run entity

- PRD: 4.2
- Does: A `Run` aggregate with seed, World index, current node, `RunStats` (P1.4), equipped Charms, Imprints held, four armor upgrade slots (empty in this plan), Binder, Loadout, difficulty modifiers and Assist flag (both always empty or false in this plan), map graphs (filled by P19.1) and status (InProgress, Won, Died, Abandoned). Serialisable to JSON with a schema version field (P22.3).
- Needs: P16.3, P1.4
- Test (unit): `Sim.Run › run_round_trips_to_json` — given a run with a Binder and loadout, when serialised and deserialised, then every field is equal and the schema version is present.

#### P17.2 Seed accepted and stored

- PRD: 3.2.4
- Does: A run is created with a caller-supplied seed string or, absent one, a generated one; the seed is stored on the run, printed on the run-end summary (P23.5), and every subsystem `Rng` is forked from it (P1.2). Same-seed map equality is P19.3.
- Needs: P17.1, P1.2
- Test (unit): `Sim.Run › custom_seed_stored_and_forks_rng` — given seed "chiki-1", when two runs are created with it, then both store the seed and their map `Rng` streams are identical.

#### P17.3 Meta progression on the profile

- PRD: 3.9.2
- Does: The profile's meta container (P15.1) holds Charm unlocks, card unlocks (Global Binder ids), Imprint-pool unlocks, difficulty modifiers and cosmetics as sets of ids; it survives run end and reload.
- Needs: P15.1, P16.7
- Test (integration): `Client.Meta › unlocks_survive_reload` — given a Charm unlocked on profile "A", when the profile is saved and reloaded, then the unlock is present.

#### P17.4 Equip Charms pre-run

- PRD: 3.9.6
- Does: Before a run starts the player chooses 0, 1 or 2 Charms from the profile's unlocked set into two slots; a third is rejected; an unowned Charm is rejected; the choice is fixed once the run starts.
- Needs: P17.3, P17.1
- Test (unit): `Sim.Run › equip_up_to_two_owned_charms` — given two unlocked Charms, when both are equipped, then the run holds 2; a third is rejected; an unowned id is rejected; changing after start is rejected.

#### P17.5 Run start state

- PRD: 3.2.2
- Does: A new run has the starter Binder auto-filled into both lines (P16.3), the equipped Charms (P17.4), zero Imprints, ARD at maximum, Base DMG 0, Essence 0, CRP 0.
- Needs: P17.4, P16.3
- Test (unit): `Sim.Run › start_state` — given a new run with one Charm, when inspected, then loadout is full, Charms 1, Imprints 0, ARD 300 of 300, Base DMG 0, Essence 0, CRP 0.

#### P17.6 CRP range

- PRD: 3.8.1
- Does: CRP on `RunStats` starts at 0 and is clamped to 0..100 on every change; the visibility half is P23.4.
- Needs: P1.4
- Test (unit): `Sim.Crp › clamped_0_to_100` — given CRP 98, when +5 is applied, then it is 100; given 2, when −5 is applied, then 0.

### Phase 18 — A run ends

*Delivers everything that happens when a run finishes: resets, Binder discard, Imprints lost, Charm triggers, death as a run outcome, and relationship data. Done when every P18 test is green and the suite passes.*

#### P18.1 Run progression resets

- PRD: 3.9.1
- Does: Ending a run (Won or Died) discards its Binder, Imprints, Essence, route, armor upgrades and CRP; a new run starts from P17.5 state regardless of the previous run.
- Needs: P17.5
- Test (unit): `Sim.Run › new_run_after_end_is_fresh` — given a run ended with Essence 300 and CRP 60, when a new run starts on the same profile, then Essence is 0 and CRP is 0.

#### P18.2 Binder persists for the run and dies with it

- PRD: 3.5.4
- Does: Every acquired card enters the run's Binder and stays across battles; at run end the Binder is discarded; card instances never carry into the next run.
- Needs: P18.1, P16.3
- Test (unit): `Sim.Binder › acquired_card_persists_until_run_end` — given a card acquired after battle 1, when battle 2 begins, then it is in the Binder; when the run ends and a new one starts, then it is not.

#### P18.3 Imprints in a run

- PRD: 3.9.3
- Does: An `AcquireImprint(tier)` rolls one from the fixture pool of that tier using the run's `Rng`; there is no slot limit; a stackable Imprint acquired twice registers two effects; all are lost at run end. Sources are P21.2 and P21.4.
- Needs: P16.6, P17.2, P18.1
- Test (unit): `Sim.Imprints › no_limit_and_lost_at_end` — given 7 Imprints acquired, when the run holds them, then all 7 effects are registered; when the run ends, then the next run has 0.
- Test (unit): `Sim.Imprints › stackable_stacks` — given a stackable +2 Base DMG Imprint acquired twice, when read, then Base DMG is 4.

#### P18.4 Charm triggers

- PRD: 3.9.8
- Does: Equipped Charms register their effects with the framework at battle start; a Charm fires the moment its trigger event (chiefly PerfectDefense at BattleEnded) is appended; an effect marked "until reset" persists across battles and is cleared when the run ends.
- Needs: P16.7, P17.4, P4.7, P18.1
- Test (unit): `Sim.Charms › clean_victory_pays_on_perfect_defense` — given Clean Victory equipped, when a battle ends with Perfect Defense, then Essence rose by 10; when it ends with damage taken, unchanged.
- Test (unit): `Sim.Charms › until_reset_lasts_the_run` — given an until-reset effect fired in battle 1, when battle 2 starts, then it still applies; when a new run starts, it does not.

#### P18.5 Death ends the run

- PRD: 3.3.9.3
- Does: A battle outcome of Died (P3.8) sets the run status to Died and ends it through P18.1.
- Needs: P3.8, P18.1
- Test (unit): `Sim.Run › battle_death_ends_run` — given a run in progress, when a battle ends Died, then the run status is Died and no further node can be entered.

#### P18.6 NPC relationship entity

- PRD: 4.12
- Does: A `Relationship` record per NPC (Fisherman, Flower Girl, Gambler, Björn, Gero) on the profile: level, RP toward next level, unlocks granted.
- Needs: P15.1
- Test (integration): `Client.Relationships › five_records_on_new_profile` — given a fresh profile, when relationships are read, then five records exist at level 1 with 0 RP.

#### P18.7 RP levels

- PRD: 3.10.3
- Does: Main NPCs level 1–10, secondary 1–5; level N to N+1 costs N+1 RP; levels are capped; RP persists across runs on the profile.
- Needs: P18.6
- Test (unit): `Sim.Relationships › level_up_costs_n_plus_1` — given Flower Girl at level 1, when 2 RP are added, then level 2 with 0 toward next; when 2 more, still level 2 with 2 toward level 3.
- Test (unit): `Sim.Relationships › secondary_caps_at_5` — given Björn at level 5, when RP is added, then level stays 5.

#### P18.8 RP gain recorded

- PRD: 3.10.4
- Does: A `GrantRp(npc, amount, source)` API records the gain with its source (dialogue, minigame, sacrifice, interaction) and applies P18.7; no in-scope feature calls it in play yet.
- Needs: P18.7
- Test (unit): `Sim.Relationships › grant_records_source` — given a grant of 3 RP from "sacrifice", when read, then RP toward next is 3 and the last source is "sacrifice".

### Phase 19 — The map

*Delivers seeded World graphs with the PRD's shape and distribution, forward-only movement, and the Corruption clock that movement drives. Done when every P19 test is green and the suite passes.*

#### P19.1 Graph shape

- PRD: 3.2.5
- Does: `MapGenerator.Generate(rng, world)` builds a layered directed graph: one entry node; the first layer branches into 2–3 routes; every route shares a node with another route at least once before the last layer; no path has more than 4 consecutive nodes with a single outgoing edge; the last layer is exactly one Boss node. Generation retries with the next `Rng` fork until all constraints hold.
- Needs: P1.2
- Test (unit): `Sim.Map › shape_constraints_hold_over_200_seeds` — given 200 seeds, when each graph is generated, then every graph has one entry, 2–3 first-layer routes, at least one reconnection per route, no run of more than 4 choiceless nodes, and one final Boss.

#### P19.2 Size and distribution

- PRD: 3.2.6
- Does: A graph has 55–70 nodes; every entry-to-Boss path has 13–17 nodes before the Boss; node types are assigned so that per-World percentages fall inside the table in 3.2.6 (Shop, Event, Blacksmith and Forge nodes are generated and typed but resolve as empty stops in this plan).
- Needs: P19.1
- Test (unit): `Sim.Map › size_and_distribution_over_200_seeds` — given 200 World 1 seeds, when generated, then every graph has 55–70 nodes, path lengths of 13–17, and type shares inside the World 1 bands.

#### P19.3 Same seed, same map

- PRD: 3.2.4
- Does: The generator draws only from its forked `Rng`, so two runs with one seed produce identical graphs, node types and node content rolls (enemy per battle node).
- Needs: P19.2, P17.2
- Test (unit): `Sim.Map › same_seed_identical_graph` — given seed "chiki-1", when two runs generate World 1, then the graphs, types and enemy assignments are equal node for node; a different seed differs.

#### P19.4 Forward-only movement

- PRD: 3.2.7
- Does: `Run.MoveTo(node)` succeeds only for a node connected forward from the current node; there is no move backward; committing raises a NodeTransition event that P19.5 and P22.1 subscribe to.
- Needs: P19.1, P17.1
- Test (unit): `Sim.Map › only_forward_neighbours_allowed` — given the current node with two forward neighbours, when moving to each, then it succeeds; when moving to the previous node or a non-neighbour, then it is rejected.

#### P19.5 Corruption per transition

- PRD: 3.8.2
- Does: Every NodeTransition adds +1 CRP through P17.6.
- Needs: P19.4, P17.6
- Test (unit): `Sim.Crp › plus_one_per_transition` — given CRP 0, when 5 transitions are made, then CRP is 5.

#### P19.6 CRP changes carry source and amount

- PRD: 3.8.6
- Does: Every CRP change emits a CrpChanged event with amount and source ("node transition", or the card, Imprint or Charm id); the presenter (P23.4) shows it.
- Needs: P19.5
- Test (unit): `Sim.Crp › change_event_has_source` — given a transition, when the event stream is read, then the last CrpChanged has amount 1 and source "node transition".

#### P19.7 Three Worlds

- PRD: 3.2.1
- Does: A run generates World 1 at start and World 2 and 3 on entering them; defeating a World's Boss node advances to the next World's entry; defeating the World 3 Boss sets the run status to Won.
- Needs: P19.2, P17.1
- Test (unit): `Sim.Run › three_worlds_then_won` — given a run, when each World's Boss node is completed in turn, then the World index goes 1, 2, 3 and the status after the third is Won.

### Phase 20 — Normal battles pay out

*Delivers the Normal battle node from entry to reward, with the card classes that govern what a reward may offer. Done when every P20 test is green and the suite passes.*

#### P20.1 Normal battle node

- PRD: 3.2.8
- Does: Entering a Normal node starts a Normal-tier battle against the enemy rolled for that node from the World's pool of fixture Normal enemies (P10.4); on Won, the reward flow (P20.3) runs before the run continues.
- Needs: P19.4, P10.4, P3.7
- Test (unit): `Sim.Nodes › normal_node_fights_pool_enemy_then_rewards` — given a Normal node rolled to the Normal Tank, when entered and won, then the battle was Normal tier against that enemy and a reward offer is pending.

#### P20.2 Essence income

- PRD: 3.7.5
- Does: On Won, Essence rolled from the run `Rng` inside the tier × World band from 3.7.5 is added to `RunStats`.
- Needs: P20.1, P9.2
- Test (unit): `Sim.Rewards › essence_inside_band` — given 200 Normal wins in World 1, when incomes are collected, then every value is 8–17; in World 3, 18–37.

#### P20.3 Choose one of three cards

- PRD: 3.7.2
- Does: The Normal reward offers 3 distinct cards rolled from the eligible pool (Common or Uncommon, P20.4 and P20.5), the player picks 1 or skips, and the pick enters the Binder (P18.2).
- Needs: P20.2, P18.2, P8.8
- Test (unit): `Sim.Rewards › pick_one_of_three_into_binder` — given a Normal win, when the offer is read, then it has 3 distinct Common or Uncommon cards; when one is picked, then it is in the Binder and the others are not.

#### P20.4 Normal class is the standard pool

- PRD: 3.4.14
- Does: The reward pool is every loaded card of class Normal in the allowed rarities; Normal cards follow ordinary economy rules (nothing extra in this plan).
- Needs: P20.3
- Test (unit): `Sim.Rewards › pool_is_normal_class` — given a card set with Normal, Event and Unstable cards, when the reward pool is built, then it contains every Normal card and no other.

#### P20.5 Event cards never offered

- PRD: 3.4.15
- Does: Cards of class Event are excluded from battle-reward offers (and, when shops exist, from shop stock); over many rolls none appears.
- Needs: P20.4
- Test (unit): `Sim.Rewards › event_class_never_in_offers` — given a set with 3 Event cards, when 500 Normal offers are rolled, then no Event card appears.

#### P20.6 Reward flow runs on win

- PRD: 3.3.9.2
- Does: The BattleEnded(Won) event triggers the node's reward flow exactly once; the run cannot move until the flow is resolved.
- Needs: P20.3, P19.4
- Test (unit): `Sim.Nodes › move_blocked_until_reward_resolved` — given a won battle with an open offer, when MoveTo is called, then it is rejected; after picking or skipping, accepted.

### Phase 21 — Elites and bosses

*Delivers the two harder battle nodes, their rewards, Imprint drops and the boss's mark on the profile. Done when every P21 test is green and the suite passes.*

#### P21.1 Elite node

- PRD: 3.2.9
- Does: Entering an Elite node starts an Elite-tier battle against the fixture Elite; on Won, the Elite reward (P21.2) runs.
- Needs: P20.1, P10.4
- Test (unit): `Sim.Nodes › elite_node_fights_elite` — given an Elite node, when entered and won, then the battle tier was Elite and the Elite reward is pending.

#### P21.2 Elite reward

- PRD: 3.7.3
- Does: One card of Rare or Legendary rarity from the Normal-class pool is offered (take or skip), one Imprint is acquired at a tier rolled Common 50%, Uncommon 35%, Rare 15%, and Elite-band Essence is added.
- Needs: P21.1, P18.3, P20.2
- Test (unit): `Sim.Rewards › elite_reward_card_imprint_essence` — given an Elite win in World 1, when resolved, then the offered card is Rare or Legendary, Imprints held rose by 1, and Essence rose by 25–35.

#### P21.3 Boss node

- PRD: 3.2.10
- Does: Entering the Boss node starts a Boss-tier battle against the fixture Boss; on Won, the Boss reward (P21.4) runs, then the next World is entered or the run is Won after World 3 (P19.7).
- Needs: P20.1, P10.4, P19.7
- Test (unit): `Sim.Nodes › boss_node_advances_world` — given the World 1 Boss node, when entered and won and the reward resolved, then the World index is 2 and the run is on World 2's entry node.

#### P21.4 Boss reward

- PRD: 3.7.4
- Does: One Rare or Legendary card is offered, one Imprint is acquired (tier rolled as in P21.2), Charm unlock progress is recorded (P21.5), and Boss-band Essence is added.
- Needs: P21.3, P18.3, P20.2
- Test (unit): `Sim.Rewards › boss_reward_card_imprint_essence` — given a Boss win in World 1, when resolved, then the offer is Rare or Legendary, Imprints held rose by 1, and Essence rose by 40–50.

#### P21.5 Boss defeat counts toward Charm unlocks

- PRD: 3.9.10
- Does: Each Boss defeat increments the profile's bosses-defeated count and evaluates every Charm's unlock condition (P16.7); newly satisfied Charms are added to the meta unlocks (P17.3) immediately.
- Needs: P21.4, P17.3, P16.7
- Test (integration): `Client.Meta › first_boss_unlocks_fixture_charms` — given a profile with no unlocks, when the World 1 Boss is defeated, then bosses-defeated is 1 and both fixture Charms are unlocked.

### Phase 22 — Save, resume, log

*Delivers the durability rules: the run is saved at every transition, never mid-battle, unlocks are written the moment they happen, and every run leaves a log. Done when every P22 test is green and the suite passes.*

#### P22.1 Autosave and resume

- PRD: 3.1.5
- Does: The profile holds at most one run in progress; the run is serialised (P17.1) on every NodeTransition and whenever the map is returned to; loading the profile resumes at the saved node with the saved stats, Binder and loadout; starting a new run while one is in progress is rejected.
- Needs: P17.1, P19.4, P15.1
- Test (integration): `Client.Save › resume_at_last_node` — given a run moved to node 5 with Essence 40, when the game is restarted, then the run resumes at node 5 with Essence 40.
- Test (integration): `Client.Save › one_run_per_profile` — given a run in progress, when a new run is requested, then it is rejected.

#### P22.2 No mid-battle save

- PRD: 3.1.6
- Does: Battle state is never written to the save; quitting during a battle and resuming restarts that battle at beat 0 with ARD, Essence and CRP as they were at battle start.
- Needs: P22.1
- Test (integration): `Client.Save › quit_mid_battle_restarts_battle` — given a battle at beat 12 with ARD 200 (was 260 at start), when the game quits and resumes, then the battle is at beat 0 and ARD is 260.

#### P22.3 Versioned schema

- PRD: 4.2
- Does: The run and profile files carry a schema version; loading an older version runs a migration table; loading a newer version than the build knows refuses with a clear error instead of corrupting.
- Needs: P17.1, P15.1
- Test (unit): `Sim.Save › migrates_old_version` — given a version-1 run file lacking a field added in version 2, when loaded, then the field has its default and the file version reads 2.
- Test (unit): `Sim.Save › refuses_newer_version` — given a version-99 file, when loaded, then loading fails with a version error and the file is untouched.

#### P22.4 Unlocks written when earned

- PRD: 3.1.8
- Does: A Charm unlock (P21.5) and an RP grant (P18.8) each write the profile to disk synchronously before returning, so a crash in the following battle loses nothing earned.
- Needs: P21.5, P18.8, P15.1
- Test (integration): `Client.Save › unlock_survives_simulated_crash` — given a Boss defeat and an RP grant, when the process is terminated without a normal save and the profile is reloaded, then the Charm unlock and the RP are present.

#### P22.5 Run log file

- PRD: 3.15.1
- Does: At run end a JSON log is written under the profile's run-log folder with seed, difficulty modifiers, Assist flag, the ordered route, and per battle: enemy id, duration in beats and seconds, judgment counts per grade, damage taken, Signatures fired, cards played per slot, outcome. It contains no profile name and no OS user name.
- Needs: P18.1, P22.1
- Test (integration): `Client.RunLog › log_written_with_fields_and_no_identity` — given a run of 3 battles ended by death, when the log is read, then it has 3 battle records with every listed field, the seed, and no occurrence of the profile name or user name.

#### P22.6 Repeated-enemy flag

- PRD: 3.15.2
- Does: Each battle record carries `sameEnemyAsPrevious`, true when its enemy id equals the previous battle's.
- Needs: P22.5
- Test (unit): `Sim.RunLog › same_enemy_flagged` — given battles against Tank, Tank, Aggressor, when logged, then the flags are false, true, false.

### Phase 23 — The loop on screen

*Delivers the playable stage-four build: a map to move on, one input into battle, a Binder to edit, CRP on screen, and a run-end screen. Done when every P23 test is green and the suite passes.*

#### P23.1 Minimal map view

- PRD: — (groundwork for P23.2 to P23.5)
- Does: A map scene drawing the current World's nodes and connections with type icons, the player marker, forward neighbours selectable with the keyboard, and a settings entry (P15.3); selecting a neighbour calls `Run.MoveTo` and opens the node.
- Needs: P19.4, P12.1
- Test (integration): `Client.Map › select_neighbour_moves` — given the map at the entry node, when the first neighbour is selected, then the run's current node is that neighbour and the node's scene opens.

#### P23.2 Keep previous loadout, one input

- PRD: 3.5.6
- Does: Opening a battle node shows a pre-battle panel with the previous loadout kept; pressing Enter starts the battle immediately; an Edit action opens the Binder (P23.3).
- Needs: P23.1, P16.4
- Test (integration): `Client.Loadout › enter_starts_battle_with_kept_loadout` — given a battle node opened after a previous battle, when Enter is pressed once, then the battle starts and its loadout equals the previous battle's.

#### P23.3 Binder editing screen

- PRD: 3.5.5
- Does: A Binder screen listing every card with its preview; the 16 slots can be cleared and filled by keyboard; Confirm is disabled while any slot is empty and lists the empty slots.
- Needs: P23.2, P16.4
- Test (integration): `Client.Loadout › cannot_confirm_with_empty_slot` — given a slot cleared, when Confirm is attempted, then it is disabled and the panel names the slot; after refilling, Confirm proceeds to battle.

#### P23.4 CRP on screen

- PRD: 3.8.1
- Does: CRP is shown on the map header and in the battle HUD; a CrpChanged event (P19.6) shows a floating "+1 node transition" style label.
- Needs: P23.1, P14.1, P19.6
- Test (integration): `Client.Crp › visible_on_map_and_battle_with_change_label` — given CRP 7, when the map and then a battle render, then both show 7; when a transition happens, then a label reading "+1" with "node transition" appears.

#### P23.5 Run-end screen

- PRD: 3.9.11
- Does: When the run status becomes Won or Died, a run-end screen shows the outcome, the seed, run stats (battles, Perfect Defenses, Essence earned, CRP peak) and every unlock granted this run; Continue returns to the pre-run screen with the profile already saved.
- Needs: P23.1, P18.1, P22.4
- Test (integration): `Client.RunEnd › death_shows_summary_and_returns` — given a run that ends by death after unlocking a Charm, when the screen renders, then it shows "Died", the seed, the stats and the Charm; when Continue is pressed, then the pre-run screen is open and the profile file contains the unlock.
- Test (integration): `Client.RunEnd › victory_shows_won` — given the World 3 Boss defeated, when the screen renders, then it shows "Won".
