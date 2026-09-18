import { describe, expect, it } from 'vitest'
import { backgroundColorFor, sheetTearFor, tileTearFor, washFor } from '~/Utilities/BrandColor'
import { BRAND_BACKGROUND_COLORS, WASHES } from '~/Types/DesignSystem'

describe('backgroundColorFor', () => {
  it('is deterministic for the same input', () => {
    expect(backgroundColorFor('Espresso')).toBe(backgroundColorFor('Espresso'))
  })

  it('always returns a token from the brand palette', () => {
    expect(BRAND_BACKGROUND_COLORS).toContain(backgroundColorFor('Espresso'))
    expect(BRAND_BACKGROUND_COLORS).toContain(backgroundColorFor(''))
    expect(BRAND_BACKGROUND_COLORS).toContain(backgroundColorFor('a long name with spaces'))
  })

  it('spreads different inputs across more than one color', () => {
    const colors = new Set(['Mocha', 'Latte', 'Espresso', 'Vanilla', 'Caramel'].map(backgroundColorFor))
    expect(colors.size).toBeGreaterThan(1)
  })
})

describe('washFor', () => {
  it('is deterministic and always one of the eight washes', () => {
    expect(washFor('Brown Butter Gnocchi')).toBe(washFor('Brown Butter Gnocchi'))
    for (const seed of ['a', 'gnocchi', 'Sunday Roast Chicken', '']) expect(WASHES).toContain(washFor(seed))
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
