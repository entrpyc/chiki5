// Builds the placeholder font family of docs/plan.md P10.1: a geometric sans in two weights,
// written as TrueType files under client/Assets/_Project/UI/Fonts/ with no dependency beyond
// Node. Every glyph is the implementer's own drawing, so the family carries no licence; the
// operator drops a licensed family over the same two files later and nothing else changes.
//
// Run: node tools/gen-font.mjs
//
// How a glyph is made. Each letter, digit and mark is a *stroke skeleton*: centre lines drawn
// as polylines and arcs on a design grid whose baseline is 0, x-height 500, cap height 700,
// ascender 760 and descender -200. The skeleton is widened to an outline by laying a quad along
// every segment, a bevel wedge on the outside of every joint and a round cap at every open end,
// all wound clockwise so the overlaps fill under TrueType's non-zero rule. The bold weight is
// the same skeleton at a heavier stroke. Before widening, the skeleton's y is mapped so the
// *outer* edge of the stroke, not its centre, lands on the baseline, x-height, cap height and
// ascender, which is what keeps both weights on the same lines.
//
// Coverage is Latin-1 and Latin Extended-A, plus the General Punctuation the string table
// uses (the en dash of the Bluetooth note) and the euro and trade-mark signs. Every accented
// letter is a composite glyph: the base letter and a diacritic glyph placed by offset, so a
// diacritic is drawn once and every letter that carries it agrees.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const FONTS = join(ROOT, 'client', 'Assets', '_Project', 'UI', 'Fonts')

const FAMILY = 'Chiki Placeholder'
const UPM = 1000
const SIDE = 46 // side bearing either side of the outline
const ASCENT = 1010
const DESCENT = 270
const CAP_MARK_RISE = 215 // how far a diacritic climbs from a lowercase letter to a capital
const ASC_MARK_RISE = 270 // and to the top of an ascender

const WEIGHTS = [
  { file: 'font_regular.ttf', style: 'Regular', stroke: 78, weightClass: 400, bold: false },
  { file: 'font_bold.ttf', style: 'Bold', stroke: 128, weightClass: 700, bold: true },
]

// ---------------------------------------------------------- skeleton kit ---

/** A polyline from a flat list of coordinates. */
const P = (...c) => {
  const pts = []
  for (let i = 0; i < c.length; i += 2) pts.push([c[i], c[i + 1]])
  return { pts }
}

/** Points along an elliptical arc from angle a0 to a1 in degrees, anticlockwise when a1 > a0. */
function arcPts(cx, cy, rx, ry, a0, a1) {
  const span = a1 - a0
  const steps = Math.max(2, Math.ceil(Math.abs(span) / 9))
  const pts = []
  for (let i = 0; i <= steps; i++) {
    const a = ((a0 + (span * i) / steps) * Math.PI) / 180
    pts.push([cx + rx * Math.cos(a), cy + ry * Math.sin(a)])
  }
  return pts
}

const arc = (cx, cy, rx, ry, a0, a1) => ({ pts: arcPts(cx, cy, rx, ry, a0, a1) })

/** A closed ellipse. */
const ell = (cx, cy, rx, ry) => ({ pts: arcPts(cx, cy, rx, ry, 90, 450).slice(0, -1), closed: true })

/** A round dot; `scale` sizes it against the stroke. */
const dot = (x, y, scale = 1) => ({ dot: [x, y], scale })

/** Joins polylines and arcs, end to start, into one stroke. */
function cat(...parts) {
  const pts = []
  for (const part of parts) {
    const list = Array.isArray(part) ? part : part.pts
    for (const p of list) {
      const last = pts[pts.length - 1]
      if (last && Math.abs(last[0] - p[0]) < 0.01 && Math.abs(last[1] - p[1]) < 0.01) continue
      pts.push(p)
    }
  }
  return { pts }
}

/** Moves strokes by (dx, dy). */
const move = (strokes, dx, dy) =>
  strokes.map((s) => (s.dot ? { ...s, dot: [s.dot[0] + dx, s.dot[1] + dy] } : { ...s, pts: s.pts.map(([x, y]) => [x + dx, y + dy]) }))

/** Scales strokes about the origin, then moves them; superscripts and fractions are built so. */
const scaleMove = (strokes, k, dx, dy) =>
  strokes.map((s) =>
    s.dot
      ? { ...s, dot: [s.dot[0] * k + dx, s.dot[1] * k + dy], scale: s.scale * 0.8 }
      : { ...s, pts: s.pts.map(([x, y]) => [x * k + dx, y * k + dy]) },
  )

/** Mirrors strokes left to right about x = w / 2. */
const mirror = (strokes, w) =>
  strokes.map((s) => (s.dot ? { ...s, dot: [w - s.dot[0], s.dot[1]] } : { ...s, pts: s.pts.map(([x, y]) => [w - x, y]) }))

/** Turns strokes half a turn about (w / 2, cy): how ¡ and ¿ are made from ! and ?. */
const turn = (strokes, w, cy) =>
  strokes.map((s) =>
    s.dot ? { ...s, dot: [w - s.dot[0], 2 * cy - s.dot[1]] } : { ...s, pts: s.pts.map(([x, y]) => [w - x, 2 * cy - y]) },
  )

// ------------------------------------------------------------- skeletons ---

// Capitals: cap height 700.
const S_UPPER = [cat(arc(210, 530, 200, 170, 25, 270), arc(210, 180, 215, 180, 90, -155))]
const O_UPPER = [ell(340, 350, 340, 350)]
const D_UPPER = [cat(P(0, 0, 0, 700, 160, 700), arc(160, 350, 330, 350, 90, -90), P(160, 0, 0, 0))]
const L_UPPER = [P(0, 700, 0, 0, 380, 0)]
const H_UPPER = [P(0, 0, 0, 700), P(480, 0, 480, 700), P(0, 360, 480, 360)]
const T_UPPER = [P(0, 700, 500, 700), P(250, 700, 250, 0)]
const I_UPPER = [P(0, 0, 0, 700)]
const J_UPPER = [cat(P(360, 700, 360, 210), arc(180, 210, 180, 210, 0, -180))]
const P_BOWL = [cat(P(0, 0, 0, 700, 210, 700), arc(210, 520, 200, 180, 90, -90), P(210, 340, 0, 340))]
const C_UPPER = [arc(330, 350, 330, 350, 48, 312)]

const UPPER = {
  A: [P(0, 0, 260, 700, 520, 0), P(92, 230, 428, 230)],
  B: [cat(P(0, 360, 0, 700, 220, 700), arc(220, 530, 170, 170, 90, -90), P(220, 360, 0, 360)), cat(P(0, 360, 250, 360), arc(250, 180, 190, 180, 90, -90), P(250, 0, 0, 0, 0, 360))],
  C: C_UPPER,
  D: D_UPPER,
  E: [P(400, 700, 0, 700, 0, 0, 400, 0), P(0, 360, 340, 360)],
  F: [P(380, 700, 0, 700, 0, 0), P(0, 360, 320, 360)],
  G: [cat(arc(320, 350, 320, 350, 48, 360), P(640, 350, 380, 350))],
  H: H_UPPER,
  I: I_UPPER,
  J: J_UPPER,
  K: [P(0, 0, 0, 700), P(450, 700, 0, 250), P(160, 410, 470, 0)],
  L: L_UPPER,
  M: [P(0, 0, 0, 700, 310, 130, 620, 700, 620, 0)],
  N: [P(0, 0, 0, 700, 500, 0, 500, 700)],
  O: O_UPPER,
  P: P_BOWL,
  Q: [...O_UPPER, P(430, 170, 660, -50)],
  R: [...P_BOWL, P(210, 340, 450, 0)],
  S: S_UPPER,
  T: T_UPPER,
  U: [cat(P(0, 700, 0, 250), arc(240, 250, 240, 250, 180, 360), P(480, 250, 480, 700))],
  V: [P(0, 700, 270, 0, 540, 700)],
  W: [P(0, 700, 190, 0, 410, 620, 630, 0, 820, 700)],
  X: [P(0, 700, 500, 0), P(500, 700, 0, 0)],
  Y: [P(0, 700, 260, 340, 520, 700), P(260, 340, 260, 0)],
  Z: [P(0, 700, 460, 700, 0, 0, 460, 0)],
}

// Small letters: x-height 500, ascender 760, descender -200.
const BOWL = [ell(230, 250, 230, 250)]
const N_ARCH = [P(0, 500, 0, 0), cat(P(0, 280), arc(220, 280, 220, 220, 180, 0), P(440, 280, 440, 0))]
const O_LOWER = [ell(250, 250, 250, 250)]
const E_LOWER = [cat(P(0, 250, 500, 250), arc(250, 250, 250, 250, 0, 318))]
const S_LOWER = [cat(arc(180, 380, 170, 120, 25, 270), arc(180, 130, 185, 130, 90, -155))]
const T_LOWER = [cat(P(120, 690, 120, 130), arc(260, 130, 140, 130, 180, 290)), P(0, 500, 290, 500)]
const U_LOWER = [cat(P(0, 500, 0, 220), arc(220, 220, 220, 220, 180, 360)), P(440, 500, 440, 0)]
const DOTLESS_I = [P(0, 0, 0, 500)]
const DOTLESS_J = [cat(P(100, 500, 100, -80), arc(0, -80, 100, 120, 0, -150))]
const L_LOWER = [P(0, 760, 0, 0)]
const H_LOWER = [P(0, 760, 0, 0), cat(P(0, 280), arc(220, 280, 220, 220, 180, 0), P(440, 280, 440, 0))]
const D_LOWER = [...BOWL, P(460, 760, 460, 0)]
const G_LOWER = [...BOWL, cat(P(460, 500, 460, -20), arc(230, -20, 230, 180, 0, -165))]
const K_LOWER = [P(0, 760, 0, 0), P(400, 500, 0, 190), P(140, 300, 420, 0)]

const LOWER = {
  a: [...BOWL, P(460, 500, 460, 0)],
  b: [P(0, 760, 0, 0), ...BOWL],
  c: [arc(250, 250, 250, 250, 46, 314)],
  d: D_LOWER,
  e: E_LOWER,
  f: [cat(P(100, 0, 100, 590), arc(240, 590, 140, 160, 180, 40)), P(0, 480, 270, 480)],
  g: G_LOWER,
  h: H_LOWER,
  i: [...DOTLESS_I, dot(0, 680)],
  j: [...DOTLESS_J, dot(100, 680)],
  k: K_LOWER,
  l: L_LOWER,
  m: [P(0, 500, 0, 0), cat(P(0, 300), arc(170, 300, 170, 200, 180, 0), P(340, 300, 340, 0)), cat(P(340, 300), arc(510, 300, 170, 200, 180, 0), P(680, 300, 680, 0))],
  n: N_ARCH,
  o: O_LOWER,
  p: [P(0, 500, 0, -200), ...BOWL],
  q: [...BOWL, P(460, 500, 460, -200)],
  r: [P(0, 500, 0, 0), cat(P(0, 280), arc(210, 280, 210, 220, 180, 65))],
  s: S_LOWER,
  t: T_LOWER,
  u: U_LOWER,
  v: [P(0, 500, 230, 0, 460, 500)],
  w: [P(0, 500, 160, 0, 340, 420, 520, 0, 680, 500)],
  x: [P(0, 500, 440, 0), P(440, 500, 0, 0)],
  y: [P(0, 500, 230, 0), P(460, 500, 46, -200)],
  z: [P(0, 500, 400, 500, 0, 0, 400, 0)],
}

const DIGITS = {
  0: [ell(220, 350, 220, 350)],
  1: [P(30, 570, 200, 700, 200, 0)],
  2: [cat(arc(210, 490, 210, 210, 160, -35), P(382, 370, 0, 0, 430, 0))],
  3: [cat(arc(200, 530, 190, 170, 150, -90), arc(200, 180, 210, 180, 90, -150))],
  4: [P(330, 0, 330, 700, 0, 200, 450, 200)],
  5: [cat(P(400, 700, 60, 700, 40, 390), arc(210, 225, 210, 225, 135, -150))],
  6: [ell(220, 220, 220, 220), P(40, 300, 300, 700)],
  7: [P(0, 700, 440, 700, 140, 0)],
  8: [ell(210, 530, 175, 170), ell(210, 180, 210, 180)],
  9: [ell(220, 480, 220, 220), P(400, 400, 140, 0)],
}

const QUESTION = [cat(arc(200, 520, 200, 180, 160, -70), P(268, 351, 200, 250, 200, 190)), dot(200, 0)]
const EXCLAIM = [P(0, 700, 0, 210), dot(0, 0)]
const COMMA = [P(40, 50, 0, -120)]
const QUOTE_R = [P(40, 720, 0, 560)]
const QUOTE_L = [P(0, 720, 40, 560)]
const GUILLEMET = [P(200, 440, 50, 250, 200, 60)]
const PERCENT = [ell(90, 590, 90, 110), ell(410, 110, 90, 110), P(430, 700, 70, 0)]
const C_SMALL = [arc(250, 250, 230, 250, 46, 314)]

/** A digit shrunk to a superscript (raised) or a fraction's denominator (on the baseline). */
const sup = (d, dx = 0) => scaleMove(DIGITS[d], 0.45, dx, 395)
const sub = (d, dx = 0) => scaleMove(DIGITS[d], 0.45, dx, 0)

const SYMBOLS = {
  0x20: { strokes: [], width: 250 },
  0x21: EXCLAIM,
  0x22: [P(0, 700, 0, 520), P(160, 700, 160, 520)],
  0x23: [P(140, 660, 100, 40), P(320, 660, 280, 40), P(20, 440, 420, 440), P(0, 260, 400, 260)],
  0x24: [...S_UPPER, P(210, 790, 210, -90)],
  0x25: PERCENT,
  0x26: [arc(220, 570, 120, 130, -40, 220), P(143, 470, 480, 0), cat([[312, 486]], arc(200, 190, 190, 190, 140, 350), [[470, 330]])],
  0x27: [P(0, 700, 0, 520)],
  0x28: [arc(260, 280, 260, 480, 120, 240)],
  0x29: [arc(-130, 280, 260, 480, 60, -60)],
  0x2a: [P(150, 710, 150, 430), P(30, 640, 270, 500), P(30, 500, 270, 640)],
  0x2b: [P(0, 300, 400, 300), P(200, 100, 200, 500)],
  0x2c: COMMA,
  0x2d: [P(0, 280, 260, 280)],
  0x2e: [dot(0, 0)],
  0x2f: [P(0, -80, 340, 720)],
  0x3a: [dot(0, 0), dot(0, 440)],
  0x3b: [...COMMA, dot(40, 440)],
  0x3c: [P(400, 560, 0, 300, 400, 40)],
  0x3d: [P(0, 380, 400, 380), P(0, 200, 400, 200)],
  0x3e: [P(0, 560, 400, 300, 0, 40)],
  0x3f: QUESTION,
  0x40: [ell(310, 300, 120, 130), P(430, 430, 430, 200, 520, 140, 640, 220), arc(330, 300, 330, 330, -12, 330)],
  0x5b: [P(160, 760, 0, 760, 0, -140, 160, -140)],
  0x5c: [P(0, 720, 340, -80)],
  0x5d: [P(0, 760, 160, 760, 160, -140, 0, -140)],
  0x5e: [P(0, 440, 180, 700, 360, 440)],
  0x5f: [P(0, -150, 460, -150)],
  0x60: [P(0, 720, 120, 600)],
  0x7b: [P(200, 760, 100, 700, 100, 360, 0, 300, 100, 240, 100, -80, 200, -140)],
  0x7c: [P(0, 780, 0, -200)],
  0x7d: [P(0, 760, 100, 700, 100, 360, 200, 300, 100, 240, 100, -80, 0, -140)],
  0x7e: [cat(arc(90, 260, 90, 70, 180, 0), arc(270, 260, 90, 70, 180, 360))],

  0xa0: { strokes: [], width: 250 },
  0xa1: turn(EXCLAIM, 0, 250),
  0xa2: [...C_SMALL, P(250, 620, 250, -110)],
  0xa3: [cat(arc(290, 560, 150, 140, 20, 180), P(140, 560, 140, 110, 50, 0, 440, 0)), P(20, 340, 320, 340)],
  0xa4: [ell(220, 320, 160, 160), P(40, 500, 100, 440), P(400, 500, 340, 440), P(40, 140, 100, 200), P(400, 140, 340, 200)],
  0xa5: [P(0, 700, 260, 340, 520, 700), P(260, 340, 260, 0), P(60, 280, 460, 280), P(60, 150, 460, 150)],
  0xa6: [P(0, 780, 0, 380), P(0, 200, 0, -200)],
  0xa7: [...S_UPPER, ell(210, 350, 110, 80)],
  0xa9: [ell(360, 350, 360, 360), arc(380, 350, 160, 170, 45, 315)],
  0xaa: [ell(110, 570, 110, 120), P(220, 690, 220, 450), P(0, 360, 240, 360)],
  0xab: [...GUILLEMET, ...move(GUILLEMET, 200, 0)],
  0xac: [P(0, 340, 400, 340, 400, 160)],
  0xad: [P(0, 280, 260, 280)],
  0xae: [ell(360, 350, 360, 360), cat(P(260, 160, 260, 540, 370, 540), arc(370, 450, 90, 90, 90, -90), P(370, 360, 260, 360)), P(360, 360, 470, 160)],
  0xb0: [ell(110, 590, 110, 110)],
  0xb1: [P(0, 340, 400, 340), P(200, 140, 200, 540), P(0, 20, 400, 20)],
  0xb2: sup(2),
  0xb3: sup(3),
  0xb5: [P(0, 500, 0, -200), ...U_LOWER],
  0xb6: [P(300, 700, 300, -100), P(420, 700, 420, -100), cat(P(420, 700, 160, 700), arc(160, 520, 160, 180, 90, 270), P(160, 340, 300, 340))],
  0xb7: [dot(0, 280)],
  0xb9: sup(1),
  0xba: [ell(120, 580, 110, 120), P(0, 360, 240, 360)],
  0xbb: mirror([...GUILLEMET, ...move(GUILLEMET, 200, 0)], 400),
  0xbc: [...sup(1), P(40, 0, 560, 700), ...sub(4, 380)],
  0xbd: [...sup(1), P(40, 0, 560, 700), ...sub(2, 400)],
  0xbe: [...sup(3), P(80, 0, 600, 700), ...sub(4, 400)],
  0xbf: turn(QUESTION, 400, 250),
  0xc6: [P(0, 0, 380, 700, 760, 700), P(380, 700, 380, 0, 760, 0), P(380, 360, 720, 360), P(135, 250, 380, 250)],
  0xd0: [...D_UPPER, P(-90, 360, 180, 360)],
  0xd7: [P(40, 120, 360, 440), P(40, 440, 360, 120)],
  0xd8: [...O_UPPER, P(40, -40, 640, 740)],
  0xde: [P(0, 0, 0, 700), cat(P(0, 560, 210, 560), arc(210, 370, 200, 190, 90, -90), P(210, 180, 0, 180))],
  0xdf: [cat(P(0, 0, 0, 560), arc(180, 560, 180, 200, 180, -80), [[210, 380]]), cat([[210, 380]], arc(200, 185, 220, 195, 90, -150))],
  0xe6: [ell(210, 250, 210, 250), cat(P(420, 250, 900, 250), arc(660, 250, 240, 250, 0, 318))],
  0xf0: [...O_LOWER, P(380, 440, 300, 680, 170, 760), P(170, 620, 430, 700)],
  0xf7: [P(0, 280, 400, 280), dot(200, 480), dot(200, 80)],
  0xf8: [...O_LOWER, P(0, -40, 500, 540)],
  0xfe: [P(0, 760, 0, -200), ...BOWL],

  0x110: [...D_UPPER, P(-90, 360, 180, 360)],
  0x111: [...D_LOWER, P(300, 650, 560, 650)],
  0x126: [...H_UPPER, P(-70, 550, 550, 550)],
  0x127: [...H_LOWER, P(-90, 630, 230, 630)],
  0x131: DOTLESS_I,
  0x132: [...I_UPPER, ...move(J_UPPER, 170, 0)],
  0x133: [...LOWER.i, ...move(LOWER.j, 150, 0)],
  0x138: [P(0, 500, 0, 0), P(360, 500, 0, 200), P(130, 290, 380, 0)],
  0x13f: [...L_UPPER, dot(220, 350)],
  0x140: [...L_LOWER, dot(170, 350)],
  0x141: [...L_UPPER, P(-70, 250, 200, 460)],
  0x142: [...L_LOWER, P(-130, 250, 130, 470)],
  0x149: [P(20, 720, 0, 560), ...move(N_ARCH, 140, 0)],
  0x14a: [P(0, 0, 0, 700), cat(P(0, 420), arc(240, 420, 240, 280, 180, 0), P(480, 420, 480, -40), arc(380, -40, 100, 120, 0, -160))],
  0x14b: [P(0, 500, 0, 0), cat(P(0, 280), arc(220, 280, 220, 220, 180, 0), P(440, 280, 440, -80), arc(340, -80, 100, 120, 0, -160))],
  0x152: [arc(340, 350, 340, 350, 90, 270), P(340, 700, 760, 700), P(340, 0, 760, 0), P(340, 700, 340, 0), P(340, 360, 700, 360)],
  0x153: [ell(230, 250, 230, 250), cat(P(460, 250, 920, 250), arc(690, 250, 230, 250, 0, 318))],
  0x166: [...T_UPPER, P(100, 360, 400, 360)],
  0x167: [...T_LOWER, P(30, 290, 230, 290)],
  0x17f: [cat(P(100, 0, 100, 560), arc(240, 560, 140, 190, 180, 40))],

  0x2013: [P(0, 280, 500, 280)],
  0x2014: [P(0, 280, 900, 280)],
  0x2018: QUOTE_L,
  0x2019: QUOTE_R,
  0x201a: COMMA,
  0x201c: [...QUOTE_L, ...move(QUOTE_L, 160, 0)],
  0x201d: [...QUOTE_R, ...move(QUOTE_R, 160, 0)],
  0x201e: [...COMMA, ...move(COMMA, 160, 0)],
  0x2020: [P(150, 720, 150, -120), P(0, 520, 300, 520)],
  0x2021: [P(150, 720, 150, -120), P(0, 520, 300, 520), P(0, 160, 300, 160)],
  0x2022: [dot(0, 280, 1.6)],
  0x2026: [dot(0, 0), dot(250, 0), dot(500, 0)],
  0x2030: [...PERCENT, ell(620, 110, 90, 110)],
  0x2039: GUILLEMET,
  0x203a: mirror(GUILLEMET, 200),
  0x20ac: [arc(380, 350, 310, 350, 50, 310), P(0, 420, 400, 420), P(0, 280, 400, 280)],
  0x2122: [P(0, 700, 220, 700), P(110, 700, 110, 440), P(300, 440, 300, 700, 400, 560, 500, 700, 500, 440)],
}

// Diacritics, drawn over a lowercase letter and centred on x = 0; they keep their own y (no
// baseline mapping), because they sit above the letter rather than on its lines.
const MARKS = {
  grave: [P(55, 590, -65, 730)],
  acute: [P(-55, 590, 65, 730)],
  circumflex: [P(-130, 590, 0, 720, 130, 590)],
  tilde: [cat(arc(-90, 650, 90, 55, 180, 0), arc(90, 650, 90, 55, 180, 360))],
  dieresis: [dot(-115, 655), dot(115, 655)],
  ring: [ell(0, 670, 80, 80)],
  macron: [P(-150, 650, 150, 650)],
  breve: [arc(0, 710, 140, 110, 180, 360)],
  dotaccent: [dot(0, 655)],
  caron: [P(-130, 720, 0, 590, 130, 720)],
  hungarumlaut: [P(-120, 590, -40, 730), P(60, 590, 140, 730)],
  cedilla: [P(10, 30, 10, -60, 80, -110, 40, -180, -60, -180)],
  ogonek: [P(40, 30, -30, -90, 10, -185, 100, -175)],
  commabelow: [P(20, -60, -20, -210)],
  commaabove: [P(20, 590, -20, 740)],
  caronright: [P(40, 750, 0, 590)],
}

// Spacing forms of the marks that Latin-1 encodes as characters of their own.
const SPACING_MARKS = { 0xa8: 'dieresis', 0xaf: 'macron', 0xb4: 'acute', 0xb8: 'cedilla' }

// ------------------------------------------------------------ composites ---

// Every accented letter: its base, its mark, and where the mark sits. `at` is 'top' (over the
// letter's centre), 'bottom' (under it), 'right' (after the letter: ogonek, the caron of ď).
const ACCENTS = {
  grave: 'ÀÈÌÒÙàèìòù',
  acute: 'ÁÉÍÓÚÝáéíóúýĆćĹĺŃńŔŕŚśŹź',
  circumflex: 'ÂÊÎÔÛâêîôûĈĉĜĝĤĥĴĵŜŝŴŵŶŷ',
  tilde: 'ÃÑÕãñõĨĩŨũ',
  dieresis: 'ÄËÏÖÜäëïöüÿŸ',
  ring: 'ÅåŮů',
  macron: 'ĀāĒēĪīŌōŪū',
  breve: 'ĂăĔĕĞğĬĭŎŏŬŭ',
  dotaccent: 'ĊċĖėĠġİŻż',
  caron: 'ČčĎĚěŇňŘřŠšŤŽžĽ',
  hungarumlaut: 'ŐőŰű',
  cedilla: 'ÇçŞşŢţ',
  ogonek: 'ĄąĘęĮįŲų',
  commabelow: 'ĢĶķĻļŅņŖŗ',
  commaabove: 'ģ',
}

// Letters whose caron sits to the right of an ascender or a stem, drawn as an apostrophe.
const CARON_RIGHT = new Set(['ď', 'ľ', 'ť', 'Ľ'])
// Letters whose mark sits over the stem of an ascender rather than over the x-height.
const ASCENDER_TOP = new Set(['ĥ', 'ĺ'])

const BELOW = new Set(['cedilla', 'ogonek', 'commabelow'])

/** The base letter of an accented one: its first decomposed code point, dotless for i and j under a mark above. */
function baseOf(ch, mark) {
  const base = ch.normalize('NFD')[0]
  if (BELOW.has(mark)) return base
  if (base === 'i') return 'ı'
  if (base === 'j') return 'ȷ'
  return base
}

function accentPlan() {
  const plan = []
  const seen = new Set()
  for (const [mark, letters] of Object.entries(ACCENTS)) {
    for (const ch of letters) {
      if (seen.has(ch)) continue
      seen.add(ch)
      plan.push({ ch, base: baseOf(ch, mark), mark })
    }
  }
  for (const ch of CARON_RIGHT) {
    if (seen.has(ch)) {
      plan.find((p) => p.ch === ch).mark = 'caronright'
    } else {
      seen.add(ch)
      plan.push({ ch, base: baseOf(ch, 'caronright'), mark: 'caronright' })
    }
  }
  return plan
}

// --------------------------------------------------------------- outline ---

/** Maps a skeleton y so the stroke's outer edge, not its centre, lands on each guide line. */
function mapY(y, w) {
  const h = w / 2
  const guides = [
    [-200, -200 + h],
    [0, h],
    [500, 500 - h],
    [700, 700 - h],
    [760, 760 - h],
  ]
  if (y <= guides[0][0]) return y + h
  if (y >= guides[guides.length - 1][0]) return y - h
  for (let i = 0; i < guides.length - 1; i++) {
    const [a0, b0] = guides[i]
    const [a1, b1] = guides[i + 1]
    if (y <= a1) return b0 + ((y - a0) * (b1 - b0)) / (a1 - a0)
  }
  return y
}

function signedArea(poly) {
  let a = 0
  for (let i = 0; i < poly.length; i++) {
    const [x0, y0] = poly[i]
    const [x1, y1] = poly[(i + 1) % poly.length]
    a += x0 * y1 - x1 * y0
  }
  return a / 2
}

/** TrueType fills clockwise contours in a y-up space: negative signed area. */
function clockwise(poly) {
  return signedArea(poly) > 0 ? poly.slice().reverse() : poly
}

function circle(cx, cy, r, n) {
  const pts = []
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2
    pts.push([cx + r * Math.cos(a), cy + r * Math.sin(a)])
  }
  return clockwise(pts)
}

/** Widens one stroke into convex contours: segment quads, joint wedges and round caps. */
function widen(stroke, w, mapped) {
  const h = w / 2
  const contours = []
  if (stroke.dot) {
    const [x, y] = stroke.dot
    contours.push(circle(x, mapped ? mapY(y, w) : y, h * 1.18 * stroke.scale, 20))
    return contours
  }

  const pts = stroke.pts.map(([x, y]) => [x, mapped ? mapY(y, w) : y])
  const n = pts.length
  const closed = !!stroke.closed
  const segCount = closed ? n : n - 1
  const normals = []
  for (let i = 0; i < segCount; i++) {
    const a = pts[i]
    const b = pts[(i + 1) % n]
    const dx = b[0] - a[0]
    const dy = b[1] - a[1]
    const len = Math.hypot(dx, dy)
    if (len < 1e-6) {
      normals.push(null)
      continue
    }
    const nx = -dy / len
    const ny = dx / len
    normals.push([nx, ny])
    contours.push(
      clockwise([
        [a[0] + nx * h, a[1] + ny * h],
        [b[0] + nx * h, b[1] + ny * h],
        [b[0] - nx * h, b[1] - ny * h],
        [a[0] - nx * h, a[1] - ny * h],
      ]),
    )
  }

  // Joints: a wedge fills the outside of every bend, and a sharp bend is rounded as well.
  const joints = closed ? n : n - 2
  for (let j = 0; j < joints; j++) {
    const i = closed ? j : j + 1
    const n0 = normals[(i - 1 + segCount) % segCount]
    const n1 = normals[i % segCount]
    if (!n0 || !n1) continue
    const p = pts[i]
    const cross = n0[0] * n1[1] - n0[1] * n1[0]
    const turnAngle = Math.acos(Math.max(-1, Math.min(1, n0[0] * n1[0] + n0[1] * n1[1])))
    if (turnAngle < 1e-3) continue
    const side = cross > 0 ? -1 : 1
    contours.push(
      clockwise([
        [p[0], p[1]],
        [p[0] + side * n0[0] * h, p[1] + side * n0[1] * h],
        [p[0] + side * n1[0] * h, p[1] + side * n1[1] * h],
      ]),
    )
    if (turnAngle > Math.PI / 5) contours.push(circle(p[0], p[1], h, 16))
  }

  if (!closed) {
    contours.push(circle(pts[0][0], pts[0][1], h, 18))
    contours.push(circle(pts[n - 1][0], pts[n - 1][1], h, 18))
  }

  return contours
}

function bounds(contours) {
  let xMin = Infinity
  let yMin = Infinity
  let xMax = -Infinity
  let yMax = -Infinity
  for (const c of contours) {
    for (const [x, y] of c) {
      xMin = Math.min(xMin, x)
      yMin = Math.min(yMin, y)
      xMax = Math.max(xMax, x)
      yMax = Math.max(yMax, y)
    }
  }
  return { xMin, yMin, xMax, yMax }
}

/** A glyph from skeleton strokes: its contours rounded to the grid, shifted to its side bearing. */
function simpleGlyph(name, strokes, w, { mapped = true, width = null, centred = false } = {}) {
  let contours = []
  for (const s of strokes) contours.push(...widen(s, w, mapped))
  if (contours.length === 0) {
    return { name, contours: [], advance: width ?? 250, shift: 0 }
  }
  const b = bounds(contours)
  // A mark keeps x = 0 as its centre so composites can place it; a letter starts at its bearing.
  const shift = centred ? 0 : SIDE - b.xMin
  contours = contours.map((c) => c.map(([x, y]) => [Math.round(x + shift), Math.round(y)])).map(dedupe).filter((c) => c.length >= 3)
  const advance = centred ? 0 : Math.round(b.xMax - b.xMin + 2 * SIDE)
  return { name, contours, advance, shift }
}

function dedupe(c) {
  const out = []
  for (const p of c) {
    const last = out[out.length - 1]
    if (last && last[0] === p[0] && last[1] === p[1]) continue
    out.push(p)
  }
  if (out.length > 1 && out[0][0] === out[out.length - 1][0] && out[0][1] === out[out.length - 1][1]) out.pop()
  return out
}

// ------------------------------------------------------------ glyph set ---

function buildGlyphs(weight) {
  const w = weight.stroke
  const glyphs = []
  const byChar = new Map()
  const byName = new Map()

  const add = (g, ch = null) => {
    g.index = glyphs.length
    glyphs.push(g)
    byName.set(g.name, g)
    if (ch !== null) {
      byChar.set(ch, g)
      g.codes = [...(g.codes ?? []), ch.codePointAt(0)]
    }
    return g
  }

  // .notdef: a hollow box, the glyph a missing character draws.
  const box = { name: '.notdef', advance: 560, shift: 0 }
  const outer = clockwise([[60, 0], [500, 0], [500, 700], [60, 700]])
  const inner = clockwise([[60 + w, w], [500 - w, w], [500 - w, 700 - w], [60 + w, 700 - w]]).reverse()
  box.contours = [outer, inner]
  add(box)

  const simple = (code, strokes, opts) => add(simpleGlyph('uni' + code.toString(16).toUpperCase().padStart(4, '0'), strokes, w, opts), String.fromCodePoint(code))

  for (const [code, def] of Object.entries(SYMBOLS).filter(([c]) => Number(c) < 0x41)) {
    simple(Number(code), def.strokes ?? def, def.width ? { width: def.width } : undefined)
  }
  for (const [d, strokes] of Object.entries(DIGITS)) simple(0x30 + Number(d), strokes)
  for (const [ch, strokes] of Object.entries(UPPER)) simple(ch.codePointAt(0), strokes)
  for (const [ch, strokes] of Object.entries(LOWER)) simple(ch.codePointAt(0), strokes)
  for (const [code, def] of Object.entries(SYMBOLS).filter(([c]) => Number(c) >= 0x41)) {
    simple(Number(code), def.strokes ?? def, def.width ? { width: def.width } : undefined)
  }

  // Unencoded helpers: the dotless j base and the diacritics.
  const dotlessJ = add(simpleGlyph('uni0237', DOTLESS_J, w))
  byChar.set('ȷ', dotlessJ)
  const marks = {}
  for (const [name, strokes] of Object.entries(MARKS)) marks[name] = add(simpleGlyph(name, strokes, w, { mapped: false, centred: true }))

  // Spacing marks: the mark on a glyph of its own width.
  for (const [code, mark] of Object.entries(SPACING_MARKS)) {
    const g = simpleGlyph('uni' + Number(code).toString(16).toUpperCase(), MARKS[mark], w, { mapped: false })
    add(g, String.fromCodePoint(Number(code)))
  }

  // Composites.
  for (const { ch, base, mark } of accentPlan()) {
    const b = byChar.get(base)
    if (!b) throw new Error(`no base glyph '${base}' for '${ch}'`)
    const upper = base !== base.toLowerCase()
    const m = marks[mark]
    let dx = Math.round(b.advance / 2)
    let dy = 0
    if (mark === 'ogonek') dx = b.advance - SIDE - Math.round(w * 0.6)
    if (mark === 'caronright') {
      dx = base === 'L' ? SIDE + Math.round(w * 1.4) : b.advance - SIDE + Math.round(w * 0.2)
      if (base === 't') dx = Math.round(120 + b.shift + w * 0.9)
      dy = base === 't' ? -40 : base === 'L' ? -40 : 0
    } else if (ASCENDER_TOP.has(ch)) {
      dx = Math.round(b.shift)
      dy = ASC_MARK_RISE
    } else if (!BELOW.has(mark)) {
      dy = upper ? CAP_MARK_RISE : 0
    }
    if (base === 'ȷ') dx = Math.round(100 + b.shift)
    add({ name: 'uni' + ch.codePointAt(0).toString(16).toUpperCase().padStart(4, '0'), components: [{ glyph: b, dx: 0, dy: 0 }, { glyph: m, dx, dy }], advance: b.advance }, ch)
  }

  // Every character the plan names must be drawn: Latin-1, Latin Extended-A and the extras.
  const required = []
  for (let c = 0x20; c <= 0x7e; c++) required.push(c)
  for (let c = 0xa0; c <= 0x17f; c++) required.push(c)
  required.push(...Object.keys(SYMBOLS).map(Number).filter((c) => c > 0x17f))
  const missing = required.filter((c) => !byChar.has(String.fromCodePoint(c)))
  if (missing.length) throw new Error('no glyph for ' + missing.map((c) => 'U+' + c.toString(16).toUpperCase().padStart(4, '0') + ' ' + String.fromCodePoint(c)).join(', '))

  return glyphs
}

// ---------------------------------------------------------------- binary ---

class Writer {
  constructor() {
    this.parts = []
    this.length = 0
  }
  u8(v) {
    this.push(Buffer.from([v & 0xff]))
  }
  u16(v) {
    const b = Buffer.alloc(2)
    b.writeUInt16BE(v & 0xffff)
    this.push(b)
  }
  i16(v) {
    const b = Buffer.alloc(2)
    b.writeInt16BE(v)
    this.push(b)
  }
  u32(v) {
    const b = Buffer.alloc(4)
    b.writeUInt32BE(v >>> 0)
    this.push(b)
  }
  i64(v) {
    const b = Buffer.alloc(8)
    b.writeBigInt64BE(BigInt(v))
    this.push(b)
  }
  tag(s) {
    this.push(Buffer.from(s, 'ascii'))
  }
  bytes(b) {
    this.push(Buffer.from(b))
  }
  push(b) {
    this.parts.push(b)
    this.length += b.length
  }
  pad4() {
    while (this.length % 4) this.u8(0)
  }
  buffer() {
    return Buffer.concat(this.parts)
  }
}

function glyphBounds(g) {
  if (g.components) {
    let b = { xMin: Infinity, yMin: Infinity, xMax: -Infinity, yMax: -Infinity }
    for (const c of g.components) {
      const cb = glyphBounds(c.glyph)
      if (!isFinite(cb.xMin)) continue
      b = {
        xMin: Math.min(b.xMin, cb.xMin + c.dx),
        yMin: Math.min(b.yMin, cb.yMin + c.dy),
        xMax: Math.max(b.xMax, cb.xMax + c.dx),
        yMax: Math.max(b.yMax, cb.yMax + c.dy),
      }
    }
    return b
  }
  return g.contours.length ? bounds(g.contours) : { xMin: 0, yMin: 0, xMax: 0, yMax: 0 }
}

function encodeSimple(g) {
  const w = new Writer()
  const b = glyphBounds(g)
  w.i16(g.contours.length)
  w.i16(b.xMin)
  w.i16(b.yMin)
  w.i16(b.xMax)
  w.i16(b.yMax)
  let end = -1
  for (const c of g.contours) {
    end += c.length
    w.u16(end)
  }
  w.u16(0) // no instructions
  const flags = []
  const xs = new Writer()
  const ys = new Writer()
  let px = 0
  let py = 0
  for (const c of g.contours) {
    for (const [x, y] of c) {
      let f = 0x01 // on curve
      const dx = x - px
      const dy = y - py
      if (dx === 0) f |= 0x10
      else if (Math.abs(dx) < 256) {
        f |= 0x02 | (dx > 0 ? 0x10 : 0)
        xs.u8(Math.abs(dx))
      } else xs.i16(dx)
      if (dy === 0) f |= 0x20
      else if (Math.abs(dy) < 256) {
        f |= 0x04 | (dy > 0 ? 0x20 : 0)
        ys.u8(Math.abs(dy))
      } else ys.i16(dy)
      flags.push(f)
      px = x
      py = y
    }
  }
  for (const f of flags) w.u8(f)
  w.bytes(xs.buffer())
  w.bytes(ys.buffer())
  w.pad4()
  return w.buffer()
}

function encodeComposite(g) {
  const w = new Writer()
  const b = glyphBounds(g)
  w.i16(-1)
  w.i16(b.xMin)
  w.i16(b.yMin)
  w.i16(b.xMax)
  w.i16(b.yMax)
  g.components.forEach((c, i) => {
    let flags = 0x0001 | 0x0002 | 0x0004 // word args, x/y offsets, rounded to the grid
    if (i < g.components.length - 1) flags |= 0x0020 // more components
    if (i === 0) flags |= 0x0200 // the base's metrics are the composite's
    w.u16(flags)
    w.u16(c.glyph.index)
    w.i16(c.dx)
    w.i16(c.dy)
  })
  w.pad4()
  return w.buffer()
}

function cmapTable(glyphs) {
  const map = []
  for (const g of glyphs) for (const code of g.codes ?? []) map.push([code, g.index])
  map.sort((a, b) => a[0] - b[0])

  // Runs of consecutive codes whose glyph ids step by one share a segment and an idDelta.
  const segs = []
  for (const [code, gid] of map) {
    const last = segs[segs.length - 1]
    if (last && code === last.end + 1 && gid - code === last.delta) last.end = code
    else segs.push({ start: code, end: code, delta: gid - code })
  }
  segs.push({ start: 0xffff, end: 0xffff, delta: 1 })

  const segX2 = segs.length * 2
  const searchRange = 2 * 2 ** Math.floor(Math.log2(segs.length))
  const sub = new Writer()
  sub.u16(4)
  sub.u16(16 + segs.length * 8)
  sub.u16(0)
  sub.u16(segX2)
  sub.u16(searchRange)
  sub.u16(Math.log2(searchRange / 2))
  sub.u16(segX2 - searchRange)
  for (const s of segs) sub.u16(s.end)
  sub.u16(0)
  for (const s of segs) sub.u16(s.start)
  for (const s of segs) sub.u16((s.delta + 0x10000) & 0xffff)
  for (let i = 0; i < segs.length; i++) sub.u16(0)

  const w = new Writer()
  w.u16(0)
  w.u16(2)
  w.u16(0) // Unicode
  w.u16(3) // BMP
  w.u32(20)
  w.u16(3) // Windows
  w.u16(1) // Unicode BMP
  w.u32(20)
  w.bytes(sub.buffer())
  return { buffer: w.buffer(), first: map[0][0], last: map[map.length - 1][0] }
}

function nameTable(weight) {
  const ps = FAMILY.replace(/\s+/g, '') + '-' + weight.style
  const records = [
    [0, 'Generated by tools/gen-font.mjs; the implementer\'s own drawing, no licence attached.'],
    [1, FAMILY],
    [2, weight.style],
    [3, `${ps};1.000`],
    [4, `${FAMILY} ${weight.style}`],
    [5, 'Version 1.000'],
    [6, ps],
  ]
  const strings = []
  const entries = []
  let offset = 0
  for (const [id, text] of records) {
    const mac = Buffer.from(text, 'latin1')
    const win = Buffer.alloc(text.length * 2)
    for (let i = 0; i < text.length; i++) win.writeUInt16BE(text.charCodeAt(i), i * 2)
    entries.push([1, 0, 0, id, mac.length, offset])
    strings.push(mac)
    offset += mac.length
    entries.push([3, 1, 0x409, id, win.length, offset])
    strings.push(win)
    offset += win.length
  }
  entries.sort((a, b) => a[0] - b[0] || a[1] - b[1] || a[2] - b[2] || a[3] - b[3])
  const w = new Writer()
  w.u16(0)
  w.u16(entries.length)
  w.u16(6 + entries.length * 12)
  for (const e of entries) e.forEach((v) => w.u16(v))
  w.bytes(Buffer.concat(strings))
  return w.buffer()
}

function checksum(buf) {
  const padded = Buffer.concat([buf, Buffer.alloc((4 - (buf.length % 4)) % 4)])
  let sum = 0
  for (let i = 0; i < padded.length; i += 4) sum = (sum + padded.readUInt32BE(i)) >>> 0
  return sum
}

// A fixed timestamp, so the same script writes the same bytes: 2026-09-25 in seconds since 1904.
const TIMESTAMP = 3841776000

function buildFont(weight) {
  const glyphs = buildGlyphs(weight)

  const glyf = new Writer()
  const loca = []
  for (const g of glyphs) {
    loca.push(glyf.length)
    if (g.components) glyf.bytes(encodeComposite(g))
    else if (g.contours.length) glyf.bytes(encodeSimple(g))
  }
  loca.push(glyf.length)

  let xMin = Infinity
  let yMin = Infinity
  let xMax = -Infinity
  let yMax = -Infinity
  let maxPoints = 0
  let maxContours = 0
  let maxCompositePoints = 0
  let maxCompositeContours = 0
  let minLsb = Infinity
  let minRsb = Infinity
  let maxExtent = -Infinity
  let advanceMax = 0
  let widthSum = 0
  let widthCount = 0
  for (const g of glyphs) {
    const b = glyphBounds(g)
    advanceMax = Math.max(advanceMax, g.advance)
    if (g.advance > 0) {
      widthSum += g.advance
      widthCount++
    }
    if (!isFinite(b.xMin) || (g.contours && g.contours.length === 0)) continue
    xMin = Math.min(xMin, b.xMin)
    yMin = Math.min(yMin, b.yMin)
    xMax = Math.max(xMax, b.xMax)
    yMax = Math.max(yMax, b.yMax)
    minLsb = Math.min(minLsb, b.xMin)
    minRsb = Math.min(minRsb, g.advance - b.xMax)
    maxExtent = Math.max(maxExtent, b.xMax)
    if (g.components) {
      let pts = 0
      let cts = 0
      for (const c of g.components) {
        pts += c.glyph.contours.reduce((s, k) => s + k.length, 0)
        cts += c.glyph.contours.length
      }
      maxCompositePoints = Math.max(maxCompositePoints, pts)
      maxCompositeContours = Math.max(maxCompositeContours, cts)
    } else {
      maxPoints = Math.max(maxPoints, g.contours.reduce((s, k) => s + k.length, 0))
      maxContours = Math.max(maxContours, g.contours.length)
    }
  }

  const head = new Writer()
  head.u32(0x00010000)
  head.u32(0x00010000)
  head.u32(0) // checkSumAdjustment, filled in last
  head.u32(0x5f0f3cf5)
  head.u16(0x000b)
  head.u16(UPM)
  head.i64(TIMESTAMP)
  head.i64(TIMESTAMP)
  head.i16(xMin)
  head.i16(yMin)
  head.i16(xMax)
  head.i16(yMax)
  head.u16(weight.bold ? 1 : 0)
  head.u16(8)
  head.i16(2)
  head.i16(1) // long loca offsets
  head.i16(0)

  const hhea = new Writer()
  hhea.u32(0x00010000)
  hhea.i16(ASCENT)
  hhea.i16(-DESCENT)
  hhea.i16(0)
  hhea.u16(advanceMax)
  hhea.i16(minLsb)
  hhea.i16(minRsb)
  hhea.i16(maxExtent)
  hhea.i16(1)
  hhea.i16(0)
  hhea.i16(0)
  for (let i = 0; i < 4; i++) hhea.i16(0)
  hhea.i16(0)
  hhea.u16(glyphs.length)

  const maxp = new Writer()
  maxp.u32(0x00010000)
  maxp.u16(glyphs.length)
  maxp.u16(maxPoints)
  maxp.u16(maxContours)
  maxp.u16(maxCompositePoints)
  maxp.u16(maxCompositeContours)
  maxp.u16(2) // zones
  maxp.u16(0)
  maxp.u16(0)
  maxp.u16(0)
  maxp.u16(0)
  maxp.u16(0)
  maxp.u16(0)
  maxp.u16(2) // component elements
  maxp.u16(1) // component depth

  const cmap = cmapTable(glyphs)

  const os2 = new Writer()
  os2.u16(4)
  os2.i16(Math.round(widthSum / widthCount))
  os2.u16(weight.weightClass)
  os2.u16(5)
  os2.u16(0) // installable embedding
  os2.i16(650)
  os2.i16(600)
  os2.i16(0)
  os2.i16(75)
  os2.i16(650)
  os2.i16(600)
  os2.i16(0)
  os2.i16(350)
  os2.i16(Math.round(weight.stroke * 0.8))
  os2.i16(280)
  os2.i16(0)
  os2.bytes([2, 11, weight.bold ? 8 : 5, 0, 0, 0, 0, 0, 0, 0]) // panose: Latin text, sans
  os2.u32((1 << 0) | (1 << 1) | (1 << 2) | (1 << 31)) // Basic Latin, Latin-1, Latin Extended-A, General Punctuation
  os2.u32((1 << 1) | (1 << 2)) // Currency Symbols, Letterlike Symbols
  os2.u32(0)
  os2.u32(0)
  os2.tag('NONE')
  os2.u16(weight.bold ? 0x0020 : 0x0040)
  os2.u16(Math.min(cmap.first, 0xffff))
  os2.u16(Math.min(cmap.last, 0xffff))
  os2.i16(800)
  os2.i16(-200)
  os2.i16(Math.max(0, ASCENT + DESCENT - UPM))
  os2.u16(ASCENT)
  os2.u16(DESCENT)
  os2.u32((1 << 0) | (1 << 1)) // Latin 1, Latin 2
  os2.u32(0)
  os2.i16(500)
  os2.i16(700)
  os2.u16(0)
  os2.u16(0x20)
  os2.u16(2)

  const hmtx = new Writer()
  for (const g of glyphs) {
    const b = glyphBounds(g)
    hmtx.u16(g.advance)
    hmtx.i16(isFinite(b.xMin) ? b.xMin : 0)
  }

  const locaW = new Writer()
  for (const o of loca) locaW.u32(o)

  const post = new Writer()
  post.u32(0x00030000)
  post.u32(0)
  post.i16(-110)
  post.i16(60)
  post.u32(0)
  post.u32(0)
  post.u32(0)
  post.u32(0)
  post.u32(0)

  const tables = {
    'OS/2': os2.buffer(),
    cmap: cmap.buffer,
    glyf: glyf.buffer(),
    head: head.buffer(),
    hhea: hhea.buffer(),
    hmtx: hmtx.buffer(),
    loca: locaW.buffer(),
    maxp: maxp.buffer(),
    name: nameTable(weight),
    post: post.buffer(),
  }

  const tags = Object.keys(tables).sort()
  const numTables = tags.length
  const entrySelector = Math.floor(Math.log2(numTables))
  const searchRange = 2 ** entrySelector * 16
  const out = new Writer()
  out.u32(0x00010000)
  out.u16(numTables)
  out.u16(searchRange)
  out.u16(entrySelector)
  out.u16(numTables * 16 - searchRange)
  let offset = 12 + numTables * 16
  const body = []
  for (const tag of tags) {
    const data = tables[tag]
    out.tag(tag.padEnd(4, ' '))
    out.u32(checksum(data))
    out.u32(offset)
    out.u32(data.length)
    const padded = Buffer.concat([data, Buffer.alloc((4 - (data.length % 4)) % 4)])
    body.push(padded)
    offset += padded.length
  }
  const font = Buffer.concat([out.buffer(), ...body])

  // head.checkSumAdjustment makes the whole file sum to the magic number.
  const headOffset = 12 + numTables * 16 + body.slice(0, tags.indexOf('head')).reduce((s, b) => s + b.length, 0)
  font.writeUInt32BE((0xb1b0afba - checksum(font)) >>> 0, headOffset + 8)
  return { font, glyphs }
}

for (const weight of WEIGHTS) {
  const { font, glyphs } = buildFont(weight)
  const path = join(FONTS, weight.file)
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, font)
  const encoded = glyphs.reduce((s, g) => s + (g.codes?.length ?? 0), 0)
  const composites = glyphs.filter((g) => g.components).length
  console.log(`${weight.file.padEnd(18)} ${String(font.length).padStart(7)} bytes  ${glyphs.length} glyphs, ${encoded} characters, ${composites} composites, stroke ${weight.stroke}`)
}
