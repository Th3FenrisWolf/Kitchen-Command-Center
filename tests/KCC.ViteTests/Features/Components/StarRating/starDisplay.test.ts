import { describe, expect, it } from 'vitest'
import { formatRating, starStates } from '~/Components/StarRating/starDisplay'

describe('starDisplay', () => {
  describe('starStates', () => {
    it('returns one entry per star (default max 5)', () => {
      expect(starStates(0)).toHaveLength(5)
      expect(starStates(3, 10)).toHaveLength(10)
    })

    it('fills whole stars and leaves the rest empty', () => {
      expect(starStates(3)).toEqual(['full', 'full', 'full', 'empty', 'empty'])
    })

    it('uses a half star at the .5 boundary', () => {
      expect(starStates(3.5)).toEqual(['full', 'full', 'full', 'half', 'empty'])
    })

    it('rounds to the nearest half star', () => {
      expect(starStates(3.7)).toEqual(['full', 'full', 'full', 'half', 'empty'])
      expect(starStates(3.3)).toEqual(['full', 'full', 'full', 'half', 'empty'])
    })

    it('clamps out-of-range values', () => {
      expect(starStates(-2)).toEqual(['empty', 'empty', 'empty', 'empty', 'empty'])
      expect(starStates(99)).toEqual(['full', 'full', 'full', 'full', 'full'])
    })
  })

  describe('formatRating', () => {
    it('formats to one decimal place', () => {
      expect(formatRating(4)).toBe('4.0')
      expect(formatRating(4.25)).toBe('4.3')
    })
  })
})
