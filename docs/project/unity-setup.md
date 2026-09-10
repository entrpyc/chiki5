# Chiki — Unity client setup decisions

Settled 2026-09-10 for the client half of `docs/plan.md` (Phases 12 onward). Rules stay in the PRD; this file records technology and asset-pipeline choices the PRD deliberately leaves open. Change a decision here first, then the plan items that cite it.

## Decided by the operator

| Area | Decision | Consequence |
| :-- | :-- | :-- |
| Engine | Unity (2022 LTS or later), C#; simulation as a plain .NET library `Chiki.Sim` | Simulation never references `UnityEngine`; client references the built DLL |
| Art style | HD hand-drawn, 1080p reference, bilinear filtering | 100 pixels per unit; sprites may rotate and scale freely; a 2× atlas variant for 4K is optional later |
| Animation | Frame-by-frame sprite sheets | No rigs; every action is a drawn sequence; specs below keep the cost bounded |
| Frame budget | Lean: 8 fps, 4–8 frames per action, about 40 frames per enemy | One 2048 px atlas per enemy; telegraphs read through key poses |
| Beat sync | Clips authored as a whole number of beats and retimed to the track BPM at runtime | Idle and wind-ups land on beats from the DSP clock at any BPM |
| Audio | Unity built-in audio, DSP-time scheduling | Tracks carry a tempo map (PRD 3.3.1.9); the beat clock follows BPM changes |
| Charts | One chart per enemy on that enemy's own track, actions on beats or quarter beats, chart and song loop from the start together (PRD 3.6.31, 3.6.32) | Every battle is a designed piece of music; a chart authoring tool is needed before World 2 content |
| UI | uGUI | World-space canvases for the Rhythm Line and slot rows; screen-space for menus |

## Artist spec that follows

- Character height 360–440 px at 1080p, feet on a shared baseline, facing right; the player faces right and enemies face left.
- Required actions per enemy: idle (loop, 2 beats), attack left, attack right, defend, charge wind-up (loop, 1 beat), hit, death. Bosses add one clip per phase transition.
- Attack clips: the strike frame is marked in the sheet metadata; the client aligns that frame to the action's beat and plays the wind-up frames before it.
- Every clip's frame count is authored for 8 fps at BPM 120; the client scales playback speed by the BPM in force ÷ 120, so a tempo change mid-track retimes clips at the change.
- Deliver as PNG sequences named `spr_<kind>_<subject>_<action>_<nn>.png`; the importer packs them into the subject's sheet and the World atlas.
- Cards: one 512 × 720 px illustration per card plus a shared frame per category colour; icons for statuses at 64 px.

## Defaults set without a question (override here)

- **Aspect and safe area:** 16:9 canvas, layout designed inside a 16:10 safe area; ultrawide letterboxes the HUD and extends background only.
- **Folders:** `Assets/_Project/{Art,Audio,Data,Prefabs,Scenes,Scripts,UI}`; third-party packages stay outside `_Project`. Art is split by World, then by subject.
- **Atlases:** one per World (enemies and props), one for cards, one for UI; each enemy's frames live in its World atlas.
- **Content to visuals:** JSON definitions in the simulation; `VisualCatalogue` ScriptableObjects map card, enemy, status and Charm ids to art and prefabs. No art reference ever lives in a definition.
- **Addressables:** not used; direct references from catalogues.
- **Version control:** Git LFS for `.png .psd .wav .ogg .aseprite`; Unity force-text serialisation; UnityYAMLMerge configured.
- **Text:** all player-facing strings in a string table asset from the first screen (PRD 3.12.7).
- **Scenes:** persistent `Boot` (profile store, audio, beat clock) plus additive `Map`, `Battle`, `Binder`, `Calibration`, `RunEnd`.
- **Assemblies:** `Chiki.Sim` (DLL), `Chiki.Client`, `Chiki.Client.Editor`, `Chiki.Client.Tests.EditMode`, `Chiki.Client.Tests.PlayMode`.
- **Frame pacing:** vsync on by default, target 60 fps; every beat-driven visual reads the DSP clock, never `Time.time` or Animator time.
- **Sprite animation driver:** a `BeatAnimator` component that steps frames from the beat clock, so sheets stay in sync with the track and with each other.
- **Chart files:** JSON per enemy under `data/charts/`, positions as integers in quarter beats, validated in the simulation suite. Authoring starts in a text editor for the demo; a waveform chart editor in the Unity editor is planned before World 2 content.
- **Track metadata:** each audio file ships with a JSON sidecar holding offset, length in beats and the tempo map; the composer supplies BPM markers per section.
