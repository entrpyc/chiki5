// Draws the Phase 3 art of docs/plan.md: the three category slot frames and their icons (P3.1),
// the radial cooldown sweep (P3.2), the slot glow and press flash (P3.3) and the disabled flash
// (P3.4). Every file is written at the size its plan item states, as 8-bit RGBA PNG with straight
// alpha, named by the asset conventions and placed under client/Assets/_Project/Art/Shared/slots/,
// with the 9-slice borders written into the `.slices.json` sidecars beside the frames.
//
// Run: node tools/gen-phase3-art.mjs
//
// Two rules shape everything here. The category colours are the PRD's (3.4.2): Attack red,
// Defense blue, Ability green, and the icon always appears with the colour, never instead of it,
// so the three icons are silhouettes that stay apart with every hue removed. And a 9-sliced
// sprite's middle is stretched to the slot, so nothing inside a sprite varies except within the
// border strips its sidecar declares: every rim, bracket and falloff below lives inside them.

// The canvas, the distance fields and the PNG writer are shared with the other generators.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  bleed,
  canvas,
  paint,
  sdCircle,
  sdPolygon,
  sdRoundBox,
  sdRoundPolygon,
  sdSegment,
  union,
  writePng,
} from './art-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const SLOTS = join(ROOT, 'client', 'Assets', '_Project', 'Art', 'Shared', 'slots')

// ---------------------------------------------------------------- palette ---

const INK = [14, 12, 20] // the outline every icon carries, so it reads on any slot interior
const WELL = [17, 18, 26] // the dark interior every frame encloses
const CATEGORY = {
  attack: [214, 71, 66], // PRD 3.4.2: Attack red
  defense: [62, 126, 214], // PRD 3.4.2: Defense blue
  ability: [72, 183, 108], // PRD 3.4.2: Ability green
}

/** The slot's drawn size (P3.1) and the border the frame, flash and disabled flash slice at. */
const SLOT_W = 160
const SLOT_H = 120
const SLOT_BORDER = 16
const SLOT_RADIUS = 13 // inside the border, so a corner curve never lands in a stretched tile

const written = []

function emit(name, c, note) {
  bleed(c)
  writePng(join(SLOTS, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

/** Writes a subject's 9-slice sidecar, in the shape SliceSidecar reads (P2.1). */
function slices(kind, subject, borders) {
  const body = Object.entries(borders)
    .map(([variant, b]) => `    "${variant}": { "left": ${b}, "bottom": ${b}, "right": ${b}, "top": ${b} }`)
    .join(',\n')
  const path = join(SLOTS, `${kind}_${subject}.slices.json`)
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, `{\n  "borders": {\n${body}\n  }\n}\n`)
  written.push({ name: `${kind}_${subject}.slices.json`, size: '', note: 'slice sidecar' })
}

/** The rounded box every slot-sized sprite is cut from, half a pixel inside the canvas. */
function slotBox(inset = 0.5) {
  return sdRoundBox(SLOT_W / 2, SLOT_H / 2, SLOT_W / 2 - inset, SLOT_H / 2 - inset, SLOT_RADIUS)
}

// ------------------------------------------------- P3.1 frames per category ---

/**
 * One category's slot frame: a dark well inside a rim in the category's colour, an inner hairline
 * and a bracket at each corner. The well is flat, so the stretched middle is uniform; the rim,
 * the hairline and the brackets all lie within 16 px of an edge.
 */
function slotFrame(colour) {
  const c = canvas(SLOT_W, SLOT_H)
  const box = slotBox()
  paint(c, box, { fill: WELL, alpha: 0.72 })
  paint(c, box, { stroke: colour, width: 3, alpha: 0.95 })

  // Corner brackets, drawn from (7, 7) inwards: 15.4 px at the furthest, inside the 16 px border.
  for (const sx of [1, -1]) {
    for (const sy of [1, -1]) {
      const x = sx > 0 ? 7 : SLOT_W - 7
      const y = sy > 0 ? 7 : SLOT_H - 7
      const arm = union(sdSegment(x, y, x + sx * 7, y, 1.4), sdSegment(x, y, x, y + sy * 7, 1.4))
      paint(c, arm, { fill: colour, alpha: 0.8 })
    }
  }

  return c
}

for (const [category, colour] of Object.entries(CATEGORY)) {
  emit(`spr_ui_slot-frame_${category}_01.png`, slotFrame(colour), `9-slice, ${SLOT_BORDER} px borders`)
}

// -------------------------------------------------- P3.1 icons per category ---

/** Every category icon: an ink outline swelled under the shape, then the shape in its colour. */
function icon(sdf, colour) {
  const c = canvas(64, 64)
  paint(c, sdf, { fill: INK, grow: 2, alpha: 0.92 })
  paint(c, sdf, { fill: colour })
  return c
}

// Attack: a blade point-up — a spike over a wide crossguard over a narrow grip and a round
// pommel. Tall and barred, so it is neither the shield's broad wedge nor the clover's lobes.
{
  const sword = union(
    sdPolygon([
      [32, 4],
      [41, 22],
      [41, 39],
      [23, 39],
      [23, 22],
    ]),
    sdRoundBox(32, 43, 19, 3.5, 3),
    sdRoundBox(32, 52, 3.5, 7, 3),
    sdCircle(32, 59, 4.5),
  )
  emit('spr_category_attack_static_01.png', icon(sword, CATEGORY.attack), 'blade')
}

// Defense: a shield — broad and round-shouldered at the top, drawn to a point at the bottom, with
// a chevron band across it. Nothing else in the set is a single solid wedge.
{
  const shield = sdRoundPolygon(
    [
      [9, 11],
      [32, 4],
      [55, 11],
      [53, 34],
      [32, 60],
      [11, 34],
    ],
    4,
  )
  const c = icon(shield, CATEGORY.defense)
  const band = union(sdSegment(14, 26, 32, 38, 3), sdSegment(32, 38, 50, 26, 3))
  paint(c, band, { fill: INK, alpha: 0.34 })
  emit('spr_category_defense_static_01.png', c, 'shield')
}

// Ability: three lobes around a core — no point, no bar, no wedge, so it is the one silhouette in
// the set that reads as round from every side.
{
  const lobes = []
  for (let k = 0; k < 3; k++) {
    const a = (-90 + k * 120) * (Math.PI / 180)
    lobes.push(sdCircle(32 + 17 * Math.cos(a), 32 + 17 * Math.sin(a), 13))
  }

  const c = icon(union(sdCircle(32, 32, 11), ...lobes), CATEGORY.ability)
  paint(c, sdCircle(32, 32, 5.5), { fill: INK, alpha: 0.3 })
  emit('spr_category_ability_static_01.png', c, 'three lobes')
}

// ------------------------------------------------------ P3.2 radial cooldown ---

// The sweep the client dims and fills as an Image.Filled radial (PRD 3.3.5.4): plain white, with
// the frame's corners. Filled images are not sliced, so this one carries no border and is drawn
// square at 128 so it scales to any slot without favouring an axis.
{
  const c = canvas(128, 128)
  paint(c, sdRoundBox(64, 64, 63.5, 63.5, 16), { fill: [255, 255, 255] })
  emit('spr_ui_slot-cooldown_static_01.png', c, 'radial sweep, dimmed and filled in code')
}

// ------------------------------------------------- P3.3 glow and press flash ---

// The glow behind a playable slot (PRD 3.3.8.1): 172 x 132, so it stands 6 px proud of the frame
// on every side, and 22 px borders. Inside the border the wash is flat, since that region is what
// the slot stretches; outside it the glow falls to nothing by the sprite's edge.
{
  const w = 172
  const h = 132
  const border = 22
  const c = canvas(w, h)
  const inner = sdRoundBox(w / 2, h / 2, w / 2 - border, h / 2 - border, 16)
  paint(
    c,
    sdRoundBox(w / 2, h / 2, w / 2, h / 2, 0),
    {
      fill: [255, 238, 196],
      alpha: (x, y) => {
        const d = inner(x, y)
        return 0.42 * (d <= 0 ? 1 : Math.pow(Math.max(0, 1 - d / border), 1.5))
      },
    },
  )
  emit('spr_ui_slot-glow_static_01.png', c, `9-slice, ${border} px borders`)
}

/**
 * A slot-sized wash whose rim is bright and whose middle is flat: the falloff runs over exactly
 * the 16 px border, so the stretched middle stays the constant `core`.
 */
function slotWash(colour, core, rim, curve) {
  const c = canvas(SLOT_W, SLOT_H)
  const box = slotBox()
  paint(c, box, {
    fill: colour,
    alpha: (x, y) => {
      const d = box(x, y)
      if (d <= -SLOT_BORDER) return core
      return core + (rim - core) * Math.pow(1 + Math.min(0, d) / SLOT_BORDER, curve)
    },
  })
  return c
}

// The key-press flash (PRD 3.3.8.1): white, and the client fades it out over half a beat.
emit('spr_ui_slot-flash_static_01.png', slotWash([255, 255, 255], 0.5, 1, 1.2), `9-slice, ${SLOT_BORDER} px borders`)

// ---------------------------------------------------------- P3.4 disabled ---

// The disabled flash (PRD 3.3.5.3): a barred crimson rim over an almost clear middle, the
// opposite read to the press flash's filled white, so a refused press never looks like a played
// one. The bar inside the rim sits 4 px in, within the border.
{
  const c = slotWash([204, 66, 62], 0.14, 0.95, 2)
  paint(c, slotBox(), { stroke: [255, 214, 208], width: 2, grow: -5, alpha: 0.55 })
  emit('spr_ui_slot-disabled_static_01.png', c, `9-slice, ${SLOT_BORDER} px borders`)
}

// ------------------------------------------------------------- sidecars ---

slices('ui', 'slot-frame', { attack: SLOT_BORDER, defense: SLOT_BORDER, ability: SLOT_BORDER })
slices('ui', 'slot-glow', { static: 22 })
slices('ui', 'slot-flash', { static: SLOT_BORDER })
slices('ui', 'slot-disabled', { static: SLOT_BORDER })

// ------------------------------------------------------------------ done ---

for (const f of written) {
  console.log(`${f.name.padEnd(44)} ${f.size.padEnd(10)} ${f.note}`)
}

const sprites = written.filter((f) => f.name.endsWith('.png')).length
console.log(`\n${sprites} sprites and ${written.length - sprites} sidecars written under Art/Shared/slots/`)
