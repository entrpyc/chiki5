// Draws the Phase 10 art of docs/plan.md: the UI skin every menu is built from — the four
// button states, the panel, the backdrop and the toggle box and check (P10.1) — the logo and the
// title background (P10.2), the three run-end outcome plates (P10.3) and the calibration beat
// marker (P10.4). Every file is written at the size its plan item states, as 8-bit RGBA PNG with
// straight alpha, named by the asset conventions, with the 9-slice borders of the button and the
// panel written into `.slices.json` sidecars beside them.
//
// Run: node tools/gen-phase10-art.mjs
//
// Three rules shape everything here. A 9-sliced sprite's middle is stretched both ways, so the
// button and panel vary only inside the border strips their sidecars declare; their faces shade
// top to bottom, which survives a vertical stretch as a gentler gradient. Full-screen art is
// 2560 px across and keeps the outer 320 px on each side free of anything that matters, so a
// 16:9 window crops it without losing a thing (the frame rule of P5.3). And no asset carries
// lettering the string table owns: the outcome plates are blank for "Won", "Died" and
// "Abandoned" to sit on, and the only letters drawn are the logo's, which are the game's name
// as a mark, not text a translation would change.

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
const UI = join(SHARED, 'ui')
const TITLE = join(SHARED, 'title')
const RUNEND = join(SHARED, 'runend')
const CALIBRATION = join(SHARED, 'calibration')

// ---------------------------------------------------------------- palette ---

// The flat colours the menus were drawn in before the skin shipped (ScreenFactory), so a skinned
// screen and a fallback one agree on hue.
const INK = [14, 12, 20]
const NIGHT = [15, 15, 23] // ScreenFactory.Backdrop
const PANEL = [31, 31, 43] // ScreenFactory.Panel
const FACE = [56, 61, 92] // ScreenFactory.ButtonFace
const GOLD = [242, 191, 64] // ScreenFactory.Accent
const PINK = [255, 138, 176]
const RIM = [118, 124, 168]

const written = []

function emit(folder, name, c, note, { bleedEdges = true } = {}) {
  if (bleedEdges) bleed(c)
  writePng(join(folder, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

/** Writes a subject's 9-slice sidecar, in the shape SliceSidecar reads (P2.1). */
function slices(folder, kind, subject, borders) {
  const body = Object.entries(borders)
    .map(([variant, b]) => `    "${variant}": { "left": ${b}, "bottom": ${b}, "right": ${b}, "top": ${b} }`)
    .join(',\n')
  const name = `${kind}_${subject}.slices.json`
  const path = join(folder, name)
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, `{\n  "borders": {\n${body}\n  }\n}\n`)
  written.push({ name, size: '', note: 'slice sidecar' })
}

/** A deterministic random source, so the sparkles land in the same places on every machine. */
function random(seed) {
  let state = seed >>> 0
  return () => {
    state = (state * 1664525 + 1013904223) >>> 0
    return state / 4294967296
  }
}

const sdRing = (cx, cy, r, width) => (x, y) => Math.abs(Math.hypot(x - cx, y - cy) - r) - width / 2

/** A stroked arc from angle a0 to a1 (degrees, y down, clockwise on screen), `half` wide either side. */
function sdArc(cx, cy, r, a0, a1, half) {
  const start = (a0 * Math.PI) / 180
  const span = ((a1 - a0) * Math.PI) / 180
  const ends = [
    [cx + r * Math.cos(start), cy + r * Math.sin(start)],
    [cx + r * Math.cos(start + span), cy + r * Math.sin(start + span)],
  ]
  return (x, y) => {
    let a = Math.atan2(y - cy, x - cx) - start
    a = ((a % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI)
    if (a <= span) return Math.abs(Math.hypot(x - cx, y - cy) - r) - half
    return Math.min(...ends.map(([ex, ey]) => Math.hypot(x - ex, y - ey))) - half
  }
}

// ----------------------------------------------------- P10.1 button states ---

// 420 x 96 with 24 px borders: a pill-cornered face (radius 20, inside the border) with a rim,
// a lit top edge and a darker foot. The four states differ the way a player reads them at a
// glance: highlighted is lighter with a gold rim, pressed is darker and sunk (its light comes
// from below), disabled is grey and flat.
const BUTTON_W = 420
const BUTTON_H = 96
const BUTTON_BORDER = 24

function button({ top, bottom, rim, rimWidth = 3, alpha = 1, glow = null }) {
  const c = canvas(BUTTON_W, BUTTON_H)
  const face = sdRoundBox(BUTTON_W / 2, BUTTON_H / 2, BUTTON_W / 2 - 3, BUTTON_H / 2 - 5, 20)
  if (glow) paint(c, face, { fill: glow, grow: 2.5, alpha: 0.55 * alpha })
  paint(c, face, { fill: INK, grow: 1.5, alpha: 0.9 * alpha })
  paint(c, face, {
    fill: (_, y) => lerp(top, bottom, Math.min(1, Math.max(0, (y - 5) / (BUTTON_H - 10)))),
    alpha,
  })
  paint(c, face, { stroke: rim, width: rimWidth, grow: -rimWidth / 2 - 1, alpha })
  // The sheen: a thin light line along the top border strip only, so stretching never smears it.
  paint(c, sdRoundBox(BUTTON_W / 2, 12, BUTTON_W / 2 - 22, 2, 2), { fill: [255, 255, 255], alpha: 0.18 * alpha })
  return c
}

emit(UI, 'spr_ui_button_normal_01.png', button({ top: lerp(FACE, [255, 255, 255], 0.14), bottom: lerp(FACE, [0, 0, 0], 0.22), rim: RIM }), 'normal, 9-slice 24 px')
emit(
  UI,
  'spr_ui_button_highlighted_01.png',
  button({ top: lerp(FACE, [255, 255, 255], 0.3), bottom: FACE, rim: GOLD, rimWidth: 4, glow: GOLD }),
  'highlighted: lighter, gold rim',
)
emit(
  UI,
  'spr_ui_button_pressed_01.png',
  button({ top: lerp(FACE, [0, 0, 0], 0.38), bottom: lerp(FACE, [0, 0, 0], 0.1), rim: lerp(GOLD, [0, 0, 0], 0.25), rimWidth: 4 }),
  'pressed: darker, lit from below',
)
emit(UI, 'spr_ui_button_disabled_01.png', button({ top: [74, 74, 82], bottom: [52, 52, 58], rim: [92, 92, 100], alpha: 0.72 }), 'disabled: grey, flat')
slices(UI, 'ui', 'button', { normal: BUTTON_BORDER, highlighted: BUTTON_BORDER, pressed: BUTTON_BORDER, disabled: BUTTON_BORDER })

// ---------------------------------------------------------- P10.1 panel ---

// 512 x 512 with 32 px borders: a dark well with a doubled rim — an outer ink line and an inner
// pale one — and rounded corners of radius 26, all inside the border. The middle is flat.
{
  const size = 512
  const border = 32
  const c = canvas(size, size)
  const outer = sdRoundBox(size / 2, size / 2, size / 2 - 4, size / 2 - 4, 26)
  paint(c, outer, { fill: [0, 0, 0], grow: 3, alpha: 0.35 })
  paint(c, outer, { fill: INK, grow: 1 })
  paint(c, outer, { fill: (_, y) => (y < border ? lerp(PANEL, [255, 255, 255], 0.05 * (1 - y / border)) : PANEL) })
  paint(c, outer, { stroke: RIM, width: 2.5, grow: -9, alpha: 0.85 })
  paint(c, outer, { stroke: lerp(RIM, [0, 0, 0], 0.5), width: 2, grow: -1.5, alpha: 0.9 })
  emit(UI, 'spr_ui_panel_static_01.png', c, `9-slice, ${border} px borders`)
  slices(UI, 'ui', 'panel', { static: border })
}

// ------------------------------------------------------- P10.1 backdrop ---

// 2560 x 1080 behind every menu: the night colour with a soft rise of light toward the centre,
// a faint lattice of beat dots and a vignette. Opaque, and nothing sits in the outer 320 px.
function nightBackdrop(w, h, { spot, lattice = true, seed = 1 }) {
  const c = canvas(w, h)
  const cx = w / 2
  const cy = h * 0.46
  paintAll(
    c,
    (x, y) => {
      const d = Math.hypot((x - cx) / (w * 0.42), (y - cy) / (h * 0.62))
      const lit = Math.max(0, 1 - d)
      const base = lerp(NIGHT, spot, lit * lit * 0.55)
      const vignette = Math.min(1, Math.hypot((x - cx) / (w * 0.5), (y - cy) / (h * 0.62)))
      return lerp(base, [4, 4, 8], vignette * vignette * 0.6)
    },
    () => 1,
  )
  if (lattice) {
    const rand = random(seed)
    for (let gy = 60; gy < h; gy += 90) {
      for (let gx = 340; gx < w - 340; gx += 90) {
        const r = 2 + rand() * 1.6
        const x = gx + (Math.floor(gy / 90) % 2) * 45
        const d = Math.hypot((x - cx) / (w * 0.4), (gy - cy) / (h * 0.6))
        const a = Math.max(0, 0.16 - d * 0.12)
        if (a > 0) paint(c, sdCircle(x, gy, r), { fill: lerp(spot, [255, 255, 255], 0.3), alpha: a, box: [x - 6, gy - 6, x + 6, gy + 6] })
      }
    }
  }
  return c
}

emit(UI, 'spr_ui_backdrop_static_01.png', nightBackdrop(2560, 1080, { spot: [60, 56, 104], seed: 10 }), 'opaque, 16:9 centre', { bleedEdges: false })

// ------------------------------------------------------- P10.1 toggle ---

// The box a toggle's check sits in (44 x 44) and the check itself (26 x 26), drawn to be tinted
// white: the box a dark rounded square with a pale rim, the check a gold tick.
{
  const c = canvas(44, 44)
  const box = sdRoundBox(22, 22, 20, 20, 8)
  paint(c, box, { fill: INK, grow: 1.5 })
  paint(c, box, { fill: (_, y) => lerp([30, 32, 48], [22, 23, 34], y / 44) })
  paint(c, box, { stroke: RIM, width: 2.5, grow: -2 })
  emit(UI, 'spr_ui_toggle_box_01.png', c, 'toggle box')
}

{
  const c = canvas(26, 26)
  const tick = union(sdSegment(4.5, 13.5, 10.5, 20, 3.4), sdSegment(10.5, 20, 21.5, 6, 3.4))
  paint(c, tick, { fill: INK, grow: 1.2, alpha: 0.9 })
  paint(c, tick, { fill: GOLD })
  emit(UI, 'spr_ui_toggle_check_01.png', c, 'toggle check')
}

// -------------------------------------------------------- P10.2 the logo ---

// About 1200 x 400 on transparency: the game's name as a chunky rounded wordmark, CHIKI, in a
// pink-to-gold fill with an ink outline and a drop shadow. Both Is wear a note head instead of a
// dot, and a row of beat ticks runs under the word, so the mark reads as rhythm as well as name.
{
  const w = 1200
  const h = 400
  const c = canvas(w, h)
  const half = 30 // the strokes are 60 px wide
  const top = 125
  const bottom = 305
  const mid = (top + bottom) / 2
  const r = (bottom - top) / 2

  const letters = []
  let x = 150
  // C: an open ring facing right.
  letters.push(sdArc(x + r, mid, r, 40, 320, half))
  x += 2 * r + 90
  // H
  letters.push(sdSegment(x, top, x, bottom, half), sdSegment(x + 170, top, x + 170, bottom, half), sdSegment(x, mid, x + 170, mid, half))
  x += 170 + 95
  // I
  const i1 = x
  letters.push(sdSegment(x, top + 40, x, bottom, half))
  x += 95
  // K
  letters.push(sdSegment(x, top, x, bottom, half), sdSegment(x + 160, top, x + 8, mid + 10, half), sdSegment(x + 55, mid - 8, x + 170, bottom, half))
  x += 170 + 95
  // I
  const i2 = x
  letters.push(sdSegment(x, top + 40, x, bottom, half))

  const word = union(...letters)
  const heads = union(sdCircle(i1 + 10, top - 20, 34), sdCircle(i2 + 10, top - 20, 34))
  const stems = union(sdSegment(i1 + 40, top - 22, i1 + 40, top - 100, 7), sdSegment(i2 + 40, top - 22, i2 + 40, top - 100, 7))

  const ticks = []
  for (let k = 0; k < 9; k++) {
    const tx = 190 + k * ((x - 150) / 8)
    const tall = k % 4 === 0
    ticks.push(sdRoundBox(tx, 358, 7, tall ? 22 : 13, 5))
  }
  const beat = union(...ticks)

  const shape = union(word, heads, stems)
  paint(c, shape, { fill: [0, 0, 0], grow: 12, alpha: 0.3, clip: null })
  paint(c, (px, py) => shape(px - 8, py - 12), { fill: [0, 0, 0], alpha: 0.35 })
  paint(c, union(shape, beat), { fill: INK, grow: 9 })
  paint(c, word, { fill: (_, py) => lerp(PINK, GOLD, Math.min(1, Math.max(0, (py - top) / (bottom - top)))) })
  paint(c, union(heads, stems), { fill: GOLD })
  paint(c, beat, { fill: (px) => lerp(PINK, GOLD, (px - 150) / (x - 150)) })
  // A highlight along the top of every stroke: the cute, lacquered finish.
  paint(c, (px, py) => word(px, py + 14) + 12, { fill: [255, 255, 255], alpha: 0.28, clip: word })
  emit(TITLE, 'spr_logo_chiki_static_01.png', c, 'wordmark, transparent')
}

// ------------------------------------------------ P10.2 title background ---

// 2560 x 1080 behind the logo: the menu night lifted to a violet stage — a spotlight on the
// centre, concentric beat rings spreading from it, a floor band and a scatter of sparkles —
// with the frame rule of P5.3: nothing essential in the outer 320 px either side.
{
  const w = 2560
  const h = 1080
  const c = nightBackdrop(w, h, { spot: [110, 72, 150], lattice: false })
  const cx = w / 2
  const cy = 430
  for (let k = 1; k <= 6; k++) {
    const r = 150 + k * 110
    paint(c, sdRing(cx, cy, r, k % 4 === 0 ? 5 : 3), { fill: lerp(PINK, [180, 160, 255], k / 6), alpha: 0.2 - k * 0.022, box: [cx - r - 8, cy - r - 8, cx + r + 8, cy + r + 8] })
  }

  const floorTop = 860
  paintAll(
    c,
    () => [26, 20, 44],
    (x, y) => (y < floorTop ? 0 : Math.min(0.85, (y - floorTop) / 60)),
  )
  paint(c, sdRoundBox(cx, floorTop, 900, 3, 3), { fill: lerp(PINK, GOLD, 0.5), alpha: 0.35, box: [cx - 910, floorTop - 8, cx + 910, floorTop + 8] })

  const rand = random(7)
  for (let k = 0; k < 90; k++) {
    const sx = 340 + rand() * (w - 680)
    const sy = 40 + rand() * (floorTop - 120)
    const s = 2 + rand() * 4
    const star = union(sdSegment(sx - s * 2, sy, sx + s * 2, sy, s * 0.35), sdSegment(sx, sy - s * 2, sx, sy + s * 2, s * 0.35))
    paint(c, star, { fill: [255, 240, 250], alpha: 0.25 + rand() * 0.4, box: [sx - s * 3, sy - s * 3, sx + s * 3, sy + s * 3] })
  }

  emit(TITLE, 'spr_bg_title_static_01.png', c, 'opaque, 16:9 centre', { bleedEdges: false })
}

// ------------------------------------------------ P10.3 outcome plates ---

// 1200 x 240 banners with no lettering: a ribbon whose tails fold behind a rounded plate, in the
// outcome's colour, with an emblem at each end — a star for Won, a cracked heart for Died, a
// hollow ring for Abandoned — so the three are told apart before the word is read. The plate's
// centre is left plain for the outcome word the screen writes over it.
function plate(colour, emblem) {
  const w = 1200
  const h = 240
  const c = canvas(w, h)
  const dark = lerp(colour, [0, 0, 0], 0.45)
  const tailL = sdPolygon([
    [30, 70],
    [210, 70],
    [210, 200],
    [30, 200],
    [90, 135],
  ])
  const tailR = sdPolygon([
    [1170, 70],
    [990, 70],
    [990, 200],
    [1170, 200],
    [1110, 135],
  ])
  const tails = union(tailL, tailR)
  const body = sdRoundBox(w / 2, 115, 430, 88, 30)
  paint(c, tails, { fill: INK, grow: 5 })
  paint(c, tails, { fill: dark })
  paint(c, body, { fill: [0, 0, 0], grow: 8, alpha: 0.3 })
  paint(c, body, { fill: INK, grow: 5 })
  paint(c, body, { fill: (_, y) => lerp(lerp(colour, [255, 255, 255], 0.22), dark, Math.min(1, Math.max(0, (y - 27) / 176))) })
  paint(c, body, { stroke: lerp(colour, [255, 255, 255], 0.5), width: 3, grow: -10, alpha: 0.8 })
  for (const ex of [240, 960]) {
    const e = emblem(ex, 115)
    paint(c, e, { fill: INK, grow: 4 })
    paint(c, e, { fill: lerp(colour, [255, 255, 255], 0.6) })
  }
  return c
}

function star(cx, cy) {
  const verts = []
  for (let k = 0; k < 10; k++) {
    const a = (-90 + k * 36) * (Math.PI / 180)
    const r = k % 2 === 0 ? 42 : 18
    verts.push([cx + r * Math.cos(a), cy + r * Math.sin(a)])
  }
  return sdRoundPolygon(verts, 2)
}

function crackedHeart(cx, cy) {
  const heart = union(sdCircle(cx - 17, cy - 10, 22), sdCircle(cx + 17, cy - 10, 22), sdPolygon([
    [cx - 37, cy],
    [cx + 37, cy],
    [cx, cy + 40],
  ]))
  const crack = union(sdSegment(cx + 2, cy - 30, cx - 8, cy - 6, 3.5), sdSegment(cx - 8, cy - 6, cx + 6, cy + 10, 3.5), sdSegment(cx + 6, cy + 10, cx - 2, cy + 36, 3.5))
  return (x, y) => Math.max(heart(x, y), -crack(x, y))
}

function hollowRing(cx, cy) {
  return sdRing(cx, cy, 30, 14)
}

emit(RUNEND, 'spr_ui_outcome_won_01.png', plate([224, 168, 40], star), 'gold, stars')
emit(RUNEND, 'spr_ui_outcome_died_01.png', plate([176, 36, 56], crackedHeart), 'crimson, cracked hearts')
emit(RUNEND, 'spr_ui_outcome_abandoned_01.png', plate([112, 116, 132], hollowRing), 'grey, hollow rings')

// --------------------------------------------- P10.4 calibration marker ---

// 140 x 140: a target that reads as a pulse — a gold ring, a gap and a filled core with a light
// catch — drawn to be tinted white and scaled on the beat by the calibration screen.
{
  const c = canvas(140, 140)
  paint(c, sdCircle(70, 70, 64), { fill: GOLD, alpha: 0.18 })
  paint(c, sdRing(70, 70, 56, 10), { fill: INK, grow: 2 })
  paint(c, sdRing(70, 70, 56, 10), { fill: GOLD })
  paint(c, sdCircle(70, 70, 34), { fill: INK, grow: 2 })
  paint(c, sdCircle(70, 70, 34), { fill: (_, y) => lerp(lerp(GOLD, [255, 255, 255], 0.35), lerp(GOLD, [0, 0, 0], 0.2), (y - 36) / 68) })
  paint(c, sdCircle(58, 58, 10), { fill: [255, 255, 255], alpha: 0.55 })
  emit(CALIBRATION, 'spr_ui_calibration-marker_static_01.png', c, 'beat target')
}

// ------------------------------------------------------------------ done ---

for (const f of written) {
  console.log(`${f.name.padEnd(40)} ${f.size.padEnd(12)} ${f.note}`)
}

const sprites = written.filter((f) => f.name.endsWith('.png')).length
console.log(`\n${sprites} sprites and ${written.length - sprites} sidecars written under Art/Shared/`)
