import { describe, expect, it } from 'vitest'
import { parseDurations } from '~/Pages/VariantDetail/useStepTimers'

describe('useStepTimers / parseDurations', () => {
  it('parses a single minutes duration', () => {
    const specs = parseDurations('Simmer for 5 minutes until thick.')
    expect(specs).toHaveLength(1)
    expect(specs[0]!.seconds).toBe(300)
    expect(specs[0]!.label).toBe('5 minutes')
  })

  it('parses the abbreviated "min" form', () => {
    const specs = parseDurations('Rest 10 min.')
    expect(specs).toHaveLength(1)
    expect(specs[0]!.seconds).toBe(600)
    expect(specs[0]!.label).toBe('10 min')
  })

  it('parses hours and converts to seconds', () => {
    const specs = parseDurations('Bake for 1 hour.')
    expect(specs).toHaveLength(1)
    expect(specs[0]!.seconds).toBe(3600)
    expect(specs[0]!.label).toBe('1 hour')
  })

  it('parses the abbreviated "hr" form and plural hours', () => {
    expect(parseDurations('Chill 2 hrs.')[0]!.seconds).toBe(7200)
    expect(parseDurations('Smoke for 3 hours.')[0]!.seconds).toBe(10800)
  })

  it('uses the upper bound of a range but keeps the original label', () => {
    const specs = parseDurations('Saute for 10-12 minutes.')
    expect(specs).toHaveLength(1)
    expect(specs[0]!.seconds).toBe(720)
    expect(specs[0]!.label).toBe('10-12 minutes')
  })

  it('handles an en-dash range', () => {
    const specs = parseDurations('Bake 25–30 minutes.')
    expect(specs[0]!.seconds).toBe(1800)
    expect(specs[0]!.label).toBe('25–30 minutes')
  })

  it('parses multiple durations from one step', () => {
    const specs = parseDurations('Knead for 8 minutes, then rest 1 hour.')
    expect(specs).toHaveLength(2)
    expect(specs[0]!.seconds).toBe(480)
    expect(specs[1]!.seconds).toBe(3600)
  })

  it('returns an empty array when there is no duration', () => {
    expect(parseDurations('Stir until combined.')).toEqual([])
  })

  it('assigns a stable id per match index', () => {
    const specs = parseDurations('Boil 3 minutes then steep 5 minutes.')
    expect(specs.map((s) => s.id)).toEqual([0, 1])
  })
})
