import { describe, expect, it } from 'vitest'
import type { RecipeVariantSummary } from '~/Types/Recipe'
import {
  accentForName,
  ACCENTS,
  averageMinutes,
  contributorCount,
  featuredVariant,
  filterVariants,
  sortVariants,
  tagOptions,
} from '~/Pages/RecipeDetail/variantFilters'

const v = (over: Partial<RecipeVariantSummary> = {}): RecipeVariantSummary => ({
  name: 'Variant',
  description: '',
  slug: '/v',
  tags: [],
  authorName: '',
  totalTime: 30,
  publishedDate: '2026-01-01T00:00:00Z',
  ...over,
})

describe('variantFilters', () => {
  describe('tagOptions', () => {
    it('returns All first, then unique sorted tags', () => {
      const list = [v({ tags: ['Baked', 'Chewy'] }), v({ tags: ['Baked', 'Quick'] })]
      expect(tagOptions(list)).toEqual(['All', 'Baked', 'Chewy', 'Quick'])
    })
  })

  describe('filterVariants', () => {
    it('filters by tag', () => {
      const list = [v({ name: 'A', tags: ['Quick'] }), v({ name: 'B', tags: ['Baked'] })]
      const out = filterVariants(list, { search: '', tag: 'Quick', sort: 'newest' })
      expect(out.map((x) => x.name)).toEqual(['A'])
    })

    it('searches across name, author, description and tags', () => {
      const list = [
        v({ name: 'Espresso', authorName: 'Ana' }),
        v({ name: 'Vanilla', description: 'with espresso notes' }),
        v({ name: 'Plain' }),
      ]
      const out = filterVariants(list, { search: 'espresso', tag: 'All', sort: 'newest' })
      expect(out.map((x) => x.name).sort()).toEqual(['Espresso', 'Vanilla'])
    })
  })

  describe('sortVariants', () => {
    it('sorts fastest by ascending total time', () => {
      const list = [v({ name: 'Slow', totalTime: 40 }), v({ name: 'Fast', totalTime: 5 })]
      expect(sortVariants(list, 'fastest').map((x) => x.name)).toEqual(['Fast', 'Slow'])
    })

    it('sorts newest by descending published date', () => {
      const list = [
        v({ name: 'Old', publishedDate: '2026-01-01T00:00:00Z' }),
        v({ name: 'New', publishedDate: '2026-06-01T00:00:00Z' }),
      ]
      expect(sortVariants(list, 'newest').map((x) => x.name)).toEqual(['New', 'Old'])
    })
  })

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

  describe('accentForName', () => {
    it('is deterministic and within the accent set', () => {
      expect(accentForName('Espresso')).toBe(accentForName('Espresso'))
      expect(ACCENTS).toContain(accentForName('Espresso'))
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
