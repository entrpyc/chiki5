// The posed puppet the character generators draw (docs/plan.md, Phase 5). A character is a
// skeleton — hips, torso, head, two arms, two legs and a held implement — keyframed once per
// frame and filled as outlined rounded limbs on a 512 x 512 canvas. Every limb is a capsule
// painted inside its own bounding box, so a frame costs its own silhouette rather than the whole
// canvas, and every pose is stated as joint angles rather than as pixels, so a clip is a list of
// angles and nothing is repainted by hand.
//
// Angles are degrees from straight down, positive toward the direction the character faces. The
// puppet is always built facing right; a character that faces left is mirrored when it is
// written, so the same pose numbers mean the same thing for both sides of the stage.

import { bleed, canvas, lerp, paint, sdCircle, sdRoundBox, sdSegment } from './art-lib.mjs'

/** Every character frame is drawn on this canvas (docs/plan.md, Phase 5 assets).*/
export const FRAME = 512

/** The canvas x the puppet's hips stand on. */
export const MID = FRAME / 2

/** The canvas y a sole rests on: the bottom-centre pivot's own line, with room under it for the outline. */
export const SOLE = 506

/** The ankle of a character whose legs are that thick, so its soles land on {@link SOLE}. */
export const footY = (s) => SOLE - s.legR

const INK = [14, 12, 20]
const OUTLINE = 3.2

const rad = (deg) => (deg * Math.PI) / 180

/** A capsule's bounding box, swelled by the outline and one pixel of antialiasing. */
function capsuleBox(a, b, r) {
  const pad = r + OUTLINE + 2
  return [Math.min(a.x, b.x) - pad, Math.min(a.y, b.y) - pad, Math.max(a.x, b.x) + pad, Math.max(a.y, b.y) + pad]
}

/** One limb: an ink outline swelled under a rounded capsule, then the capsule in its own colour. */
function limb(c, a, b, r, colour) {
  const sdf = sdSegment(a.x, a.y, b.x, b.y, r)
  const box = capsuleBox(a, b, r)
  paint(c, sdf, { fill: INK, grow: OUTLINE, alpha: 0.95, box })
  paint(c, sdf, { fill: colour, box })
}

/** One blob: the same outline-then-fill for a circle, used for the head, the joints and the hair. */
function blob(c, centre, r, colour, alpha = 1) {
  const sdf = sdCircle(centre.x, centre.y, r)
  const pad = r + OUTLINE + 2
  const box = [centre.x - pad, centre.y - pad, centre.x + pad, centre.y + pad]
  paint(c, sdf, { fill: INK, grow: OUTLINE, alpha: 0.95 * alpha, box })
  paint(c, sdf, { fill: colour, alpha, box })
}

/** A rounded slab: the torso and the plates on it. */
function slab(c, cx, cy, hx, hy, round, colour, angle = 0) {
  const s = Math.sin(-rad(angle))
  const t = Math.cos(-rad(angle))
  const base = sdRoundBox(0, 0, hx, hy, round)
  const sdf = (x, y) => {
    const dx = x - cx
    const dy = y - cy
    return base(dx * t - dy * s, dx * s + dy * t)
  }

  const reach = Math.hypot(hx, hy) + OUTLINE + 2
  const box = [cx - reach, cy - reach, cx + reach, cy + reach]
  paint(c, sdf, { fill: INK, grow: OUTLINE, alpha: 0.95, box })
  paint(c, sdf, { fill: colour, box })
}

/** The pose every clip's keyframes are stated against: standing square, arms down, legs straight. */
export const NEUTRAL = {
  bob: 0, // whole body up (negative) or down (positive)
  shift: 0, // whole body toward the facing direction
  crouch: 0, // how far the hips drop toward the feet
  lean: 0, // torso angle from upright
  headTilt: 0, // head angle on top of the torso's
  nearArm: [10, 16], // the arm on the viewer's side: upper then forearm
  farArm: [-8, -14], // the arm behind the torso
  nearLeg: [4, -2], // thigh then shin
  farLeg: [-6, 2],
  reach: 0, // how far the held implement is thrust along the near forearm
}

/** A pose: the neutral one with the given joints restated. */
export function pose(over = {}) {
  return { ...NEUTRAL, ...over }
}

/** Blends two poses, for the frames between a wind-up and its strike. */
export function between(a, b, t) {
  const out = {}
  for (const key of Object.keys(NEUTRAL)) {
    const from = a[key] ?? NEUTRAL[key]
    const to = b[key] ?? NEUTRAL[key]
    out[key] = Array.isArray(from) ? from.map((v, i) => v + (to[i] - v) * t) : from + (to - from) * t
  }

  return out
}

/** Where every joint of one pose sits on the canvas, facing right. */
export function joints(s, p) {
  const hipX = MID + p.shift
  const hipY = footY(s) + p.bob - (s.thigh + s.shin) + p.crouch
  const hip = { x: hipX, y: hipY }

  const lean = rad(p.lean)
  const shoulder = { x: hipX + Math.sin(lean) * s.torsoH, y: hipY - Math.cos(lean) * s.torsoH }

  const chain = (root, angles, lengths) => {
    const a = rad(angles[0])
    const mid = { x: root.x + Math.sin(a) * lengths[0], y: root.y + Math.cos(a) * lengths[0] }
    const b = rad(angles[0] + angles[1])
    const end = { x: mid.x + Math.sin(b) * lengths[1], y: mid.y + Math.cos(b) * lengths[1] }
    return { mid, end }
  }

  const armRoot = (side) => ({ x: shoulder.x + Math.cos(lean) * side * s.shoulderOut, y: shoulder.y + Math.sin(lean) * side * s.shoulderOut + s.shoulderDrop })
  const near = chain(armRoot(1), p.nearArm, [s.upperArm, s.foreArm])
  const far = chain(armRoot(-1), p.farArm, [s.upperArm, s.foreArm])
  const nearLeg = chain({ x: hipX + s.hipOut, y: hipY }, p.nearLeg, [s.thigh, s.shin])
  const farLeg = chain({ x: hipX - s.hipOut, y: hipY }, p.farLeg, [s.thigh, s.shin])

  const headAngle = lean + rad(p.headTilt)
  const head = {
    x: shoulder.x + Math.sin(headAngle) * (s.neck + s.headR),
    y: shoulder.y - Math.cos(headAngle) * (s.neck + s.headR),
  }

  return { hip, shoulder, head, headAngle, near, far, nearLeg, farLeg }
}

/**
 * Draws one frame of a character in one pose. The far limbs go down first in a darker shade, then
 * the torso, then the near limbs, then the head, so the silhouette reads back to front without a
 * depth buffer. `s.draw` is the character's own decoration — hair, headpiece, plates — and runs
 * after the head so it can sit on top of it.
 */
export function drawPose(s, p) {
  const c = canvas(FRAME, FRAME)
  drawPoseOn(c, s, p)
  bleed(c)
  return c
}

/**
 * Draws one pose onto a canvas the caller owns, in the puppet's own space; a canvas with a view
 * (art-lib.mjs, `paint`) draws it magnified, which is how a portrait is the fighter up close.
 */
export function drawPoseOn(c, s, p) {
  const j = joints(s, p)
  const back = (colour) => lerp(colour, [0, 0, 0], 0.34)

  limb(c, { x: j.hip.x - s.hipOut, y: j.hip.y }, j.farLeg.mid, s.legR, back(s.trouser))
  limb(c, j.farLeg.mid, j.farLeg.end, s.legR * 0.86, back(s.trouser))
  foot(c, j.farLeg.end, s, back(s.boot), -1)

  limb(c, j.shoulder, j.far.mid, s.armR, back(s.sleeve))
  limb(c, j.far.mid, j.far.end, s.armR * 0.84, back(s.skin))

  slab(c, (j.hip.x + j.shoulder.x) / 2, (j.hip.y + j.shoulder.y) / 2, s.torsoW / 2, s.torsoH / 2 + s.armR, s.torsoRound, s.coat, p.lean)

  limb(c, { x: j.hip.x + s.hipOut, y: j.hip.y }, j.nearLeg.mid, s.legR, s.trouser)
  limb(c, j.nearLeg.mid, j.nearLeg.end, s.legR * 0.86, s.trouser)
  foot(c, j.nearLeg.end, s, s.boot, 1)

  limb(c, j.shoulder, j.near.mid, s.armR, s.sleeve)
  limb(c, j.near.mid, j.near.end, s.armR * 0.84, s.skin)

  blob(c, j.head, s.headR, s.skin)
  if (s.draw) {
    s.draw(c, j, s, p, { limb, blob, slab, rad })
  }

  if (s.implement) {
    s.implement(c, j, s, p, { limb, blob, slab, rad })
  }
}

/** A foot: a short capsule lying along the ground under the ankle, toward the facing direction. */
function foot(c, ankle, s, colour, side) {
  const toe = { x: ankle.x + s.footLength, y: ankle.y + s.legR * 0.2 }
  limb(c, { x: ankle.x - s.footLength * 0.3, y: ankle.y }, toe, s.legR * 0.78, colour)
  void side
}

/** The hand's point and the direction the near forearm runs, for a character's held implement. */
export function grip(j) {
  const dx = j.near.end.x - j.near.mid.x
  const dy = j.near.end.y - j.near.mid.y
  const length = Math.hypot(dx, dy) || 1
  return { x: j.near.end.x, y: j.near.end.y, ux: dx / length, uy: dy / length }
}

/** Mirrors a canvas about its vertical centre, for the characters that face left. */
export function mirror(c) {
  const out = canvas(c.w, c.h)
  for (let y = 0; y < c.h; y++) {
    for (let x = 0; x < c.w; x++) {
      const from = (y * c.w + (c.w - 1 - x)) * 4
      const to = (y * c.w + x) * 4
      for (let k = 0; k < 4; k++) {
        out.px[to + k] = c.px[from + k]
      }
    }
  }

  return out
}

/** The opaque part of a canvas, so a generator can state and check a character's drawn height. */
export function opaqueBox(c) {
  let minX = c.w
  let minY = c.h
  let maxX = -1
  let maxY = -1
  for (let y = 0; y < c.h; y++) {
    for (let x = 0; x < c.w; x++) {
      if (c.px[(y * c.w + x) * 4 + 3] <= 0) continue
      if (x < minX) minX = x
      if (x > maxX) maxX = x
      if (y < minY) minY = y
      if (y > maxY) maxY = y
    }
  }

  return maxX < 0 ? { width: 0, height: 0 } : { x: minX, y: minY, width: maxX - minX + 1, height: maxY - minY + 1 }
}

export { INK, rad }
