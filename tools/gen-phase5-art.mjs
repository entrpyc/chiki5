// Draws the Phase 5 art of docs/plan.md: the arena background (P5.3), Lulu's seven clips (P5.4)
// and Ren's seven clips (P5.5), each with the sidecar that gives every clip its beats, loop flag
// and strike frame. Characters come from the shared puppet in puppet-lib.mjs: a skeleton posed
// once per frame and filled as outlined rounded limbs on a 512 x 512 canvas, feet on the canvas's
// own baseline so the bottom-centre pivot the importer gives these kinds puts every frame on the
// stage's floor line.
//
// Run: node tools/gen-phase5-art.mjs
//
// Three rules shape the characters. A clip is a list of joint angles, never a list of pixels, so
// a strike frame is the pose it names and the frames around it are the same pose on its way in
// and out. Frames are authored at one per quarter beat, so a six-frame attack whose strike is
// frame 3 starts half a beat early and the client needs to know nothing but the strike number.
// And Lulu and Ren are told apart with the colour removed: Lulu is small-bodied and big-headed
// with a wand, Ren is a head shorter in proportion but half again as wide, helmeted, with a bar.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  canvas,
  lerp,
  paint,
  paintAll,
  sdCircle,
  sdPolygon,
  sdRoundBox,
  sdSegment,
  writePng,
} from './art-lib.mjs'
import { FRAME, SOLE, between, drawPose, grip, mirror, opaqueBox, pose } from './puppet-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ART = join(ROOT, 'client', 'Assets', '_Project', 'Art')
const LULU = join(ART, 'Shared', 'lulu')
const REN = join(ART, 'World1', 'ren')
const BG = join(ART, 'Shared', 'bg')

const INK = [14, 12, 20]
const written = []

function write(folder, name, contents, note) {
  const path = join(folder, name)
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, contents)
  written.push({ name, size: '', note })
}

function emit(folder, name, c, note) {
  writePng(join(folder, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

// ------------------------------------------------------------------ poses ---

/** The frames between two poses, the first being `from` and the last just short of `to`. */
function ramp(from, to, count) {
  return Array.from({ length: count }, (_, i) => between(from, to, count === 1 ? 1 : i / count))
}

/** Eight frames of standing and breathing over two beats; frame 1 is the square, tallest pose. */
function idleFrames() {
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
function strikeFrames(windUp, strike) {
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
const attackLeft = () =>
  strikeFrames(
    pose({ shift: -8, crouch: 9, lean: -13, headTilt: 4, nearArm: [-72, -42], farArm: [-30, -18], nearLeg: [-8, 4] }),
    pose({ shift: 14, crouch: 15, lean: 21, headTilt: -6, nearArm: [96, 24], farArm: [-44, -12], nearLeg: [30, -20], farLeg: [-22, 10], reach: 1 }),
  )

/** An overhead chop: the arm goes up and behind, then down through the landing. */
const attackRight = () =>
  strikeFrames(
    pose({ shift: -6, crouch: 7, lean: -11, headTilt: 6, nearArm: [-152, -28], farArm: [-56, -20], nearLeg: [-6, 3] }),
    pose({ shift: 11, crouch: 19, lean: 26, headTilt: -10, nearArm: [56, 46], farArm: [-20, -34], nearLeg: [26, -17], farLeg: [-18, 8], reach: 1 }),
  )

/** Five frames of guard: both arms come up across the chest, hold, and come down. */
function defendFrames() {
  const rest = pose({ crouch: 5, lean: 2 })
  const guard = pose({ shift: -7, crouch: 19, lean: 9, headTilt: -8, nearArm: [64, -98], farArm: [58, -102], nearLeg: [10, -8], farLeg: [-14, 8] })
  return [between(rest, guard, 0.55), guard, guard, between(guard, rest, 0.4), between(guard, rest, 0.8)]
}

/** Six frames of Lulu's Ability: a gathering crouch, then both arms thrown overhead on the beat. */
const abilityFrames = () =>
  strikeFrames(
    pose({ crouch: 16, lean: -6, headTilt: 8, nearArm: [-22, -24], farArm: [-18, -20] }),
    pose({ bob: -7, crouch: 7, lean: -9, headTilt: 12, nearArm: [-166, 12], farArm: [-150, 16], nearLeg: [8, -6], farLeg: [-8, 6], reach: 1 }),
  )

/** Four frames of Ren's wind-up, looping over one beat: the bar hauled back, coiling tighter. */
function chargeFrames() {
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
function hitFrames() {
  return [
    pose({ shift: -11, crouch: 11, lean: -19, headTilt: -15, nearArm: [-44, -30], farArm: [-52, -26] }),
    pose({ shift: -15, crouch: 17, lean: -25, headTilt: -19, nearArm: [-58, -22], farArm: [-64, -18], nearLeg: [-14, 9] }),
    pose({ shift: -7, crouch: 11, lean: -11, headTilt: -9, nearArm: [-30, -16], farArm: [-34, -14] }),
    pose({ shift: -2, crouch: 6, lean: -3, headTilt: -3, nearArm: [2, 8], farArm: [-6, -10] }),
  ]
}

/** Eight frames of collapse, the last of them the one the client holds. */
function deathFrames() {
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

// ------------------------------------------------------------- characters ---

/**
 * Lulu (PRD 3.14.1): the cute exterior. A big head on a small body, a rose bob with two tufts,
 * a mint coat and a wand — the only round-headed, light-limbed silhouette on the stage.
 */
const lulu = {
  kind: 'player',
  subject: 'lulu',
  folder: LULU,
  facesLeft: false,
  headR: 63,
  neck: 9,
  torsoH: 96,
  torsoW: 70,
  torsoRound: 22,
  shoulderOut: 22,
  shoulderDrop: 6,
  hipOut: 11,
  upperArm: 54,
  foreArm: 50,
  armR: 12,
  thigh: 74,
  shin: 70,
  legR: 15,
  footLength: 26,
  skin: [250, 216, 192],
  sleeve: [126, 216, 190],
  coat: [96, 196, 176],
  trouser: [72, 92, 128],
  boot: [56, 66, 96],
  hair: [232, 118, 152],
  accent: [252, 240, 216],
  draw(c, j, s, p, kit) {
    // The bob: one blob behind the head and two tufts, drawn over the skull so the face is the
    // lower third of it. Nothing else on the stage has hair this wide.
    kit.blob(c, { x: j.head.x - 6, y: j.head.y - 12 }, s.headR * 0.98, s.hair)
    kit.blob(c, { x: j.head.x - s.headR * 0.78, y: j.head.y + 6 }, s.headR * 0.42, s.hair)
    kit.blob(c, { x: j.head.x + s.headR * 0.7, y: j.head.y - 2 }, s.headR * 0.36, s.hair)
    // The face: the skin shows below the fringe.
    paint(c, sdCircle(j.head.x + 8, j.head.y + 20, s.headR * 0.62), { fill: s.skin, box: box(j.head, s.headR + 30) })
    // Two eyes and a collar, the only marks on the character.
    paint(c, sdCircle(j.head.x + 2, j.head.y + 16, 6.5), { fill: INK, alpha: 0.9, box: box(j.head, s.headR + 30) })
    paint(c, sdCircle(j.head.x + 30, j.head.y + 14, 6.5), { fill: INK, alpha: 0.9, box: box(j.head, s.headR + 30) })
    kit.slab(c, j.shoulder.x, j.shoulder.y + 4, s.torsoW * 0.33, 9, 7, s.accent, p.lean)
  },
  implement(c, j, s) {
    // The wand: a slim shaft along the forearm with a lit tip, so a strike reads as a strike.
    const g = grip(j)
    const tip = { x: g.x + g.ux * 78, y: g.y + g.uy * 78 }
    paint(c, sdSegment(g.x, g.y, tip.x, tip.y, 6), { fill: INK, grow: 3, alpha: 0.95, box: span(g, tip, 14) })
    paint(c, sdSegment(g.x, g.y, tip.x, tip.y, 6), { fill: s.accent, box: span(g, tip, 14) })
    paint(c, sdCircle(tip.x, tip.y, 13), { fill: INK, grow: 3, alpha: 0.95, box: box(tip, 20) })
    paint(c, sdCircle(tip.x, tip.y, 13), { fill: s.hair, box: box(tip, 20) })
  },
}

/**
 * Ren (PRD 3.14.1, plan P5.5): the tutorial Tank. Half again as wide as Lulu, a small helmeted
 * head sunk between heavy pauldrons, and a bar held in both hands — a block of a silhouette that
 * reads as something to be timed rather than outrun.
 */
const ren = {
  kind: 'enemy',
  subject: 'ren',
  folder: REN,
  facesLeft: true,
  headR: 46,
  neck: 6,
  torsoH: 116,
  torsoW: 108,
  torsoRound: 26,
  shoulderOut: 36,
  shoulderDrop: 10,
  hipOut: 16,
  upperArm: 60,
  foreArm: 56,
  armR: 16,
  thigh: 78,
  shin: 74,
  legR: 20,
  footLength: 32,
  skin: [214, 176, 150],
  sleeve: [74, 84, 112],
  coat: [58, 68, 96],
  trouser: [44, 50, 72],
  boot: [34, 38, 56],
  hair: [104, 116, 138],
  accent: [196, 152, 74],
  draw(c, j, s, p, kit) {
    // The helmet: iron over the top two thirds of the skull with a brass brow, and a visor slit.
    paint(c, sdCircle(j.head.x, j.head.y - 6, s.headR * 1.04), { fill: INK, grow: 3.2, alpha: 0.95, box: box(j.head, s.headR + 30) })
    paint(c, sdCircle(j.head.x, j.head.y - 6, s.headR * 1.04), { fill: s.hair, box: box(j.head, s.headR + 30) })
    kit.slab(c, j.head.x + 4, j.head.y + 6, s.headR * 0.96, 7, 5, s.accent, p.lean)
    paint(c, sdRoundBox(j.head.x + 12, j.head.y + 22, 18, 5, 4), { fill: INK, alpha: 0.75, box: box(j.head, s.headR + 30) })
    // The pauldrons: one blob on each shoulder, the near one over the arm it caps.
    kit.blob(c, { x: j.shoulder.x - s.shoulderOut, y: j.shoulder.y + 2 }, 28, lerp(s.hair, [0, 0, 0], 0.3))
    kit.blob(c, { x: j.shoulder.x + s.shoulderOut, y: j.shoulder.y + 2 }, 30, s.hair)
    // The chest plate, held inside the torso so the slab never breaks the silhouette.
    kit.slab(c, (j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2 - 6, s.torsoW * 0.3, s.torsoH * 0.28, 10, s.accent, p.lean)
  },
  implement(c, j, s) {
    // The bar: a long slab along the forearm with a blunt head, held in the near hand.
    const g = grip(j)
    const butt = { x: g.x - g.ux * 26, y: g.y - g.uy * 26 }
    const tip = { x: g.x + g.ux * 104, y: g.y + g.uy * 104 }
    paint(c, sdSegment(butt.x, butt.y, tip.x, tip.y, 10), { fill: INK, grow: 3.2, alpha: 0.95, box: span(butt, tip, 18) })
    paint(c, sdSegment(butt.x, butt.y, tip.x, tip.y, 10), { fill: lerp(s.hair, [255, 255, 255], 0.12), box: span(butt, tip, 18) })
    const head = { x: g.x + g.ux * 96, y: g.y + g.uy * 96 }
    paint(c, sdCircle(head.x, head.y, 22), { fill: INK, grow: 3.2, alpha: 0.95, box: box(head, 30) })
    paint(c, sdCircle(head.x, head.y, 22), { fill: s.accent, box: box(head, 30) })
  },
}

const box = (p, r) => [p.x - r, p.y - r, p.x + r, p.y + r]
const span = (a, b, r) => [Math.min(a.x, b.x) - r, Math.min(a.y, b.y) - r, Math.max(a.x, b.x) + r, Math.max(a.y, b.y) + r]

/** The clip set of docs/project/unity-setup.md: the enemy's seven, with Ability for the player's Charge. */
function clipsOf(character) {
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

function drawCharacter(character) {
  const clips = clipsOf(character)
  const heights = []
  for (const clip of clips) {
    clip.frames.forEach((p, i) => {
      let c = drawPose(character, p)
      if (character.facesLeft) {
        c = mirror(c)
      }

      const name = `spr_${character.kind}_${character.subject}_${clip.variant}_${(i + 1).toString().padStart(2, '0')}.png`
      emit(character.folder, name, c, i + 1 === clip.strike ? 'strike frame' : `${clip.variant} ${i + 1}`)
      if (clip.variant === 'idle' && i === 0) {
        heights.push(opaqueBox(c).height)
      }
    })
  }

  const body = clips
    .map((clip) => {
      const strike = clip.strike ? `, "strikeFrame": ${clip.strike}` : ''
      return `    "${clip.variant}": { "beats": ${clip.beats}, "loop": ${clip.loop}${strike} }`
    })
    .join(',\n')
  write(
    character.folder,
    `${character.kind}_${character.subject}.clips.json`,
    `{\n  "clips": {\n${body}\n  }\n}\n`,
    'clip sidecar',
  )

  const frames = clips.reduce((n, clip) => n + clip.frames.length, 0)
  console.log(
    `${character.subject.padEnd(6)} ${clips.length} clips, ${frames} frames, idle frame 1 stands ${heights[0]} px tall`,
  )
  if (heights[0] < 360 || heights[0] > 440) {
    throw new Error(`${character.subject} stands ${heights[0]} px; the spec is 360 to 440 (docs/plan.md P5.4, P5.5)`)
  }
}

// ----------------------------------------------------- P5.3 the arena ---

/**
 * The arena (P5.3): 2560 x 1080, whose central 1920 is the 16:9 frame and whose outer 320 px a
 * side hold nothing essential. The floor line sits 200 px above the bottom edge, which is where
 * both fighters' feet stand. Everything is drawn in bands so the eye reads floor, wall and sky
 * without a single detail competing with the Rhythm Line above it.
 */
function arena() {
  const w = 2560
  const h = 1080
  const floorY = h - 200
  const mid = w / 2
  const c = canvas(w, h)

  const sky = [22, 20, 34]
  const haze = [58, 44, 68]
  const stone = [44, 42, 58]
  const plate = [62, 58, 74]

  // Sky: cool above, warmer toward the horizon the arena wall stands on.
  paintAll(
    c,
    (_, y) => lerp(sky, haze, Math.min(1, Math.max(0, y / floorY))),
    () => 1,
  )

  // The wall: a band of arches right across, each one darker inside than the stone around it.
  const wallTop = floorY - 300
  paint(c, sdRoundBox(mid, (wallTop + floorY) / 2, w / 2, (floorY - wallTop) / 2, 0), { fill: stone })
  for (let x = 90; x < w; x += 190) {
    paint(c, sdRoundBox(x, floorY - 96, 52, 104, 50), { fill: lerp(sky, [0, 0, 0], 0.45) })
    paint(c, sdRoundBox(x, floorY - 96, 52, 104, 50), { stroke: lerp(stone, [255, 255, 255], 0.12), width: 5 })
  }

  // The lip the wall meets the floor on, so the floor line is visible without a drawn rule.
  paint(c, sdRoundBox(mid, floorY - 6, w / 2, 12, 0), { fill: lerp(plate, [255, 255, 255], 0.1) })

  // The floor: slabs in perspective, wider and lighter toward the viewer.
  for (let i = 0; i < 7; i++) {
    const t = i / 7
    const top = floorY + t * t * 200
    const next = floorY + ((i + 1) / 7) ** 2 * 200
    paint(c, sdRoundBox(mid, (top + next) / 2, w / 2, (next - top) / 2, 0), {
      fill: lerp(lerp(stone, plate, 0.4), lerp(plate, [124, 116, 132], 0.5), t),
    })
    paint(c, sdRoundBox(mid, next, w / 2, 1.5, 0), { fill: lerp(sky, [0, 0, 0], 0.3), alpha: 0.5 })
  }

  // The duelling ring the two fighters stand inside, an ellipse in the middle of the floor.
  paintAll(
    c,
    () => [176, 150, 108],
    (x, y) => {
      const d = Math.abs(Math.hypot((x - mid) / 640, (y - (floorY + 96)) / 84) - 1)
      return d < 0.035 ? 0.4 : 0
    },
  )

  // Two braziers, both inside the central 1920 so the 16:9 frame holds the whole light.
  for (const x of [mid - 700, mid + 700]) {
    paint(c, sdPolygon([
      [x - 26, floorY - 4],
      [x + 26, floorY - 4],
      [x + 16, floorY - 92],
      [x - 16, floorY - 92],
    ]), { fill: lerp(stone, [0, 0, 0], 0.3) })
    paintAll(
      c,
      () => [252, 186, 96],
      (px, py) => Math.max(0, 0.85 - Math.hypot(px - x, (py - (floorY - 118)) * 1.2) / 70),
    )
    paintAll(
      c,
      () => [236, 128, 52],
      (px, py) => Math.max(0, 0.32 - Math.hypot(px - x, (py - (floorY - 118)) * 0.8) / 620),
    )
  }

  // A vignette so the fighters sit in the lit middle and the outer 320 px a side fall away.
  paintAll(
    c,
    () => [0, 0, 0],
    (x, y) => Math.min(0.66, Math.max(0, Math.hypot((x - mid) / 1180, (y - h * 0.55) / 700) - 0.62)),
  )

  return c
}

// ------------------------------------------------------------------ run ---

emit(BG, 'spr_bg_arena_static_01.png', arena(), 'floor line 200 px above the bottom')
drawCharacter(lulu)
drawCharacter(ren)

const sprites = written.filter((f) => f.name.endsWith('.png')).length
console.log(`\n${sprites} sprites and ${written.length - sprites} sidecars written under Art/`)
console.log(`frame canvas ${FRAME} x ${FRAME}, soles on y = ${SOLE}`)
