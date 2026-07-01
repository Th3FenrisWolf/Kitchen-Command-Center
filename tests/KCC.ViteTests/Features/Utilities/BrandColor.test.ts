import { describe, expect, it } from 'vitest'
import { BRAND_COLORS, brandColorFor } from '~/Utilities/BrandColor'

describe('brandColorFor', () => {
  it('is deterministic for the same input', () => {
    expect(brandColorFor('Espresso')).toBe(brandColorFor('Espresso'))
  })

  it('always returns a token from the brand palette', () => {
    expect(BRAND_COLORS).toContain(brandColorFor('Espresso'))
    expect(BRAND_COLORS).toContain(brandColorFor(''))
    expect(BRAND_COLORS).toContain(brandColorFor('a long name with spaces'))
  })

  it('spreads different inputs across more than one color', () => {
    const colors = new Set(['Mocha', 'Latte', 'Espresso', 'Vanilla', 'Caramel'].map(brandColorFor))
    expect(colors.size).toBeGreaterThan(1)
  })
})
