import { describe, expect, it } from 'vitest'
import type { Nutrition } from '~/Types/Recipe'
import { buildNutritionRows, hasNutrition } from '~/Components/VariantDetail/variantNutritionRows'

const labels = {
  calories: 'Calories',
  proteinG: 'Protein',
  carbsG: 'Carbs',
  fatG: 'Fat',
  saturatedFatG: 'Saturated Fat',
  fiberG: 'Fiber',
  sugarG: 'Sugar',
  sodiumMg: 'Sodium',
}

const full: Nutrition = {
  calories: 520,
  proteinG: 24,
  carbsG: 60,
  fatG: 18,
  saturatedFatG: 6,
  fiberG: 5,
  sugarG: 8,
  sodiumMg: 740,
}

describe('variantNutritionRows', () => {
  describe('hasNutrition', () => {
    it('is true when any value is present', () => {
      expect(hasNutrition({ calories: 100 })).toBe(true)
    })

    it('treats zero as a real value', () => {
      expect(hasNutrition({ sugarG: 0 })).toBe(true)
    })

    it('is false when every field is null or undefined', () => {
      expect(hasNutrition({})).toBe(false)
      expect(hasNutrition({ calories: null, proteinG: undefined })).toBe(false)
    })
  })

  describe('buildNutritionRows', () => {
    it('returns all eight rows in canonical order when full', () => {
      const rows = buildNutritionRows(full, labels)
      expect(rows.map((r) => r.key)).toEqual([
        'calories',
        'proteinG',
        'carbsG',
        'fatG',
        'saturatedFatG',
        'fiberG',
        'sugarG',
        'sodiumMg',
      ])
      expect(rows[0]).toEqual({ key: 'calories', label: 'Calories', value: 520, unit: '' })
      expect(rows[1]).toEqual({ key: 'proteinG', label: 'Protein', value: 24, unit: 'g' })
      expect(rows[4]).toEqual({ key: 'saturatedFatG', label: 'Saturated Fat', value: 6, unit: 'g' })
      expect(rows[7]).toEqual({ key: 'sodiumMg', label: 'Sodium', value: 740, unit: 'mg' })
    })

    it('omits fields that are null or undefined (partial)', () => {
      const rows = buildNutritionRows({ calories: 300, proteinG: null, carbsG: 40, fatG: undefined }, labels)
      expect(rows.map((r) => r.key)).toEqual(['calories', 'carbsG'])
    })

    it('keeps fields whose value is zero', () => {
      const rows = buildNutritionRows({ calories: 0, fatG: 0 }, labels)
      expect(rows.map((r) => r.key)).toEqual(['calories', 'fatG'])
      expect(rows[0].value).toBe(0)
    })

    it('returns an empty array when nothing is provided', () => {
      expect(buildNutritionRows({}, labels)).toEqual([])
    })
  })
})
