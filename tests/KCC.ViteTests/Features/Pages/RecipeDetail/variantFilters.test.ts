import { describe, expect, it } from 'vitest'
import type { VariantSummary } from '~/Types/Recipe'
import { filterVariants, sortVariants, tagOptions } from '~/Components/RecipeDetail/variantFilters'

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

describe('variantFilters', () => {
  describe('tagOptions', () => {
    it('returns All first, then unique sorted tags', () => {
      const list = [v({ tags: ['Baked', 'Chewy'] }), v({ tags: ['Baked', 'Quick'] })]
      expect(tagOptions(list)).toEqual(['', 'Baked', 'Chewy', 'Quick'])
    })
  })

  describe('filterVariants', () => {
    it('filters by tag', () => {
      const list = [v({ name: 'A', tags: ['Quick'] }), v({ name: 'B', tags: ['Baked'] })]
      const out = filterVariants(list, {
        search: '',
        tag: 'Quick',
        sort: 'newest',
      })
      expect(out.map((x) => x.name)).toEqual(['A'])
    })

    it('searches across name, author, description and tags', () => {
      const list = [
        v({ name: 'Espresso', authorName: 'Ana' }),
        v({ name: 'Vanilla', description: 'with espresso notes' }),
        v({ name: 'Plain' }),
      ]
      const out = filterVariants(list, {
        search: 'espresso',
        tag: '',
        sort: 'newest',
      })
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

  describe('sortVariants by rating', () => {
    it('orders by averageRating descending with unrated last', () => {
      const list = [
        v({ name: 'Unrated' }),
        v({ name: 'Low', averageRating: 2, reviewCount: 1 }),
        v({ name: 'High', averageRating: 5, reviewCount: 3 }),
      ]
      expect(sortVariants(list, 'rating').map((x) => x.name)).toEqual(['High', 'Low', 'Unrated'])
    })

    it('treats reviewCount 0 as unrated regardless of averageRating', () => {
      const list = [
        v({ name: 'Ghost', averageRating: 4, reviewCount: 0 }),
        v({ name: 'Real', averageRating: 1, reviewCount: 2 }),
      ]
      expect(sortVariants(list, 'rating').map((x) => x.name)).toEqual(['Real', 'Ghost'])
    })
  })
})
