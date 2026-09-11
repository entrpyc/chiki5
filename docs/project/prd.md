# Chiki — PRD

| Platform     | Single-player desktop game for Windows and macOS, keyboard-driven (mouse for one minigame), distributed through Steam; a free demo build ships for Steam Next Fest (Oct 19–26, 2026) |
| :----------- | :---------------------------------- |
| Target Users | Players aged 13+ who enjoy rhythm games (Hi-Fi Rush, Crypt of the NecroDancer, OSU) and roguelike deckbuilders (Slay the Spire, Monster Train, Hades); demo audience at Next Fest first, full launch audience after |
| Scope        | Full scope — everything the product is meant to be, with the September demo build called out as a delivery surface (5.3) |

## Implementation status

Counted on 2026-09-11 from the markers in this document: every numbered requirement in sections 3, 5 and 6, plus the section 4 entities. Withdrawn requirements (3.3.1.2, 3.3.2.4) and section 7 are not counted.

| Status | Count | Share |
| :-- | --: | --: |
| ✅ Done | 130 | 50.8% |
| 🔨 Partly done | 47 | 18.4% |
| Not started | 79 | 30.9% |
| Total | 256 | 100% |

## 1. Executive summary

Chiki (working title) is a single-player, rhythm-based roguelike deckbuilder for PC. The player navigates a branching map across three worlds, collects cards into a Binder, binds sixteen of them to a fixed keyboard layout, and fights one enemy at a time in combat that runs on the musical beat of the current track. Player and enemy both act on beats; the enemy telegraphs every action ahead of time on a visible Rhythm Line; and the outcome of each beat depends on which card the player chose to slot and how precisely they pressed it. A run lasts about an hour, ends in death or in victory over the third world's boss, and either way feeds permanent unlocks back into the next run.

The problem the product attacks is that rhythm games reward execution but rarely reward planning, while deckbuilders reward planning but resolve execution with a click. Chiki fuses the two through five design pillars: the eight combat keys never change meaning, only the cards in them do; every action is on the beat and the music is never bent to fit a mechanic; the player has full information about what the enemy will do and when; preparation before a fight and execution during it are two halves of a single skill; and every run is a bargain with Corruption, a run-scoped stat that rises as the player moves and that trades power against risk. Each part of the product serves those pillars: the Binder and Loadout handle preparation, the beat-synced judgment system handles execution, enemies are built to be read, and the economy, map and Corruption systems make the route itself a set of decisions.

Around the core loop sit the systems that make runs replayable: a procedurally generated seeded map per world with shops, events, a blacksmith and forges; run-scoped Imprints and permanent Charms; five recurring NPCs whose relationships persist across runs and unlock content one level at a time; three NPC minigames; and a narrative frame in which failed runs are a time loop and the cute surface hides a dark core. Every rule in this document is meant to map to a unit test in a rules simulation layer, and every value that is content rather than rule lives in a data table.

There is no second audience inside the product: no admin, no operator, no server. The team is the content author, working through data tables and a decision log. The one operational concern is playtesting: the demo exists to answer whether fighting the same enemy twice in a row stays interesting, so the game writes a local summary of every run that a tester can hand to the team.

## 2. Product overview

| | |
| :-- | :-- |
| Product name | Chiki (working title; alternatives Final Knell, Silent Knell, Dirgebound) |
| Product type | Rhythm action / roguelike / deckbuilding card tactics, single-player |
| Target audience | Rhythm-game and roguelike-deckbuilder players, 13+, comfortable with timing-based play and build planning |
| Canonical comparison | Slay the Spire × Crypt of the NecroDancer |
| Platform | Windows and macOS desktop, keyboard-first with mouse for Fishing |
| Access model | Offline single-player; several named local profiles per install, synced through the store's cloud save; no accounts of our own |
| Usage cadence | Runs of about one hour; battles of 30–180 seconds; designed for repeated runs across weeks via permanent unlocks |
| External integrations | Steam store, Steam Cloud save; no other services |
| Content authoring | Data tables (cards, charms, enemies, imprints, traits) plus an append-only decision log; rule IDs map to simulation unit tests |
| Core purpose | Make planning a loadout and executing it on the beat one inseparable skill, inside a replayable roguelike run |

## 3. Features

### 🔨 3.1 Profiles & saves

*Everything permanent in the game belongs to a profile: unlocked Charms and cards, relationship levels with NPCs, calibration and settings, and the run in progress. Several people may share one machine, so profiles are named and separate, and all of them follow the store account through cloud save. There is no server of our own; the store's cloud save is the only thing that leaves the machine.*

**Functional requirements**

- 🔨 **3.1.1** The game holds any number of named profiles on one install and shows a profile picker at launch; the last used profile is preselected so a returning player continues with one input.
  - *Remaining:* no profile picker at launch; Boot always loads one fixed profile named `default`, so the player never names a profile, and there is no last-used preselection or one-input continue.
- ✅ **3.1.2** Each profile owns its own meta progression (3.9.2), NPC relationship levels (3.10.3), settings and calibration offset (3.12.1), tutorial completion (3.13.3), run history and run logs (3.15.1); nothing is shared between profiles.
- 🔨 **3.1.3** A player can create, rename and delete a profile; deletion requires a typed confirmation of the profile name and destroys that profile's meta progression irreversibly.
  - *Remaining:* rename, delete, the typed-name confirmation and the irreversible deletion of meta progression; creation exists only in code, with no create screen.
- **3.1.4** All profiles on the install are synced through the store's cloud save (5.2.2); when the local and cloud copies conflict, the player is shown both with their last-played time and chooses which to keep.
- ✅ **3.1.5** A profile holds at most one run in progress; the run is saved automatically at every node transition (3.2.7) and whenever the player returns to the map, and resuming returns the player to the map at the last saved node.
- ✅ **3.1.6** A battle is never saved partway: quitting during a battle restarts that battle from its first beat on resume, with ARD, Essence and CRP as they were when the battle began.
- **3.1.7** From the map the player can abandon the run in progress after a confirmation; an abandoned run ends as a death (3.9.11).
- ✅ **3.1.8** Meta unlocks (3.9.2) and relationship points (3.10.4) are written to the profile at the moment they are earned, so a crash loses at most the battle in progress.

### 🔨 3.2 Run & map

*A run is three worlds in a row, each a seeded, forward-only node graph that ends at that world's boss. The map is where the player spends Corruption to move, chooses between risk and safety, and reads their own state at a glance. Node behaviours belong to the features that own them; the map owns the graph, the movement rules and the player's run-wide stats.*

**Functional requirements**

- ✅ **3.2.1** A run consists of three Worlds played in order; each World is a separate node graph that ends at its Boss (3.2.10), and defeating the World 3 Boss wins the run (3.9.11).
- ✅ **3.2.2** A run starts with the starter Binder (3.5.3), the 0–2 Charms the player equipped pre-run (3.9.6), zero Imprints, ARD at its maximum, Base DMG 0, Essence 0 and CRP 0 (3.8.1).
- ✅ **3.2.3** The player's run-wide stats are: ARD (armor durability, the player's HP pool) with a baseline maximum of 300; Base DMG, where every +1 adds +1 damage to every attack card (3.3.4.3); Essence (3.7.1); and CRP (3.8.1). Current ARD carries between battles and is restored only by repair (3.7.12), cards and events; maximum ARD can be raised by Charms, events and relationship unlocks.
- ✅ **3.2.4** Every run has a seed; the player may enter a custom seed before starting, and the same seed produces the same path shapes, node layout, props, event distribution, enemy variation and shop stock. The seed is shown on the map and on the run-end screen (3.9.11) so it can be shared.
- ✅ **3.2.5** Each World generates one entry node; early nodes branch into 2–3 routes; every route reconnects with another at least once before the Boss; no path runs more than 4 nodes without offering a choice; and the final node is always the Boss.
- ✅ **3.2.6** A World graph holds 55–70 nodes in total, and a single traversal visits about 15 nodes before the Boss. Node types are distributed per World within these bands (percent of generated nodes):

| Node | World 1 | World 2 | World 3 |
| :-- | :-- | :-- | :-- |
| Normal battle | 50–60 | 40–50 | 40–50 |
| Elite | 5–10 | 8–18 | 10–15 |
| Shop | 5–15 | 5–15 | 5–15 |
| Event | 15–20 | 15–20 | 10–15 |
| Blacksmith | 5–15 | 5–15 | 5–15 |
| Forge | 5–15 | 8–18 | 10–20 |
| Boss | 1 (final) | 1 (final) | 1 (final) |

- ✅ **3.2.7** Movement is node to node along a connection, forward only, with no backtracking; the player must pick exactly one branch, and committing to a node is a node transition that adds Corruption (3.8.2) and saves the run (3.1.5).

Node catalogue — each row is a node type the map can generate, and the feature that defines what happens inside it:

| # | Node | What happens on entry |
| :-- | :-- | :-- |
| ✅ 3.2.8 | Normal battle | A Normal-tier battle (3.3.9.1) against one enemy from the World's pool; on victory, Normal rewards (3.7.2). |
| ✅ 3.2.9 | Elite | An Elite-tier battle (3.3.9.1); on victory, Elite rewards (3.7.3). |
| ✅ 3.2.10 | Boss | A Boss-tier battle (3.3.9.1); on victory, Boss rewards (3.7.4) and the next World unlocks, or the run is won after World 3 (3.2.1). |
| 🔨 3.2.11 | Shop | Gero's shop (3.7.6). |
| 🔨 3.2.12 | Event | A non-combat choice encounter (3.10.1); NPC events may open a minigame (3.11.1). |
| 🔨 3.2.13 | Blacksmith | Björn's blacksmith: repair or upgrade armor (3.7.11). |
| 🔨 3.2.14 | Forge | Resolves randomly on entry into Upgrade, Trait or Sacrifice (3.4.17). |

- *Remaining, 3.2.11:* the node is generated and can be entered but resolves as an empty stop; Gero's shop (3.7.6) is not built.
- *Remaining, 3.2.12:* the node resolves as an empty stop; no choice encounter (3.10.1) and no minigame (3.11.1).
- *Remaining, 3.2.13:* the node resolves as an empty stop; no repair or armor upgrade (3.7.11).
- *Remaining, 3.2.14:* the node resolves as an empty stop; no random Upgrade, Trait or Sacrifice outcome (3.4.17).

- **3.2.15** Each World has a unique biome art set, node decorations, palette, soundtrack and BPM theme, enemy pool (3.6.1) and environmental storytelling (3.14.2); atmospheric props are placed procedurally relative to the branching shape, so forks fill the wedge between routes and straights densify their sides.
- 🔨 **3.2.16** The map screen shows the player icon, node connections, node type icons with labels, the continent name, the player's ARD, CRP, Base DMG, equipped Charms and Imprints, Essence, the seed (3.2.4), a Binder button (3.5.11) and settings (3.12.1).
  - *Remaining:* the continent name (the header shows "World N"), Base DMG, equipped Charms, Imprints, the Binder button (3.5.11) and node type icons (types are flat colours with labels); no test checks ARD, Essence or the seed on the map. Built: player icon, connections, typed node labels, ARD, CRP, Essence, seed, settings.
- 🔨 **3.2.17** Difficulty scales per World through the enemy pool, reward quality, event difficulty, shop pricing (3.7.7), elite and boss HP scaling, and enemy damage (3.7.16); the enemy HP formula assumes an expected average card damage of 12, 14 and 16 in Worlds 1, 2 and 3 respectively (3.7.15).
  - *Remaining:* per-World enemy pools (every World draws from one pool), reward quality, event difficulty and shop pricing (3.7.7); Elite and Boss HP scale only through AvgCardDMG. Built: AvgCardDMG 12/14/16 and the per-World enemy damage rise.
- 🔨 **3.2.18** The session targets are a Normal battle of 30–60 s, an Elite of 60–90 s, a Boss of 90–180 s (3.3.9.1) and a full run of about one hour across three Worlds including map and menu time.
  - *Remaining:* actual battle length and the one-hour run are never measured; only the authored intended duration per tier is validated.

### 🔨 3.3 Combat

*Combat is turnless, beat-synchronised, card-driven, information-rich and skill-based. The music sets the clock; the enemy's Rhythm Line tells the player what is coming; the player answers each enemy action with one of sixteen slotted cards, or nothing. Enemy actions come from a chart written for that enemy's music, so every battle is a designed piece of music. Timing judgment governs both what the card does and how much damage comes in; category choice governs only what the card does. Everything is measured in beats.*

#### 🔨 3.3.1 Beat framework

- ✅ **3.3.1.1** Every battle is synchronised to the current track's BPM, and a beat timeline (the Rhythm Line) showing the enemy's telegraphed actions (3.6.3) is visible for the whole battle.
- **3.3.1.2** Withdrawn. Player action opportunities are the enemy's charted actions (3.3.1.8); there is no fixed action grid.
- ✅ **3.3.1.3** On each enemy action the player may play exactly one card or take no action; between enemy actions no card can be played.
- ✅ **3.3.1.4** All durations in combat (statuses, cooldowns, effects) are measured in beats, never seconds.
- ✅ **3.3.1.5** Timing windows scale with BPM so perceived difficulty is constant across tracks.
- ✅ **3.3.1.6** The music is never desynchronised, time-stretched, paused or interrupted by a game mechanic; a mechanic that changes rhythm changes the enemy's chart (3.6.13), never the track.
- ✅ **3.3.1.7** Every encounter is one player against one enemy.
- ✅ **3.3.1.8** Every player action opportunity is an enemy action from the enemy's chart (3.6.31): the Judgment Window sits on the action's charted position, the player may respond with one card or nothing (3.3.1.3), and a charted position may fall on a beat or on any quarter-beat subdivision of it.
- ✅ **3.3.1.9** A track carries a tempo map (4.14): its BPM may change at given beat positions and the beat clock follows every change, so chart positions are expressed in beats and quarter beats, never in milliseconds, and timing windows (3.3.1.5) follow the BPM in force at the action.

#### 🔨 3.3.2 Input

- ✅ **3.3.2.1** The key layout is fixed for the entire game: Q and W are Ability slots, E and R Left Attack, U and I Right Attack, O and P Defense (3.4.1). The layout spans two lines of these eight keys, sixteen slots in all (3.5.1); only the cards in the slots ever change.
- ✅ **3.3.2.2** Pressing the line-switch key (3.3.2.6) switches the active line; both lines are always visible and the inactive line is drawn thinner (3.3.5.4).
- ✅ **3.3.2.3** Space held together with a slot key sends that slot's card to the Signature Chain instead of playing it (3.3.6.1); Space is used for nothing else in combat.
- **3.3.2.4** Withdrawn. Line switching and Signature send use different keys (3.3.2.6), so no tap-versus-chord disambiguation exists.
- ✅ **3.3.2.5** Keys are bound by physical position, so a keyboard layout other than QWERTY keeps the same hand shape; on-screen slot labels show the character the physical key actually produces.
- ✅ **3.3.2.6** The line switch is its own dedicated key, V, and is never part of a chord. It takes effect on key-down, at any time including between enemy actions, is never graded, is never a Miss, and starts no cooldown.

#### ✅ 3.3.3 Judgment

- ✅ **3.3.3.1** Each pressed input is graded by its timing against the Judgment Window: Perfect (strict inner window), Good (wider window) or Miss (outside both). A Miss is exclusively the grade of a pressed input.
- ✅ **3.3.3.2** Taking no action on an enemy action consumes no card, starts no cooldown and records no judgment, so Miss-triggered effects (3.6.6) do not fire; the enemy's action resolves at full value (3.3.4.2). Enemies that punish inaction do so through an explicit ability (3.6.8).
- ✅ **3.3.3.3** If the player is Stunned when an enemy action arrives, that action is treated exactly as no input (3.3.3.2).

#### ✅ 3.3.4 Resolution

- ✅ **3.3.4.1** Timing judgment governs both the player card's output and the incoming enemy damage on that beat; category choice governs only what the card does, never how much the player takes.
- ✅ **3.3.4.2** When the enemy attacks on a beat, incoming damage is `EnemyDMG × IncomingMult × StatusMults − Block`, where IncomingMult is 0% on Perfect, 50% on Good, 100% on Miss and 100% on no input; Block depletes first and the remainder hits ARD (3.2.3).
- ✅ **3.3.4.3** The player's card effect is `CardValue × JudgmentMult × (1 + BaseDMG%) × StatusMults`, where JudgmentMult is 100% on Perfect, 50% on Good and 0% on Miss; for Defense cards CardValue is the Block gained and scales the same way. Cards define only their CardValue and modifiers (3.4.10).
- ✅ **3.3.4.4** The player effect is applied according to the efficacy matrix (rows: the charted enemy action being answered; columns: what the player played). Every row is a charted action; there is no idle row because an idle enemy offers no action to answer (3.3.1.8):

| Enemy action / player plays | Defense | Attack, correct side | Attack, wrong side | Ability | Signature send |
| :-- | :-- | :-- | :-- | :-- | :-- |
| Attacks Left or Right | gain Block per card | full card damage (counter) | deals 0 (whiffed side) | effect resolves | card banked |
| Defends | effect resolves | damage reduced by the enemy's defense level | reduced, often 0 | effect resolves | card banked |
| Buffs | effect resolves | full damage | full damage (no wrong side exists) | effect resolves | card banked |

- ✅ **3.3.4.5** Displayed damage and Block values are rounded to the nearest whole number.
- ✅ **3.3.4.6** Timing mitigation applies to damage only: an enemy status application lands regardless of judgment unless a card effect or immunity blocks it.
- ✅ **3.3.4.7** True DMG is dealt directly to HP or ARD, ignoring Block and all reductions.
- ✅ **3.3.4.8** The following edge cases resolve as listed, and each row is one unit test in the rules simulation (6.7):

| Case | Outcome |
| :-- | :-- |
| Perfect, wrong-side attack while the enemy attacks | deal 0; take 0 |
| Good, correct-side counter | deal 50%; take 50% minus Block |
| Miss, any card | deal 0; take 100% minus Block; slot enters cooldown |
| Perfect Ability while the enemy attacks | effect resolves fully; take 0 |
| Good Defense while the enemy attacks | gain 50% of the card's Block; take 50% minus Block |
| No input while the enemy attacks | take 100% minus Block; no cooldown; Miss-triggered effects do not fire |
| Signature send with Perfect timing while the enemy attacks | card banked; take 0 |
| Stun is active when an enemy action arrives | action treated as no input |
| Enemy applies a non-damage debuff on a Perfect beat | debuff lands |

#### 🔨 3.3.5 Slot cooldowns

- ✅ **3.3.5.1** Any pressed card starts its slot's cooldown regardless of judgment, so a Missed card still cools down; each of the 16 slots (key × line) cools down independently; cooldown is 2–6 beats defined per card (3.4.7) and counts down one per beat from the press.
- **3.3.5.2** Enemy abilities may impose or extend slot cooldowns or locks (3.6.4).
- ✅ **3.3.5.3** A slot on cooldown cannot be played: pressing it gives disabled feedback (sound and flash), consumes nothing, starts nothing and is not a Miss.
- ✅ **3.3.5.4** A slot on cooldown shows a dim overlay and a numeric or radial beat countdown; both lines are always on screen and the inactive line is rendered thinner.

#### 🔨 3.3.6 Signature Chain

- ✅ **3.3.6.1** Space plus a slot key (3.3.2.3) banks that slot's card into one of three Signature Chain slots instead of playing it; incoming damage on a banking beat follows timing as normal (3.3.4.2).
- ✅ **3.3.6.2** When three cards are banked the Signature triggers automatically and the chain empties; the Signature deals 30 damage to the enemy. Any other Signature variant requires a decision-log entry.
- ✅ **3.3.6.3** A Missed Signature send still banks the card, and banking starts the slot's cooldown (3.3.5.1).
- **3.3.6.4** Cards banked in the chain can be destroyed by an enemy ability before the Signature triggers (3.6.14).

#### 🔨 3.3.7 Status effects

- ✅ **3.3.7.1** Status durations are in beats; effects resolve after actions unless a status says otherwise; stacks are additive; reapplying a status resets its timer; status icons sit above the HP and ARD bars with tooltips.
- ✅ **3.3.7.2** When several statuses trigger on the same beat they resolve in this order: Stun, then damage multipliers (Weak), then Reflect and Thorns, then damage over time (Bleed) at beat end.

| # | Status | Effect | Duration |
| :-- | :-- | :-- | :-- |
| ✅ 3.3.7.3 | Scar | Per stack, +2% chance that the enemy takes double damage on a Perfect hit | 10 beats per stack |
| ✅ 3.3.7.4 | Weak | Target deals −X% damage (X per source) | 8 beats |
| ✅ 3.3.7.5 | Stun | Target skips its next action beat (player: 3.3.3.3) | 1 action beat |
| ✅ 3.3.7.6 | Bleed | Stacks × 1 damage per beat | 8 beats |
| ✅ 3.3.7.7 | Thorns | The next enemy attack takes X damage; stackable | until consumed |
| 3.3.7.8 | Disarmed | Disables all Trait effects (3.4.19) on the target | per source |

- **3.3.7.9** Some statuses are cleansable by card effects; Elite and Boss enemies carry cleanse resistance so that cleansing is weaker against them.

#### ✅ 3.3.8 Feedback

- ✅ **3.3.8.1** The game gives an audio cue per judgment grade, a key-press flash, a glow on cards whose window is open, Rhythm Line highlights for incoming actions, and light camera shake and effects on heavy hits.
- ✅ **3.3.8.2** Judgment is computed with the profile's calibration offset applied (3.12.1), and an optional metronome can be toggled on (3.12.2).

#### ✅ 3.3.9 Encounter tiers & outcomes

- ✅ **3.3.9.1** Encounters come in three tiers with intended durations and character: Normal, 30–60 s, low complexity and few mechanics; Elite, 60–90 s, stronger patterns and debuffs that demand a deliberate loadout; Boss, 90–180 s, multi-phase with unique telegraphs and rhythm patterns. Enemy HP is derived from the intended duration (3.7.15).
- ✅ **3.3.9.2** A battle is won when the enemy's HP reaches 0; the reward flow for the node then runs (3.2.8, 3.2.9, 3.2.10).
- ✅ **3.3.9.3** The player dies when ARD reaches 0, which ends the run (3.9.11).
- ✅ **3.3.9.4** Perfect Defense is taking zero damage for an entire battle; it is recorded per battle and is the trigger condition for several Charms (3.9.8).
- ✅ **3.3.9.5** Block and all statuses are cleared at the end of a battle; they never carry to the next.

### 🔨 3.4 Cards

*A card is the unit of player action. It belongs to exactly one Category, which fixes where it can be slotted, and one Rarity, which fixes its power band. Card content lives in a data table; this feature holds the rules every card must obey and the three Forge operations that change a card during a run.*

**Functional requirements**

- ✅ **3.4.1** Every card belongs to exactly one Category, which determines its legal slots: Ability (Q, W), Left Attack (E, R), Right Attack (U, I), Defense (O, P) (3.3.2.1).
- **3.4.2** Categories are colour-coded and icon-coded: Attack red, Defense blue, Ability green.
- 🔨 **3.4.3** Left and Right attacks are intentionally symmetric: they differ only by which telegraph they answer, and there are no side archetypes.
  - *Remaining:* a test for the mirror case (tests cover only one direction of the wrong-side answer) and a validation that no card is a side archetype; efficacy already treats both sides alike.
- ✅ **3.4.4** Every card has exactly one of four Rarities, each with a scaling band that its damage or Block must fall in:

| Rarity | Damage | Block | Role |
| :-- | :-- | :-- | :-- |
| Common | 8–12 | 5–10 | reliable baseline, simple effects, early-game bulk |
| Uncommon | 12–16 | 10–16 | synergy-focused or conditional power, mid-run |
| Rare | 16–22 | 16–21 | run-defining, unique mechanics, limited availability |
| Legendary | 22–26 | 22+ | ultra-limited (3.4.5) |

- **3.4.5** Legendary is an ultra-limited tier: 3–4 Legendary cards exist in the whole game.
- **3.4.6** The card pool targets a rarity distribution of 25% Common, 45% Uncommon and 30% Rare, plus the Legendary cards (3.4.5).
- ✅ **3.4.7** Card anatomy is: name, Category, Rarity, damage or value (Base-DMG-scaled, 3.3.4.3), cooldown in beats (3.3.5.1), status effects applied, special rules and conditions, and optional flavor text written in the myth register (3.14.6).
- ✅ **3.4.8** An effect may appear only on the categories, and at or above the minimum rarity, given here:

| Effect | Attack | Defense | Ability |
| :-- | :-- | :-- | :-- |
| Bleed | Common | — | Common |
| Weak | Common | — | Common |
| Scar | Common | — | Common |
| Thorns | — | Uncommon | Uncommon |
| Stun | Rare | — | Rare |
| Block (attack cards that also grant Block) | Common | — | — |
| Disarmed | Uncommon | — | Uncommon |
| Repair (restore ARD) | — | Uncommon | Uncommon |
| True DMG | Rare | — | — |

- ✅ **3.4.9** Card mechanics are of four kinds: direct damage; scaling damage that grows with player buffs or with CRP (3.8.8); status application (3.3.7.3 to 3.3.7.8); and reaction effects with a condition such as "on Perfect", "if this kills" or "if the enemy is attacking this beat".
- ✅ **3.4.10** The damage formula is defined once (3.3.4.3); a card defines only its CardValue and modifiers and never restates the formula.
- 🔨 **3.4.11** Cards are acquired from battle rewards (3.7.2), elite rewards (3.7.3), boss rewards (3.7.4), shops (3.7.6), events (3.10.2) and Fishing (3.11.4).
  - *Remaining:* cards from shops (3.7.6), events (3.10.2) and Fishing (3.11.4); Normal, Elite and Boss rewards already grant cards.
- 🔨 **3.4.12** An acquired card goes into the Run Binder (3.5.4) and is available from the next loadout selection.
  - *Remaining:* a test that an acquired card can be slotted at the next loadout selection; entering the Run Binder is built and tested.
- ✅ **3.4.13** A card is of exactly one class:

| # | Class | Source | Rule |
| :-- | :-- | :-- | :-- |
| ✅ 3.4.14 | Normal | battle rewards, shops | standard pool; normal upgrade and economy rules |
| ✅ 3.4.15 | Event | Events and Sacrifice only | unique effects; never offered in shops or battle rewards |
| ✅ 3.4.16 | Unstable | Events and Sacrifice only | has a lifespan of N battles (typically 2–3), counted down each battle it is in the Binder, then destroyed |

- **3.4.17** A Forge node (3.2.14) resolves randomly on entry into one of Upgrade (3.4.18), Trait (3.4.19) or Sacrifice (3.4.20).
- 🔨 **3.4.18** Upgrade: the player picks one Binder card and its damage or Block value is improved by that card's defined upgrade step; a card can be upgraded once per run.
  - *Remaining:* the Forge flow that offers the upgrade, a test, and combat reading the upgraded value (battle resolves the card definition's value, so an upgraded card still deals its base value). Built: one upgrade per card, adding the card's upgrade step.
- **3.4.19** Trait: the player is offered three Traits plus one "remove Trait" option and picks one to apply to one Binder card; a card holds at most one Trait, and the Trait lasts until the run ends. The Trait pool is content (4.11).
- **3.4.20** Sacrifice: the player destroys one Binder card and chooses either a CRP shift of 1 (Common), 3 (Uncommon), 5 (Rare) or 10 (Legendary) in the direction they pick (3.8.3), or one random Event Card (3.4.15).
- ✅ **3.4.21** Every card in the data table must comply with the rules of this feature; a card that violates them is redesigned or removed before it ships.

### 🔨 3.5 Binder & loadout

*There is no draw pile and no hand. The player owns a Binder, the cards collected this run, and builds a Loadout of sixteen from it. Entering a battle is one input by default; the loadout screen is offered, never forced, because thirty-second battles cannot afford a menu before each one.*

**Functional requirements**

- ✅ **3.5.1** The Battle Loadout is exactly 16 cards across two lines of the eight key slots (3.3.2.1); all slotted options are visible at all times and nothing is drawn or hidden.
- ✅ **3.5.2** Each line holds 2 Ability, 2 Left Attack, 2 Right Attack and 2 Defense cards, so the full loadout is 4 of each; a card can only sit in a slot of its Category (3.4.1), and a Binder card can occupy at most one slot.
- ✅ **3.5.3** Every run begins with a starter Binder of basic cards sufficient to fill both lines, and both lines are filled from it like any loadout.
- ✅ **3.5.4** Every acquired card enters the Run Binder; the Binder persists for the run and is discarded when the run ends. Cards never carry across runs; meta unlocks change the pool (3.5.9), not the Binder.
- ✅ **3.5.5** Before every battle the player may open the Binder, preview cards, and rebuild the 16-card loadout; a loadout with an empty slot cannot enter battle.
- ✅ **3.5.6** The default before a battle is to keep the previous loadout; entering battle is then a single input.
- **3.5.7** The game invites editing, with a highlight rather than a modal, when the Binder has changed since the last battle or when the upcoming enemy's archetype (3.6.1) differs from the last one fought.
- **3.5.8** The upcoming enemy's card (3.6.26) is visible while the loadout is being edited.
- **3.5.9** Cards unlocked permanently join the Global Binder (3.9.2) and can appear as rewards and shop stock in future runs.
- 🔨 **3.5.10** Unstable cards in the loadout show their remaining battle count (3.4.16), and a card destroyed by lifespan, Sacrifice or an event leaves its slot empty until the next loadout edit (3.5.5).
  - *Remaining:* the remaining battle count on Unstable cards in the loadout, destruction by Sacrifice or event, and refusing entry with the emptied slot (the pre-battle panel's Enter throws instead). Built and tested: lifespan destruction empties the slot.
- **3.5.11** The Binder can be opened from the map (3.2.16) to review cards, Traits and Unstable lifespans without editing the loadout.

### 🔨 3.6 Enemies

*Every enemy is a readable puzzle: one role, one rhythm profile, a small budget of abilities and traits, and a Rhythm Line that shows what it will do and when. The player is punished only by execution and preparation, never by hidden information; the one deliberate exception, Fake Move, has its own visual language and a tell that can be learned.*

**Functional requirements**

- ✅ **3.6.1** Every enemy has exactly one role: Aggressor (high damage, 0.8× HP, weak to disruption), Tank (1.2× HP, low damage, Block and stances) or Mentalist (1.0× HP; statuses, control, deceptive actions, damage over time). The multiplier applies to the formula HP (3.7.15).
- ✅ **3.6.2** Every enemy has exactly one rhythm profile: Fast (frequent weak attacks) or Slow (rare heavy attacks).
- ✅ **3.6.3** Every enemy displays its upcoming actions on the Rhythm Line: what the action is, on which beat it lands, and how many beats remain. Any exception is an explicit ability with its own visual language (3.6.11).

Tier capability budget — what each tier may carry:

| Tier | Abilities | Traits | Status effects used |
| :-- | :-- | :-- | :-- |
| Normal | 1 | 0–1 | 0–1 |
| Elite | 1 | 1–2 | 1–2 |
| Boss | 1–2 | 2 | 1–2 |

- ✅ **3.6.4** An enemy carries no more abilities, traits and status effects than its tier's budget in the table above; abilities that lock or extend player slot cooldowns act through 3.3.5.2.

Ability pool — each ability is one row; IDs A08 and A15 are retired and never reused:

| # | ID | Ability | Effect |
| :-- | :-- | :-- | :-- |
| ✅ 3.6.5 | A01 | Rising Tempo | Passive: +3 Base DMG each time this enemy deals damage. |
| ✅ 3.6.6 | A02 | Misstep Pain | Passive: the player takes 5 damage on a Good and 10 on a Miss; no-input beats are exempt (3.3.3.2). |
| 3.6.7 | A03 | Counterblade | Passive: hitting the enemy during its attack parries 50% of the damage back to the player. |
| ✅ 3.6.8 | A04 | Pressure | Passive: if the player takes no action on an enemy action, the enemy's next attack deals 2× damage. The canonical punisher of inaction (3.3.3.2). |
| ✅ 3.6.9 | A05 | Iron Veil | For 5 beats the enemy takes 80% less damage; shown by an icon and a darker Rhythm Line. |
| 3.6.10 | A06 | Backflash Barrier | For 5 beats 25% of damage dealt to the enemy is reflected to the player; icon and darker Rhythm Line. |
| 3.6.11 | A07 | Fake Move | A deceptive telegraph; fakes are rendered darker and have a learnable tell, so full information (3.6.3) holds. |
| 3.6.12 | A09 | Heavy Hand | For 10–20 beats the player's cards resolve one beat late. Feel risk: prototype before locking (7.2.4). |
| 3.6.13 | A10 | Beat Rush | The enemy's rhythm speeds up by 50% for several beats; darker Rhythm Line. Enemy-side only; the track is untouched (3.3.1.6). |
| 3.6.14 | A11 | Chain Breaker | Destroys the cards banked in the Signature Chain before the Signature triggers (3.3.6.4). |
| 3.6.15 | A12 | Double Step | Some telegraphed notes require a double press, per key or as a sequence. |
| ✅ 3.6.16 | A13 | Charge / Buff | A telegraphed wind-up of 3–5 beats, then an empowered move. |
| 3.6.17 | A14 | Frenzy Mode | Below 50% HP the enemy enters a frenzied state with powerful buffs. |
| 3.6.18 | A16 | Corruption Aegis | Disables the player's CRP bonuses (3.8.7); the enemy's HP scales +1% per 1 CRP. |

Trait pool — each trait is one row:

| # | ID | Trait | Effect |
| :-- | :-- | :-- | :-- |
| 3.6.19 | T01 | Accuracy Bet | Each player Miss makes the enemy's next attack deal +5% damage. |
| ✅ 3.6.20 | T02 | Stoneform | Three consecutive beats without taking damage grant the enemy +10 Block. |
| 3.6.21 | T03 | Absorb Shell | Converts 20% of damage taken into HP. |
| 3.6.22 | T04 | Pure Heart | While free of debuffs, regenerates 10% HP every 10 beats, with a visible 10-beat divider; a debuff resets the count. |
| 3.6.23 | T05 | Blood Leech | Applying Bleed heals the enemy 2 HP. |
| 3.6.24 | T06 | Thorns Shell | The attacker takes 2 damage per hit on this enemy. |
| ✅ 3.6.25 | T07 | Guard | Starts combat with 30 Block. |

- 🔨 **3.6.26** Before a fight, and while the loadout is being edited (3.5.8), the enemy card shows: portrait, name, BPM, powers (abilities and traits), a quote line, a New or Type badge, a Binder button and a Fight button.
  - *Remaining:* portrait, BPM, powers, quote line and the New or Type badge (portrait and quote exist in data only). Built: enemy name, Fight button, Binder (Edit) button.
- **3.6.27** Abilities and traits are assigned to roles: Rising Tempo, Misstep Pain, Counterblade and Pressure to Aggressors; Iron Veil, Backflash Barrier and Corruption Aegis to Tanks; Fake Move, Heavy Hand, Beat Rush and Chain Breaker to Mentalists; Double Step, Charge/Buff and Frenzy Mode to all. Accuracy Bet belongs to Aggressors; Stoneform, Absorb Shell, Pure Heart, Blood Leech and Guard to Tanks; Thorns Shell to all.
- ✅ **3.6.28** Every enemy is bound to exactly one track of its own from its World's soundtrack (3.2.15) and to one chart authored for that track (3.6.31); no enemy fights to another enemy's music.
- ✅ **3.6.29** Enemy HP is the formula value (3.7.15) times the role multiplier (3.6.1); enemy damage per hit follows the tier and role bands (3.7.16).
- **3.6.30** A Boss fight has multiple phases, each with unique telegraphs and rhythm patterns (3.3.9.1).
- ✅ **3.6.31** Every enemy has one chart: a finite, ordered list of actions (attack left, attack right, defend with a defense level, buff, charge with a wind-up of 3–5 beats) at positions in beats and quarter beats on its track, written for that music against the track's tempo map (3.3.1.9). The chart is the only source of enemy actions and therefore of player action opportunities (3.3.1.8); a chart with no actions, an action outside the track's length, or a position not on a quarter beat fails validation.
- ✅ **3.6.32** When the chart and its track reach their end before the battle has ended, both loop from their start together; the loop is seamless and the music is never stopped (3.3.1.6).

### 🔨 3.7 Economy, shops & blacksmith

*Essence is the one currency. It is scarce by design: every run is winnable with smart choices, shops tempt but cannot always be afforded, elites are worth their risk, and late rewards matter. This feature owns the reward tables, all prices, the two commerce nodes, and the balancing formulas that turn intended battle durations into enemy numbers.*

**Functional requirements**

- 🔨 **3.7.1** Essence is the single run currency. It is spent on shop cards and Imprints (3.7.6), card removal (3.7.9), event choices (3.10.2), armor repair (3.7.12) and armor upgrades (3.7.13); it is earned from battles (3.7.5), events, minigames (3.11.1) and selling cards (3.7.10). Essence never goes below zero, and an option the player cannot afford is shown but disabled with its price.
  - *Remaining:* spending (shop, card removal, events, repair, armor upgrades), income from events, minigames and selling, options shown disabled with their price, and a test for the zero floor. Built: Essence as the single currency, floored at zero, earned from battles.
- ✅ **3.7.2** Normal battle reward: the player chooses 1 card of 3 offered (Common or Uncommon), plus Essence per the income table (3.7.5).
- ✅ **3.7.3** Elite reward: 1 high-rarity card plus 1 guaranteed Imprint (3.9.3), plus Essence (3.7.5).
- ✅ **3.7.4** Boss reward: 1 Rare or Legendary card, 1 Imprint, Charm unlock progress (3.9.10), plus Essence (3.7.5).
- ✅ **3.7.5** Essence income per battle is rolled within these bands:

| Tier | World 1 | World 2 | World 3 |
| :-- | :-- | :-- | :-- |
| Normal | 8–17 | 12–25 | 18–37 |
| Elite | 25–35 | 37–52 | 55–78 |
| Boss | 40–50 | 60–75 | 90–112 |

- **3.7.6** A Shop node (3.2.11) offers 5 Normal Cards (3.4.14), 3 Imprints and one sell-or-remove action per visit (3.7.9, 3.7.10).
- **3.7.7** A shop card's price is `RarityBase × WorldMultiplier`, with RarityBase rolled in Common 15–30, Uncommon 30–60, Rare 60–90, Legendary 110–160, and WorldMultiplier 1.0× in World 1, 1.2× in World 2 and 1.4× in World 3.
- **3.7.8** Of the 5 cards in a shop, at least 2 roll a price in the lower 50% of their rarity's range, so a shop is never entirely out of reach.
- **3.7.9** An Imprint in a shop costs 50–80 Essence; removing a card from the Binder costs 40 Essence.
- **3.7.10** Selling a card yields Common 10, Uncommon 20, Rare 40, Legendary 75 Essence.
- **3.7.11** A Blacksmith node (3.2.13) lets the player choose exactly one of Repair ARD (3.7.12) or Upgrade Armor (3.7.13) per visit; Upgrade Armor is offered only once unlocked (3.7.14).
- **3.7.12** Repair: the player freely chooses what percentage of missing ARD to restore and the cost updates live as `RepairCost = MissingARD% × CostPerPercent`, with CostPerPercent 0.3 in World 1, 0.5 in World 2 and 0.7 in World 3.
- **3.7.13** Upgrade Armor: armor has 4 upgrade slots per run, unlocked strictly in order Common, Uncommon, Rare, Legendary by sacrificing a card of that rarity; the sacrificed card's Category selects the upgrade pool; the game rolls 3 candidates from that pool, the player picks 1, the upgrade is permanent for the run, and the unused candidates are discarded for that slot.
- **3.7.14** Upgrade Armor becomes available once Björn's relationship reaches level 2 (3.10.9); until then the Blacksmith offers Repair only.
- ✅ **3.7.15** Enemy HP is derived from intended battle duration: `ActionsPerMinute` = the chart's action count divided by the chart's length in minutes (3.6.31); `AAPM = ActionsPerMinute × AttackRatio` (expected share of enemy actions the player answers with an attack, typically 0.6); `AvgAttackDMG = AvgCardDMG × (P% × 1 + G% × 0.5)` with AvgCardDMG per World (3.2.17); `DPM = AAPM × AvgAttackDMG`; `DPS = DPM / 60`; `EnemyHP = DPS × IntendedBattleDuration`, then the role multiplier (3.6.1). Worked check: a chart with 60 actions per minute, AttackRatio 0.6, AvgAttackDMG 10, a 60 s Normal fight gives base HP 360, so Aggressor 288, Mentalist 360, Tank 432.
- ✅ **3.7.16** Enemy damage per hit is tuned against the ARD baseline (3.2.3) and rises 15% per World from the World 1 base:

| Role | Normal | Elite | Boss |
| :-- | :-- | :-- | :-- |
| Aggressor | 10–15 | 18–25 | 35–45 |
| Mentalist | 8–12 | 15–20 | 30–40 |
| Tank | 6–10 | 12–18 | 25–35 |

- ✅ **3.7.17** The mistake budget these numbers must produce: a Normal fight is survivable with 10–20 mistakes, an Elite with 6–10, a Boss with 4–7.
- **3.7.18** The run power curve is weak at the start (few cards, no Imprints), growing mid-run through synergies, powerful late through Imprints and refined loadouts, and challenged again at each Boss; reward pacing (3.7.2 to 3.7.5) and enemy scaling (3.2.17) are tuned to hold that shape.

### 🔨 3.8 Corruption (CRP)

*Corruption is the run's doom clock and its power dial at once. It rises every time the player moves, it can be spent or courted through sacrifice, events, cards and wagers, and it trades speed, power and safety against each other. The architecture and the gain rate are locked; the effects of the three threshold bands are content still being designed.*

**Functional requirements**

- ✅ **3.8.1** CRP is a run-scoped stat from 0 to 100, starting at 0, clamped at both ends, and always visible on the map (3.2.16) and during battle.
- ✅ **3.8.2** Every node transition (3.2.7) adds +1 CRP, so moving costs time and time advances corruption.
- 🔨 **3.8.3** CRP is also moved, in either direction, by Sacrifice (3.4.20), event outcomes (3.10.2), card effects (3.4.9) and minigame wagers (3.11.2).
  - *Remaining:* Sacrifice (3.4.20), event outcomes (3.10.2) and minigame wagers (3.11.2), and a test or shipped content that moves CRP through a card effect; card, Imprint and Charm effects can already move CRP with a source in code.
- **3.8.4** CRP has threshold bands at 25, 50 and 75. Crossing a threshold applies that band's effect set (power gain paired with risk escalation, corrupted card effects) and advances the player's visual transformation stage (3.14.7); the effect sets themselves are content, defined per band in the decision log.
- **3.8.5** At CRP 100 the player transforms: a brief, uncontrollable beast rampage plays out, after which the run ends as a death (3.9.11).
- ✅ **3.8.6** Every CRP change shows its source and amount at the moment it happens, so the player can attribute it.
- **3.8.7** Corruption Aegis (3.6.18) disables the player's CRP-derived bonuses for that battle; CRP itself does not change.
- ✅ **3.8.8** Cards and Imprints may scale with CRP (3.4.9); such scaling is stated on the card and is the primary way high CRP pays out as power.

### 🔨 3.9 Progression & meta unlocks

*Two layers: run progression, which resets on death or victory and exists for experimentation and the feel of a power ramp; and meta progression, which persists on the profile and exists for long-term motivation and mastery. Charms are the permanent, chosen, build-defining layer; Imprints are the temporary, rolled, chaotic one. Every failure reads as progression, not punishment.*

**Functional requirements**

- ✅ **3.9.1** Run progression (cards, Imprints, Essence, route, Trait-modified cards, armor upgrades, CRP) resets fully when a run ends by death or victory.
- ✅ **3.9.2** Meta progression persists on the profile (3.1.2): Charm unlocks, card unlocks into the Global Binder (3.5.9), Imprint-pool unlocks, difficulty modifiers (3.9.12) and, optionally, cosmetics.
- ✅ **3.9.3** Imprints are acquired during a run from Elites (guaranteed, 3.7.3), shops (3.7.6), events (3.10.2) and rare nodes; each is rolled from a tier table (Common, Uncommon, Rare); there is no slot limit, a typical run collects 3–6, they stack where their text says so, and they are lost at run end.
- **3.9.4** Early-tier Imprints are simple stat changes and late-tier Imprints are synergies; the equipped Imprints are visible on the map (3.2.16) and in battle.
- 🔨 **3.9.5** Charms are unlocked permanently through boss defeats, milestones, challenges and rare post-run rewards; they are never rolled randomly into a run.
  - *Remaining:* milestone and challenge unlocks (their facts are never recorded, so they cannot fire) and rare post-run rewards. Built and tested: boss-defeat unlocks, never rolled into a run.
- ✅ **3.9.6** Before a run the player equips 0–2 Charms from the unlocked set into 2 slots; the choice is final for that run unless an event deactivates a Charm (3.10.2).
- 🔨 **3.9.7** A Charm is stronger than an Imprint and defines build identity; Charm content is a data table (4.9).
  - *Remaining:* nothing checks that a Charm is stronger than an Imprint or defines build identity; Charm content is already a data table (4.9).
- ✅ **3.9.8** Charm triggers reference battle facts recorded by combat, chiefly Perfect Defense (3.3.9.4); a Charm's effect fires at the moment its trigger is satisfied and, where it says "until reset", lasts until the next run.
- **3.9.9** Each main NPC's level-10 unlock is an exclusive Charm, one per NPC, that alters the game's ending (3.10.8, 3.14.5).
- ✅ **3.9.10** Each boss defeat counts as progress toward Charm unlock milestones; the unlock conditions are content per Charm (4.9).
- ✅ **3.9.11** When a run ends by death, abandonment (3.1.7), CRP 100 (3.8.5) or victory (3.2.1), a run-end screen shows the outcome, the seed (3.2.4), run stats, and every permanent unlock granted; the profile is updated (3.1.8) and the player returns to the pre-run screen with new cards, Charms or mechanics able to enter future pools.
- **3.9.12** Meta progression can unlock difficulty modifiers that the player may switch on pre-run; the modifier catalogue is content.

### 🔨 3.10 Events & relationships

*Events are the non-combat choice encounters on the map. Five NPCs recur across runs and remember the player: their Relationship Points persist on the profile, and each level grants exactly one new piece of content that is added to the pools, never swapped in. Early runs stay consistent; later runs get richer.*

**Functional requirements**

- **3.10.1** An Event node (3.2.12) presents a choice encounter of one of three categories: Main Character Events (the Fisherman, the Flower Girl, the Gambler), Secondary Character Events (Björn the Blacksmith, Gero the Shopkeeper) or Neutral Events with no NPC (worldbuilding, risk and reward, CRP interactions, puzzles, lore).
- **3.10.2** An event outcome is drawn from this taxonomy, and an event's Essence, ARD or CRP cost is shown before the player chooses:

| Group | Outcomes |
| :-- | :-- |
| Cards | gain a card; gain an Event Card (3.4.15); gain an Unstable Card (3.4.16); upgrade a card (3.4.18); add a Trait (3.4.19); remove a card |
| Stats | repair ARD; increase max ARD; increase CRP; reduce CRP (3.8.3); increase Base DMG |
| Other | gain an Imprint (3.9.3); gain Essence; gain Relationship Points (3.10.4) |
| Negative | lose ARD; remove a Trait; lose Essence; lose an Imprint; deactivate a Charm (3.9.6); lose a random card |

- ✅ **3.10.3** Relationship Points (RP) are tracked per NPC on the profile and persist across runs; main NPCs have levels 1–10 and secondary NPCs levels 1–5.
- ✅ **3.10.4** RP is gained through dialogue choices, minigame results (3.11.1), sacrifices, and repeated positive interaction.
- ✅ **3.10.5** Levelling from level N to N+1 costs N+1 RP (2 to reach level 2, 3 to reach level 3, and so on), so maxing a main NPC costs 54 RP in total.
- **3.10.6** Each level reached grants exactly one unlock: an event, card, Trait, Imprint, Charm, node upgrade or permanent stat; unlocks add to the pools and never replace existing content.
- **3.10.7** The relationship content budget is 48 pieces: 27 for main NPCs, 12 for secondary NPCs, 9 neutral.
- **3.10.8** Level 10 of each main NPC is a Special Event that delivers that NPC's exclusive ending-altering Charm (3.9.9).

Unlock tracks — content as currently authored (blank cells are unwritten content, not rules):

| Lv | Flower Girl | Gambler | Fisherman |
| :-- | :-- | :-- | :-- |
| 2 | Event: Spring Blossom | Event | Event |
| 3 | Card: Grace of Two | Card | Card |
| 4 | Trait: Reload | Trait | Trait |
| 5 | Event: Flower Festival | Event: Friendship Gift | Charm: Casting Guide |
| 6 | Imprint: Vitamin B | Imprint: Gamblit Dice | Imprint |
| 7 | Card: Meditate | Card | Card |
| 8 | Event: She Seems Sad | Event: Deja Vu | Event |
| 9 | Permanent max ARD +20 | Permanent max ARD +20 | Permanent max ARD +20 |
| 10 | Special: Familiar Feeling | Special: Grave Bet | Special: A Memory from the Sea |

| Lv | Björn (Blacksmith) | Gero (Shopkeeper) |
| :-- | :-- | :-- |
| 2 | Node upgrade: Armor Upgrading (3.7.14) | Node upgrade |
| 3 | Card | Event |
| 4 | Event | Event |
| 5 | Charm: Thick Skin | Charm: Deep Wallet |

- **3.10.9** Björn's level 2 unlock enables Upgrade Armor at the Blacksmith (3.7.14); Gero's level 2 unlock is a shop node upgrade whose content is unwritten.
- **3.10.10** A main NPC's event can open that NPC's minigame (3.11.1); the minigame's result feeds RP (3.10.4) and rewards per its own tables.
- **3.10.11** An NPC's current level and progress to the next are visible in the event with that NPC and on the pre-run screen.

### 3.11 Minigames

*Three NPC-bound minigames give the relationships a mechanical face: a memory gamble with the Gambler, a two-phase fishing game with the Fisherman, and a rhythm game sung by the Flower Girl. Their reward tables are content; their mechanics are rules.*

**Functional requirements**

- **3.11.1** Each minigame is reached through its NPC's event (3.10.10): Double or Nothing (3.11.2) with the Gambler, Fishing (3.11.4) with the Fisherman, Flower Rhythm (3.11.7) with the Flower Girl.
- **3.11.2** Double or Nothing: a sequence of 3–10 playing cards is shown briefly, then hidden, and the player must re-select them in the exact order from only the shown cards; any wrong pick is a loss. Before playing, the player wagers one resource; a win doubles the wagered movement and a loss costs it:

| Wager | Before | Amount | After win | After loss |
| :-- | :-- | :-- | :-- | :-- |
| Essence | 132 | 50 | 182 | 82 |
| CRP | 67 | 12 | 79 | 55 |
| ARD | 230 | 100 | 300 (max) | 130 |
| Card | 12 cards | 2 chosen cards | 10 (the 2 chosen cards destroyed) | 14 (2 random cards gained) |

- **3.11.3** The reveal time grows with the card count: 3 cards 3.2s, 4 cards 3.7s, 5 cards 4.3s, 6 cards 4.8s, 7 cards 5.4s, 8 cards 5.9s, 9 cards 6.5s, 10 cards 7.0s.
- **3.11.4** Fishing, phase one (Cast Power): one mouse press casts; a vertical power bar with coloured zones decides the catch: Red (very small) fails with nothing; Orange yields Essence or cards; Yellow yields a Trait or an Imprint; Green (very small) yields a Charm.
- **3.11.5** A cast of quality above 7 has a chance to yield an event item instead of the usual reward; if every Charm is already unlocked, Green grants a highest-tier reward from another category.
- **3.11.6** Fishing, phase two (Reeling): a fish swims across the screen through 10 rings at changing speed; the player tracks it with the mouse and clicks as it passes each ring; each correctly timed click is a hit, and reward quality scales with the number of hits.
- **3.11.7** Flower Rhythm: four flower slots are bound to D, F, J and K; a horizontal track streams beat dots toward a target square showing a flower icon; when a dot aligns the player presses the key of the shown flower, and the icon changes after each hit. The beats follow the Flower Girl's sung melody, which is never bent (3.3.1.6).
- **3.11.8** Flower Rhythm pays Essence by accuracy: 0–25% pays 0, 26–50% pays 30, 51–75% pays 65, 76–100% pays 100.
- **3.11.9** A fuller reward-allocation matrix across NPCs (Charm, Imprint, Trait, cards, ARD, CRP and more) exists as content with no cells allocated yet; when allocated it extends these tables and does not change the mechanics above.

### 🔨 3.12 Settings & accessibility

*A rhythm game lives or dies on latency, so calibration is a first-class feature rather than a settings afterthought. The keys are fixed by design (3.3.2.1); accessibility comes from timing help, audio cues and calibration, not from rebinding.*

**Functional requirements**

- ✅ **3.12.1** A latency calibration screen runs an audio and video offset test and stores the resulting offset on the profile; it is offered on a profile's first launch before the first battle and is reachable from settings at any time, including from the map. Bluetooth audio adds 100–300 ms and the screen says so.
- 🔨 **3.12.2** A metronome can be toggled on and off, and its volume set independently (3.12.4).
  - *Remaining:* a user-facing metronome volume (3.12.4) and a test of the settings-menu toggle; the toggle and its beat clicks are built and tested.
- 🔨 **3.12.3** Assist mode widens the Perfect and Good windows (3.3.3.1) by a fixed factor; runs played with Assist mode on are marked as such on the run-end screen (3.9.11) and in the run log (3.15.1).
  - *Remaining:* the Assist toggle, widening the Perfect and Good windows, and marking the run-end screen; runs always record Assist as off. Built: the flag on the profile and in the run log.
- 🔨 **3.12.4** Audio volumes for music, sound effects and the metronome are set separately.
  - *Remaining:* the volume settings screen and applying music and sound effect volumes; only the metronome volume is applied.
- **3.12.5** Display settings cover fullscreen or windowed mode, resolution and vertical sync.
- 🔨 **3.12.6** Combat keys cannot be rebound (3.3.2.1); settings show the physical layout with the characters the player's keyboard produces (3.3.2.5).
  - *Remaining:* settings do not show the physical layout; keys are already fixed and battle labels follow the keyboard layout.
- 🔨 **3.12.7** The game ships in English; all player-facing text is held outside code so further languages can be added without rule changes.
  - *Remaining:* raw ids still reach the player (the reward panel's Imprint, run-end unlocks, CRP source fallbacks), and no test shows a second language added as data. Built: the string table `data/strings/en.json`.

### 🔨 3.13 Onboarding & tutorial

*The first minutes must teach the beat, the judgment, the two lines and the Signature Chain by doing, against an enemy tuned by the same formulas as everything else.*

**Functional requirements**

- **3.13.1** A tutorial battle against Ren teaches, in order: the Rhythm Line and answering enemy actions (3.3.1.8), timing judgment (3.3.3.1), the four categories and correct-side counters (3.3.4.4), switching lines (3.3.2.2), and banking a Signature (3.3.6.1).
- 🔨 **3.13.2** The tutorial uses a fixed tutorial Binder and loadout so every player sees the same cards; Ren is tuned to the ARD baseline (3.2.3) through the standard formulas (3.7.15, 3.7.16).
  - *Remaining:* the fixed tutorial Binder and loadout, and wiring Ren into the tutorial (3.13.1); Ren is already a Normal Tank tuned through the standard formulas.
- **3.13.3** The tutorial runs automatically on a profile's first run and is skippable on every later run; completion is stored on the profile (3.1.2).

### 🔨 3.14 Narrative & presentation

*The surface is cute and the core is dark, on purpose. Runs are a time loop; corruption is a fiction as well as a stat; the NPCs the player befriends decide the ending. Story content is authored separately; this feature holds the rules that the systems already rely on.*

**Functional requirements**

- **3.14.1** The tone is energetic, tense and skill-focused, with stylised, readable combat visuals that emphasise timing and clarity; the cute exterior (Lulu) contrasts deliberately with the dark core (corruption, loss).
- **3.14.2** Each of the three Worlds is one continent with its own fiction, biome, soundtrack and enemy pool, delivered through map art and props (3.2.15).
- **3.14.3** The recurring cast is Aika, Björn (the Blacksmith node), Gero (the Shop node), the Fisherman, the Flower Girl, the Gambler, Daren, Griit, Vult, Zarr and Lulu; NPC scenes are events (3.10.1).
- **3.14.4** A failed run is framed diegetically as a time loop, so the failure loop (3.9.11) reads as story, not punishment.
- **3.14.5** The ending changes according to which level-10 relationship Charms (3.9.9) the player has equipped when the World 3 Boss falls; a true ending exists behind a specific combination.
- 🔨 **3.14.6** Card and Imprint flavor text is written in the myth register, the voice of Griit, Vult and Zarr.
  - *Remaining:* flavor text on Imprints (no field exists), one starter card line outside the myth voice, and showing flavor text anywhere in the client; cards carry flavor text, mostly in the myth register.
- **3.14.7** The player's appearance shows their corruption stage, advancing at each CRP threshold (3.8.4) and culminating in the beast form at 100 (3.8.5).

### 🔨 3.15 Playtest run logs

*The demo exists to answer one question: fight the same enemy twice in a row, and is the second fight still interesting? The game answers it with data that never leaves the tester's machine unless they send it.*

**Functional requirements**

- ✅ **3.15.1** At the end of every run the game writes a run log file under the profile containing: seed, difficulty modifiers and Assist flag (3.12.3), the route taken, and per battle the enemy, duration in beats and seconds, judgment counts, damage taken, Signatures fired, cards played per slot, and the outcome; the log holds no personal data and no profile name.
- ✅ **3.15.2** The log flags every battle whose enemy is the same as the previous battle's, so repeated-enemy fights can be compared directly.
- 🔨 **3.15.3** Run logs are kept until the player deletes them; settings offer an "open run logs folder" action so a tester can find and send them.
  - *Remaining:* the "open run logs folder" action in settings; logs are already kept under the profile until deleted.
- 🔨 **3.15.4** Nothing in a run log is transmitted anywhere by the game (6.5).
  - *Remaining:* a test proving nothing is transmitted, and removing the Unity analytics and web request modules from the package manifest; no code transmits anything today.

## 4. Data & metadata definitions

*What data exists in the product and who owns each field. Conceptual — this describes ownership and provenance, not storage.*

### 🔨 4.1 Player profile

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Name | User-set | Unique on the install (3.1.1) |
| Created, last played | Auto-set | Shown in the picker and on sync conflict (3.1.4) |
| Charm unlocks | Auto-tracked | Granted per 3.9.5 and 3.9.10 |
| Global Binder card unlocks | Auto-tracked | 3.5.9 |
| Imprint-pool unlocks, difficulty modifiers, cosmetics | Auto-tracked | 3.9.2 |
| NPC relationships | Auto-tracked | One entry per NPC (4.12) |
| Calibration offset | User-set | 3.12.1 |
| Settings | User-set | 3.12.2 to 3.12.6 |
| Tutorial completed | Auto-set | 3.13.3 |
| Run in progress | Auto-retained | At most one (4.2), 3.1.5 |
| Run history | Auto-tracked | Outcomes and seeds of past runs (3.9.11) |

- *Remaining, 4.1:* Global Binder card unlocks, Imprint-pool unlocks, difficulty modifiers, cosmetics and tutorial completed exist but are never filled; settings lack resolution and the player can change only the metronome toggle. Present: name, created, last played, Charm unlocks, NPC relationships, calibration offset, run in progress, run history.

### ✅ 4.2 Run

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Seed | User-set or Auto-generated | 3.2.4 |
| World index, current node | Auto-tracked | 3.2.1, 3.2.7 |
| ARD current and maximum | Auto-calculated | 3.2.3 |
| Base DMG, Essence, CRP | Auto-calculated | 3.2.3, 3.7.1, 3.8.1 |
| Equipped Charms | User-set | 0–2, chosen pre-run (3.9.6); may be deactivated by events |
| Imprints held | Auto-tracked | 3.9.3 |
| Armor upgrade slots | Auto-tracked | 4 slots with the chosen upgrade in each (3.7.13) |
| Run Binder | Auto-tracked | Card instances (4.5), 3.5.4 |
| Loadout | User-set | 4.6 |
| Difficulty modifiers, Assist flag | User-set | 3.9.12, 3.12.3 |
| Map graphs | Auto-generated | One per World (4.3) |
| Status | Auto-set | in progress, won, died, abandoned (3.9.11) |

### 🔨 4.3 Map graph & node

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Nodes and connections | Auto-generated | From the seed per 3.2.5 and 3.2.6 |
| Node type | Auto-generated | One of the catalogue rows 3.2.8 to 3.2.14 |
| Node content roll | Auto-generated | Enemy, event, shop stock or Forge outcome fixed by the seed (3.2.4) |
| Visited | Auto-tracked | 3.2.7 |
| Props and decoration placement | Auto-generated | 3.2.15 |

- *Remaining, 4.3:* event, shop stock and Forge outcome rolls, and props and decoration placement. Present: nodes and connections, node type, enemy content roll, visited.

### ✅ 4.4 Card definition

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Name, Category, Rarity, class | Admin-set | Designer-authored content; 3.4.1, 3.4.4, 3.4.13 |
| Damage or value | Admin-set | Within the rarity band (3.4.4) |
| Cooldown in beats | Admin-set | 2–6 (3.3.5.1) |
| Effects, special rules, reaction conditions | Admin-set | Must satisfy 3.4.8 and 3.4.9 |
| Upgrade step | Admin-set | 3.4.18 |
| Unstable lifespan | Admin-set | Only for Unstable class (3.4.16) |
| Flavor text | Admin-set | 3.14.6 |
| Unlock source | Admin-set | Starter, pool, boss, relationship level (3.10.6) |

### ✅ 4.5 Card instance

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Definition | Auto-set | Reference to 4.4 |
| Upgraded | Auto-tracked | 3.4.18 |
| Trait | Auto-tracked | At most one (3.4.19) |
| Battles remaining | Auto-calculated | Unstable cards only (3.4.16) |
| Shop price | Auto-generated | Rolled per 3.7.7 and 3.7.8 while in a shop |

### ✅ 4.6 Loadout

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Slot assignments | User-set | 16 slots, line × key, each holding one card instance of the matching Category (3.5.2) |
| Active line | Auto-tracked | Toggled in battle by the line-switch key (3.3.2.6) |
| Slot cooldowns | Auto-calculated | Per slot during battle (3.3.5.1) |

### ✅ 4.7 Enemy definition

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Name, tier, role, rhythm profile | Admin-set | 3.6.1, 3.6.2, 3.3.9.1 |
| Track and BPM | Admin-set | 3.6.28 |
| HP | Auto-calculated | 3.7.15 then 3.6.1 |
| Damage per hit | Admin-set | Within the band (3.7.16) |
| Abilities, traits, statuses used | Admin-set | Within the tier budget (3.6.4) |
| Chart | Admin-set | Reference to the enemy's chart (4.16); phases for Bosses (3.6.30) |
| Portrait, quote line | Admin-set | 3.6.26 |

### ✅ 4.8 Battle

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Enemy, track, tier | Auto-set | From the node (4.3) |
| Beat clock | Auto-calculated | Driven by audio playback (6.1) |
| Player Block, statuses on both sides | Auto-calculated | 3.3.4.2, 3.3.7.1 |
| Signature Chain contents | Auto-tracked | 3.3.6.1 |
| Judgment log | Auto-tracked | Per enemy action (3.3.3.1); feeds 3.15.1 |
| Damage taken, Perfect Defense | Auto-calculated | 3.3.9.4 |
| Outcome | Auto-set | Won or died (3.3.9.2, 3.3.9.3) |

### ✅ 4.9 Charm

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Name, rarity | Admin-set | Content table |
| Trigger, effect | Admin-set | 3.9.8 |
| Unlock condition | Admin-set | Boss, milestone, challenge, relationship level (3.9.5, 3.9.9) |
| Ending-altering | Admin-set | True for the three level-10 Charms (3.9.9) |

### ✅ 4.10 Imprint

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Name, tier, effect | Admin-set | 3.9.3 |
| Stackable | Admin-set | 3.9.3 |
| Source | Admin-set | Pool, or relationship level (3.10.6) |

### 4.11 Trait

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Name, effect | Admin-set | Applied per 3.4.19; disabled by Disarmed (3.3.7.8) |
| Source | Admin-set | Pool, or relationship level (3.10.6) |

### ✅ 4.12 NPC relationship

| Field | Set by | Notes |
| :-- | :-- | :-- |
| NPC | Admin-set | One of the five (3.10.1) |
| Level | Auto-calculated | 1–10 main, 1–5 secondary (3.10.3) |
| RP toward next level | Auto-tracked | 3.10.4, 3.10.5 |
| Unlocks granted | Auto-tracked | One per level (3.10.6) |

### 4.13 Event definition

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Category, NPC | Admin-set | 3.10.1 |
| Availability | Admin-set | Relationship level required (3.10.6), World |
| Choices and outcomes | Admin-set | From the taxonomy (3.10.2) |
| Minigame opened | Admin-set | 3.10.10 |

### ✅ 4.14 Music track

| Field | Set by | Notes |
| :-- | :-- | :-- |
| World, encounter tier | Admin-set | 3.2.15 |
| Tempo map | Admin-set | Starting BPM, offset, and BPM changes at beat positions (3.3.1.9) |
| Beat map | Auto-derived | Audio time of every beat and quarter beat from the tempo map (3.3.1.9) |
| Length in beats | Auto-derived | Where the chart and track loop (3.6.32) |

### ✅ 4.16 Chart

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Enemy, track | Admin-set | One chart per enemy, on that enemy's track (3.6.28) |
| Actions | Admin-set | Ordered list of kind and quarter-beat position (3.6.31) |
| Length in beats | Auto-derived | Equals the track's length; loop point (3.6.32) |
| Actions per minute | Auto-calculated | Feeds the HP formula (3.7.15) |

### ✅ 4.15 Run log

| Field | Set by | Notes |
| :-- | :-- | :-- |
| Seed, modifiers, Assist flag, route | Auto-generated | 3.15.1 |
| Per-battle records | Auto-generated | From the battle judgment log (4.8) |
| Repeated-enemy flag | Auto-derived | 3.15.2 |

## 5. Platform & distribution

### 🔨 5.1 Desktop application

- 🔨 **5.1.1** The game runs natively on Windows 10 and later and on macOS 12 and later, as a 64-bit build for each.
  - *Remaining:* Windows 10 minimum and 64-bit targets, build scripts and a verified build for each platform; the project already targets macOS 12.
- 🔨 **5.1.2** Input is keyboard for all of combat and navigation (3.3.2.1) with the mouse used only where a feature says so (Fishing, 3.11.4; menus); no controller support is required at launch.
  - *Remaining:* keyboard control of the stop, run-end, pre-run and settings screens, which need the mouse today; combat, map, pre-battle, Binder and reward screens are keyboard-driven.
- 🔨 **5.1.3** The game runs fully offline; no feature depends on a network connection (6.3).
  - *Remaining:* a test proving no feature needs a connection; no code uses the network today.
- ✅ **5.1.4** The game leans on the OS audio stack for a low-latency, timestamped playback clock (6.1) and lets the player compensate any remaining offset (3.12.1).

### 🔨 5.2 Steam distribution

- **5.2.1** The game is distributed through Steam for both platforms; the store page uses the canonical comparison "Slay the Spire × Crypt of the NecroDancer" and the Rhythm Line and Signature Chain names (never "Pulse Line", "Combo Chart", "deck", "relic", "gold" or "HP" for the player).
- 🔨 **5.2.2** All profiles (3.1.4) sync through Steam Cloud under the Steam account; the game works without Steam Cloud and simply stays local.
  - *Remaining:* Steam Cloud sync; profiles already work locally without Steam.
- **5.2.3** Store achievements, if used, mirror meta unlocks (3.9.5) and never gate content.

### 🔨 5.3 Demo build

A free demo ships for Steam Next Fest, Oct 19–26, 2026, with this content budget:

| Item | Count |
| :-- | :-- |
| Worlds | 1 |
| Normal enemies | 6 |
| Elite | 1 |
| Boss | 1 |
| Shop | 1 |
| Events | 3 |
| Blacksmith | 2 |
| Forge | 2 |
| Cards | 30 |
| Imprints | 10 |
| Charms | 2 (chosen from 8 candidates) |
| NPCs | Fisherman, Flower Girl, Gambler; Björn and Gero as node NPCs |

- 🔨 **5.3.1** The demo is World 1 only, played with the full rule set of section 3 and the content counts above; relationship content is limited to the demo's 3 events.
  - *Remaining:* the World-1-only demo mode and the demo content counts (6 Normal enemies, 30 cards, 10 Imprints, 2 Charms chosen from 8, 3 events) with working Shop, Event, Blacksmith and Forge nodes. Built: the full rule set runs on fixture content (3 Normal enemies, 1 Elite, 1 Boss, 20 Common cards, 6 Imprints, 2 Charms).
- 🔨 **5.3.2** The demo includes the calibration screen (3.12.1), the keep-previous loadout flow (3.5.6), the tutorial (3.13.1), Fishing per 3.11.4 to 3.11.6 with phase two polish cut before phase one if time runs short, and run logs (3.15.1).
  - *Remaining:* the tutorial (3.13.1) and Fishing (3.11.4 to 3.11.6); calibration, the keep-previous loadout flow and run logs are built.
- 🔨 **5.3.3** The demo Signature deals 30 damage (3.3.6.2), and the demo's Blacksmith offers all four armor slots (3.7.13) so Legendary sacrifice can be tested.
  - *Remaining:* the Blacksmith with all four armor slots (3.7.13); the 30-damage Signature is built.
- 🔨 **5.3.4** The demo must let a tester fight the same enemy twice in a row so the playtest question (3.15.2) can be answered before any World 2 or 3 content is built.
  - *Remaining:* a way to fight the same enemy twice in a row; battle nodes roll their enemy at random within the tier. The run log already flags repeated enemies.

## 6. Non-functional requirements

| # | Category | Requirement |
| :-- | :-- | :-- |
| ✅ 6.1 | Timing accuracy | Inputs are timestamped and graded against the audio playback clock, not the render frame, so judgment error is independent of framerate and stays within 2 ms of the audio clock after calibration (3.12.1). |
| ✅ 6.2 | Performance | The game holds 60 frames per second at 1080p on a 2019 laptop with integrated graphics, and never drops a beat of audio when it drops a frame (3.3.1.6). |
| 🔨 6.3 | Availability | Fully offline; no server of ours exists and no feature degrades without a connection (5.1.3); Steam Cloud is the only network use (5.2.2). |
| 🔨 6.4 | Storage & retention | Profiles and runs are kept until the player deletes them (3.1.3); run logs are kept until deleted (3.15.3); a profile with its history stays under 50 MB so cloud sync remains cheap. |
| 🔨 6.5 | Privacy | No gameplay data, log or identifier leaves the machine except through Steam Cloud save; run logs contain no personal data (3.15.1, 3.15.4). |
| 🔨 6.6 | Cost | Zero running cost: no hosted services, no per-call APIs, no telemetry endpoint. |
| ✅ 6.7 | Testability | The rules of section 3 live in a simulation layer separable from rendering and audio; every locked rule maps to at least one unit test, and every row of 3.3.4.8 is one. |
| 🔨 6.8 | Determinism | Given the same seed and the same timestamped input sequence, a run replays identically (3.2.4), so bugs and balance cases can be reproduced from a run log (3.15.1). |
| 🔨 6.9 | Content operability | Cards, Charms, Imprints, Traits, enemies and events are data tables edited without code changes (4.4, 4.7, 4.9, 4.10, 4.11, 4.13); a table entry that violates a section 3 rule fails validation at build time (3.4.21). |
| 🔨 6.10 | Accessibility | Assist mode (3.12.3), metronome (3.12.2), calibration (3.12.1) and category icons alongside colours (3.4.2) are available in every build including the demo. |
| 🔨 6.11 | Localisation | English at launch with all text externalised (3.12.7). |
| 🔨 6.12 | Session length | A full run takes about one hour and a Normal battle 30–60 s (3.2.18); the loadout flow keeps menu time below battle time (3.5.6). |

- *Remaining, 6.3:* Steam Cloud as the one network use (5.2.2) and a test that nothing else goes online; no online feature exists today.
- *Remaining, 6.4:* profile deletion (3.1.3) and enforcing or measuring the 50 MB bound; profiles and run logs are already kept with no expiry.
- *Remaining, 6.5:* a test that nothing leaves the machine; run logs are already free of profile name and user identity (tested).
- *Remaining, 6.6:* a test or check that no telemetry exists, and removing the Unity analytics module from the manifest; no hosted service or API is used and Unity analytics is disabled.
- *Remaining, 6.8:* replaying a run from its seed plus timestamped inputs, with a test; the run log stores no inputs, so a run cannot be reproduced from it. Seeded forks already reproduce the map and enemy rolls.
- *Remaining, 6.9:* Trait and Event tables, rule validation for Charms and Imprints, and validation at build time (it runs only in `dotnet test`); cards, charts and enemies are validated.
- *Remaining, 6.10:* Assist mode (3.12.3) and category icons alongside colours (3.4.2); calibration and the metronome are available.
- *Remaining, 6.11:* raw ids that still reach the player (see 3.12.7); English text is externalised in the string table.
- *Remaining, 6.12:* measuring the full run length and menu time against battle time; Normal intended durations are validated at 30 to 60 s and entering battle is one input.

## 7. Technical feasibility & high-level approach

*This section exists to show the product above can be built and to sketch how. It deliberately stops short of designing it.*

**In outline,** Chiki is a deterministic rules simulation wrapped in a rhythm front end. The simulation owns the map graph, the Binder, the economy, Corruption and every combat resolution rule, takes a seed and a stream of timestamped inputs, and produces state; it can run headless for unit tests and replays. The front end owns audio playback, the beat clock derived from it, the input parser, and rendering. Combat is scheduled in beats off the audio clock: each track's tempo map produces a beat map at quarter-beat resolution, the enemy's chart places its actions on it, and every keypress is stamped with the audio time at which it occurred and graded against the nearest Judgment Window. Content is data: cards, Charms, Imprints, Traits, enemies and events are tables loaded at start and validated against the rules. Profiles are local files that Steam Cloud mirrors. Nothing runs on a server.

### 7.1 What makes it possible

- **7.1.1** An audio playback clock with sample-accurate position and timestamped input events, so judgment (3.3.3.1) is graded against the music and not the frame (6.1); needed by all of 3.3 and by Flower Rhythm (3.11.7).
- **7.1.2** A seeded pseudo-random generator threaded through map generation, node content rolls, shop prices and enemy variation, so a seed reproduces a run (3.2.4, 6.8).
- **7.1.3** A simulation layer with no rendering dependency, so each rule in section 3 has a unit test (6.7) and a run log can be replayed (3.15.1).
- **7.1.4** An input layer that reads slot keys, the Space-plus-key chord (3.3.2.3) and the dedicated line-switch key (3.3.2.6) by physical position (3.3.2.5) and stamps each with audio time, needed by line switching (3.3.2.2) and the Signature Chain (3.3.6.1).
- **7.1.5** Data-table content with build-time validation against the card, enemy and event rules (6.9), needed by 3.4, 3.6, 3.9 and 3.10.
- **7.1.6** A calibration routine that measures audio and video offset per machine (3.12.1), needed on every platform and especially over Bluetooth audio.
- **7.1.7** A cross-platform engine build for Windows and macOS (5.1.1) with the OS audio stacks exposed at low latency (5.1.4).
- **7.1.8** External dependencies outside the team's control: the Steam client and Steam Cloud (5.2.2), the OS audio drivers on both platforms, and the tester's audio hardware.
- **7.1.9** The existing balancing spreadsheet (the Enemy_ARD_Calculator) already implements the enemy HP formula (3.7.15) and stays the authoring tool for enemy numbers.

### 7.2 Hard parts & unknowns

- **7.2.1** Line-switch reach: with the switch on V (3.3.2.6) the left index finger leaves the R slot to switch lines, and a switch pressed within the same beat as a Space chord must still resolve unambiguously; if the reach costs Perfects in fast fights, the two-line loadout (3.5.1) loses value. Borne by 3.3.2.6; verify in the first combat prototype. The former tap-versus-chord parser risk is retired by 3.3.2.4's withdrawal.
- **7.2.2** Audio latency on macOS and over Bluetooth: an uncalibrated offset of 100–300 ms makes every input a Miss; borne by 3.12.1 and 6.1, and the reason calibration is offered before the first battle.
- **7.2.3** Perfect streams nullify incoming damage regardless of the card played (3.3.4.2), so at high skill Defense slots may lose their value; if playtests confirm it, Block economy or Good and Miss rates need retuning before World 2 and 3 content. Borne by 3.3.4.1 and 3.5.2.
- **7.2.4** Heavy Hand (3.6.12) delays a card's effect by one beat, which breaks the input-to-effect causality the rest of the game relies on; if it does not read, the ability is cut rather than the rule bent.
- **7.2.5** The demo's playtest question (5.3.4): if the second fight against the same enemy is not interesting, in-battle variance (procedural chart variation, adaptive behaviour or a per-beat resource) has to be designed before any further world content, which changes 3.6 and possibly 3.3.
- **7.2.6** Corruption threshold effects (3.8.4) and the Corruption Aegis numbers (3.6.18) are undesigned content on a locked architecture; until they exist, CRP is a pure doom clock and the "power dial" half of pillar five is untested. The CRP 100 outcome (3.8.5) is written here as the current proposal.
- **7.2.7** Scar (3.3.7.3) puts a chance roll on top of a Perfect, which rhythm players may read as the game undermining precise execution; a deterministic redesign is open in the decision log and would change only that row.
- **7.2.8** Content gaps: most rows of the card table are unfilled, the per-tier availability of abilities and traits is unwritten (3.6.4), the relationship unlock tracks have blanks (3.10.9), and the minigame reward matrix is unallocated (3.11.9). The demo's 30 cards, 6 enemies and 3 events are the gating content.
- **7.2.9** Multiple profiles under one Steam Cloud account (3.1.4): cloud conflict resolution is per file, so a household with two active profiles on two machines can produce conflicts the player has to resolve by hand.
- **7.2.10** Physical-position key binding (3.3.2.5) needs scancode-level input on both platforms; the ; key in particular moves on several international layouts, and the label must follow the real character or the tutorial (3.13.1) will mislead.
