// Draws the Phase 8 art of docs/plan.md: the five enemy portraits (P8.3), the New badge plate
// with its 9-slice sidecar (P8.2), and the four power icons and three role icons of the enemy
// card (P8.4). Thirteen sprites, each at the size its item states, as 8-bit RGBA PNG with
// straight alpha, named by the asset conventions.
//
// Run: node tools/gen-phase8-art.mjs
//
// A portrait is the fighter's own puppet (puppet-lib.mjs) at portrait scale: the same skeleton
// in a standing pose, the same proportions, palette and headpiece, framed on head and shoulders
// and drawn through a view that magnifies the puppet's space onto the 512 x 512 canvas, so every
// edge is resolved at the portrait's own resolution rather than enlarged from a fighter frame.
// Ren's numbers are Phase 5's. Kess, Vey, Orm and Malk have no fighter art yet (Phase 6); their
// proportions are seeded from the enemy id here, and Phase 6 is to draw them from the same specs.
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
import { INK, joints, mirror, pose, rad } from './puppet-lib.mjs'

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

// ------------------------------------------------------------- seeding ---

/** FNV-1a over the id, then a xorshift stream: the same id always draws the same character. */
function seeded(id) {
  let h = 0x811c9dc5
  for (const ch of id) {
    h ^= ch.charCodeAt(0)
    h = Math.imul(h, 0x01000193) >>> 0
  }

  let state = h || 1
  return (lo, hi) => {
    state ^= state << 13
    state >>>= 0
    state ^= state >>> 17
    state ^= state << 5
    state >>>= 0
    return lo + (state / 0xffffffff) * (hi - lo)
  }
}

// ---------------------------------------------------------------- view ---

/** The portrait's outline in canvas pixels: heavier than a fighter frame's, as a close-up's should be. */
const OUTLINE = 6

/**
 * A magnifying view of the puppet's space onto a canvas: a point (x, y) of the puppet lands on
 * ((x - ox) * k, (y - oy) * k). Distances are scaled with it, so a shape's antialiasing stays one
 * canvas pixel wide at any magnification.
 */
function view(c, k, ox, oy) {
  const toCanvas = (b) => [(b[0] - ox) * k, (b[1] - oy) * k, (b[2] - ox) * k, (b[3] - oy) * k]
  const draw = (sdf, opts = {}) =>
    paint(c, (x, y) => sdf(x / k + ox, y / k + oy) * k, { ...opts, box: opts.box ? toCanvas(opts.box) : null })

  /** A shape with the ink outline swelled under it, then the shape in its colour. */
  const inked = (sdf, fill, box, alpha = 1) => {
    draw(sdf, { fill: INK, grow: OUTLINE, alpha: 0.95 * alpha, box: box && pad(box, OUTLINE / k) })
    draw(sdf, { fill, alpha, box })
  }

  return {
    draw,
    inked,
    limb(a, b, r, fill) {
      inked(sdSegment(a.x, a.y, b.x, b.y, r), fill, span(a, b, r + 2))
    },
    blob(p, r, fill, alpha = 1) {
      inked(sdCircle(p.x, p.y, r), fill, around(p, r + 2), alpha)
    },
    slab(cx, cy, hx, hy, round, fill, angle = 0) {
      inked(rotated(sdRoundBox(0, 0, hx, hy, round), cx, cy, angle), fill, around({ x: cx, y: cy }, Math.hypot(hx, hy) + 2))
    },
  }
}

const around = (p, r) => [p.x - r, p.y - r, p.x + r, p.y + r]
const span = (a, b, r) => [Math.min(a.x, b.x) - r, Math.min(a.y, b.y) - r, Math.max(a.x, b.x) + r, Math.max(a.y, b.y) + r]
const pad = (b, r) => [b[0] - r, b[1] - r, b[2] + r, b[3] + r]

/** A field turned by an angle in degrees about a centre. */
function rotated(field, cx, cy, angle) {
  const s = Math.sin(-rad(angle))
  const t = Math.cos(-rad(angle))
  return (x, y) => {
    const dx = x - cx
    const dy = y - cy
    return field(dx * t - dy * s, dx * s + dy * t)
  }
}

// ------------------------------------------------------------ the cast ---

/**
 * Ren (P5.5): the numbers of tools/gen-phase5-art.mjs, so the portrait is the fighter. Helmet
 * over the skull with a brass brow and a visor slit, heavy pauldrons, a brass chest plate.
 */
const ren = {
  subject: 'ren',
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
  shoulders(v, j, s) {
    v.blob({ x: j.shoulder.x - s.shoulderOut, y: j.shoulder.y + 2 }, 28, lerp(s.hair, [0, 0, 0], 0.3))
    v.blob({ x: j.shoulder.x + s.shoulderOut, y: j.shoulder.y + 2 }, 30, s.hair)
  },
  chest(v, j, s, p) {
    v.slab((j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2 - 6, s.torsoW * 0.3, s.torsoH * 0.28, 10, s.accent, p.lean)
  },
  head(v, j, s, p) {
    v.inked(sdCircle(j.head.x, j.head.y - 6, s.headR * 1.04), s.hair, around(j.head, s.headR + 12))
    v.slab(j.head.x + 4, j.head.y + 6, s.headR * 0.96, 7, 5, s.accent, p.lean)
    v.draw(sdRoundBox(j.head.x + 12, j.head.y + 22, 18, 5, 4), { fill: INK, alpha: 0.75, box: around(j.head, s.headR) })
  },
}

/**
 * A cast member without fighter art yet (Phase 6): the Phase 5 body plan with proportions seeded
 * from the enemy id inside the bands the fighters share (360 to 440 px tall on a 512 canvas), and
 * the palette and headpiece given.
 */
function castMember(id, look) {
  const r = seeded(id)
  const headR = Math.round(r(40, 54))
  return {
    subject: id.replace(/^enemy-/, ''),
    headR,
    neck: Math.round(r(5, 10)),
    torsoH: Math.round(r(98, 118)),
    torsoW: Math.round(r(72, 112)),
    torsoRound: Math.round(r(16, 28)),
    shoulderOut: Math.round(r(22, 38)),
    shoulderDrop: Math.round(r(6, 11)),
    hipOut: Math.round(r(11, 17)),
    upperArm: Math.round(r(52, 62)),
    foreArm: Math.round(r(48, 58)),
    armR: Math.round(r(11, 17)),
    thigh: Math.round(r(70, 80)),
    shin: Math.round(r(66, 76)),
    legR: Math.round(r(14, 20)),
    footLength: Math.round(r(24, 32)),
    ...look,
  }
}

/** Kess (Aggressor, fast, Rising Tempo): lean, ember-red, a crest of three swept blades and a scarf. */
const kess = castMember('enemy-kess', {
  skin: [226, 184, 150],
  sleeve: [150, 52, 46],
  coat: [124, 38, 40],
  hair: [40, 30, 36],
  accent: [244, 150, 60],
  shoulders(v, j, s) {
    // A scarf wound at the throat, knotted on the near side.
    v.limb({ x: j.shoulder.x - s.shoulderOut * 0.7, y: j.shoulder.y + 4 }, { x: j.shoulder.x + s.shoulderOut * 0.7, y: j.shoulder.y + 2 }, 11, s.accent)
    v.blob({ x: j.shoulder.x + s.shoulderOut * 0.5, y: j.shoulder.y + 12 }, 8, lerp(s.accent, [0, 0, 0], 0.2))
  },
  chest(v, j, s, p) {
    v.slab((j.hip.x + j.shoulder.x) / 2 + 4, (j.hip.y + j.shoulder.y) / 2 - 10, 6, s.torsoH * 0.34, 4, lerp(s.coat, [0, 0, 0], 0.35), p.lean + 16)
  },
  head(v, j, s) {
    // Three swept blades rising off the back of the skull, longest in the middle.
    const base = { x: j.head.x - s.headR * 0.2, y: j.head.y - s.headR * 0.55 }
    for (const [len, angle, w] of [[1.25, -150, 11], [1.55, -128, 13], [1.2, -104, 11]]) {
      const tip = { x: base.x + Math.sin(rad(angle)) * s.headR * len, y: base.y + Math.cos(rad(angle)) * s.headR * len }
      v.inked(sdPolygon([[base.x - w, base.y + 4], [base.x + w, base.y], [tip.x, tip.y]]), s.hair, span(base, tip, w + 4))
    }

    v.blob(j.head, s.headR, s.skin)
    v.inked(sdCircle(j.head.x - 6, j.head.y - 14, s.headR * 0.86), s.hair, around(j.head, s.headR + 8))
    v.draw(sdCircle(j.head.x + 4, j.head.y + 10, s.headR * 0.62), { fill: s.skin, box: around(j.head, s.headR) })
    // A narrowed eye and a war stripe.
    v.draw(sdRoundBox(j.head.x + 22, j.head.y + 6, 9, 3.2, 3), { fill: INK, alpha: 0.9, box: around(j.head, s.headR) })
    v.draw(sdRoundBox(j.head.x + 12, j.head.y + 22, 16, 2.6, 2.6), { fill: s.accent, box: around(j.head, s.headR) })
  },
})

/** Vey (Mentalist, fast, Bleed): slight, a violet hood drawn to a point, a pale mask with one slit. */
const vey = castMember('enemy-vey', {
  skin: [206, 196, 214],
  sleeve: [92, 62, 126],
  coat: [70, 46, 102],
  hair: [118, 82, 160],
  accent: [206, 58, 82],
  shoulders(v, j, s) {
    // The hood's mantle, one wide drape over both shoulders.
    v.inked(
      sdRoundPolygon([
        [j.shoulder.x - s.shoulderOut * 1.7, j.shoulder.y + 34],
        [j.shoulder.x - s.headR * 0.8, j.shoulder.y - 12],
        [j.shoulder.x + s.headR * 0.8, j.shoulder.y - 12],
        [j.shoulder.x + s.shoulderOut * 1.7, j.shoulder.y + 34],
        [j.shoulder.x, j.shoulder.y + 58],
      ], 6),
      s.hair,
      around(j.shoulder, s.shoulderOut * 2 + 60),
    )
  },
  chest(v, j, s) {
    // A red clasp at the mantle's point: the blood this one draws.
    v.inked(sdPolygon([[j.shoulder.x, j.shoulder.y + 28], [j.shoulder.x + 10, j.shoulder.y + 42], [j.shoulder.x, j.shoulder.y + 56], [j.shoulder.x - 10, j.shoulder.y + 42]]), s.accent, around(j.shoulder, 70))
  },
  head(v, j, s) {
    // The hood: a pointed cowl rising behind and above the head, the mask inside it.
    const tip = { x: j.head.x - s.headR * 0.95, y: j.head.y - s.headR * 1.7 }
    v.inked(
      union(
        sdCircle(j.head.x, j.head.y, s.headR * 1.18),
        sdPolygon([[j.head.x - s.headR * 1.05, j.head.y - s.headR * 0.2], [tip.x, tip.y], [j.head.x + s.headR * 0.5, j.head.y - s.headR * 0.9]]),
      ),
      s.hair,
      pad(span(tip, { x: j.head.x + s.headR * 1.2, y: j.head.y + s.headR * 1.2 }, 0), 12),
    )
    v.inked(sdCircle(j.head.x + s.headR * 0.18, j.head.y + s.headR * 0.12, s.headR * 0.78), lerp(s.coat, [0, 0, 0], 0.55), around(j.head, s.headR + 8))
    v.inked(sdRoundBox(j.head.x + s.headR * 0.28, j.head.y + s.headR * 0.14, s.headR * 0.52, s.headR * 0.6, s.headR * 0.4), s.skin, around(j.head, s.headR + 8))
    // One slit and a single red mark under it.
    v.draw(sdRoundBox(j.head.x + s.headR * 0.4, j.head.y + s.headR * 0.02, s.headR * 0.28, 3, 3), { fill: INK, alpha: 0.9, box: around(j.head, s.headR) })
    v.draw(sdCircle(j.head.x + s.headR * 0.4, j.head.y + s.headR * 0.4, 3.5), { fill: s.accent, box: around(j.head, s.headR) })
  },
})

/** Orm (Elite Tank, slow, Iron Veil, Stoneform, Guard): a stone head crowned with a jagged ridge, boulder shoulders. */
const orm = castMember('enemy-orm', {
  skin: [150, 158, 140],
  sleeve: [96, 104, 92],
  coat: [78, 86, 76],
  hair: [118, 126, 110],
  accent: [132, 196, 170],
  squareHead: true,
  shoulders(v, j, s) {
    // Two boulders, cut with flat faces rather than rounded like Ren's pauldrons.
    for (const side of [-1, 1]) {
      const c = { x: j.shoulder.x + side * s.shoulderOut * 1.1, y: j.shoulder.y + 4 }
      const r = side < 0 ? 30 : 34
      const verts = Array.from({ length: 7 }, (_, i) => {
        const a = (i / 7) * Math.PI * 2 + side * 0.3
        const wobble = 0.82 + 0.18 * Math.abs(Math.sin(i * 2.3))
        return [c.x + Math.cos(a) * r * wobble, c.y + Math.sin(a) * r * wobble]
      })
      v.inked(sdPolygon(verts), side < 0 ? lerp(s.hair, [0, 0, 0], 0.3) : s.hair, around(c, r + 4))
    }
  },
  chest(v, j, s, p) {
    // A glowing seam down the chest: the stone that sets when it is left alone.
    v.slab((j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2 - 8, 5, s.torsoH * 0.3, 4, s.accent, p.lean)
  },
  head(v, j, s) {
    // A squared stone head with a ridge of three uneven peaks along the top.
    const h = s.headR
    v.inked(sdRoundBox(j.head.x, j.head.y, h * 0.98, h * 0.94, h * 0.3), s.skin, around(j.head, h + 8))
    v.inked(
      sdPolygon([
        [j.head.x - h * 0.98, j.head.y - h * 0.5],
        [j.head.x - h * 0.7, j.head.y - h * 1.35],
        [j.head.x - h * 0.3, j.head.y - h * 0.86],
        [j.head.x + h * 0.05, j.head.y - h * 1.55],
        [j.head.x + h * 0.42, j.head.y - h * 0.9],
        [j.head.x + h * 0.75, j.head.y - h * 1.2],
        [j.head.x + h * 0.98, j.head.y - h * 0.5],
      ]),
      s.hair,
      pad(around(j.head, h * 1.6), 4),
    )
    // Deep-set glowing eyes and a crack.
    v.draw(sdRoundBox(j.head.x + h * 0.18, j.head.y + h * 0.05, h * 0.2, h * 0.09, 3), { fill: s.accent, box: around(j.head, h) })
    v.draw(sdRoundBox(j.head.x + h * 0.66, j.head.y + h * 0.05, h * 0.14, h * 0.09, 3), { fill: s.accent, box: around(j.head, h) })
    v.draw(sdSegment(j.head.x - h * 0.5, j.head.y - h * 0.4, j.head.x - h * 0.2, j.head.y + h * 0.3, 1.6), { fill: INK, alpha: 0.7, box: around(j.head, h) })
  },
})

/** Malk (Boss Aggressor, fast): broad, crimson and gold, a horned crown and a high collar. */
const malk = castMember('enemy-malk', {
  skin: [196, 150, 138],
  sleeve: [112, 26, 40],
  coat: [84, 18, 34],
  hair: [30, 22, 28],
  accent: [226, 182, 82],
  shoulders(v, j, s) {
    // A high collar standing up behind the neck, gold-edged.
    const c = { x: j.shoulder.x - 6, y: j.shoulder.y - 8 }
    v.inked(sdRoundPolygon([[c.x - s.shoulderOut * 1.5, c.y + 20], [c.x - s.shoulderOut * 1.1, c.y - 40], [c.x + s.shoulderOut * 0.4, c.y - 22], [c.x + s.shoulderOut * 0.9, c.y + 20]], 5), s.accent, around(c, s.shoulderOut * 2 + 30))
    v.inked(sdRoundPolygon([[c.x - s.shoulderOut * 1.3, c.y + 20], [c.x - s.shoulderOut * 0.95, c.y - 28], [c.x + s.shoulderOut * 0.3, c.y - 14], [c.x + s.shoulderOut * 0.75, c.y + 20]], 5), s.coat, around(c, s.shoulderOut * 2 + 30))
    v.blob({ x: j.shoulder.x - s.shoulderOut, y: j.shoulder.y + 6 }, 24, lerp(s.sleeve, [0, 0, 0], 0.3))
    v.blob({ x: j.shoulder.x + s.shoulderOut, y: j.shoulder.y + 6 }, 26, s.sleeve)
  },
  chest(v, j, s, p) {
    v.slab((j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2 - 4, s.torsoW * 0.22, s.torsoH * 0.3, 8, s.accent, p.lean)
    v.blob({ x: (j.hip.x + j.shoulder.x) / 2, y: (j.hip.y + j.shoulder.y) / 2 - 10 }, 9, s.sleeve)
  },
  head(v, j, s) {
    const h = s.headR
    // Two horns sweeping up and back, then the crown band, then the face under it.
    for (const side of [-1, 1]) {
      const root = { x: j.head.x + side * h * 0.55, y: j.head.y - h * 0.55 }
      const tip = { x: root.x + side * h * 0.35 - h * 0.35, y: root.y - h * 1.15 }
      v.inked(sdPolygon([[root.x - 12, root.y + 6], [root.x + 12, root.y + 2], [tip.x, tip.y]]), side < 0 ? lerp(s.hair, [255, 255, 255], 0.1) : s.hair, span(root, tip, 18))
    }

    v.blob(j.head, h, s.skin)
    v.inked(sdCircle(j.head.x - 4, j.head.y - h * 0.3, h * 0.86), s.hair, around(j.head, h + 8))
    v.draw(sdCircle(j.head.x + 6, j.head.y + h * 0.22, h * 0.62), { fill: s.skin, box: around(j.head, h) })
    v.inked(sdRoundBox(j.head.x, j.head.y - h * 0.42, h * 0.92, h * 0.15, 4), s.accent, around(j.head, h + 8))
    for (const x of [-0.5, 0, 0.5]) {
      v.inked(sdPolygon([[j.head.x + h * (x - 0.13), j.head.y - h * 0.52], [j.head.x + h * x, j.head.y - h * 0.86], [j.head.x + h * (x + 0.13), j.head.y - h * 0.52]]), s.accent, around(j.head, h + 12))
    }

    // Two hard eyes under a heavy brow.
    v.draw(sdRoundBox(j.head.x + h * 0.08, j.head.y + h * 0.14, h * 0.14, h * 0.07, 2.5), { fill: INK, alpha: 0.95, box: around(j.head, h) })
    v.draw(sdRoundBox(j.head.x + h * 0.6, j.head.y + h * 0.14, h * 0.12, h * 0.07, 2.5), { fill: INK, alpha: 0.95, box: around(j.head, h) })
    v.draw(sdRoundBox(j.head.x + h * 0.34, j.head.y + h * 0.02, h * 0.5, 3, 3), { fill: INK, alpha: 0.7, box: around(j.head, h) })
  },
})

// ---------------------------------------------------------- P8.3 portraits ---

const PORTRAIT = 512

/**
 * Head and shoulders of one cast member at portrait scale, facing left as the fighter does: the
 * puppet's standing pose, framed from above the headpiece to below the shoulders, magnified so
 * that span fills the canvas, then mirrored.
 */
function portrait(s) {
  const p = pose({ crouch: 5, lean: 2, headTilt: -2 })
  const j = joints(s, p)
  const top = j.head.y - s.headR * 1.95
  const bottom = j.shoulder.y + s.torsoH * 0.62
  const k = PORTRAIT / (bottom - top)
  const ox = j.head.x - PORTRAIT / k / 2 + s.headR * 0.1
  const c = canvas(PORTRAIT, PORTRAIT)
  const v = view(c, k, ox, top)
  const back = (colour) => lerp(colour, [0, 0, 0], 0.34)

  // Back to front, as the fighter is drawn: far arm, torso, near arm, shoulders, head. The upper
  // arms hang from the shoulder joints, where a fighter frame is too small to show they start.
  const armRoot = (side) => ({ x: j.shoulder.x + side * s.shoulderOut, y: j.shoulder.y + s.shoulderDrop })
  v.limb(armRoot(-1), j.far.mid, s.armR, back(s.sleeve))
  v.slab((j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2, s.torsoW / 2, s.torsoH / 2 + s.armR, s.torsoRound, s.coat, p.lean)
  s.chest(v, j, s, p)
  v.limb(armRoot(1), j.near.mid, s.armR, s.sleeve)
  s.shoulders(v, j, s, p)
  if (!s.squareHead) {
    v.blob(j.head, s.headR, s.skin)
  }

  s.head(v, j, s, p)

  return mirror(c)
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
