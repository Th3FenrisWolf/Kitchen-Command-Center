/**
 * Hand-drawn geometry as real SVG paths. A rounded rectangle is walked point by point and each point is
 * pushed along its outward normal by noise that is exactly periodic over the perimeter, so the loop closes
 * seamlessly. Real paths stay smooth at any zoom; a displaced CSS border would tear.
 */

export type Point = { x: number; y: number }

type Segment =
  | { t: 'l'; x0: number; y0: number; x1: number; y1: number; nx: number; ny: number; len: number }
  | { t: 'a'; cx: number; cy: number; a0: number; a1: number; len: number }

type Perimeter = { segs: Segment[]; r: number; total: number }

/** Small, fast seeded PRNG so the same seed always draws the same line. */
export function mulberry32(seed: number): () => number {
  let a = seed | 0
  return () => {
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

/** Smooth noise over t in [0, 1) built from whole-number frequencies, so f(0) === f(1). */
export function periodicNoise(seed: number, frequencies: number[]): (t: number) => number {
  const rnd = mulberry32(seed * 7919 + 13)
  let norm = 0
  const parts = frequencies.map((k) => {
    const amplitude = 1 / Math.pow(k, 0.85)
    norm += amplitude
    return { k, amplitude, phase: rnd() * Math.PI * 2 }
  })
  return (t) => parts.reduce((sum, p) => sum + p.amplitude * Math.sin(2 * Math.PI * p.k * t + p.phase), 0) / norm
}

function perimeter(w: number, h: number, radius: number): Perimeter {
  const r = Math.max(0, Math.min(radius, Math.min(w, h) / 2))
  const segs: Segment[] = []
  const line = (x0: number, y0: number, x1: number, y1: number, nx: number, ny: number) =>
    segs.push({ t: 'l', x0, y0, x1, y1, nx, ny, len: Math.hypot(x1 - x0, y1 - y0) })
  const arc = (cx: number, cy: number, a0: number, a1: number) =>
    segs.push({ t: 'a', cx, cy, a0, a1, len: Math.abs(a1 - a0) * r })
  const H = Math.PI / 2
  line(r, 0, w - r, 0, 0, -1)
  arc(w - r, r, -H, 0)
  line(w, r, w, h - r, 1, 0)
  arc(w - r, h - r, 0, H)
  line(w - r, h, r, h, 0, 1)
  arc(r, h - r, H, Math.PI)
  line(0, h - r, 0, r, -1, 0)
  arc(r, r, Math.PI, 1.5 * Math.PI)
  return { segs, r, total: segs.reduce((sum, s) => sum + s.len, 0) }
}

function pointAt(p: Perimeter, distance: number): Point & { nx: number; ny: number } {
  let acc = 0
  for (let i = 0; i < p.segs.length; i++) {
    const seg = p.segs[i]!
    if (distance <= acc + seg.len || i === p.segs.length - 1) {
      const u = seg.len ? (distance - acc) / seg.len : 0
      if (seg.t === 'l')
        return { x: seg.x0 + (seg.x1 - seg.x0) * u, y: seg.y0 + (seg.y1 - seg.y0) * u, nx: seg.nx, ny: seg.ny }
      const angle = seg.a0 + (seg.a1 - seg.a0) * u
      const nx = Math.cos(angle)
      const ny = Math.sin(angle)
      return { x: seg.cx + nx * p.r, y: seg.cy + ny * p.r, nx, ny }
    }
    acc += seg.len
  }
  throw new Error('unreachable: distance beyond perimeter')
}

const f = (n: number) => n.toFixed(2)

/** Catmull-Rom → cubic Bézier, closed. */
export function closedSpline(pts: Point[]): string {
  const n = pts.length
  let d = `M${f(pts[0]!.x)} ${f(pts[0]!.y)}`
  for (let i = 0; i < n; i++) {
    const p0 = pts[(i - 1 + n) % n]!
    const p1 = pts[i]!
    const p2 = pts[(i + 1) % n]!
    const p3 = pts[(i + 2) % n]!
    d += `C${f(p1.x + (p2.x - p0.x) / 6)} ${f(p1.y + (p2.y - p0.y) / 6)},${f(p2.x - (p3.x - p1.x) / 6)} ${f(p2.y - (p3.y - p1.y) / 6)},${f(p2.x)} ${f(p2.y)}`
  }
  return d + 'Z'
}

/** Catmull-Rom → cubic Bézier, open. */
export function openSpline(pts: Point[]): string {
  const n = pts.length
  let d = `M${f(pts[0]!.x)} ${f(pts[0]!.y)}`
  for (let i = 0; i < n - 1; i++) {
    const p0 = pts[Math.max(0, i - 1)]!
    const p1 = pts[i]!
    const p2 = pts[i + 1]!
    const p3 = pts[Math.min(n - 1, i + 2)]!
    d += `C${f(p1.x + (p2.x - p0.x) / 6)} ${f(p1.y + (p2.y - p0.y) / 6)},${f(p2.x - (p3.x - p1.x) / 6)} ${f(p2.y - (p3.y - p1.y) / 6)},${f(p2.x)} ${f(p2.y)}`
  }
  return d
}

/** A wobbling rounded rectangle, inset so the stroke stays inside the w × h box. */
export function rectPath(w: number, h: number, radius: number, amp = 3, seed = 1, wobbles?: number[]): string {
  const pad = amp + 3.4
  const p = perimeter(Math.max(2, w - pad * 2), Math.max(2, h - pad * 2), radius)
  const n = Math.max(26, Math.min(260, Math.round(p.total / 7)))
  // One gentle wave roughly every 175px, so long edges wander as much as short ones.
  const w0 = Math.max(2, Math.min(12, Math.round(p.total / 240)))
  const noise = periodicNoise(
    seed,
    wobbles ?? [w0, Math.max(w0 + 1, Math.round(w0 * 1.7)), Math.max(w0 + 2, Math.round(w0 * 2.7))],
  )
  const pts: Point[] = []
  for (let i = 0; i < n; i++) {
    const t = i / n
    const at = pointAt(p, t * p.total)
    const o = noise(t) * amp
    pts.push({ x: pad + at.x + at.nx * o, y: pad + at.y + at.ny * o })
  }
  return closedSpline(pts)
}

/** A single drawn line from a to b, wandering gently across its length and landing on both ends. */
export function linePath(x0: number, y0: number, x1: number, y1: number, amp: number, seed = 1, steps?: number): string {
  const dx = x1 - x0
  const dy = y1 - y0
  const length = Math.hypot(dx, dy) || 1
  const nx = -dy / length
  const ny = dx / length
  // Same trap as `hatch`'s gap: `??` only replaces `undefined`, where the design's `steps ||` also caught a
  // zero that divides into a NaN path — a stroke that silently renders as nothing.
  const n = steps && steps > 0 ? steps : Math.max(4, Math.round(length / 14))
  const rnd = mulberry32(seed * 104729 + 7)
  const phase = rnd() * Math.PI * 2
  const k = 1 + rnd() * 1.6
  const phase2 = rnd() * Math.PI * 2
  const pts: Point[] = []
  for (let i = 0; i <= n; i++) {
    const t = i / n
    const envelope = Math.sin(Math.PI * t)
    const o = (Math.sin(Math.PI * k * t * 2 + phase) * 0.7 + Math.sin(Math.PI * 3.1 * t + phase2) * 0.3) * amp * envelope
    pts.push({ x: x0 + dx * t + nx * o, y: y0 + dy * t + ny * o })
  }
  return openSpline(pts)
}

export interface HatchOptions {
  gap?: number
  lean?: number
  amp?: number
  seed?: number
}

/** Hatched shadow: parallel drawn strokes raking across a w × h strip. */
export function hatch(w: number, h: number, { gap = 6.5, lean = 0.95, amp = 1.1, seed = 3 }: HatchOptions = {}): string[] {
  // A non-positive or NaN gap would never advance the loop; the design's `opts.gap || 6.5` guarded this and
  // the destructuring default does not, since it only replaces `undefined`.
  const step = gap > 0 ? gap : 6.5
  const out: string[] = []
  let i = 0
  for (let x = -h * lean; x < w; x += step) {
    out.push(linePath(x, h, x + h * lean, 0, amp, seed + i * 17, 4))
    i++
  }
  return out
}
