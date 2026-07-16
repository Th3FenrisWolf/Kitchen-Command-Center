import { describe, expect, it } from 'vitest'
import { MIN_RATING, clampRating, stepRating } from '~/Components/StarRating/ratingInput'

describe('ratingInput', () => {
  it('MIN_RATING is a single half-star', () => {
    expect(MIN_RATING).toBe(0.5)
  })

  describe('clampRating', () => {
    it('rounds to the nearest half star', () => {
      expect(clampRating(3.3)).toBe(3.5)
      expect(clampRating(3.2)).toBe(3)
    })
    it('clamps above max and below the minimum', () => {
      expect(clampRating(10)).toBe(5)
      expect(clampRating(-2)).toBe(0.5)
    })
    it('respects a custom max', () => {
      expect(clampRating(12, 10)).toBe(10)
    })
  })

  describe('stepRating', () => {
    it('moves by the delta in half-star steps', () => {
      expect(stepRating(3, 0.5)).toBe(3.5)
      expect(stepRating(3, -0.5)).toBe(2.5)
    })
    it('clamps at the max and the 0.5 minimum', () => {
      expect(stepRating(5, 0.5)).toBe(5)
      expect(stepRating(0.5, -0.5)).toBe(0.5)
    })
    it('starts from the minimum when nothing is selected yet', () => {
      expect(stepRating(0, 0.5)).toBe(0.5)
      expect(stepRating(0, -0.5)).toBe(0.5)
    })
  })
})
