import type { Nutrition } from '~/Types/Recipe'

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

function isProvided(value: number | null | undefined): value is number {
  return typeof value === 'number'
}

export function hasNutrition(nutrition: Nutrition): boolean {
  return FIELDS.some((f) => isProvided(nutrition[f.key]))
}

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
