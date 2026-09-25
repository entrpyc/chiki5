// Draws the Phase 7 art of docs/plan.md: the three category card frames with their illustration
// windows and plain text areas, and the four rarity treatments as border and gem overlays (P7.1),
// and one illustration per card in data/sets/starter.json (P7.2). Every file is written at
// 512 x 720, as 8-bit RGBA PNG with straight alpha, named by the asset conventions and placed
// under client/Assets/_Project/Art/Shared/cards/.
//
// Run: node tools/gen-phase7-art.mjs
//
// Three rules shape everything here. A card face is three layers stacked in one 512 x 720 box —
// illustration, frame, rarity — so every layer shares the one geometry below, and the client lays
// its text over the plain areas this file leaves (CardFace.cs mirrors these numbers). A frame is
// told apart by its Category's colour (PRD 3.4.2) and a rarity by the shape of its gem as well as
// its colour, so neither depends on hue alone. And an illustration is seeded by its card id and
// nothing else: the emblem sits inside the central 512 x 400 so the compact face still reads it,
// no two cards share one, and a renamed card is redrawn by running this file again.

import { readFileSync } from 'node:fs'
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
const CARDS = join(ROOT, 'client', 'Assets', '_Project', 'Art', 'Shared', 'cards')
const STARTER = join(ROOT, 'data', 'sets', 'starter.json')

// --------------------------------------------------------------- geometry ---

/** The card's drawn size (P7.1); the full face shows it at half, the compact face at about a fifth. */
const W = 512
const H = 720
const CORNER = 28

/** The transparent window the illustration shows through. */
const WINDOW = { x0: 28, y0: 96, x1: 484, y1: 536, r: 16 }

/** The plain plate the name is written on. */
const NAME = { x0: 40, y0: 22, x1: 472, y1: 84, r: 14 }

/** The plain plate the rules, statuses and flavour are written on. */
const RULES = { x0: 36, y0: 566, x1: 476, y1: 698, r: 14 }

/** The value badge at the window's bottom-left corner and the cooldown badge at its bottom-right. */
const VALUE = { x: 76, y: 536, r: 42 }
const COOLDOWN = { x: 436, y: 536, r: 42 }

/** The rarity gem, centred between the window and the rules plate. */
const GEM = { x: 256, y: 548, r: 30 }

/** The emblem of an illustration stays inside this circle, itself inside the central 512 x 400 (P7.2). */
const EMBLEM = { x: 256, y: 330, r: 160 }

// ---------------------------------------------------------------- palette ---

const INK = [14, 12, 20]
const WELL = [17, 18, 26]

/** The Category colours of PRD 3.4.2, the same as the slot frames of P3.1. */
const CATEGORY = {
  attack: [214, 71, 66],
  defense: [62, 126, 214],
  ability: [72, 183, 108],
}

/** The rarity colours; each rarity's gem also has its own shape. */
const RARITY = {
  common: [140, 134, 126], // iron
  uncommon: [206, 214, 228], // silver
  rare: [240, 190, 66], // gold
  legendary: [244, 112, 52], // flame
}

const written = []

function emit(name, c, note) {
  bleed(c)
  writePng(join(CARDS, name), c)
  written.push({ name, size: `${c.w} x ${c.h}`, note })
}

const box = (b, inset = 0) => sdRoundBox((b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2, (b.x1 - b.x0) / 2 - inset, (b.y1 - b.y0) / 2 - inset, b.r)
const boundsOf = (b, pad = 4) => [b.x0 - pad, b.y0 - pad, b.x1 + pad, b.y1 + pad]
const around = (x, y, r) => [x - r, y - r, x + r, y + r]

/** The outline of the whole card, half a pixel inside the canvas. */
const cardShape = sdRoundBox(W / 2, H / 2, W / 2 - 0.5, H / 2 - 0.5, CORNER)

/** A field minus another: inside the first and outside the second. */
const subtract = (a, b) => (x, y) => Math.max(a(x, y), -b(x, y))

// ------------------------------------------------------- P7.1 card frames ---

/**
 * One Category's frame: a dark body tinted with the Category's colour, cut open where the
 * illustration shows, a rim in the colour, a name plate, a rules plate, and two badges — a
 * rhombus for the value and a ticked dial for the cooldown — whose centres the client writes on.
 * Every plate is flat, so text reads over it at any size.
 */
function cardFrame(colour) {
  const c = canvas(W, H)
  const window = box(WINDOW)
  const body = lerp(WELL, colour, 0.2)
  const plate = lerp(colour, [0, 0, 0], 0.55)
  const light = lerp(colour, [255, 255, 255], 0.35)

  paint(c, subtract(cardShape, window), { fill: body })

  // The rim, inside the outer 6 px a rarity border takes.
  const rim = sdRoundBox(W / 2, H / 2, W / 2 - 11, H / 2 - 11, CORNER - 10)
  paint(c, rim, { stroke: colour, width: 7 })
  paint(c, rim, { stroke: light, width: 1.5, grow: -4.5, alpha: 0.7 })

  // The window's own frame, drawn over its edge so the illustration meets a clean line.
  paint(c, window, { stroke: INK, width: 7, box: boundsOf(WINDOW, 8) })
  paint(c, window, { stroke: colour, width: 3, box: boundsOf(WINDOW, 8) })

  // The name plate.
  paint(c, box(NAME), { fill: plate, box: boundsOf(NAME) })
  paint(c, box(NAME), { stroke: light, width: 2.5, box: boundsOf(NAME) })

  // The rules plate: the darkest area, where the smallest text sits.
  paint(c, box(RULES), { fill: lerp(WELL, colour, 0.08), box: boundsOf(RULES) })
  paint(c, box(RULES), { stroke: colour, width: 2.5, box: boundsOf(RULES) })

  // The value badge: a rhombus, read as "how hard".
  const value = sdRoundPolygon(
    [
      [VALUE.x, VALUE.y - VALUE.r],
      [VALUE.x + VALUE.r, VALUE.y],
      [VALUE.x, VALUE.y + VALUE.r],
      [VALUE.x - VALUE.r, VALUE.y],
    ],
    6,
  )
  const valueBox = around(VALUE.x, VALUE.y, VALUE.r + 12)
  paint(c, value, { fill: INK, grow: 4, box: valueBox })
  paint(c, value, { fill: plate, box: valueBox })
  paint(c, value, { stroke: colour, width: 3.5, box: valueBox })

  // The cooldown badge: a dial with twelve ticks, read as "how long".
  const dialBox = around(COOLDOWN.x, COOLDOWN.y, COOLDOWN.r + 12)
  const dial = sdCircle(COOLDOWN.x, COOLDOWN.y, COOLDOWN.r - 6)
  paint(c, dial, { fill: INK, grow: 4, box: dialBox })
  paint(c, dial, { fill: plate, box: dialBox })
  paint(c, dial, { stroke: colour, width: 3.5, box: dialBox })
  const ticks = []
  for (let k = 0; k < 12; k++) {
    const a = (k * 30 * Math.PI) / 180
    const r0 = COOLDOWN.r - 2
    const r1 = COOLDOWN.r + 5
    ticks.push(sdSegment(COOLDOWN.x + r0 * Math.cos(a), COOLDOWN.y + r0 * Math.sin(a), COOLDOWN.x + r1 * Math.cos(a), COOLDOWN.y + r1 * Math.sin(a), 1.6))
  }

  paint(c, union(...ticks), { fill: light, box: dialBox })
  return c
}

for (const [category, colour] of Object.entries(CATEGORY)) {
  emit(`spr_ui_card-frame_${category}_01.png`, cardFrame(colour), `${category} frame, window and plates`)
}

// ---------------------------------------------------- P7.1 rarity overlays ---

/** The gem of each rarity, a different class of shape per rarity so colour is never the only cue. */
function gemShape(rarity) {
  const { x, y, r } = GEM
  const ring = (n, radius, phase = -90, inner = null) => {
    const verts = []
    const count = inner === null ? n : n * 2
    for (let k = 0; k < count; k++) {
      const a = ((phase + (k * 360) / count) * Math.PI) / 180
      const rr = inner === null || k % 2 === 0 ? radius : inner
      verts.push([x + rr * Math.cos(a), y + rr * Math.sin(a)])
    }

    return verts
  }

  switch (rarity) {
    case 'common':
      return sdCircle(x, y, r * 0.72) // a round stud
    case 'uncommon':
      return sdRoundPolygon(ring(4, r), 2) // a cut rhombus
    case 'rare':
      return sdRoundPolygon(ring(6, r, -90), 2) // a hexagon
    default:
      return sdRoundPolygon(ring(8, r * 1.12, -90, r * 0.58), 1.5) // an eight-pointed star
  }
}

/**
 * One rarity's treatment: a border in the outermost 6 px of the card and a gem between the window
 * and the rules plate, transparent everywhere else so it lies over any frame.
 */
function rarityOverlay(rarity) {
  const colour = RARITY[rarity]
  const light = lerp(colour, [255, 255, 255], 0.55)
  const dark = lerp(colour, [0, 0, 0], 0.45)
  const c = canvas(W, H)

  const border = sdRoundBox(W / 2, H / 2, W / 2 - 3.5, H / 2 - 3.5, CORNER - 3)
  const sheen = (x, y) => lerp(light, colour, Math.min(1, Math.abs(y - H / 2) / (H / 2)))
  // A band 6 px wide on the border's line, filled rather than stroked so it carries the sheen.
  const band = (x, y) => Math.abs(border(x, y)) - 3
  paint(c, band, { fill: sheen })

  const gem = gemShape(rarity)
  const gemBox = around(GEM.x, GEM.y, GEM.r * 1.5)
  paint(c, gem, { fill: INK, grow: 4, box: gemBox })
  paint(c, gem, {
    fill: (x, y) => lerp(light, lerp(colour, dark, 0.25), Math.min(1, Math.max(0, (y - (GEM.y - GEM.r)) / (2 * GEM.r)))),
    box: gemBox,
  })
  // A facet highlight up and to the left, so the gem reads as cut rather than flat.
  paint(c, sdCircle(GEM.x - GEM.r * 0.28, GEM.y - GEM.r * 0.3, GEM.r * 0.22), { fill: [255, 255, 255], alpha: 0.7, clip: gem, box: gemBox })

  if (rarity === 'legendary') {
    // A halo only the highest rarity carries.
    paintAll(
      c,
      () => colour,
      (x, y) => {
        const d = Math.hypot(x - GEM.x, y - GEM.y)
        return d < GEM.r * 1.1 || d > GEM.r * 1.5 ? 0 : 0.35 * (1 - (d - GEM.r * 1.1) / (GEM.r * 0.4))
      },
    )
  }

  return c
}

for (const rarity of Object.keys(RARITY)) {
  emit(`spr_ui_card-rarity_${rarity}_01.png`, rarityOverlay(rarity), `${rarity} border and gem`)
}

// ------------------------------------------------ P7.2 card illustrations ---

/** FNV-1a over the card id: the only input an illustration is drawn from. */
function hash(text) {
  let h = 0x811c9dc5
  for (let i = 0; i < text.length; i++) {
    h ^= text.charCodeAt(i)
    h = Math.imul(h, 0x01000193) >>> 0
  }

  return h >>> 0
}

/** mulberry32: a small deterministic generator, so the same id always draws the same emblem. */
function random(seed) {
  let a = seed >>> 0
  return () => {
    a = (a + 0x6d2b79f5) >>> 0
    let t = a
    t = Math.imul(t ^ (t >>> 15), t | 1)
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

function hsl(h, s, l) {
  const k = (n) => (n + h / 30) % 12
  const a = s * Math.min(l, 1 - l)
  const f = (n) => l - a * Math.max(-1, Math.min(k(n) - 3, Math.min(9 - k(n), 1)))
  return [f(0) * 255, f(8) * 255, f(4) * 255]
}

/**
 * One card's illustration: a two-tone ground behind an emblem of n-fold symmetry — petals or
 * blades around a ring around a core — whose fold count, petal form, proportions and hues all
 * come from the card id. The ground fills the canvas so the window never shows transparency;
 * the emblem stays inside EMBLEM so the compact face still reads it.
 */
function illustration(id) {
  const rnd = random(hash(id))
  const pick = (list) => list[Math.floor(rnd() * list.length)]
  const c = canvas(W, H)

  const hue = rnd() * 360
  const accentHue = (hue + 150 + rnd() * 60) % 360
  const deep = hsl(hue, 0.45, 0.12)
  const mid = hsl(hue, 0.5, 0.28)
  const main = hsl(hue, 0.62, 0.55)
  const accent = hsl(accentHue, 0.72, 0.62)
  const pale = hsl(hue, 0.5, 0.86)

  // The ground: a vertical fall from mid to deep, with a glow behind the emblem.
  paintAll(
    c,
    (x, y) => {
      const glow = Math.max(0, 1 - Math.hypot(x - EMBLEM.x, y - EMBLEM.y) / (EMBLEM.r * 1.7))
      return lerp(lerp(mid, deep, Math.min(1, y / H)), main, glow * 0.45)
    },
    () => 1,
  )

  // Faint concentric rings in the ground, spaced by the seed.
  const spacing = 26 + Math.floor(rnd() * 22)
  paintAll(
    c,
    () => pale,
    (x, y) => {
      const d = Math.hypot(x - EMBLEM.x, y - EMBLEM.y)
      if (d < EMBLEM.r * 1.05) return 0
      const m = Math.abs((d % spacing) - spacing / 2)
      return m < 1.2 ? 0.07 : 0
    },
  )

  const folds = 3 + Math.floor(rnd() * 6) // 3 to 8
  const phase = rnd() * (360 / folds)
  const form = pick(['blade', 'petal', 'spike', 'orb'])
  const reach = EMBLEM.r * (0.82 + rnd() * 0.18)
  const inner = EMBLEM.r * (0.22 + rnd() * 0.18)
  const width = 10 + rnd() * 22
  const ringR = inner + (reach - inner) * (0.3 + rnd() * 0.3)
  const ringW = 6 + rnd() * 10
  const core = pick(['circle', 'polygon', 'star'])
  const coreSides = 3 + Math.floor(rnd() * 5)
  const emblemBox = around(EMBLEM.x, EMBLEM.y, EMBLEM.r + 12)

  const petals = []
  for (let k = 0; k < folds; k++) {
    const a = ((phase + (k * 360) / folds - 90) * Math.PI) / 180
    const ux = Math.cos(a)
    const uy = Math.sin(a)
    const px = -uy
    const py = ux
    const at = (r, side = 0) => [EMBLEM.x + ux * r + px * side, EMBLEM.y + uy * r + py * side]
    switch (form) {
      case 'blade': {
        const [bx, by] = at(inner)
        const [tx, ty] = at(reach)
        const [lx, ly] = at((inner + reach) / 2, width)
        const [rx, ry] = at((inner + reach) / 2, -width)
        petals.push(sdPolygon([[bx, by], [lx, ly], [tx, ty], [rx, ry]]))
        break
      }
      case 'petal': {
        const [ax, ay] = at(inner + width)
        const [bx, by] = at(reach - width)
        petals.push(sdSegment(ax, ay, bx, by, width))
        break
      }
      case 'spike': {
        const [lx, ly] = at(inner, width * 0.9)
        const [rx, ry] = at(inner, -width * 0.9)
        const [tx, ty] = at(reach)
        petals.push(sdPolygon([[lx, ly], [tx, ty], [rx, ry]]))
        break
      }
      default: {
        const [ox, oy] = at(reach - width * 1.1)
        const [sx, sy] = at(inner + 4)
        petals.push(union(sdCircle(ox, oy, width * 1.1), sdSegment(sx, sy, ox, oy, Math.max(3, width * 0.25))))
      }
    }
  }

  const star = union(...petals)
  paint(c, star, { fill: INK, grow: 5, alpha: 0.85, box: emblemBox })
  paint(c, star, {
    fill: (x, y) => lerp(pale, main, Math.min(1, Math.hypot(x - EMBLEM.x, y - EMBLEM.y) / reach)),
    box: emblemBox,
  })

  const ring = (x, y) => Math.abs(Math.hypot(x - EMBLEM.x, y - EMBLEM.y) - ringR) - ringW / 2
  paint(c, ring, { fill: INK, grow: 3, alpha: 0.85, box: emblemBox })
  paint(c, ring, { fill: accent, box: emblemBox })

  let coreField
  if (core === 'circle') {
    coreField = sdCircle(EMBLEM.x, EMBLEM.y, inner)
  } else {
    const verts = []
    const count = core === 'star' ? coreSides * 2 : coreSides
    for (let k = 0; k < count; k++) {
      const a = ((-90 + (k * 360) / count) * Math.PI) / 180
      const r = core === 'star' && k % 2 === 1 ? inner * 0.5 : inner
      verts.push([EMBLEM.x + r * Math.cos(a), EMBLEM.y + r * Math.sin(a)])
    }

    coreField = sdRoundPolygon(verts, 3)
  }

  paint(c, coreField, { fill: INK, grow: 4, box: emblemBox })
  paint(c, coreField, {
    fill: (x, y) => lerp(accent, pale, Math.max(0, 1 - Math.hypot(x - EMBLEM.x, y - EMBLEM.y) / inner)),
    box: emblemBox,
  })

  return { canvas: c, note: `${folds}-fold ${form}, ${core} core` }
}

const starter = JSON.parse(readFileSync(STARTER, 'utf8'))
for (const card of starter.cards) {
  const subject = card.id.replace(/^card-/, '')
  const drawn = illustration(card.id)
  emit(`spr_card_${subject}_static_01.png`, drawn.canvas, drawn.note)
}

// ------------------------------------------------------------------ done ---

for (const f of written) {
  console.log(`${f.name.padEnd(40)} ${f.size.padEnd(10)} ${f.note}`)
}

console.log(`\n${written.length} sprites written under Art/Shared/cards/`)
