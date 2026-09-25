// Draws the Phase 9 art of docs/plan.md: the seven map node icons, the player marker, the two
// tileable path sprites and the map backdrop (P9.3), and the two Charm icons and six Imprint
// icons (P9.5). Every file is written at the size its plan item states, as 8-bit RGBA PNG with
// straight alpha, named by the asset conventions.
//
// Run: node tools/gen-phase9-art.mjs
//
// Three rules shape everything here. A node is told apart by its shape, not its hue, because the
// map shows seven types side by side and a type must read at a glance (PRD 3.2.16): each icon is
// a different class of form — a blade, a horned helm, a crown, a coin, a spark, an anvil, a
// flame — that would not be confused with the colour removed. A path sprite is tiled along a
// connection of any length, so its left and right edges meet without a seam. And the backdrop
// follows the arena's frame rules (P5.3): its central 1920 x 1080 is the 16:9 frame and its outer
// 320 px a side hold nothing essential.

import { mkdirSync } from 'node:fs'
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
const MAP = join(SHARED, 'map')
const CHARMS = join(SHARED, 'charms')
const IMPRINTS = join(SHARED, 'imprints')

// ---------------------------------------------------------------- palette ---

const INK = [14, 12, 20] // the outline every icon carries, so it reads on any background

/** The node colours the map shows before the art ships (MapScreen.NodeColor), so the two never disagree. */
const NODE_COLOUR = {
  'normal-battle': [204, 77, 77],
  elite: [230, 128, 38],
  boss: [191, 26, 128],
  shop: [64, 153, 230],
  event: [128, 89, 204],
  blacksmith: [128, 128, 140],
  forge: [230, 179, 51],
}

const written = []

function emit(folder, name, c, note) {
  mkdirSync(folder, { recursive: true })
  bleed(c)
  writePng(join(folder, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

/** A signed ring: a band of the given width centred on a circle of the given radius. */
const sdRing = (cx, cy, r, width) => (x, y) => Math.abs(Math.hypot(x - cx, y - cy) - r) - width / 2

/** Keeps what one field holds and the other does not. */
const subtract = (a, b) => (x, y) => Math.max(a(x, y), -b(x, y))

/** Every icon: an ink outline swelled under the shape, then the shape in its colour, then an optional detail. */
function icon(sdf, colour, detail = null, size = 64) {
  const c = canvas(size, size)
  paint(c, sdf, { fill: INK, grow: 2, alpha: 0.92 })
  paint(c, sdf, { fill: colour })
  // A soft highlight across the upper half, so the icons read as objects rather than stamps.
  paint(c, sdf, {
    fill: [255, 255, 255],
    alpha: (_, y) => Math.max(0, 0.22 - (y / size) * 0.4),
  })
  if (detail) detail(c)
  return c
}

// --------------------------------------------------------- P9.3 node icons ---

function node(id, sdf, detail = null) {
  emit(MAP, `spr_node_${id}_static_01.png`, icon(sdf, NODE_COLOUR[id], detail), id)
}

// Normal battle: one sword, point up and to the right. The only diagonal, bladed form.
node(
  'normal-battle',
  union(
    sdSegment(22, 42, 50, 14, 4.5), // blade
    sdSegment(14, 36, 28, 50, 3.5), // crossguard
    sdSegment(19, 45, 11, 53, 3.5), // grip
    sdCircle(9, 55, 4), // pommel
  ),
  (c) => paint(c, sdSegment(25, 39, 47, 17, 1), { fill: [255, 236, 230], alpha: 0.8 }),
)

// Elite: a helm with two horns. The only form with two points rising from its sides.
node(
  'elite',
  union(
    sdRoundBox(32, 38, 15, 15, 7),
    sdPolygon([[18, 30], [8, 8], [26, 24]]),
    sdPolygon([[46, 30], [56, 8], [38, 24]]),
  ),
  (c) => paint(c, sdRoundBox(32, 38, 9, 2.5, 1.5), { fill: INK, alpha: 0.85 }),
)

// Boss: a crown of five points on a band. The only form with a toothed top edge.
node(
  'boss',
  union(
    sdRoundBox(32, 47, 22, 7, 2),
    sdPolygon([[10, 44], [10, 16], [21, 30], [32, 10], [43, 30], [54, 16], [54, 44]]),
  ),
  (c) => {
    for (const x of [20, 32, 44]) paint(c, sdCircle(x, 47, 3), { fill: [255, 230, 140] })
  },
)

// Shop: a coin with a raised rim and a square hole. The only ring.
node('shop', subtract(sdCircle(32, 32, 24), sdRoundBox(32, 32, 6, 6, 1.5)), (c) => {
  paint(c, sdRing(32, 32, 18, 2.5), { fill: INK, alpha: 0.55 })
})

// Event: a four-pointed spark. The only star.
{
  const verts = []
  for (let k = 0; k < 8; k++) {
    const a = (k * 45 - 90) * (Math.PI / 180)
    const r = k % 2 === 0 ? 28 : 8
    verts.push([32 + r * Math.cos(a), 32 + r * Math.sin(a)])
  }

  node('event', sdPolygon(verts), (c) => paint(c, sdCircle(32, 32, 4), { fill: [240, 230, 255] }))
}

// Blacksmith: an anvil — a flat top with a horn, a waist and a foot. The only wide, flat form.
node(
  'blacksmith',
  union(
    sdPolygon([[6, 18], [50, 18], [58, 22], [50, 28], [20, 28]]),
    sdRoundBox(34, 36, 8, 10, 2),
    sdRoundBox(34, 50, 18, 5, 2),
  ),
)

// Forge: a flame. The only teardrop, point up.
node(
  'forge',
  union(
    sdCircle(32, 42, 16),
    sdPolygon([[17, 38], [32, 4], [47, 38]]),
  ),
  (c) => {
    paint(c, union(sdCircle(32, 46, 8), sdPolygon([[25, 44], [32, 24], [39, 44]])), { fill: [255, 236, 150] })
  },
)

// ------------------------------------------------ P9.3 marker and paths ---

// The player marker: a pin with a bright head and a hole, point down onto the node it stands on.
{
  const shape = subtract(union(sdCircle(32, 24, 18), sdPolygon([[17, 32], [32, 60], [47, 32]])), sdCircle(32, 24, 7))
  const c = icon(shape, [245, 240, 232], (cv) => paint(cv, sdRing(32, 24, 7, 3), { fill: [242, 191, 64] }))
  emit(MAP, 'spr_ui_map-marker_static_01.png', c, 'pin, point at the bottom')
}

/**
 * A path tile, 64 x 8, tiled left to right along a connection. The unwalked path is a dash
 * centred in the tile with equal gaps at both edges, so tiles join into an even dashed line; the
 * walked path runs edge to edge, so tiles join into one solid, brighter line.
 */
function pathTile(walked) {
  const c = canvas(64, 8)
  if (walked) {
    paint(c, sdRoundBox(32, 4, 33, 3, 0), { fill: [150, 160, 196] })
    paint(c, sdRoundBox(32, 3, 33, 1, 0), { fill: [214, 220, 240], alpha: 0.7 })
  } else {
    paint(c, sdRoundBox(32, 4, 20, 2.5, 2.5), { fill: [96, 98, 122] })
  }

  return c
}

emit(MAP, 'spr_ui_map-path_static_01.png', pathTile(false), 'dashed, tileable left to right')
emit(MAP, 'spr_ui_map-path_walked_01.png', pathTile(true), 'solid, tileable left to right')

// ------------------------------------------------------ P9.3 the backdrop ---

/** A deterministic generator, so the same script writes the same bytes. */
function mulberry32(seed) {
  let a = seed >>> 0
  return () => {
    a = (a + 0x6d2b79f5) >>> 0
    let t = a
    t = Math.imul(t ^ (t >>> 15), t | 1)
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

/**
 * The map backdrop (P9.3): 2560 x 1080 on the arena's frame rules (P5.3). A dark land seen from
 * above: a cool ground, soft hills scattered across it and faint contour rings around each, all
 * low in contrast so the board's nodes and paths stay the brightest thing on screen. Nothing
 * essential sits in the outer 320 px a side, which only a wider screen shows.
 */
function backdrop() {
  const w = 2560
  const h = 1080
  const mid = w / 2
  const c = canvas(w, h)
  const ground = [24, 28, 36]
  const lowland = [30, 38, 44]

  paintAll(
    c,
    (x, y) => lerp(ground, lowland, 0.5 + 0.5 * Math.sin(x / 420) * Math.cos(y / 310)),
    () => 1,
  )

  const random = mulberry32(9)
  for (let i = 0; i < 46; i++) {
    const x = 120 + random() * (w - 240)
    const y = 60 + random() * (h - 120)
    const r = 50 + random() * 120
    const hill = lerp(lowland, [52, 58, 62], 0.3 + random() * 0.5)
    const box = [x - r - 60, y - r - 60, x + r + 60, y + r + 60]
    paint(c, sdCircle(x, y, r), { fill: hill, alpha: 0.35, grow: 0, box })
    paint(c, sdCircle(x, y, r * 0.55), { fill: hill, alpha: 0.3, box })
    paint(c, sdRing(x, y, r + 26, 2), { fill: [70, 78, 90], alpha: 0.25, box })
    paint(c, sdRing(x, y, r + 50, 1.5), { fill: [70, 78, 90], alpha: 0.15, box })
  }

  // A river winding across the frame, drawn as a chain of short segments.
  let px = 0
  let py = h * 0.62
  for (let x = 0; x <= w; x += 40) {
    const y = h * 0.62 + Math.sin(x / 260) * 90 + Math.sin(x / 97) * 18
    paint(c, sdSegment(px, py, x, y, 9), { fill: [36, 54, 72], alpha: 0.8, box: [Math.min(px, x) - 12, Math.min(py, y) - 12, Math.max(px, x) + 12, Math.max(py, y) + 12] })
    px = x
    py = y
  }

  // A vignette so the board sits in the lit middle and the outer 320 px a side fall away.
  paintAll(
    c,
    () => [0, 0, 0],
    (x, y) => Math.min(0.7, Math.max(0, Math.hypot((x - mid) / 1180, (y - h / 2) / 640) - 0.6)),
  )

  return c
}

emit(MAP, 'spr_bg_map_static_01.png', backdrop(), 'map backdrop, central 1920 x 1080 is the 16:9 frame')

// ------------------------------------------------- P9.5 Charm icons ---

const CHARM_COLOUR = [224, 196, 120] // Charms are permanent: warm metal
const IMPRINT_COLOUR = {
  // Imprints are run-scoped: each its own hue, read by shape all the same
  'keen-edge': [214, 222, 236],
  'thick-hide': [168, 124, 88],
  'bramble-skin': [96, 168, 84],
  'quick-guard': [84, 156, 224],
  'war-drum': [204, 72, 64],
  'stone-heart': [150, 150, 160],
}

// Clean Victory: a shield with a star set in it. Perfect Defense pays Essence (data/charms).
{
  const verts = []
  for (let k = 0; k < 10; k++) {
    const a = (k * 36 - 90) * (Math.PI / 180)
    const r = k % 2 === 0 ? 12 : 5
    verts.push([32 + r * Math.cos(a), 30 + r * Math.sin(a)])
  }

  const shield = sdRoundPolygon([[10, 10], [54, 10], [54, 30], [32, 58], [10, 30]], 2)
  const c = icon(shield, CHARM_COLOUR, (cv) => paint(cv, sdPolygon(verts), { fill: [255, 250, 225] }))
  emit(CHARMS, 'spr_charm_clean-victory_static_01.png', c, 'shield and star')
}

// Momentum Plate: a hexagonal plate carrying two forward chevrons.
{
  const hex = []
  for (let k = 0; k < 6; k++) {
    const a = (k * 60) * (Math.PI / 180)
    hex.push([32 + 26 * Math.cos(a), 32 + 26 * Math.sin(a)])
  }

  const c = icon(sdRoundPolygon(hex, 2), CHARM_COLOUR, (cv) => {
    for (const x of [22, 34]) {
      paint(cv, union(sdSegment(x, 21, x + 10, 32, 3), sdSegment(x + 10, 32, x, 43, 3)), { fill: [92, 64, 28] })
    }
  })
  emit(CHARMS, 'spr_charm_momentum-plate_static_01.png', c, 'hex plate with chevrons')
}

// ------------------------------------------------ P9.5 Imprint icons ---

function imprint(id, sdf, detail = null) {
  emit(IMPRINTS, `spr_imprint_${id}_static_01.png`, icon(sdf, IMPRINT_COLOUR[id], detail), id)
}

// Keen Edge (+Base DMG): an upright blade, point up.
imprint(
  'keen-edge',
  union(
    sdPolygon([[32, 4], [40, 16], [38, 44], [26, 44], [24, 16]]),
    sdRoundBox(32, 47, 14, 3, 1.5),
    sdRoundBox(32, 55, 3.5, 6, 1.5),
  ),
  (c) => paint(c, sdSegment(32, 10, 32, 40, 1), { fill: [255, 255, 255], alpha: 0.8 }),
)

// Thick Hide (+max ARD): three overlapping scales stacked into a pelt.
imprint(
  'thick-hide',
  union(sdCircle(22, 24, 13), sdCircle(42, 24, 13), sdCircle(32, 40, 16)),
  (c) => {
    paint(c, sdRing(32, 40, 16, 2), { fill: INK, alpha: 0.5 })
    paint(c, sdRing(22, 24, 13, 2), { fill: INK, alpha: 0.35 })
    paint(c, sdRing(42, 24, 13, 2), { fill: INK, alpha: 0.35 })
  },
)

// Bramble Skin (Thorns at battle start): a vine looping round, with thorns along it.
{
  const thorns = []
  for (let k = 0; k < 6; k++) {
    const a = (k * 60 + 30) * (Math.PI / 180)
    thorns.push(sdPolygon([
      [32 + 18 * Math.cos(a - 0.22), 32 + 18 * Math.sin(a - 0.22)],
      [32 + 30 * Math.cos(a), 32 + 30 * Math.sin(a)],
      [32 + 18 * Math.cos(a + 0.22), 32 + 18 * Math.sin(a + 0.22)],
    ]))
  }

  imprint('bramble-skin', union(sdRing(32, 32, 17, 6), ...thorns))
}

// Quick Guard (Block at battle start): a round buckler with three speed lines trailing it.
imprint(
  'quick-guard',
  union(sdCircle(38, 32, 20), sdSegment(4, 22, 14, 22, 2.5), sdSegment(2, 32, 14, 32, 2.5), sdSegment(4, 42, 14, 42, 2.5)),
  (c) => paint(c, sdCircle(38, 32, 6), { fill: [220, 236, 252] }),
)

// War Drum (damage dealt x1.1): a drum body between two rims, with a stick across it.
imprint(
  'war-drum',
  union(
    sdRoundBox(30, 38, 20, 14, 4),
    sdRoundBox(30, 24, 22, 4, 2),
    sdRoundBox(30, 52, 22, 4, 2),
    sdSegment(40, 18, 58, 4, 2.5),
  ),
  (c) => {
    paint(c, union(sdSegment(12, 28, 22, 48, 1.5), sdSegment(22, 28, 32, 48, 1.5), sdSegment(32, 28, 42, 48, 1.5)), { fill: INK, alpha: 0.5 })
  },
)

// Stone Heart (damage taken x0.9): a heart cut from stone, with a crack.
imprint(
  'stone-heart',
  union(sdCircle(22, 24, 13), sdCircle(42, 24, 13), sdPolygon([[10, 30], [54, 30], [32, 58]])),
  (c) => paint(c, union(sdSegment(32, 16, 28, 30, 1.5), sdSegment(28, 30, 35, 40, 1.5)), { fill: INK, alpha: 0.6 }),
)

// ------------------------------------------------------------------ run ---

for (const f of written) console.log(`${f.name.padEnd(46)} ${f.size.padEnd(12)} ${f.note}`)
console.log(`\n${written.length} sprites written under Art/Shared/`)
