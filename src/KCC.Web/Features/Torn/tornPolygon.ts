/**
 * Torn-paper silhouettes as CSS `polygon()` strings. Every vertex is `calc(A% + Bpx)`: the percentage carries
 * the position along the edge, the pixel term carries the inset, the chamfer and the noise along the outward
 * normal. One preset therefore clips a sheet of any size with the same 2–3px tear. Port of the mockup's
 * torn.js, which computed absolute pixels for a measured box; here the box is unknown at build time.
 * Depth is constant; the wavelength stretches with the box, so each preset family targets a size band
 * (see docs/brand/kit.md → Tears).
 */

/** Which corner the clean chamfer cuts. Never top-left: the label lives there. */
export type Corner = 1 | 2 | 3 // top-right | bottom-right | bottom-left

export interface TearOptions {
  seed: number
  /** Tear amplitude in px. Sheets 2.4, tiles 1.8. */
  amp?: number
  /** Chamfer length in px; 0 for an uncut sheet. */
  chamfer?: number
  corner?: Corner
  /** Vertices per straight side. The chamfer adds a few of its own. */
  pointsPerSide?: number
}

/** A coordinate as a percentage of the box plus a pixel term. */
export interface Coord {
  pct: number
  px: number
}

export interface Vertex {
  x: Coord
  y: Coord
}

/** Small, fast seeded PRNG so the same seed always tears the same way. */
export function mulberry32(seed: number): () => number {
  let a = seed | 0
  return () => {
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

/** Smooth noise over t in [0, 1) from whole-number frequencies, so f(0) === f(1) and the loop closes. */
export function periodicNoise(seed: number, frequencies: number[]): (t: number) => number {
  const rnd = mulberry32(seed * 7919 + 13)
  let norm = 0
  const parts = frequencies.map((k) => {
    const amplitude = 1 / Math.pow(k, 0.7)
    norm += amplitude
    return { k, amplitude, phase: rnd() * Math.PI * 2 }
  })
  return (t) => parts.reduce((sum, p) => sum + p.amplitude * Math.sin(2 * Math.PI * p.k * t + p.phase), 0) / norm
}

interface Segment {
  x0: Coord
  y0: Coord
  x1: Coord
  y1: Coord
  /** Outward normal. */
  nx: number
  ny: number
  points: number
  /** The chamfer: its noise is damped to 8% so it reads as a cut, not a tear. */
  clean: boolean
}

const at = (pct: number, px: number): Coord => ({ pct, px })
const lerp = (a: Coord, b: Coord, u: number): Coord => ({
  pct: a.pct + (b.pct - a.pct) * u,
  px: a.px + (b.px - a.px) * u,
})

/** Clockwise perimeter inset by `pad`, with the chamfer at `corner` when `chamfer` > 0. */
function perimeter(pad: number, chamfer: number, corner: Corner, pointsPerSide: number): Segment[] {
  const d = Math.SQRT1_2
  const side = (x0: Coord, y0: Coord, x1: Coord, y1: Coord, nx: number, ny: number): Segment => ({
    x0,
    y0,
    x1,
    y1,
    nx,
    ny,
    points: pointsPerSide,
    clean: false,
  })
  const cut = (x0: Coord, y0: Coord, x1: Coord, y1: Coord, nx: number, ny: number): Segment => ({
    x0,
    y0,
    x1,
    y1,
    nx,
    ny,
    points: Math.max(3, Math.round(chamfer / 4)),
    clean: true,
  })
  const L = at(0, pad) // left edge, as an x
  const R = at(100, -pad) // right edge, as an x
  const T = at(0, pad) // top edge, as a y
  const B = at(100, -pad) // bottom edge, as a y
  const Rc = at(100, -pad - chamfer) // where the chamfer leaves the right or bottom edge
  const Lc = at(0, pad + chamfer) // where the chamfer leaves the left or top edge
  if (!chamfer) {
    return [side(L, T, R, T, 0, -1), side(R, T, R, B, 1, 0), side(R, B, L, B, 0, 1), side(L, B, L, T, -1, 0)]
  }
  if (corner === 1) {
    return [
      side(L, T, Rc, T, 0, -1),
      cut(Rc, T, R, Lc, d, -d),
      side(R, Lc, R, B, 1, 0),
      side(R, B, L, B, 0, 1),
      side(L, B, L, T, -1, 0),
    ]
  }
  if (corner === 2) {
    return [
      side(L, T, R, T, 0, -1),
      side(R, T, R, Rc, 1, 0),
      cut(R, Rc, Rc, B, d, d),
      side(Rc, B, L, B, 0, 1),
      side(L, B, L, T, -1, 0),
    ]
  }
  return [
    side(L, T, R, T, 0, -1),
    side(R, T, R, B, 1, 0),
    side(R, B, Lc, B, 0, 1),
    cut(Lc, B, L, Rc, -d, d),
    side(L, Rc, L, T, -1, 0),
  ]
}

/** The vertices of one tear, before formatting. */
export function tornVertices({ seed, amp = 2.4, chamfer = 0, corner = 1, pointsPerSide = 80 }: TearOptions): Vertex[] {
  // Outward excursions reach 1.275 × amp (|noise| ≤ 1 plus half the jitter); the inset pad = amp + 1.2
  // contains them only while amp ≤ 4.3. Beyond that a vertex lands outside the box and coordToCss emits a
  // bare negative length without complaint.
  if (!(amp <= 4.3)) throw new RangeError(`amp ${amp} exceeds the 4.3px the inset can contain`)
  // Fewer than three points per side yields polygon() with too few vertices to clip anything.
  if (!(pointsPerSide >= 3)) throw new RangeError(`pointsPerSide ${pointsPerSide} is below 3`)
  // Inset so the outward excursions (at most 1.275 × amp) stay inside the box.
  const pad = amp + 1.2
  const segments = perimeter(pad, chamfer, corner, pointsPerSide)
  const noise = periodicNoise(seed, [9, 19, 41, 83])
  const rnd = mulberry32(seed * 131 + 5)
  const vertices: Vertex[] = []
  let sideIndex = 0
  for (const s of segments) {
    for (let j = 0; j < s.points; j++) {
      const u = j / s.points
      // t walks 0 → 1 over the four sides; a chamfer's points share the corner they cut and are damped anyway.
      const t = s.clean ? sideIndex / 4 : (sideIndex + u) / 4
      let o = noise(t) * amp + (rnd() - 0.5) * amp * 0.55
      if (rnd() < 0.035) o -= amp * (0.8 + rnd() * 1.1) // the odd deeper fibre nick, always inward
      if (s.clean) o *= 0.08
      const x = lerp(s.x0, s.x1, u)
      const y = lerp(s.y0, s.y1, u)
      vertices.push({ x: { pct: x.pct, px: x.px + s.nx * o }, y: { pct: y.pct, px: y.px + s.ny * o } })
    }
    if (!s.clean) sideIndex++
  }
  return vertices
}

const trim = (n: number, digits: number): string => {
  const fixed = n.toFixed(digits)
  const trimmed = fixed.includes('.') ? fixed.replace(/0+$/, '').replace(/\.$/, '') : fixed
  return trimmed === '-0' ? '0' : trimmed
}

/** `calc(A% + Bpx)`, collapsing to `A%` or `Bpx` when the other term is zero. */
export function coordToCss({ pct, px }: Coord): string {
  const p = trim(pct, 2)
  const b = trim(px, 1)
  if (b === '0') return `${p}%`
  if (p === '0') return `${b}px`
  return b.startsWith('-') ? `calc(${p}% - ${b.slice(1)}px)` : `calc(${p}% + ${b}px)`
}

/** A CSS `polygon()` for `clip-path`. */
export function tornPolygon(options: TearOptions): string {
  return `polygon(${tornVertices(options)
    .map((v) => `${coordToCss(v.x)} ${coordToCss(v.y)}`)
    .join(', ')})`
}

/** Resolves a coordinate for a concrete box size; for tests and tooling. */
export const resolveCoord = ({ pct, px }: Coord, size: number): number => (pct / 100) * size + px
