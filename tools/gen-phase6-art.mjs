// Draws the Phase 6 cast of docs/plan.md: Kess (P6.1), Vey (P6.2), Orm (P6.3) and Malk (P6.4),
// seven clips each with the sidecar that gives every clip its beats, loop flag and strike frame.
// The clips are the ones Phase 5 authored, in cast-lib.mjs, and the puppet is the one Ren is
// drawn from, so every new enemy lands on the same 512 x 512 canvas with its soles on the same
// baseline and its strike frames in the same places.
//
// Run: node tools/gen-phase6-art.mjs
//
// What separates the four is proportion, palette and headpiece, and all three are seeded from the
// enemy id: `seeded('kess')` is a deterministic stream that jitters the archetype's limb lengths
// and turns its palette, so renaming an enemy redraws it and no hand-tuned number is ever the
// reason two of them look alike. The archetype itself comes from the plan's own description of
// each enemy — Kess the fast Aggressor is the long, light one; Vey the Mentalist is robed and
// hooded; Orm the Elite Tank is the squat block; Malk the Boss is the tall crowned one — and the
// run ends by checking that all five silhouettes on the stage, Lulu and Ren included, differ.

import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { lerp, paint, sdCircle, sdPolygon, sdRoundBox, sdSegment } from './art-lib.mjs'
import { box, drawCharacter, span } from './cast-lib.mjs'
import { FRAME, SOLE, grip } from './puppet-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ART = join(ROOT, 'client', 'Assets', '_Project', 'Art')
const WORLD1 = join(ART, 'World1')

const INK = [14, 12, 20]
const written = []

// ------------------------------------------------------------------ seed ---

/**
 * A deterministic stream of numbers for one enemy id: an FNV-1a hash of the id driving a
 * 32-bit xorshift. Nothing here reaches for `Math.random`, so the whole cast is the same on
 * every machine and rerunning the generator rewrites the same bytes.
 */
function seeded(id) {
  let state = 2166136261
  for (const ch of id) {
    state = Math.imul(state ^ ch.charCodeAt(0), 16777619) >>> 0
  }

  const next = () => {
    state ^= state << 13
    state >>>= 0
    state ^= state >>> 17
    state ^= state << 5
    state >>>= 0
    return state / 4294967296
  }

  next()
  return { between: (low, high) => low + next() * (high - low) }
}

/**
 * The archetype with its lengths jittered by the id's own stream. The jitter is small — a
 * twentieth either way — so an archetype stays itself while no two ids share a measurement.
 */
function proportioned(id, archetype) {
  const rng = seeded(id)
  const jittered = { ...archetype }
  for (const key of ['headR', 'torsoH', 'torsoW', 'upperArm', 'foreArm', 'thigh', 'shin']) {
    jittered[key] = Math.round(archetype[key] * rng.between(0.95, 1.05))
  }

  // The palette turns around the archetype's own hue, so an Aggressor never cools into a Tank.
  const warm = rng.between(0.06, 0.2)
  jittered.hair = lerp(archetype.hair, archetype.accent, warm)
  return jittered
}

// ------------------------------------------------------------- characters ---

/**
 * Kess (plan P6.1): Normal, Aggressor, fast rhythm, Rising Tempo. Long-limbed and narrow, a
 * swept crest over a slit mask and a knife along the near forearm — the only silhouette on the
 * stage that is taller than Lulu and thinner than her.
 */
export const kess = proportioned('kess', {
  kind: 'enemy',
  subject: 'kess',
  folder: join(WORLD1, 'kess'),
  facesLeft: true,
  headR: 40,
  neck: 9,
  torsoH: 104,
  torsoW: 62,
  torsoRound: 18,
  shoulderOut: 20,
  shoulderDrop: 4,
  hipOut: 10,
  upperArm: 62,
  foreArm: 60,
  armR: 11,
  thigh: 74,
  shin: 70,
  legR: 14,
  footLength: 28,
  skin: [232, 186, 168],
  sleeve: [186, 74, 62],
  coat: [158, 52, 46],
  trouser: [78, 34, 38],
  boot: [52, 24, 28],
  hair: [226, 106, 58],
  accent: [248, 196, 96],
  draw(c, j, s, p, kit) {
    // The crest: three spikes swept back off the crown, the tallest in the middle.
    const spikes = [
      [0.1, 1.5, 12],
      [-0.5, 1.9, 14],
      [-1.05, 1.25, 11],
    ]
    for (const [at, reach, width] of spikes) {
      const root = { x: j.head.x + at * s.headR, y: j.head.y - s.headR * 0.5 }
      const tip = { x: root.x - s.headR * 0.62, y: root.y - s.headR * reach }
      paint(c, sdPolygon([
        [root.x - width, root.y + 6],
        [root.x + width, root.y + 6],
        [tip.x, tip.y],
      ]), { fill: INK, grow: 3.2, alpha: 0.95, box: span(root, tip, 24) })
      paint(c, sdPolygon([
        [root.x - width, root.y + 6],
        [root.x + width, root.y + 6],
        [tip.x, tip.y],
      ]), { fill: s.hair, box: span(root, tip, 24) })
    }

    // The mask: a band across the eyes with one lit slit, so the face reads at a glance.
    kit.slab(c, j.head.x + 4, j.head.y + 2, s.headR * 0.92, 11, 5, lerp(s.trouser, [0, 0, 0], 0.2), p.lean)
    paint(c, sdRoundBox(j.head.x + 16, j.head.y + 2, 13, 3.5, 3), { fill: s.accent, box: box(j.head, s.headR + 24) })
  },
  implement(c, j, s) {
    // The knife: short, wide at the guard, running along the forearm and past the hand.
    const g = grip(j)
    const butt = { x: g.x - g.ux * 18, y: g.y - g.uy * 18 }
    const tip = { x: g.x + g.ux * 54, y: g.y + g.uy * 54 }
    const across = { x: -g.uy, y: g.ux }
    const blade = [
      [butt.x + across.x * 7, butt.y + across.y * 7],
      [butt.x - across.x * 7, butt.y - across.y * 7],
      [g.x + g.ux * 20 - across.x * 10, g.y + g.uy * 20 - across.y * 10],
      [tip.x, tip.y],
      [g.x + g.ux * 20 + across.x * 10, g.y + g.uy * 20 + across.y * 10],
    ]
    paint(c, sdPolygon(blade), { fill: INK, grow: 3.2, alpha: 0.95, box: span(butt, tip, 22) })
    paint(c, sdPolygon(blade), { fill: lerp(s.accent, [255, 255, 255], 0.35), box: span(butt, tip, 22) })
  },
})

/**
 * Vey (plan P6.2): Normal, Mentalist, fast rhythm, Charge / Buff, applies Bleed. A hood with no
 * face under it over a robe that widens to the floor, and a sickle — short legs and a bell of a
 * body, read from across the stage as the one that never shows its head.
 */
export const vey = proportioned('vey', {
  kind: 'enemy',
  subject: 'vey',
  folder: join(WORLD1, 'vey'),
  facesLeft: true,
  headR: 42,
  neck: 8,
  torsoH: 104,
  torsoW: 94,
  torsoRound: 42,
  shoulderOut: 24,
  shoulderDrop: 8,
  hipOut: 20,
  upperArm: 56,
  foreArm: 54,
  armR: 12,
  thigh: 74,
  shin: 70,
  legR: 17,
  footLength: 26,
  skin: [206, 196, 226],
  sleeve: [108, 78, 156],
  coat: [84, 58, 130],
  trouser: [56, 40, 90],
  boot: [40, 28, 64],
  hair: [150, 104, 196],
  accent: [126, 232, 214],
  draw(c, j, s, p, kit) {
    // The hood: a cowl over the whole skull drawn out to a peak and down onto both shoulders, so
    // no face shows anywhere in the silhouette — only the dark opening and the one lit point.
    const peak = { x: j.head.x - s.headR * 0.5, y: j.head.y - s.headR * 1.55 }
    const hood = [
      [j.shoulder.x - s.shoulderOut - 14, j.shoulder.y + 10],
      [j.head.x - s.headR * 1.1, j.head.y],
      [peak.x, peak.y],
      [j.head.x + s.headR * 1.1, j.head.y - s.headR * 0.2],
      [j.shoulder.x + s.shoulderOut + 14, j.shoulder.y + 10],
    ]
    const hoodBox = span(peak, j.shoulder, s.shoulderOut + 32)
    paint(c, sdPolygon(hood), { fill: INK, grow: 3.2, alpha: 0.95, box: hoodBox })
    kit.blob(c, { x: j.head.x, y: j.head.y - 4 }, s.headR * 1.08, s.hair)
    paint(c, sdPolygon(hood), { fill: s.hair, box: hoodBox })
    kit.blob(c, { x: j.head.x + 10, y: j.head.y + 10 }, s.headR * 0.66, lerp(s.trouser, [0, 0, 0], 0.55))
    paint(c, sdCircle(j.head.x + 18, j.head.y + 8, 7), { fill: s.accent, box: box(j.head, s.headR + 20) })

    // The robe: a hem that flares past the hips, which is why Vey's legs barely show.
    kit.slab(c, j.hip.x - 2, j.hip.y + 14, s.torsoW * 0.62, 34, 24, s.coat, p.lean * 0.4)
    kit.slab(c, (j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2, 9, s.torsoH * 0.34, 8, s.accent, p.lean)
  },
  implement(c, j, s) {
    // The sickle: a straight haft with a hooked blade, drawn as two arcs off the haft's end.
    const g = grip(j)
    const tip = { x: g.x + g.ux * 44, y: g.y + g.uy * 44 }
    paint(c, sdSegment(g.x, g.y, tip.x, tip.y, 6), { fill: INK, grow: 3.2, alpha: 0.95, box: span(g, tip, 16) })
    paint(c, sdSegment(g.x, g.y, tip.x, tip.y, 6), { fill: lerp(s.trouser, [255, 255, 255], 0.2), box: span(g, tip, 16) })

    const across = { x: -g.uy, y: g.ux }
    for (let i = 0; i < 5; i++) {
      const t = i / 4
      const at = {
        x: tip.x + g.ux * Math.sin(t * 1.5) * 20 + across.x * (1 - Math.cos(t * 1.5)) * 26,
        y: tip.y + g.uy * Math.sin(t * 1.5) * 20 + across.y * (1 - Math.cos(t * 1.5)) * 26,
      }
      const r = 9 - i * 1.1
      paint(c, sdCircle(at.x, at.y, r), { fill: INK, grow: 3, alpha: 0.95, box: box(at, r + 8) })
      paint(c, sdCircle(at.x, at.y, r), { fill: s.accent, box: box(at, r + 8) })
    }
  },
})

/**
 * Orm (plan P6.3): Elite, Tank, slow rhythm, Iron Veil, Stoneform and Guard, applies Weak. Wider
 * than it is graceful — short thick legs, a barrel of a torso, a horned head sunk in with no
 * neck at all, and a slab shield instead of a weapon. The widest thing on the stage.
 */
export const orm = proportioned('orm', {
  kind: 'enemy',
  subject: 'orm',
  folder: join(WORLD1, 'orm'),
  facesLeft: true,
  headR: 39,
  neck: 1,
  torsoH: 106,
  torsoW: 130,
  torsoRound: 30,
  shoulderOut: 46,
  shoulderDrop: 14,
  hipOut: 24,
  upperArm: 58,
  foreArm: 54,
  armR: 21,
  thigh: 74,
  shin: 70,
  legR: 25,
  footLength: 38,
  skin: [166, 172, 158],
  sleeve: [104, 112, 98],
  coat: [82, 92, 78],
  trouser: [62, 70, 60],
  boot: [44, 50, 44],
  hair: [124, 132, 116],
  accent: [206, 176, 118],
  draw(c, j, s, p, kit) {
    // Two horns off the temples, swept forward, and the brow ridge between them.
    for (const side of [-1, 1]) {
      const root = { x: j.head.x + side * s.headR * 0.82, y: j.head.y - s.headR * 0.32 }
      const tip = { x: root.x + side * 30, y: root.y - 42 }
      const horn = [
        [root.x - side * 8, root.y + 17],
        [root.x + side * 22, root.y + 7],
        [tip.x + side * 4, tip.y + 4],
        [tip.x - side * 4, tip.y - 2],
      ]
      paint(c, sdPolygon(horn), { fill: INK, grow: 3.2, alpha: 0.95, box: span(root, tip, 28) })
      paint(c, sdPolygon(horn), { fill: s.accent, box: span(root, tip, 28) })
    }

    kit.slab(c, j.head.x + 3, j.head.y - 6, s.headR * 0.94, 9, 6, lerp(s.hair, [0, 0, 0], 0.3), p.lean)
    paint(c, sdCircle(j.head.x + 15, j.head.y + 13, 6), { fill: INK, alpha: 0.85, box: box(j.head, s.headR + 20) })

    // Boulder shoulders and three belly bands, so the block reads as stone rather than armour.
    kit.blob(c, { x: j.shoulder.x - s.shoulderOut, y: j.shoulder.y + 4 }, 34, lerp(s.hair, [0, 0, 0], 0.34))
    kit.blob(c, { x: j.shoulder.x + s.shoulderOut, y: j.shoulder.y + 4 }, 37, s.hair)
    for (let i = 0; i < 3; i++) {
      const t = 0.3 + i * 0.22
      kit.slab(
        c,
        j.shoulder.x + (j.hip.x - j.shoulder.x) * t,
        j.shoulder.y + (j.hip.y - j.shoulder.y) * t,
        s.torsoW * 0.3,
        6,
        5,
        lerp(s.coat, [0, 0, 0], 0.3),
        p.lean,
      )
    }
  },
  implement(c, j, s) {
    // The shield: a slab held flat against the near forearm, the only round-cornered plate here.
    const g = grip(j)
    const centre = { x: g.x + g.ux * 6, y: g.y + g.uy * 6 }
    const angle = (Math.atan2(g.ux, g.uy) * 180) / Math.PI
    const draw = (cx, cy, hx, hy, round, colour, grow) => {
      const s0 = Math.sin(((angle) * Math.PI) / 180)
      const c0 = Math.cos(((angle) * Math.PI) / 180)
      const base = sdRoundBox(0, 0, hx, hy, round)
      const sdf = (x, y) => {
        const dx = x - cx
        const dy = y - cy
        return base(dx * c0 - dy * s0, dx * s0 + dy * c0)
      }

      const reach = Math.hypot(hx, hy) + 8
      paint(c, sdf, { fill: colour, grow, alpha: grow ? 0.95 : 1, box: [cx - reach, cy - reach, cx + reach, cy + reach] })
    }

    draw(centre.x, centre.y, 24, 42, 14, INK, 3.2)
    draw(centre.x, centre.y, 24, 42, 14, lerp(s.hair, [255, 255, 255], 0.1), 0)
    draw(centre.x, centre.y, 11, 24, 8, s.accent, 0)
  },
})

/**
 * Malk (plan P6.4): Boss, Aggressor, fast rhythm, Rising Tempo and Charge / Buff, Stoneform and
 * Guard, applies Bleed and Weak. The tallest thing on the stage: a crowned head over a mantle,
 * long legs and a scythe half again as long as Ren's bar. Boss phase clips wait for multi-phase
 * bosses, which are not built.
 */
export const malk = proportioned('malk', {
  kind: 'enemy',
  subject: 'malk',
  folder: join(WORLD1, 'malk'),
  facesLeft: true,
  headR: 43,
  neck: 12,
  torsoH: 118,
  torsoW: 90,
  torsoRound: 16,
  shoulderOut: 32,
  shoulderDrop: 8,
  hipOut: 16,
  upperArm: 64,
  foreArm: 60,
  armR: 15,
  thigh: 72,
  shin: 70,
  legR: 18,
  footLength: 32,
  skin: [198, 188, 206],
  sleeve: [56, 46, 74],
  coat: [38, 30, 54],
  trouser: [30, 24, 44],
  boot: [22, 18, 32],
  hair: [86, 62, 122],
  accent: [232, 188, 92],
  draw(c, j, s, p, kit) {
    // The crown: five spikes across the brow, the middle one the tallest, all of them gold.
    for (let i = 0; i < 5; i++) {
      const at = (i - 2) / 2
      const root = { x: j.head.x + at * s.headR * 0.86, y: j.head.y - s.headR * 0.72 }
      const height = 52 - Math.abs(i - 2) * 11
      const tip = { x: root.x + at * 7, y: root.y - height }
      paint(c, sdPolygon([
        [root.x - 11, root.y + 10],
        [root.x + 11, root.y + 10],
        [tip.x, tip.y],
      ]), { fill: INK, grow: 3.2, alpha: 0.95, box: span(root, tip, 22) })
      paint(c, sdPolygon([
        [root.x - 11, root.y + 10],
        [root.x + 11, root.y + 10],
        [tip.x, tip.y],
      ]), { fill: s.accent, box: span(root, tip, 22) })
    }

    // The face: dark under the crown with two lit eyes, the only warm marks on a cold character.
    kit.blob(c, j.head, s.headR * 0.9, lerp(s.hair, [0, 0, 0], 0.5))
    paint(c, sdCircle(j.head.x + 8, j.head.y + 2, 6), { fill: s.accent, box: box(j.head, s.headR + 20) })
    paint(c, sdCircle(j.head.x + 28, j.head.y, 6), { fill: s.accent, box: box(j.head, s.headR + 20) })

    // The mantle: a collar that spreads past both shoulders, so the boss reads wide up top.
    kit.slab(c, j.shoulder.x, j.shoulder.y + 10, s.shoulderOut + 24, 17, 12, s.hair, p.lean)
    kit.slab(c, (j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2, s.torsoW * 0.24, s.torsoH * 0.3, 8, s.accent, p.lean)
  },
  implement(c, j, s) {
    // The scythe: a long haft past both ends of the hand and a blade hooking off its tip.
    const g = grip(j)
    const butt = { x: g.x - g.ux * 32, y: g.y - g.uy * 32 }
    const tip = { x: g.x + g.ux * 92, y: g.y + g.uy * 92 }
    paint(c, sdSegment(butt.x, butt.y, tip.x, tip.y, 8), { fill: INK, grow: 3.2, alpha: 0.95, box: span(butt, tip, 18) })
    paint(c, sdSegment(butt.x, butt.y, tip.x, tip.y, 8), { fill: lerp(s.hair, [0, 0, 0], 0.25), box: span(butt, tip, 18) })

    const across = { x: -g.uy, y: g.ux }
    const blade = [
      [tip.x, tip.y],
      [tip.x + across.x * 50 - g.ux * 6, tip.y + across.y * 50 - g.uy * 6],
      [tip.x + across.x * 46 + g.ux * 22, tip.y + across.y * 46 + g.uy * 22],
      [tip.x + across.x * 13 + g.ux * 8, tip.y + across.y * 13 + g.uy * 8],
    ]
    paint(c, sdPolygon(blade), { fill: INK, grow: 3.2, alpha: 0.95, box: span(tip, { x: tip.x + across.x * 54, y: tip.y + across.y * 54 }, 34) })
    paint(c, sdPolygon(blade), { fill: s.accent, box: span(tip, { x: tip.x + across.x * 70, y: tip.y + across.y * 70 }, 34) })
  },
})

// ------------------------------------------------------------------ run ---

// Run as a script it writes the art; imported, it only lends its characters (the portraits of
// gen-phase8-art.mjs draw the cast from here, so each portrait is its fighter).
const isMain = process.argv[1] !== undefined && resolve(process.argv[1]) === fileURLToPath(import.meta.url)

if (isMain) {

  // Lulu's and Ren's measurements, from the last line gen-phase5-art.mjs prints, so the check below
  // covers the whole stage rather than only the four this generator draws.
  const PHASE5 = [
    { subject: 'lulu', height: 405, width: 183 },
    { subject: 'ren', height: 398, width: 221 },
  ]

  const cast = [kess, vey, orm, malk]
  const silhouettes = [...PHASE5]
  for (const character of cast) {
    const first = drawCharacter(character, written)
    silhouettes.push({ subject: character.subject, height: first.height, width: first.width })
    // The idle pose is the one an enemy holds between actions, and it is the frame the plan's own
    // test measures, so nothing in it may reach an edge of the canvas. A strike that throws an
    // implement past the frame is the house style Ren set in Phase 5; drawCharacter prints those.
    const idleCropped = first.cropped.filter((frame) => frame.startsWith('idle'))
    if (idleCropped.length > 0) {
      throw new Error(`${character.subject} is cropped standing still, on ${idleCropped.join(', ')}; shorten the headpiece or the implement`)
    }
  }

  // No two of the five share a silhouette (Phase 6 assets): a pair whose idle frame is within four
  // pixels both ways would read as the same fighter at the size the stage draws them.
  for (const one of silhouettes) {
    for (const other of silhouettes) {
      if (one.subject < other.subject && Math.abs(one.height - other.height) < 4 && Math.abs(one.width - other.width) < 4) {
        throw new Error(
          `${one.subject} and ${other.subject} stand the same: ${one.height} x ${one.width} against ${other.height} x ${other.width}`,
        )
      }
    }
  }

  const sprites = written.filter((f) => f.name.endsWith('.png')).length
  console.log(`\n${sprites} sprites and ${written.length - sprites} sidecars written under Art/World1/`)
  console.log(`frame canvas ${FRAME} x ${FRAME}, soles on y = ${SOLE}`)
}
