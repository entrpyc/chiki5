// Draws the Phase 4 art of docs/plan.md: the four-frame heavy-hit burst and its clip sidecar
// (P4.2), the six status icons and the tooltip panel (P4.3), the bar frame, the two bar fills and
// the Block icon (P4.4), and the CRP icon (P4.5). Every file is written at the size its plan item
// states, as 8-bit RGBA PNG with straight alpha, named by the asset conventions, with the
// 9-slice borders of the sprites that stretch written into `.slices.json` sidecars beside them.
//
// Run: node tools/gen-phase4-art.mjs
//
// Three rules shape everything here. A status is told apart by its shape, not its hue, because
// six icons sit in one row above a bar (PRD 3.3.7.1): each silhouette is a different class of
// form — a drop, an arrow, a star, a torn line, a spiked ring, a struck-through ring — and no two
// would be confused with the colour removed. A 9-sliced sprite's middle is stretched, so nothing
// inside the frame or the tooltip varies except within the border strips its sidecar declares.
// And the bar fills are drawn to be revealed left to right by a filled image, so they shade
// across the bar's height only and read the same at any fraction.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  bleed,
  canvas,
  lerp,
  paint,
  paintAll,
  sdCircle,
  sdPolygon,
  sdRoundBox,
  sdRoundPolygon,
  sdSegment,
  union,
  writePng,
} from './art-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const SHARED = join(ROOT, 'client', 'Assets', '_Project', 'Art', 'Shared')
const VFX = join(SHARED, 'vfx')
const STATUS = join(SHARED, 'status')
const BARS = join(SHARED, 'bars')
const CRP = join(SHARED, 'crp')

// ---------------------------------------------------------------- palette ---

const INK = [14, 12, 20] // the outline every icon carries, so it reads on any background
const WELL = [17, 18, 26] // the dark interior a frame or a panel encloses

/** The status colours the client shows before the art ships, so the two never disagree. */
const STATUS_COLOUR = {
  scar: [217, 153, 64],
  weak: [140, 140, 191],
  stun: [242, 230, 89],
  bleed: [204, 38, 51],
  thorns: [89, 179, 89],
  disarmed: [128, 128, 128],
}

const written = []

function emit(folder, name, c, note) {
  bleed(c)
  writePng(join(folder, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

/** Writes a subject's 9-slice sidecar, in the shape SliceSidecar reads (P2.1). */
function slices(folder, kind, subject, borders) {
  const body = Object.entries(borders)
    .map(([variant, b]) => `    "${variant}": { "left": ${b}, "bottom": ${b}, "right": ${b}, "top": ${b} }`)
    .join(',\n')
  write(folder, `${kind}_${subject}.slices.json`, `{\n  "borders": {\n${body}\n  }\n}\n`, 'slice sidecar')
}

function write(folder, name, contents, note) {
  const path = join(folder, name)
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, contents)
  written.push({ name, size: '', note })
}

/** A signed ring: a band of the given width centred on a circle of the given radius. */
const sdRing = (cx, cy, r, width) => (x, y) => Math.abs(Math.hypot(x - cx, y - cy) - r) - width / 2

// -------------------------------------------------- P4.2 heavy-hit burst ---

/**
 * One frame of the burst over the player (PRD 3.3.8.1). Frame 1 is the strike: a white-hot core
 * with eight short spikes. The three after it are the same burst expanding, thinning and fading,
 * so the whole beat reads as one impact rather than four shapes.
 */
function burstFrame(index) {
  const size = 256
  const mid = size / 2
  const t = index / 3
  const c = canvas(size, size)

  const spikes = []
  for (let k = 0; k < 8; k++) {
    const a = (k * 45 - 90) * (Math.PI / 180)
    const inner = 12 + t * 40
    const outer = 58 + t * 62
    const thickness = 11 * (1 - t * 0.55)
    spikes.push(
      sdSegment(
        mid + inner * Math.cos(a),
        mid + inner * Math.sin(a),
        mid + outer * Math.cos(a),
        mid + outer * Math.sin(a),
        thickness,
      ),
    )
  }

  const ringRadius = 26 + t * 78
  const ring = sdRing(mid, mid, ringRadius, 24 * (1 - t * 0.62))
  const shape = union(ring, ...spikes)

  // White at the centre, hot orange at the rim, so the burst cools outwards as it grows.
  const hot = [255, 246, 220]
  const edge = [232, 112, 48]
  const hue = (x, y) => lerp(hot, edge, Math.min(1, Math.hypot(x - mid, y - mid) / (ringRadius + 34)))
  const fade = 1 - t * 0.78

  paint(c, shape, { fill: hue, grow: 3, alpha: 0.3 * fade })
  paint(c, shape, { fill: hue, alpha: 0.95 * fade })

  if (index === 0) {
    // The strike's core, only on the frame that lands on the beat.
    paintAll(
      c,
      () => hot,
      (x, y) => Math.max(0, 1 - Math.hypot(x - mid, y - mid) / 34),
    )
  }

  return c
}

for (let i = 0; i < 4; i++) {
  emit(VFX, `spr_vfx_hit-heavy_burst_${(i + 1).toString().padStart(2, '0')}.png`, burstFrame(i), i === 0 ? 'strike frame' : `burst ${i + 1}`)
}

write(
  VFX,
  'vfx_hit-heavy.clips.json',
  '{\n  "clips": {\n    "burst": { "beats": 1, "loop": false, "strikeFrame": 1 }\n  }\n}\n',
  'clip sidecar: one beat, no loop, strikes on frame 1',
)

// ---------------------------------------------------- P4.3 status icons ---

/** Every status icon: an ink outline swelled under the shape, then the shape in its colour. */
function icon(sdf, colour, size = 64) {
  const c = canvas(size, size)
  paint(c, sdf, { fill: INK, grow: 2, alpha: 0.92 })
  paint(c, sdf, { fill: colour })
  return c
}

function status(id, sdf, detail = null) {
  const c = icon(sdf, STATUS_COLOUR[id])
  if (detail) {
    paint(c, detail, { fill: INK, alpha: 0.34 })
  }

  emit(STATUS, `spr_status_${id}_static_01.png`, c, id)
}

// Bleed: a drop — round below, drawn to a point above. The only teardrop in the set.
status(
  'bleed',
  union(sdCircle(32, 40, 17), sdPolygon([
    [32, 6],
    [46, 40],
    [18, 40],
  ])),
)

// Weak: an arrow pointing down — a shaft under a wide head. The only shape with a single axis.
status(
  'weak',
  union(sdRoundBox(32, 22, 7, 16, 4), sdPolygon([
    [14, 34],
    [50, 34],
    [32, 58],
  ])),
)

// Stun: a five-pointed star. The only radially symmetric outline with points.
{
  const verts = []
  for (let k = 0; k < 10; k++) {
    const a = (-90 + k * 36) * (Math.PI / 180)
    const r = k % 2 === 0 ? 28 : 12
    verts.push([32 + r * Math.cos(a), 32 + r * Math.sin(a)])
  }

  status('stun', sdRoundPolygon(verts, 1.5))
}

// Scar: a torn line across the icon — three segments that change direction. The only open form.
status(
  'scar',
  union(
    sdSegment(11, 52, 26, 33, 4.5),
    sdSegment(26, 33, 38, 41, 4.5),
    sdSegment(38, 41, 53, 12, 4.5),
  ),
)

// Thorns: a ring wearing eight spikes. The only outline that is a ring with something outside it.
{
  const spikes = []
  for (let k = 0; k < 8; k++) {
    const a = (k * 45) * (Math.PI / 180)
    spikes.push(sdSegment(32 + 16 * Math.cos(a), 32 + 16 * Math.sin(a), 32 + 29 * Math.cos(a), 32 + 29 * Math.sin(a), 3.6))
  }

  status('thorns', union(sdRing(32, 32, 16, 8), ...spikes))
}

// Disarmed: a ring struck through. The only outline that is a ring with something across it.
status('disarmed', union(sdRing(32, 32, 22, 8), sdSegment(16, 48, 48, 16, 4.5)))

// The tooltip panel behind a status's text (PRD 3.3.7.1): a dark panel with a pale rim, both
// inside the 12 px border, so the middle the panel stretches over is flat.
{
  const w = 260
  const h = 70
  const border = 12
  const c = canvas(w, h)
  const box = sdRoundBox(w / 2, h / 2, w / 2 - 0.5, h / 2 - 0.5, 9)
  paint(c, box, { fill: WELL, alpha: 0.96 })
  paint(c, box, { stroke: [96, 100, 124], width: 2, alpha: 0.9 })
  emit(STATUS, 'spr_ui_tooltip_static_01.png', c, `9-slice, ${border} px borders`)
  slices(STATUS, 'ui', 'tooltip', { static: border })
}

// ------------------------------------------------- P4.4 bars and Block ---

// The frame both bars sit in: a dark well inside a pale rim, everything within 8 px of an edge.
{
  const w = 560
  const h = 32
  const border = 8
  const c = canvas(w, h)
  const box = sdRoundBox(w / 2, h / 2, w / 2 - 0.5, h / 2 - 0.5, 6)
  paint(c, box, { fill: WELL, alpha: 0.9 })
  paint(c, box, { stroke: [118, 122, 148], width: 2, alpha: 0.92 })
  emit(BARS, 'spr_ui_bar_frame_01.png', c, `9-slice, ${border} px borders`)
  slices(BARS, 'ui', 'bar', { frame: border })
}

/**
 * One bar's fill: a flat colour with a sheen across its height and a darker foot, shaded on the
 * vertical axis only so the fraction a filled image reveals looks the same wherever it stops.
 */
function barFill(colour) {
  const w = 552
  const h = 24
  const c = canvas(w, h)
  const light = lerp(colour, [255, 255, 255], 0.42)
  const dark = lerp(colour, [0, 0, 0], 0.4)
  paint(c, sdRoundBox(w / 2, h / 2, w / 2 - 0.5, h / 2 - 0.5, 5), {
    fill: (_, y) => {
      const t = (y - 0.5) / (h - 1)
      return t < 0.5 ? lerp(light, colour, t / 0.5) : lerp(colour, dark, (t - 0.5) / 0.5)
    },
  })
  return c
}

emit(BARS, 'spr_ui_bar_fill-enemy_01.png', barFill([204, 51, 64]), 'enemy HP fill')
emit(BARS, 'spr_ui_bar_fill-ard_01.png', barFill([64, 166, 230]), 'ARD fill')

// Block (PRD 3.3.4.2): a plated buckler — an octagon with a raised band. It is not the Defense
// Category's pointed shield (P3.1), so a Block readout never reads as a card's Category.
{
  const verts = []
  for (let k = 0; k < 8; k++) {
    const a = (-90 + k * 45) * (Math.PI / 180)
    verts.push([32 + 28 * Math.cos(a), 32 + 28 * Math.sin(a)])
  }

  const c = icon(sdRoundPolygon(verts, 3), [150, 176, 214])
  paint(c, sdRoundBox(32, 32, 20, 4.5, 3), { fill: INK, alpha: 0.32 })
  emit(BARS, 'spr_ui_block_static_01.png', c, 'buckler')
}

// -------------------------------------------------------- P4.5 CRP icon ---

// CRP (PRD 3.8.1): a corruption shard — a tall rhombus with a hollow core and a fracture through
// it. Nothing else in the HUD is a diamond, so the badge is found at a glance.
{
  const shard = sdRoundPolygon(
    [
      [32, 4],
      [52, 32],
      [32, 60],
      [12, 32],
    ],
    3,
  )
  const c = icon(shard, [168, 92, 214])
  paint(c, union(sdSegment(32, 14, 26, 32, 3), sdSegment(26, 32, 34, 50, 3)), { fill: INK, alpha: 0.42 })
  emit(CRP, 'spr_ui_crp_static_01.png', c, 'shard')
}

// ------------------------------------------------------------------ done ---

for (const f of written) {
  console.log(`${f.name.padEnd(36)} ${f.size.padEnd(10)} ${f.note}`)
}

const sprites = written.filter((f) => f.name.endsWith('.png')).length
console.log(`\n${sprites} sprites and ${written.length - sprites} sidecars written under Art/Shared/`)
