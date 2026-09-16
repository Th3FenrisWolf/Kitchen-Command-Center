import { describe, expect, it } from 'vitest'
import { hatch, linePath, rectPath } from '~/Ink/inkPaths'

const commands = (d: string, letter: string) => (d.match(new RegExp(letter, 'g')) ?? []).length

describe('rectPath', () => {
  it('draws a closed cubic spline', () => {
    const d = rectPath(200, 120, 16, 2.1, 7)

    expect(d.startsWith('M')).toBe(true)
    expect(d.endsWith('Z')).toBe(true)
    // One curve per sampled point; the sample count is clamped to [26, 260].
    expect(commands(d, 'C')).toBeGreaterThanOrEqual(26)
    expect(commands(d, 'C')).toBeLessThanOrEqual(260)
  })

  it('clamps the sample count at both ends', () => {
    // The box above lands at 81 curves, nowhere near either bound, so the clamp needs boxes that reach it.
    expect(commands(rectPath(20, 20, 4, 1, 1), 'C')).toBe(26)
    expect(commands(rectPath(4000, 3000, 16, 2, 1), 'C')).toBe(260)
  })

  it('is deterministic for a seed and different across seeds', () => {
    expect(rectPath(200, 120, 16, 2.1, 7)).toBe(rectPath(200, 120, 16, 2.1, 7))
    expect(rectPath(200, 120, 16, 2.1, 7)).not.toBe(rectPath(200, 120, 16, 2.1, 8))
  })

  it('keeps every point inside the box', () => {
    const d = rectPath(200, 120, 16, 2.1, 7)
    const numbers = d.match(/-?\d+(?:\.\d+)?/g)!.map(Number)
    const xs = numbers.filter((_, i) => i % 2 === 0)
    const ys = numbers.filter((_, i) => i % 2 === 1)

    expect(Math.min(...xs)).toBeGreaterThanOrEqual(0)
    expect(Math.max(...xs)).toBeLessThanOrEqual(200)
    expect(Math.min(...ys)).toBeGreaterThanOrEqual(0)
    expect(Math.max(...ys)).toBeLessThanOrEqual(120)
  })
})

describe('linePath', () => {
  it('starts and ends on its endpoints, wandering only in between', () => {
    const d = linePath(0, 0, 100, 0, 1.5, 3)
    const numbers = d.match(/-?\d+(?:\.\d+)?/g)!.map(Number)

    // The wobble envelope is sin(πt), so both ends land exactly (bar floating-point dust, hence closeTo).
    expect(numbers.slice(0, 2)).toEqual([0, 0])
    expect(numbers.at(-2)).toBeCloseTo(100, 2)
    expect(Math.abs(numbers.at(-1)!)).toBeLessThan(0.01)

    // Landing on both ends is also true of a straight line, so pin the excursion the envelope allows between.
    const offsets = numbers.filter((_, i) => i % 2 === 1).map(Math.abs)
    expect(Math.max(...offsets)).toBeGreaterThan(0.5)
  })
})

describe('hatch', () => {
  it('rakes one stroke per gap across the box plus its lean', () => {
    const strokes = hatch(200, 26, { gap: 6.5, lean: 0.95, amp: 0.8, seed: 3 })

    // ceil((200 + 26 * 0.95) / 6.5) strokes start at -h*lean and step by gap until x reaches w.
    expect(strokes).toHaveLength(35)
    expect(strokes.every((d) => d.startsWith('M'))).toBe(true)
  })

  it('falls back to the default gap rather than never advancing', () => {
    // A gap that cannot step the loop hangs the caller instead of drawing wrongly, so it is worth pinning.
    expect(hatch(200, 26, { gap: 0 })).toHaveLength(35)
    expect(hatch(200, 26, { gap: Number.NaN })).toHaveLength(35)
  })
})
