import { describe, expect, it } from 'vitest'
import { SHEET_TEARS, TILE_TEARS } from '~/Torn/tears'
import { coordToCss, resolveCoord, tornPolygon, tornVertices, type Corner } from '~/Torn/tornPolygon'

const SIZES: [number, number][] = [
  [120, 80],
  [320, 240],
  [1280, 720],
]

describe('tornPolygon', () => {
  it('is deterministic for a seed', () => {
    expect(tornPolygon({ seed: 7 })).toBe(tornPolygon({ seed: 7 }))
    expect(tornPolygon({ seed: 7 })).not.toBe(tornPolygon({ seed: 8 }))
  })

  it('emits four sides of points plus the chamfer', () => {
    expect(tornVertices({ seed: 1, pointsPerSide: 10 })).toHaveLength(40)
    // A 20px chamfer adds round(20 / 4) = 5 points of its own.
    expect(tornVertices({ seed: 1, pointsPerSide: 10, chamfer: 20 })).toHaveLength(45)
  })

  it.each(SIZES)('keeps every vertex inside a %i × %i box', (w, h) => {
    for (const corner of [1, 2, 3] as Corner[]) {
      for (const v of tornVertices({ seed: 5, chamfer: 18, corner })) {
        const x = resolveCoord(v.x, w)
        const y = resolveCoord(v.y, h)
        expect(x).toBeGreaterThanOrEqual(0)
        expect(x).toBeLessThanOrEqual(w)
        expect(y).toBeGreaterThanOrEqual(0)
        expect(y).toBeLessThanOrEqual(h)
      }
    }
  })

  it.each([1, 2, 3] as Corner[])('cuts corner %i and never the top-left', (corner) => {
    const [w, h] = [320, 240]
    const amp = 2.4
    const pad = amp + 1.2
    const points = tornVertices({ seed: 3, amp, chamfer: 20, corner }).map((v) => ({
      x: resolveCoord(v.x, w),
      y: resolveCoord(v.y, h),
    }))
    const near = (x: number, y: number) => points.some((p) => Math.hypot(p.x - x, p.y - y) < amp * 4)
    // The label lives top-left, so that corner is always intact…
    expect(near(pad, pad)).toBe(true)
    // …and the cut corner has no vertex within reach: the chamfer passes 14px inside it.
    const cutAt = { 1: [w - pad, pad], 2: [w - pad, h - pad], 3: [pad, h - pad] }[corner] as [number, number]
    expect(near(...cutAt)).toBe(false)
  })

  it('formats coordinates compactly', () => {
    expect(coordToCss({ pct: 0, px: 3.6 })).toBe('3.6px')
    expect(coordToCss({ pct: 100, px: -3.6 })).toBe('calc(100% - 3.6px)')
    expect(coordToCss({ pct: 12.5, px: 0.02 })).toBe('12.5%')
    expect(coordToCss({ pct: 33.333, px: 1.25 })).toBe('calc(33.33% + 1.3px)')
  })

  it('is a polygon() function', () => {
    const css = tornPolygon({ seed: 2 })
    expect(css.startsWith('polygon(')).toBe(true)
    expect(css.endsWith(')')).toBe(true)
  })

  it.each([...SHEET_TEARS, ...TILE_TEARS].map((p) => [p.name, p] as const))(
    'keeps every vertex of the shipped %s inside boxes from tile to hero size',
    (_name, preset) => {
      for (const [w, h] of [
        [56, 56],
        [320, 240],
        [1280, 400],
      ]) {
        for (const v of tornVertices(preset)) {
          const x = resolveCoord(v.x, w)
          const y = resolveCoord(v.y, h)
          expect(x).toBeGreaterThanOrEqual(0)
          expect(x).toBeLessThanOrEqual(w)
          expect(y).toBeGreaterThanOrEqual(0)
          expect(y).toBeLessThanOrEqual(h)
        }
      }
    },
  )

  it('closes within one edge step plus the jitter at the seam', () => {
    // The loop starts and ends on the left edge; polygon() closes it with a straight chord. The periodic
    // noise makes that chord short, the per-vertex jitter (≤ .55 × amp) is the only non-periodic term.
    for (const preset of [...SHEET_TEARS, ...TILE_TEARS]) {
      const [w, h] = [320, 240]
      const pts = tornVertices(preset)
      const first = pts[0]!
      const last = pts[pts.length - 1]!
      const step = h / preset.pointsPerSide
      const gap = Math.hypot(
        resolveCoord(first.x, w) - resolveCoord(last.x, w),
        resolveCoord(first.y, h) - resolveCoord(last.y, h),
      )
      expect(gap).toBeLessThanOrEqual(step + preset.amp * 0.55 + preset.amp)
    }
  })

  it('refuses amplitudes the inset cannot contain and degenerate point counts', () => {
    expect(() => tornVertices({ seed: 1, amp: 4.4 })).toThrow(/amp/)
    expect(() => tornVertices({ seed: 1, pointsPerSide: 2 })).toThrow(/pointsPerSide/)
    expect(() => tornVertices({ seed: 1, amp: 4.3, pointsPerSide: 3 })).not.toThrow()
  })
})
