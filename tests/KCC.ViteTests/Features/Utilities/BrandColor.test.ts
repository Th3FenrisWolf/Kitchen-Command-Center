import { describe, expect, it } from 'vitest'
import { backgroundColorFor } from '~/Utilities/BrandColor'
import { BRAND_BACKGROUND_COLORS } from '~/Types/DesignSystem'

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
