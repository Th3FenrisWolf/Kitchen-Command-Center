import { describe, expect, it } from 'vitest'
import type { VariantSummary } from '~/Types/Recipe'
import { averageMinutes, contributorCount, featuredVariant } from '~/Components/RecipeDetail/variantStats'

const v = (over: Partial<VariantSummary> = {}): VariantSummary => ({
  name: 'Variant',
  description: '',
  slug: '/v',
  tags: [],
  authorName: '',
  totalTime: 30,
  publishedDate: '2026-01-01T00:00:00Z',
  ...over,
})

describe('variantStats', () => {
  describe('featuredVariant', () => {
    it('returns the newest variant', () => {
      const list = [
        v({ name: 'Old', publishedDate: '2026-01-01T00:00:00Z' }),
        v({ name: 'New', publishedDate: '2026-06-01T00:00:00Z' }),
      ]
      expect(featuredVariant(list)?.name).toBe('New')
    })

    it('returns undefined for an empty list', () => {
      expect(featuredVariant([])).toBeUndefined()
    })
  })

  describe('averageMinutes', () => {
    it('returns null for an empty list', () => {
      expect(averageMinutes([])).toBeNull()
    })

    it('rounds the mean total time', () => {
      expect(averageMinutes([v({ totalTime: 10 }), v({ totalTime: 21 })])).toBe(16)
    })

    it('averages a single variant to itself', () => {
      expect(averageMinutes([v({ totalTime: 42 })])).toBe(42)
    })
  })

  describe('contributorCount', () => {
    it('counts distinct author names', () => {
      const list = [v({ authorName: 'Ana' }), v({ authorName: 'Bo' }), v({ authorName: 'Ana' })]
      expect(contributorCount(list)).toBe(2)
    })

    it('ignores blank and whitespace-only names', () => {
      const list = [v({ authorName: '' }), v({ authorName: '  ' }), v({ authorName: 'Ana' })]
      expect(contributorCount(list)).toBe(1)
    })

    it('returns 0 for an empty list', () => {
      expect(contributorCount([])).toBe(0)
    })
  })
})
