import type { Nutrition } from '~/Types/Recipe'

/** Canonical display order + unit suffix for each macro. Calories has no unit. */
const FIELDS: ReadonlyArray<{ key: keyof Nutrition; unit: string }> = [
  { key: 'calories', unit: '' },
  { key: 'proteinG', unit: 'g' },
  { key: 'carbsG', unit: 'g' },
  { key: 'fatG', unit: 'g' },
  { key: 'saturatedFatG', unit: 'g' },
  { key: 'fiberG', unit: 'g' },
  { key: 'sugarG', unit: 'g' },
  { key: 'sodiumMg', unit: 'mg' },
]

export interface NutritionRow {
  key: keyof Nutrition
  label: string
  value: number
  unit: string
}

/** A value counts as provided when it is a number (including 0); null/undefined do not. */
function isProvided(value: number | null | undefined): value is number {
  return typeof value === 'number'
}

/** True when at least one macro has a real value (0 included). */
export function hasNutrition(nutrition: Nutrition): boolean {
  return FIELDS.some((f) => isProvided(nutrition[f.key]))
}

/**
 * Build the rows to render, in canonical order, skipping unprovided fields.
 * `labels` maps each field key to its already-localized caption.
 */
export function buildNutritionRows(nutrition: Nutrition, labels: Record<keyof Nutrition, string>): NutritionRow[] {
  const rows: NutritionRow[] = []
  for (const { key, unit } of FIELDS) {
    const value = nutrition[key]
    if (isProvided(value)) {
      rows.push({ key, label: labels[key], value, unit })
    }
  }
  return rows
}
