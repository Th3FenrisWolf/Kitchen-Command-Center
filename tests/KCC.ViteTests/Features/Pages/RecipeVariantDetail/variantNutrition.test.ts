import { describe, expect, it } from 'vitest'
import type { Nutrition } from '~/Types/Recipe'
import { buildNutritionRows, hasNutrition } from '~/Components/VariantDetail/variantNutrition'

const labels = {
  calories: 'Calories',
  proteinG: 'Protein',
  carbsG: 'Carbs',
  fatG: 'Fat',
  fiberG: 'Fiber',
  sugarG: 'Sugar',
  sodiumMg: 'Sodium',
}

const full: Nutrition = {
  calories: 520,
  proteinG: 24,
  carbsG: 60,
  fatG: 18,
  fiberG: 5,
  sugarG: 8,
  sodiumMg: 740,
}

describe('variantNutrition', () => {
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
    it('returns all seven rows in canonical order when full', () => {
      const rows = buildNutritionRows(full, labels)
      expect(rows.map((r) => r.key)).toEqual([
        'calories',
        'proteinG',
        'carbsG',
        'fatG',
        'fiberG',
        'sugarG',
        'sodiumMg',
      ])
      expect(rows[0]).toEqual({ key: 'calories', label: 'Calories', value: 520, unit: '' })
      expect(rows[1]).toEqual({ key: 'proteinG', label: 'Protein', value: 24, unit: 'g' })
      expect(rows[6]).toEqual({ key: 'sodiumMg', label: 'Sodium', value: 740, unit: 'mg' })
    })

    it('omits fields that are null or undefined (partial)', () => {
      const rows = buildNutritionRows(
        { calories: 300, proteinG: null, carbsG: 40, fatG: undefined },
        labels,
      )
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
