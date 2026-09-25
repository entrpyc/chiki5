// Draws the Phase 5 art of docs/plan.md: the arena background (P5.3), Lulu's seven clips (P5.4)
// and Ren's seven clips (P5.5), each with the sidecar that gives every clip its beats, loop flag
// and strike frame. Characters come from the shared puppet in puppet-lib.mjs: a skeleton posed
// once per frame and filled as outlined rounded limbs on a 512 x 512 canvas, feet on the canvas's
// own baseline so the bottom-centre pivot the importer gives these kinds puts every frame on the
// stage's floor line. The clips themselves live in cast-lib.mjs, shared with the Phase 6 cast.
//
// Run: node tools/gen-phase5-art.mjs
//
// Lulu and Ren are told apart with the colour removed: Lulu is small-bodied and big-headed with
// a wand, Ren is a head shorter in proportion but half again as wide, helmeted, with a bar.

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
import { box, drawCharacter, span } from './cast-lib.mjs'
import { FRAME, SOLE, grip } from './puppet-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ART = join(ROOT, 'client', 'Assets', '_Project', 'Art')
const LULU = join(ART, 'Shared', 'lulu')
const REN = join(ART, 'World1', 'ren')
const BG = join(ART, 'Shared', 'bg')

const INK = [14, 12, 20]
const written = []

function emit(folder, name, c, note) {
  writePng(join(folder, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

// ------------------------------------------------------------- characters ---

/**
 * Lulu (PRD 3.14.1): the cute exterior. A big head on a small body, a rose bob with two tufts,
 * a mint coat and a wand — the only round-headed, light-limbed silhouette on the stage.
 */
export const lulu = {
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
export const ren = {
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

// Run as a script it writes the art; imported, it only lends its characters (the portraits of
// gen-phase8-art.mjs draw Ren from here, so the portrait is the fighter).
const isMain = process.argv[1] !== undefined && resolve(process.argv[1]) === fileURLToPath(import.meta.url)

if (isMain) {
  emit(BG, 'spr_bg_arena_static_01.png', arena(), 'floor line 200 px above the bottom')
  drawCharacter(lulu, written)
  drawCharacter(ren, written)

  const sprites = written.filter((f) => f.name.endsWith('.png')).length
  console.log(`\n${sprites} sprites and ${written.length - sprites} sidecars written under Art/`)
  console.log(`frame canvas ${FRAME} x ${FRAME}, soles on y = ${SOLE}`)
}
