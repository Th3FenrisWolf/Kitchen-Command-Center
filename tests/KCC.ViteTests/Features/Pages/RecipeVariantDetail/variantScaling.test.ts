import { describe, expect, it } from 'vitest'
import type { Ingredient } from '~/Types/Recipe'
import { formatIngredientAmount, formatQuantity, scaleQuantity } from '~/Components/VariantDetail/variantScaling'

const ing = (over: Partial<Ingredient> = {}): Ingredient => ({
  name: 'flour',
  quantity: 2,
  unit: 'cups',
  isEyeballed: false,
  ...over,
})

describe('variantScaling', () => {
  describe('scaleQuantity', () => {
    it('scales by the serving ratio', () => {
      expect(scaleQuantity(2, 24, 48)).toBe(4)
      expect(scaleQuantity(1, 24, 12)).toBe(0.5)
    })

    it('returns the quantity unchanged when base servings is missing or zero', () => {
      expect(scaleQuantity(2, 0, 48)).toBe(2)
      expect(scaleQuantity(2, undefined as unknown as number, 48)).toBe(2)
    })
  })

  describe('formatQuantity', () => {
    it('formats whole numbers', () => {
      expect(formatQuantity(2)).toBe('2')
    })

    it('formats common fractions with glyphs', () => {
      expect(formatQuantity(0.5)).toBe('½')
      expect(formatQuantity(0.25)).toBe('¼')
      expect(formatQuantity(0.75)).toBe('¾')
    })

    it('combines whole numbers and fractions', () => {
      expect(formatQuantity(1.5)).toBe('1½')
      expect(formatQuantity(2.25)).toBe('2¼')
    })
  })

  describe('formatIngredientAmount', () => {
    it('returns an empty string for eyeballed ingredients', () => {
      expect(formatIngredientAmount(ing({ isEyeballed: true }), 24, 24)).toBe('')
    })

    it('returns an empty string when there is no quantity', () => {
      expect(formatIngredientAmount(ing({ quantity: undefined }), 24, 24)).toBe('')
    })

    it('scales and appends the unit', () => {
      expect(formatIngredientAmount(ing({ quantity: 2, unit: 'cups' }), 24, 48)).toBe('4 cups')
      expect(formatIngredientAmount(ing({ quantity: 0.5, unit: 'cup' }), 24, 12)).toBe('¼ cup')
    })

    it('omits a trailing space when there is no unit', () => {
      expect(formatIngredientAmount(ing({ quantity: 3, unit: '' }), 24, 24)).toBe('3')
    })
  })
})
