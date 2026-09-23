import { describe, expect, it } from 'vitest'
import { listTearFor, sheetTearFor, tileTearFor, washFor } from '~/Utilities/BrandColor'
import { WASHES } from '~/Types/DesignSystem'

describe('washFor', () => {
  it('is deterministic and always one of the eight washes', () => {
    expect(washFor('Brown Butter Gnocchi')).toBe(washFor('Brown Butter Gnocchi'))
    for (const seed of ['a', 'gnocchi', 'Sunday Roast Chicken', '']) expect(WASHES).toContain(washFor(seed))
  })

  it('spreads different inputs across more than one wash', () => {
    const washes = new Set(['Mocha', 'Latte', 'Espresso', 'Vanilla', 'Caramel'].map(washFor))
    expect(washes.size).toBeGreaterThan(1)
  })
})

describe('listTearFor', () => {
  it('cycles the six sheet tears from the list index, so neighbours never share one', () => {
    expect([0, 1, 2, 3, 4, 5].map(listTearFor)).toEqual([1, 2, 3, 4, 5, 6])
    expect(listTearFor(6)).toBe(1)
    expect(listTearFor(13)).toBe(2)
  })
})

describe('tears from a seed', () => {
  it('stay within the preset ranges', () => {
    for (const seed of ['a', 'b', 'c', 'd', 'e', 'f', 'g']) {
      expect(sheetTearFor(seed)).toBeGreaterThanOrEqual(1)
      expect(sheetTearFor(seed)).toBeLessThanOrEqual(6)
      expect(tileTearFor(seed)).toBeGreaterThanOrEqual(1)
      expect(tileTearFor(seed)).toBeLessThanOrEqual(3)
    }
  })
})
