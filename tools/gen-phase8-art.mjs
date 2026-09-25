// Draws the Phase 8 art of docs/plan.md: the five enemy portraits (P8.3), the New badge plate
// with its 9-slice sidecar (P8.2), and the four power icons and three role icons of the enemy
// card (P8.4). Thirteen sprites, each at the size its item states, as 8-bit RGBA PNG with
// straight alpha, named by the asset conventions.
//
// Run: node tools/gen-phase8-art.mjs
//
// A portrait is the fighter itself: the character record gen-phase5-art.mjs (Ren) or
// gen-phase6-art.mjs (Kess, Vey, Orm, Malk) draws the stage from, in its first idle pose, drawn
// again through a view (art-lib.mjs, `paint`) that magnifies head and shoulders onto the
// 512 x 512 canvas. Every edge is resolved at the portrait's own resolution rather than enlarged
// from a fighter frame, and a change to a fighter redraws its portrait on the next run.
//
// The icons follow the Phase 4 rule: a power is told apart by its silhouette, not its hue, so
// the seven read in greyscale — a stair, a bolt, a buckler, a cut stone, a blade, a tower, an eye.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  bleed,
  canvas,
  lerp,
  paint,
  sdCircle,
  sdPolygon,
  sdRoundBox,
  sdRoundPolygon,
  sdSegment,
  union,
  writePng,
} from './art-lib.mjs'
import { idleFrames } from './cast-lib.mjs'
import { ren } from './gen-phase5-art.mjs'
import { kess, malk, orm, vey } from './gen-phase6-art.mjs'
import { INK, drawPose, drawPoseOn, joints, mirror, opaqueBox } from './puppet-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ART = join(ROOT, 'client', 'Assets', '_Project', 'Art')
const WORLD1 = join(ART, 'World1')
const CARD = join(ART, 'Shared', 'enemy-card')

const written = []

function emit(folder, name, c, note) {
  bleed(c)
  writePng(join(folder, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

function write(folder, name, contents, note) {
  const path = join(folder, name)
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, contents)
  written.push({ name, size: '', note })
}


// ---------------------------------------------------------- P8.3 portraits ---

const PORTRAIT = 512

/** The portrait's outline in canvas pixels: heavier than a fighter frame's, as a close-up's should be. */
const OUTLINE = 6

/** The margin kept above the tallest point of the headpiece, in the fighter's own pixels. */
const HEADROOM = 10

/**
 * Head and shoulders of one fighter, facing left as it stands on the stage: its first idle pose,
 * framed from just above the top of its headpiece to below its shoulders, centred on its head and
 * magnified so that span fills the canvas. The fighter's frame is drawn first at its own scale
 * only to find where the headpiece tops out.
 */
function portrait(s) {
  const p = idleFrames()[0]
  const j = joints(s, p)
  const top = opaqueBox(drawPose(s, p)).y - HEADROOM
  const bottom = j.shoulder.y + s.torsoH * 0.62
  const k = PORTRAIT / (bottom - top)
  const c = canvas(PORTRAIT, PORTRAIT)
  c.view = { k, ox: j.head.x - PORTRAIT / k / 2, oy: top, line: OUTLINE / 3.2 }
  drawPoseOn(c, s, p)
  delete c.view
  return s.facesLeft ? mirror(c) : c
}

for (const s of [ren, kess, vey, orm, malk]) {
  emit(join(WORLD1, s.subject), `spr_portrait_${s.subject}_static_01.png`, portrait(s), 'head and shoulders, facing left')
}

// ------------------------------------------------------ P8.2 the New badge ---

/**
 * The plate the New badge's text sits on: 160 x 48, a gold pill with an ink rim, a highlight and
 * a shadow each held inside the 16 px border strips, so the middle that stretches is one flat fill.
 */
{
  const w = 160
  const h = 48
  const border = 16
  const c = canvas(w, h)
  const gold = [242, 196, 84]
  const pill = sdRoundBox(w / 2, h / 2, w / 2 - 2, h / 2 - 2, 14)
  paint(c, pill, { fill: INK, grow: 1.5, alpha: 0.95 })
  paint(c, pill, { fill: gold })
  const inside = (x, y) => pill(x, y) + 3
  paint(c, (x, y) => Math.max(inside(x, y), y - 8), { fill: lerp(gold, [255, 255, 255], 0.45), clip: inside })
  paint(c, (x, y) => Math.max(inside(x, y), h - 8 - y), { fill: lerp(gold, [120, 70, 20], 0.35), clip: inside })
  emit(CARD, 'spr_ui_badge_new_01.png', c, `9-slice, ${border} px borders, no lettering`)
  write(CARD, 'ui_badge.slices.json', `{\n  "borders": {\n    "new": { "left": ${border}, "bottom": ${border}, "right": ${border}, "top": ${border} }\n  }\n}\n`, 'slice sidecar')
}

// ------------------------------------------------ P8.4 power and role icons ---

/** Every icon: an ink outline swelled under the shape, then the shape in its colour, on 64 x 64. */
function icon(folder, name, sdf, colour, detail = null, note = '') {
  const c = canvas(64, 64)
  paint(c, sdf, { fill: INK, grow: 2, alpha: 0.92 })
  paint(c, sdf, { fill: colour })
  if (detail) {
    detail(c)
  }

  emit(folder, name, c, note)
}

// Rising Tempo (A01): three steps climbing to the right under an arrow — damage that climbs.
icon(
  CARD,
  'spr_ability_rising-tempo_static_01.png',
  union(
    sdRoundBox(15, 47, 6, 8, 2),
    sdRoundBox(30, 41, 6, 14, 2),
    sdRoundBox(45, 35, 6, 20, 2),
    sdSegment(12, 28, 40, 10, 3.2),
    sdPolygon([[34, 6], [50, 6], [44, 20]]),
  ),
  [236, 108, 60],
  null,
  'a stair under an arrow',
)

// Charge / Buff (A13): a bolt — the wind-up that ends in an empowered move.
icon(
  CARD,
  'spr_ability_charge-buff_static_01.png',
  sdRoundPolygon([[38, 4], [14, 36], [30, 36], [24, 60], [50, 26], [34, 26], [42, 4]], 1.2),
  [246, 214, 72],
  null,
  'a bolt',
)

// Guard (T07): a round buckler with a boss — Block held from the first beat.
icon(
  CARD,
  'spr_trait_guard_static_01.png',
  sdCircle(32, 32, 26),
  [96, 142, 204],
  (c) => {
    paint(c, (x, y) => Math.abs(Math.hypot(x - 32, y - 32) - 19) - 1.6, { fill: INK, alpha: 0.4 })
    paint(c, sdCircle(32, 32, 8), { fill: INK, grow: 1.5, alpha: 0.85 })
    paint(c, sdCircle(32, 32, 8), { fill: [196, 214, 236] })
  },
  'a buckler',
)

// Stoneform (T02): a cut stone with a crack — Block that sets when the enemy is left alone.
icon(
  CARD,
  'spr_trait_stoneform_static_01.png',
  sdRoundPolygon([[20, 8], [46, 6], [60, 28], [50, 56], [18, 58], [4, 32]], 1.5),
  [150, 156, 146],
  (c) => {
    paint(c, sdSegment(26, 16, 34, 32, 1.4), { fill: INK, alpha: 0.6 })
    paint(c, sdSegment(34, 32, 28, 46, 1.4), { fill: INK, alpha: 0.6 })
    paint(c, sdSegment(34, 32, 48, 38, 1.4), { fill: INK, alpha: 0.6 })
    paint(c, sdPolygon([[20, 8], [46, 6], [40, 14], [24, 15]]), { fill: [255, 255, 255], alpha: 0.28 })
  },
  'a cut stone',
)

// Aggressor (PRD 3.6.1): a blade pointing up — high damage.
icon(
  CARD,
  'spr_role_aggressor_static_01.png',
  union(
    sdPolygon([[32, 3], [42, 16], [38, 44], [26, 44], [22, 16]]),
    sdRoundBox(32, 46, 16, 3.5, 2),
    sdRoundBox(32, 55, 4, 7, 2),
  ),
  [214, 64, 64],
  (c) => paint(c, sdSegment(32, 10, 32, 40, 1.2), { fill: INK, alpha: 0.35 }),
  'a blade',
)

// Tank (PRD 3.6.1): a crenellated tower — Block and stances.
icon(
  CARD,
  'spr_role_tank_static_01.png',
  union(
    sdRoundBox(32, 38, 20, 22, 2),
    sdRoundBox(16, 13, 4, 6, 1),
    sdRoundBox(32, 13, 4, 6, 1),
    sdRoundBox(48, 13, 4, 6, 1),
  ),
  [74, 124, 196],
  (c) => paint(c, sdRoundBox(32, 49, 6, 10, 5), { fill: INK, alpha: 0.6 }),
  'a tower',
)

// Mentalist (PRD 3.6.1): an eye — statuses, control and deception.
icon(
  CARD,
  'spr_role_mentalist_static_01.png',
  (x, y) => Math.max(sdCircle(32, 58, 38)(x, y), sdCircle(32, 6, 38)(x, y)),
  [156, 104, 212],
  (c) => {
    paint(c, sdCircle(32, 32, 10), { fill: INK, grow: 1.5, alpha: 0.85 })
    paint(c, sdCircle(32, 32, 10), { fill: [236, 226, 246] })
    paint(c, sdCircle(32, 32, 4.5), { fill: INK })
  },
  'an eye',
)

// ------------------------------------------------------------------ done ---

for (const f of written) {
  console.log(`${f.name.padEnd(44)} ${f.size.padEnd(10)} ${f.note}`)
}
const sprites = written.filter((f) => f.name.endsWith('.png')).length
console.log(`\n${sprites} sprites and ${written.length - sprites} sidecar written under client/Assets/_Project/Art/`)
