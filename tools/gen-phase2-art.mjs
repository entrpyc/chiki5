// Draws the Phase 2 art of docs/plan.md: the Rhythm Line kit (P2.1), the telegraph icons and
// wind-up bar (P2.2), the telegraph glow (P2.3), and the dark line background and Iron Veil icon
// (P2.4). Every file is written at the size its plan item states, as 8-bit RGBA PNG with straight
// alpha, named by the asset conventions and placed under client/Assets/_Project/Art/.
//
// Run: node tools/gen-phase2-art.mjs
//
// The art is drawn from signed distance fields so it is resolution-honest and antialiased, and
// so a shape can be restated rather than repainted. Nothing here contains lettering (the plan's
// asset conventions); every telegraph icon is told apart by its silhouette alone, and none of the
// five carries Attack red, Defense blue or Ability green (PRD 3.4.2).

// The canvas, the distance fields and the PNG writer are shared with the other generators.

import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  bleed,
  canvas,
  lerp,
  mirrorX,
  paint,
  paintAll,
  sdCircle,
  sdPolygon,
  sdRoundBox,
  sdRoundPolygon,
  sdSegment,
  shrink,
  union,
  writePng,
} from './art-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ART = join(ROOT, 'client', 'Assets', '_Project', 'Art')

// ---------------------------------------------------------------- palette ---

const INK = [16, 13, 24] // the outline every icon carries, so it reads on any line background
const AMBER = [242, 163, 60] // both attacks: told apart by direction, not by hue
const STEEL = [168, 176, 196] // enemy Defend: desaturated, never Defense blue
const VIOLET = [197, 131, 232] // enemy Buff
const GOLD = [255, 216, 107] // Charge, and its wind-up bar
const IRON = [154, 164, 186] // Iron Veil
const TICK = [207, 208, 220]
const QUARTER = [126, 130, 143]
const PLAYHEAD_CORE = [255, 248, 200]
const PLAYHEAD_EDGE = [255, 233, 138]
const WINDOW = [255, 242, 160]


const written = []

function emit(folder, name, c, note) {
  bleed(c)
  const path = writePng(join(ART, folder, name), c)
  written.push({ name, folder, size: `${c.w} x ${c.h}`, note })
  return path
}

// ------------------------------------------------------- P2.1 Rhythm Line ---

// 9-slice, 400 x 180, 32 px left and right borders: every corner curve and every cap detail
// stays inside 32 px of an end, so the centre is uniform along x and stretches without seams.
function rhythmLineBackground({ top, bottom, groove, grooveEdge, inner, highlight, alpha }) {
  const c = canvas(400, 180)
  const panel = sdRoundBox(200, 90, 200, 90, 18)
  paint(c, panel, {
    fill: (x, y) => lerp(top, bottom, Math.pow(y / 180, 0.85)),
    alpha,
  })
  const track = sdRoundBox(200, 90, 186, 40, 10)
  paint(c, track, { fill: groove, alpha: 0.55 })
  paint(c, track, { stroke: grooveEdge, width: 1.5, alpha: 0.8 })
  paint(c, panel, { stroke: inner, width: 1.5, grow: -2.5, alpha: 0.5 })
  paint(c, sdRoundBox(200, 5.5, 192, 1, 1), { fill: highlight, alpha: 0.22 })
  return c
}

emit(
  'Shared/rhythmline',
  'spr_ui_rhythmline_bg_01.png',
  rhythmLineBackground({
    top: [30, 33, 48],
    bottom: [11, 12, 18],
    groove: [9, 10, 15],
    grooveEdge: [50, 58, 87],
    inner: [69, 78, 110],
    highlight: [124, 134, 174],
    alpha: 0.94,
  }),
  '9-slice, 32 px left and right borders',
)

// The beat tick: 4 px of bright rule with a soft column either side and both ends fading out.
{
  const c = canvas(4, 126)
  const columns = [0.32, 1, 1, 0.32]
  paintAll(
    c,
    () => TICK,
    (x, y) => {
      const ends = Math.min(1, Math.min(y, 126 - y) / 7)
      return columns[Math.floor(x)] * ends * 0.92
    },
  )
  emit('Shared/rhythmline', 'spr_ui_rhythmline_beat_01.png', c, 'beat rule')
}

// The quarter-beat tick: the same rule, dimmer and shorter (PRD 3.3.1.4 counts in quarter beats).
{
  const c = canvas(2, 54)
  paintAll(
    c,
    () => QUARTER,
    (x, y) => 0.6 * Math.min(1, Math.min(y, 54 - y) / 5),
  )
  emit('Shared/rhythmline', 'spr_ui_rhythmline_quarter_01.png', c, 'quarter-beat rule')
}

// The playhead: a hard 2 px core with a warm falloff, full height, uniform down the line.
{
  const c = canvas(8, 180)
  paintAll(
    c,
    (x) => (Math.abs(x - 4) <= 0.5 ? PLAYHEAD_CORE : PLAYHEAD_EDGE),
    (x) => {
      const d = Math.abs(x - 4)
      return d <= 0.5 ? 1 : 0.85 * Math.pow(Math.max(0, 1 - (d - 0.5) / 3.2), 1.5)
    },
  )
  emit('Shared/rhythmline', 'spr_ui_rhythmline_playhead_01.png', c, 'playhead')
}

// The Judgment Window band, idle and open (PRD 3.3.8.1). 9-slice, 64 x 144, 16 px borders: the
// posts and their inward glow live inside 16 px of each end, so the band stretches to any window.
function windowBand({ fill, post, glow, postColour }) {
  const c = canvas(64, 144)
  paint(c, sdRoundBox(32, 72, 32, 72, 6), { fill: WINDOW, alpha: fill })
  paintAll(
    c,
    () => postColour,
    (x) => glow * Math.pow(Math.max(0, 1 - Math.min(x, 64 - x) / 14), 2),
  )
  paint(c, sdRoundBox(2.5, 72, 1.5, 70, 1.5), { fill: postColour, alpha: post })
  paint(c, sdRoundBox(61.5, 72, 1.5, 70, 1.5), { fill: postColour, alpha: post })
  return c
}

emit(
  'Shared/rhythmline',
  'spr_ui_rhythmline_window_01.png',
  windowBand({ fill: 0.1, post: 0.5, glow: 0.12, postColour: [255, 235, 148] }),
  '9-slice, 16 px left and right borders',
)
emit(
  'Shared/rhythmline',
  'spr_ui_rhythmline_window-open_01.png',
  windowBand({ fill: 0.26, post: 0.95, glow: 0.28, postColour: [255, 246, 192] }),
  '9-slice, 16 px left and right borders',
)

// ---------------------------------------------------- P2.2 telegraph icons ---

/** Every telegraph icon: an ink outline swelled under the shape, then the shape in its colour. */
function icon(w, h, sdf, colour, outline = 1.7) {
  const c = canvas(w, h)
  paint(c, sdf, { fill: INK, grow: outline, alpha: 0.92 })
  paint(c, sdf, { fill: colour })
  return c
}

// Attack right: two chevrons pointing right. Attack left is its mirror, so the pair is told apart
// by direction alone and neither carries a category colour (PRD 3.4.2).
const chevrons = union(
  sdSegment(36, 16, 60, 40, 7),
  sdSegment(60, 40, 36, 64, 7),
  sdSegment(18, 26, 32, 40, 5),
  sdSegment(32, 40, 18, 54, 5),
)

emit('Shared/telegraph', 'spr_action_attack-right_static_01.png', icon(80, 80, chevrons, AMBER), 'chevrons right')
emit(
  'Shared/telegraph',
  'spr_action_attack-left_static_01.png',
  icon(80, 80, mirrorX(chevrons, 80), AMBER),
  'chevrons left',
)

// Defend: a shield, with a rib and a hem so it still reads when the marker shrinks.
{
  const shield = sdRoundPolygon(
    [
      [14, 16],
      [40, 9],
      [66, 16],
      [64, 42],
      [40, 71],
      [16, 42],
    ],
    4.2,
  )
  const c = icon(80, 80, shield, STEEL)
  paint(c, sdRoundBox(40, 30, 30, 2.2, 2), { fill: INK, alpha: 0.32, clip: shrink(shield, -3) })
  paint(c, sdRoundBox(40, 46, 1.8, 15, 1.8), { fill: INK, alpha: 0.3, clip: shrink(shield, -3) })
  emit('Shared/telegraph', 'spr_action_defend_static_01.png', c, 'shield')
}

// Buff: a four-pointed sparkle, hollow-waisted, so no spike count is shared with Charge.
{
  const points = []
  for (let k = 0; k < 8; k++) {
    const a = (-90 + k * 45) * (Math.PI / 180)
    const r = k % 2 === 0 ? 33 : 11
    points.push([40 + r * Math.cos(a), 40 + r * Math.sin(a)])
  }
  emit('Shared/telegraph', 'spr_action_buff_static_01.png', icon(80, 80, sdPolygon(points), VIOLET), 'sparkle')
}

// Charge: a solid orb with eight short rays — mass and radiance, never a chevron or a star.
{
  const rays = []
  for (let k = 0; k < 8; k++) {
    const a = k * 45 * (Math.PI / 180)
    rays.push(
      sdSegment(40 + 14 * Math.cos(a), 40 + 14 * Math.sin(a), 40 + 31 * Math.cos(a), 40 + 31 * Math.sin(a), 3),
    )
  }
  const burst = union(sdCircle(40, 40, 15), ...rays)
  const c = icon(80, 80, burst, GOLD)
  paint(c, sdCircle(40, 40, 7.5), { fill: [255, 246, 214], alpha: 0.85 })
  emit('Shared/telegraph', 'spr_action_charge_static_01.png', c, 'charging orb')
}

// The Charge wind-up bar (PRD 3.6.16), sliced along its length: 9-slice, 64 x 6, 3 px borders.
{
  const c = canvas(64, 6)
  const rows = [0.4, 0.9, 1, 1, 0.75, 0.32]
  paint(c, sdRoundBox(32, 3, 32, 3, 3), {
    fill: (x, y) => lerp([255, 240, 184], [224, 168, 47], y / 6),
    alpha: (x, y) => rows[Math.min(5, Math.floor(y))],
  })
  emit('Shared/telegraph', 'spr_ui_windup-bar_static_01.png', c, '9-slice, 3 px left and right borders')
}

// ------------------------------------------------ P2.3 telegraph highlight ---

// A soft white glow, so the code can tint it by kind while the window is open (PRD 3.3.8.1).
{
  const c = canvas(120, 120)
  paintAll(
    c,
    () => [255, 255, 255],
    (x, y) => {
      // A bell that still carries weight at 40 px, where the 80 x 80 icon's edge sits, and dies
      // to nothing exactly at the sprite's edge.
      const r = Math.min(1, Math.hypot(x - 60, y - 60) / 60)
      return Math.min(1, 0.95 * Math.pow(Math.cos((r * Math.PI) / 2), 2) + 0.15 * Math.pow(Math.max(0, 1 - r * 2.5), 2))
    },
  )
  emit('Shared/telegraph', 'spr_ui_telegraph-glow_static_01.png', c, 'soft white glow')
}

// ------------------------------------------------------- P2.4 Iron Veil ---

emit(
  'Shared/rhythmline',
  'spr_ui_rhythmline_bg-dark_01.png',
  rhythmLineBackground({
    top: [22, 16, 31],
    bottom: [7, 6, 11],
    groove: [6, 5, 10],
    grooveEdge: [59, 42, 92],
    inner: [84, 58, 126],
    highlight: [138, 110, 207],
    alpha: 0.97,
  }),
  '9-slice, 32 px left and right borders; the dark line every veiling ability reuses',
)

// The Iron Veil badge (PRD 3.6.9): an iron shield behind a drawn veil.
{
  const shield = sdRoundPolygon(
    [
      [11, 13],
      [32, 7],
      [53, 13],
      [51, 34],
      [32, 57],
      [13, 34],
    ],
    3.4,
  )
  const c = canvas(64, 64)
  paint(c, shield, { fill: INK, grow: 1.6, alpha: 0.92 })
  paint(c, shield, { fill: [25, 26, 36], alpha: 0.95 })
  paint(c, shield, { stroke: IRON, width: 2.6 })
  const inside = shrink(shield, -3)
  for (const x of [24, 32, 40]) {
    paint(c, sdRoundBox(x, 37, 2.2, 15, 2.2), { fill: IRON, alpha: 0.45, clip: inside })
  }
  paint(c, sdRoundBox(32, 23, 22, 1.6, 1.6), { fill: IRON, alpha: 0.75, clip: inside })
  emit('Shared/iron-veil', 'spr_ability_iron-veil_static_01.png', c, 'ability badge')
}

// ------------------------------------------------------------------ done ---

for (const f of written) {
  console.log(`${(f.folder + '/' + f.name).padEnd(62)} ${f.size.padEnd(10)} ${f.note}`)
}
console.log(`\n${written.length} sprites written under client/Assets/_Project/Art/`)
