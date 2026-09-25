// The clip set every fighter on the stage is drawn with (docs/plan.md, Phases 5 and 6), lifted
// out of gen-phase5-art.mjs so Ren and the four enemies of Phase 6 come off the same poses. A
// clip is a list of joint angles, never a list of pixels, so a strike frame is the pose it names
// and the frames around it are the same pose on its way in and out. Frames are authored at one
// per quarter beat, so a six-frame attack whose strike is frame 3 starts half a beat early and
// the client needs to know nothing but the strike number.
//
// A character is the proportion, palette and decoration record the puppet in puppet-lib.mjs
// draws; this file owns only what every character shares — the seven clips, the sidecar, and the
// height the plan holds each one to.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { writePng } from './art-lib.mjs'
import { FRAME, between, drawPose, mirror, opaqueBox, pose } from './puppet-lib.mjs'

/** The drawn height every character stands at (docs/plan.md P5.4, P5.5). */
export const HEIGHT = [360, 440]

/** A bounding box around a point, for the decoration a character paints over its own joints. */
export const box = (p, r) => [p.x - r, p.y - r, p.x + r, p.y + r]

/** A bounding box around two points, for a held implement's shaft. */
export const span = (a, b, r) => [
  Math.min(a.x, b.x) - r,
  Math.min(a.y, b.y) - r,
  Math.max(a.x, b.x) + r,
  Math.max(a.y, b.y) + r,
]

// ------------------------------------------------------------------ poses ---

/** The frames between two poses, the first being `from` and the last just short of `to`. */
function ramp(from, to, count) {
  return Array.from({ length: count }, (_, i) => between(from, to, count === 1 ? 1 : i / count))
}

/** Eight frames of standing and breathing over two beats; frame 1 is the square, tallest pose. */
export function idleFrames() {
  return Array.from({ length: 8 }, (_, i) => {
    const breath = Math.sin((i / 8) * Math.PI * 2)
    return pose({
      bob: -3 * breath,
      crouch: 5 + 2 * breath,
      lean: 2 + 1.5 * breath,
      headTilt: -2 - 2 * breath,
      nearArm: [10 + 4 * breath, 16 + 4 * breath],
      farArm: [-8 - 3 * breath, -14 - 3 * breath],
    })
  })
}

/** Six frames whose third is the strike: two of wind-up, the strike, three of recovery. */
export function strikeFrames(windUp, strike) {
  const rest = pose({ crouch: 5, lean: 2, nearArm: [10, 16], farArm: [-8, -14] })
  return [
    windUp,
    between(windUp, strike, 0.42),
    strike,
    between(strike, rest, 0.45),
    between(strike, rest, 0.78),
    rest,
  ]
}

/** A low swing that comes in from behind and finishes level with the floor. */
export const attackLeft = () =>
  strikeFrames(
    pose({ shift: -8, crouch: 9, lean: -13, headTilt: 4, nearArm: [-72, -42], farArm: [-30, -18], nearLeg: [-8, 4] }),
    pose({ shift: 14, crouch: 15, lean: 21, headTilt: -6, nearArm: [96, 24], farArm: [-44, -12], nearLeg: [30, -20], farLeg: [-22, 10], reach: 1 }),
  )

/** An overhead chop: the arm goes up and behind, then down through the landing. */
export const attackRight = () =>
  strikeFrames(
    pose({ shift: -6, crouch: 7, lean: -11, headTilt: 6, nearArm: [-152, -28], farArm: [-56, -20], nearLeg: [-6, 3] }),
    pose({ shift: 11, crouch: 19, lean: 26, headTilt: -10, nearArm: [56, 46], farArm: [-20, -34], nearLeg: [26, -17], farLeg: [-18, 8], reach: 1 }),
  )

/** Five frames of guard: both arms come up across the chest, hold, and come down. */
export function defendFrames() {
  const rest = pose({ crouch: 5, lean: 2 })
  const guard = pose({ shift: -7, crouch: 19, lean: 9, headTilt: -8, nearArm: [64, -98], farArm: [58, -102], nearLeg: [10, -8], farLeg: [-14, 8] })
  return [between(rest, guard, 0.55), guard, guard, between(guard, rest, 0.4), between(guard, rest, 0.8)]
}

/** Six frames of the player's Ability: a gathering crouch, then both arms thrown overhead on the beat. */
export const abilityFrames = () =>
  strikeFrames(
    pose({ crouch: 16, lean: -6, headTilt: 8, nearArm: [-22, -24], farArm: [-18, -20] }),
    pose({ bob: -7, crouch: 7, lean: -9, headTilt: 12, nearArm: [-166, 12], farArm: [-150, 16], nearLeg: [8, -6], farLeg: [-8, 6], reach: 1 }),
  )

/** Four frames of an enemy's wind-up, looping over one beat: the weapon hauled back, coiling tighter. */
export function chargeFrames() {
  return Array.from({ length: 4 }, (_, i) => {
    const t = i / 4
    const coil = Math.sin(t * Math.PI * 2)
    return pose({
      shift: -7 - 2 * coil,
      crouch: 18 + 3 * coil,
      lean: -16 - 3 * coil,
      headTilt: 7,
      nearArm: [-138 - 6 * coil, -34],
      farArm: [-104 - 5 * coil, -28],
      nearLeg: [-9, 5],
      farLeg: [14, -9],
    })
  })
}

/** Four frames of recoil: thrown back off the beat, then back on both feet. */
export function hitFrames() {
  return [
    pose({ shift: -11, crouch: 11, lean: -19, headTilt: -15, nearArm: [-44, -30], farArm: [-52, -26] }),
    pose({ shift: -15, crouch: 17, lean: -25, headTilt: -19, nearArm: [-58, -22], farArm: [-64, -18], nearLeg: [-14, 9] }),
    pose({ shift: -7, crouch: 11, lean: -11, headTilt: -9, nearArm: [-30, -16], farArm: [-34, -14] }),
    pose({ shift: -2, crouch: 6, lean: -3, headTilt: -3, nearArm: [2, 8], farArm: [-6, -10] }),
  ]
}

/** Eight frames of collapse, the last of them the one the client holds. */
export function deathFrames() {
  const standing = pose({ crouch: 6, lean: -4, headTilt: -6, nearArm: [-16, -14], farArm: [-20, -12] })
  const down = pose({
    shift: -52,
    crouch: 112,
    lean: 74,
    headTilt: 16,
    nearArm: [4, 34],
    farArm: [0, 30],
    nearLeg: [-70, 140],
    farLeg: [-64, 136],
  })
  return [...ramp(standing, down, 7), down]
}

/** The clip set of docs/project/unity-setup.md: the enemy's seven, with Ability for the player's Charge. */
export function clipsOf(character) {
  const shared = [
    { variant: 'idle', frames: idleFrames(), beats: 2, loop: true },
    { variant: 'attack-left', frames: attackLeft(), beats: 2, loop: false, strike: 3 },
    { variant: 'attack-right', frames: attackRight(), beats: 2, loop: false, strike: 3 },
    { variant: 'defend', frames: defendFrames(), beats: 2, loop: false },
  ]

  const middle =
    character.kind === 'player'
      ? [{ variant: 'ability', frames: abilityFrames(), beats: 2, loop: false, strike: 3 }]
      : [{ variant: 'charge', frames: chargeFrames(), beats: 1, loop: true }]

  return [
    ...shared,
    ...middle,
    { variant: 'hit', frames: hitFrames(), beats: 1, loop: false },
    { variant: 'death', frames: deathFrames(), beats: 2, loop: false },
  ]
}

// --------------------------------------------------------------- writing ---

/**
 * Draws one character's seven clips and the sidecar beside them, and returns the opaque box of
 * the first idle frame so the caller can hold the cast to the plan's height and check that no
 * two of them share a silhouette. `written` collects a line per file for the run's summary.
 */
export function drawCharacter(character, written = []) {
  const clips = clipsOf(character)
  const cropped = []
  let first = null
  for (const clip of clips) {
    clip.frames.forEach((p, i) => {
      let c = drawPose(character, p)
      if (character.facesLeft) {
        c = mirror(c)
      }

      const name = `spr_${character.kind}_${character.subject}_${clip.variant}_${(i + 1).toString().padStart(2, '0')}.png`
      const path = join(character.folder, name)
      mkdirSync(dirname(path), { recursive: true })
      writePng(path, c)
      written.push({ name, size: `${c.w} x ${c.h}`, note: i + 1 === clip.strike ? 'strike frame' : `${clip.variant} ${i + 1}` })
      const drawn = opaqueBox(c)
      // A frame that touches the left, right or top edge is a cropped character. The bottom is
      // the baseline the soles and their outline deliberately sit on, so it never counts.
      if (drawn.x === 0 || drawn.y === 0 || drawn.x + drawn.width >= c.w) {
        cropped.push(`${clip.variant} frame ${i + 1}`)
      }

      if (clip.variant === 'idle' && i === 0) {
        first = drawn
      }
    })
  }

  const body = clips
    .map((clip) => {
      const strike = clip.strike ? `, "strikeFrame": ${clip.strike}` : ''
      return `    "${clip.variant}": { "beats": ${clip.beats}, "loop": ${clip.loop}${strike} }`
    })
    .join(',\n')
  const sidecar = `${character.kind}_${character.subject}.clips.json`
  const sidecarPath = join(character.folder, sidecar)
  mkdirSync(dirname(sidecarPath), { recursive: true })
  writeFileSync(sidecarPath, `{\n  "clips": {\n${body}\n  }\n}\n`)
  written.push({ name: sidecar, size: '', note: 'clip sidecar' })

  const frames = clips.reduce((n, clip) => n + clip.frames.length, 0)
  console.log(
    `${character.subject.padEnd(6)} ${clips.length} clips, ${frames} frames, idle frame 1 stands ${first.height} px tall and ${first.width} px wide`,
  )
  if (first.height < HEIGHT[0] || first.height > HEIGHT[1]) {
    throw new Error(`${character.subject} stands ${first.height} px; the spec is ${HEIGHT[0]} to ${HEIGHT[1]} (docs/plan.md P5.4, P5.5)`)
  }

  if (cropped.length > 0) {
    console.log(`${' '.repeat(6)} ${cropped.length} frame(s) touch the ${FRAME} px canvas edge: ${cropped.join(', ')}`)
  }

  return { ...first, cropped }
}
