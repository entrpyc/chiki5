# CLAUDE.md — Chiki

Chiki is a single-player rhythm roguelike deckbuilder for Windows and macOS (Steam). Canonical comparison for any copy: *Slay the Spire × Crypt of the NecroDancer*. The player builds a 16-card loadout on a fixed keyboard layout and answers charted enemy actions on the beat; timing judgment governs both what the card does and how much damage comes in.

## Source of truth, in order

1. `docs/project/prd.md` — **what** the product does. Every behaviour has exactly one number (3.4.12, 6.7). Cite numbers; never restate a rule.
2. `docs/plan.md` — **what to build next** and the test that proves it. Phases are sequential; items are `P<phase>.<n>`.
3. Codebase

If something is not in these documents, it is not a requirement. Do not invent features. If a requirement is ambiguous, look in this order: PRD text → the plan item's `Does`. If still unclear, ask; do not guess silently. If you must assume, write the assumption into the plan item's `Does` line.

## How work is done

- Take the **first item in `docs/plan.md` that is not marked ✅**, in phase order. Do not skip ahead; a phase never needs a later phase.
- Read the item's PRD number(s) in full before writing code. The item's `Does` is the contract; its `Test` lines are the acceptance tests, named exactly as written (`Suite › test_name` becomes NUnit fixture `Suite`, method `test_name`).
- An item is **done only when every listed test is green and the whole suite passes**. Then prefix the item heading with ✅. A phase gets ✅ when all its items have it. Never mark on any other evidence.
- Never renumber, reorder or delete items or requirements. A withdrawn item keeps its id and the word *withdrawn*.
- Commit messages reference the item id (`P3.4: efficacy matrix`). Commit only when asked and don't add yoruself as contributor.

## Architecture rules (non-negotiable)

**Two halves.** `sim/Chiki.Sim` is a plain .NET (netstandard2.1) class library holding every rule in PRD section 3. `client/` is the Unity project. The client references the simulation as a built DLL; the simulation references nothing but the base class library. A test (P1.3) fails the build if `Chiki.Sim` references `UnityEngine`.

**Simulation rules**

- Integer or fixed-point (thousandths) arithmetic only. One rounding helper, applied once, halves round up (PRD 3.3.4.5). No `float`/`double` in rule code.
- One seeded PRNG type (`Rng`), passed explicitly and forked per subsystem (`rng.Fork("map")`). Never `System.Random`, `Guid`, `DateTime` or `UnityEngine.Random` inside the simulation. Same seed + same inputs = identical run (PRD 6.8).
- All durations are in beats (PRD 3.3.1.4). Positions are integers in quarter beats. No API takes seconds or milliseconds except the beat map, which converts.
- Every state change appends a typed event to the battle's ordered stream (PRD 4.8). Presenters, Charm triggers, run logs and tests read the stream; they never poke state.
- The effect framework (P8.1) is the only way cards, Imprints, Charms, enemy abilities and traits act: trigger + condition + modifier + lifetime. Do not special-case a card in code.
- Content is JSON under `data/` loaded at start and validated in the suite; a card, enemy or chart that breaks a PRD rule fails `dotnet test`. Definitions never reference art.
- Tuning constants (judgment window widths, Signature damage 30, ARD baseline 300, AvgCardDMG per World, Imprint tier odds) live in one file, `sim/Chiki.Sim/Tuning.cs`, each with the PRD number in a comment. Never inline them.

**Client rules**

- The only clock is `AudioSettings.dspTime` through the `BeatClock`. Inputs are stamped with audio time at the event, not the frame. Anything that moves with the music (Rhythm Line, sprite frames, glows) reads the beat clock, never `Time.time`, `Time.deltaTime` or Animator time.
- Never touch the audio source's pitch, position or pause state from gameplay code (PRD 3.3.1.6). The track and the chart loop together from the start (3.6.32).
- Keys are bound by physical position (scancode) and never rebindable. A/S Ability, D/F Left Attack, J/K Right Attack, L/; Defense, Left Shift line switch, Space+key Signature send. Space alone does nothing.
- Rules never live in a `MonoBehaviour`. The client presents state and forwards inputs through `BattleDriver`; if you find yourself computing damage in the client, stop.
- uGUI only. Visual lookups go through `VisualCatalogue` ScriptableObjects (id → sprite/prefab). HD art at 100 PPU, frame-by-frame sheets driven by `BeatAnimator`.
- Nothing leaves the machine except Steam Cloud save. No telemetry, no analytics, no network calls (PRD 6.5, 6.6).

## Repository layout

```
docs/project/prd.md          requirements (the contract)
docs/plan.md                 build order and tests
docs/project/unity-setup.md  tech decisions
decisions.md                 decision log
sim/Chiki.sln                simulation + NUnit tests
sim/Chiki.Sim/               rules library (no engine)
sim/Chiki.Sim.Tests/         one fixture per Suite name in the plan
client/                      Unity project
client/Assets/_Project/      Art Audio Data Prefabs Scenes Scripts UI
client/Assets/Tests/         EditMode and PlayMode assemblies
data/sets/ data/enemies/ data/charts/ data/tracks/   JSON content and fixtures
```

Assemblies: `Chiki.Sim`, `Chiki.Client`, `Chiki.Client.Editor`, `Chiki.Client.Tests.EditMode`, `Chiki.Client.Tests.PlayMode`. Scenes: persistent `Boot` plus additive `Map`, `Battle`, `Binder`, `Calibration`, `RunEnd`.

## Commands

```
dotnet test sim/Chiki.sln
Unity -batchmode -nographics -projectPath client -runTests -testPlatform PlayMode -testResults results.xml
node ~/.claude/skills/project-prd/check.js docs/project/prd.md          # after editing the PRD
node ~/.claude/skills/project-plan/check.js docs/plan.md docs/project/prd.md   # after editing the plan
```

Run the relevant checker after any edit to the PRD or plan; both must print `OK`.

## Vocabulary

Use the PRD's terms exactly. **Canonical:** ARD (player HP pool), CRP (Corruption), Essence, Block, Base DMG, Binder, Loadout, Rhythm Line, Judgment Window, Signature Chain / Signature, Charm (permanent meta), Imprint (run-scoped), Trait, enemy action, chart, tempo map, World, node.

**Forbidden in code, UI and docs:** *deck* (except in the genre phrase "roguelike deckbuilder"), *relic*, *gold*, *HP* for the player, *Action Beat*, *Rest Beat*, *Pulse Line*, *Pulse Core*, *Combo Chart*, *Chain Meter*, *sleeve*, *energy*. Identifiers follow the canonical terms (`RunStats.Ard`, `CrpChanged`, `SignatureChain`).

## Coding conventions

- C# 9, nullable enabled, `Chiki.Sim.*` and `Chiki.Client.*` namespaces mirroring folders. Records for definitions and events; classes for aggregates (`Battle`, `Run`, `Binder`, `Loadout`).
- One test fixture per plan `Suite` (`Sim.Resolve`, `Client.Input`, …); method names as in the plan; a test asserts the given/when/then sentence and nothing else.
- Public simulation API is the only surface tests use; no `InternalsVisibleTo` for rule tests.
- Data files are pretty-printed JSON, ids are lowercase kebab-case (`enemy-ren`, `chart-enemy-ren`), one file per set.
- Sprites: `spr_<kind>_<subject>_<action>_<nn>.png`; audio: `mus_<world>_<track>.ogg` with a JSON sidecar (offset, length in beats, tempo map).
- Player-facing strings go in the string table from the first screen (PRD 3.12.7); never a literal in a presenter.
- Save files carry a schema version and a migration table (P22.3); adding a field means adding a migration.
