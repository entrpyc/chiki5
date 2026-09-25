// The drawing kit the generators under tools/ share: a straight-alpha float canvas, signed
// distance fields for the shapes, and an 8-bit RGBA PNG writer. Shapes are drawn from distance
// fields so the art is resolution-honest and antialiased, and so a shape can be restated rather
// than repainted.

import { deflateSync } from 'node:zlib'
import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname } from 'node:path'

// ----------------------------------------------------------------- canvas ---

export function canvas(w, h) {
  return { w, h, px: new Float64Array(w * h * 4) }
}

/** Source-over onto straight alpha; a fully transparent destination simply takes the source hue. */
export function blend(c, x, y, rgb, a) {
  if (a <= 0) return
  const i = (y * c.w + x) * 4
  const p = c.px
  const da = p[i + 3]
  const out = a + da * (1 - a)
  if (out <= 1e-9) {
    p[i] = rgb[0]
    p[i + 1] = rgb[1]
    p[i + 2] = rgb[2]
    p[i + 3] = 0
    return
  }
  for (let k = 0; k < 3; k++) {
    p[i + k] = (rgb[k] * a + p[i + k] * da * (1 - a)) / out
  }
  p[i + 3] = out
}

/** Coverage of a distance field at one pixel: one pixel of analytic antialiasing. */
export function cover(d) {
  return Math.min(1, Math.max(0, 0.5 - d))
}

/**
 * Paints one distance field. `fill` is a colour or a function of (x, y); `alpha` likewise.
 * `stroke` draws a band of `width` centred on the field's zero, `grow` swells the field first,
 * and `clip` keeps only what another field contains. `box` is an optional `[x0, y0, x1, y1]`
 * the caller knows the field's coverage to lie inside; a small shape on a large canvas is then
 * paid for by its own area, which is what makes a puppet of a dozen limbs cheap to redraw.
 *
 * A canvas may carry a `view` — `{ k, ox, oy, line }` — that magnifies the drawing space onto
 * it: a point (x, y) lands on ((x - ox) * k, (y - oy) * k), and outlines and strokes are drawn
 * `line` times as wide. Code written for one scale then draws, unchanged, at another, with its
 * edges resolved at the canvas's own resolution (the portraits of P8.3 draw the fighters so).
 * A canvas without a view draws exactly as before.
 */
export function paint(c, sdf, opts) {
  if (c.view) {
    const { k, ox, oy, line = k } = c.view
    const field = sdf
    const within = opts.clip
    sdf = (x, y) => field(x / k + ox, y / k + oy) * k
    opts = {
      ...opts,
      grow: (opts.grow ?? 0) * line,
      width: (opts.width ?? 2) * line,
      clip: within ? (x, y) => within(x / k + ox, y / k + oy) * k : null,
      box: opts.box ? [(opts.box[0] - ox) * k, (opts.box[1] - oy) * k, (opts.box[2] - ox) * k, (opts.box[3] - oy) * k] : null,
    }
  }

  const { fill, alpha = 1, stroke = null, width = 2, grow = 0, clip = null, box = null } = opts
  const x0 = box ? Math.max(0, Math.floor(box[0])) : 0
  const y0 = box ? Math.max(0, Math.floor(box[1])) : 0
  const x1 = box ? Math.min(c.w, Math.ceil(box[2])) : c.w
  const y1 = box ? Math.min(c.h, Math.ceil(box[3])) : c.h
  for (let y = y0; y < y1; y++) {
    for (let x = x0; x < x1; x++) {
      const px = x + 0.5
      const py = y + 0.5
      const d = sdf(px, py) - grow
      let cv = stroke === null ? cover(d) : cover(Math.abs(d) - width / 2)
      if (cv <= 0) continue
      if (clip) {
        cv *= cover(clip(px, py))
        if (cv <= 0) continue
      }
      const rgb = typeof fill === 'function' ? fill(px, py) : fill
      const a = typeof alpha === 'function' ? alpha(px, py) : alpha
      blend(c, x, y, stroke === null ? rgb : stroke, cv * a)
    }
  }
}

/** Paints every pixel, for gradients and radial falloffs that own no outline. */
export function paintAll(c, rgbAt, alphaAt) {
  for (let y = 0; y < c.h; y++) {
    for (let x = 0; x < c.w; x++) {
      const a = alphaAt(x + 0.5, y + 0.5)
      if (a > 0) blend(c, x, y, rgbAt(x + 0.5, y + 0.5), Math.min(1, a))
    }
  }
}

/**
 * Spreads colour into fully transparent pixels so bilinear filtering never samples black from
 * outside the art (the importer filters bilinear, docs/plan.md asset conventions).
 */
export function bleed(c, passes = 3) {
  for (let pass = 0; pass < passes; pass++) {
    const src = c.px.slice()
    for (let y = 0; y < c.h; y++) {
      for (let x = 0; x < c.w; x++) {
        const i = (y * c.w + x) * 4
        if (src[i + 3] > 0) continue
        let r = 0
        let g = 0
        let b = 0
        let n = 0
        for (let dy = -1; dy <= 1; dy++) {
          for (let dx = -1; dx <= 1; dx++) {
            const nx = x + dx
            const ny = y + dy
            if (nx < 0 || ny < 0 || nx >= c.w || ny >= c.h) continue
            const j = (ny * c.w + nx) * 4
            if (src[j + 3] <= 0) continue
            r += src[j]
            g += src[j + 1]
            b += src[j + 2]
            n++
          }
        }
        if (n === 0) continue
        c.px[i] = r / n
        c.px[i + 1] = g / n
        c.px[i + 2] = b / n
      }
    }
  }
}

// -------------------------------------------------------------- distances ---

export function sdCircle(cx, cy, r) {
  return (x, y) => Math.hypot(x - cx, y - cy) - r
}

export function sdRoundBox(cx, cy, hx, hy, r) {
  return (x, y) => {
    const qx = Math.abs(x - cx) - (hx - r)
    const qy = Math.abs(y - cy) - (hy - r)
    const ox = Math.max(qx, 0)
    const oy = Math.max(qy, 0)
    return Math.hypot(ox, oy) + Math.min(Math.max(qx, qy), 0) - r
  }
}

export function sdSegment(ax, ay, bx, by, r) {
  return (x, y) => {
    const ex = bx - ax
    const ey = by - ay
    const wx = x - ax
    const wy = y - ay
    const t = Math.min(1, Math.max(0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
    return Math.hypot(wx - ex * t, wy - ey * t) - r
  }
}

/** Inigo Quilez's polygon distance: exact, signed, and happy with concave outlines. */
export function sdPolygon(verts) {
  return (x, y) => {
    let d = (x - verts[0][0]) ** 2 + (y - verts[0][1]) ** 2
    let s = 1
    for (let i = 0, j = verts.length - 1; i < verts.length; j = i, i++) {
      const ex = verts[j][0] - verts[i][0]
      const ey = verts[j][1] - verts[i][1]
      const wx = x - verts[i][0]
      const wy = y - verts[i][1]
      const t = Math.min(1, Math.max(0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
      const bx = wx - ex * t
      const by = wy - ey * t
      d = Math.min(d, bx * bx + by * by)
      const c1 = y >= verts[i][1]
      const c2 = y < verts[j][1]
      const c3 = ex * wy - ey * wx > 0
      if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s
    }
    return s * Math.sqrt(d)
  }
}

export const union = (...fs) => (x, y) => Math.min(...fs.map((f) => f(x, y)))
export const mirrorX = (f, w) => (x, y) => f(w - x, y)
export const shrink = (f, r) => (x, y) => f(x, y) + r

/** Blunts a polygon's corners: pull each vertex toward the centroid, then swell by the radius. */
export function sdRoundPolygon(verts, r) {
  const cx = verts.reduce((s, v) => s + v[0], 0) / verts.length
  const cy = verts.reduce((s, v) => s + v[1], 0) / verts.length
  const k = 0.86
  const inset = verts.map(([x, y]) => [cx + (x - cx) * k, cy + (y - cy) * k])
  const poly = sdPolygon(inset)
  return (x, y) => poly(x, y) - r
}

export const lerp = (a, b, t) => a.map((v, i) => v + (b[i] - v) * t)

// -------------------------------------------------------------------- png ---

const CRC = (() => {
  const t = new Uint32Array(256)
  for (let n = 0; n < 256; n++) {
    let c = n
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1
    t[n] = c >>> 0
  }
  return t
})()

export function crc32(buf) {
  let c = 0xffffffff
  for (let i = 0; i < buf.length; i++) c = CRC[(c ^ buf[i]) & 0xff] ^ (c >>> 8)
  return (c ^ 0xffffffff) >>> 0
}

export function chunk(type, data) {
  const out = Buffer.alloc(data.length + 12)
  out.writeUInt32BE(data.length, 0)
  out.write(type, 4, 'ascii')
  data.copy(out, 8)
  out.writeUInt32BE(crc32(out.subarray(4, 8 + data.length)), 8 + data.length)
  return out
}

export function writePng(path, c) {
  const raw = Buffer.alloc(c.h * (c.w * 4 + 1))
  let o = 0
  for (let y = 0; y < c.h; y++) {
    raw[o++] = 0 // filter None: these files are small and stay diffable as bytes
    for (let x = 0; x < c.w; x++) {
      const i = (y * c.w + x) * 4
      for (let k = 0; k < 4; k++) {
        const v = k === 3 ? c.px[i + 3] * 255 : c.px[i + k]
        raw[o++] = Math.min(255, Math.max(0, Math.round(v)))
      }
    }
  }

  const ihdr = Buffer.alloc(13)
  ihdr.writeUInt32BE(c.w, 0)
  ihdr.writeUInt32BE(c.h, 4)
  ihdr[8] = 8 // 8 bits per channel
  ihdr[9] = 6 // truecolour with alpha
  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(
    path,
    Buffer.concat([
      Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
      chunk('IHDR', ihdr),
      chunk('IDAT', deflateSync(raw, { level: 9 })),
      chunk('IEND', Buffer.alloc(0)),
    ]),
  )
  return path
}
