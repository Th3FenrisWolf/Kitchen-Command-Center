import { recipeWizardKeys } from './recipeWizardKeys'

export type TagKind = 'Diet' | 'Style'

export interface CategoryChoice {
  key: string
  name: string
}

export interface TagChoice {
  key: string
  name: string
  kind: TagKind
}

export interface DetailsChoices {
  categories: CategoryChoice[]
  tags: TagChoice[]
}

export const DIFFICULTIES = ['Easy', 'Medium', 'Hard'] as const

export type Difficulty = (typeof DIFFICULTIES)[number]

export const NUTRITION_FIELDS = [
  { field: 'calories', label: recipeWizardKeys.Calories, unit: recipeWizardKeys.Kcal, limit: 10000 },
  { field: 'proteinG', label: recipeWizardKeys.Protein, unit: recipeWizardKeys.Grams, limit: 1000 },
  { field: 'carbsG', label: recipeWizardKeys.Carbohydrates, unit: recipeWizardKeys.Grams, limit: 1000 },
  { field: 'fatG', label: recipeWizardKeys.Fat, unit: recipeWizardKeys.Grams, limit: 1000 },
  { field: 'saturatedFatG', label: recipeWizardKeys.SaturatedFat, unit: recipeWizardKeys.Grams, limit: 1000 },
  { field: 'fiberG', label: recipeWizardKeys.Fiber, unit: recipeWizardKeys.Grams, limit: 1000 },
  { field: 'sugarG', label: recipeWizardKeys.Sugar, unit: recipeWizardKeys.Grams, limit: 1000 },
  { field: 'sodiumMg', label: recipeWizardKeys.Sodium, unit: recipeWizardKeys.Milligrams, limit: 50000 },
] as const

export type NutritionField = (typeof NUTRITION_FIELDS)[number]['field']

export type NutritionTexts = Record<NutritionField, string>

export type NutritionAmounts = Record<NutritionField, number | null>

export const nutritionLimit = (field: NutritionField): number =>
  NUTRITION_FIELDS.find((entry) => entry.field === field)!.limit

export const nutritionAmount = (text: string): number | null => (text.trim() === '' ? null : Number(text.trim()))

export function nutritionState(field: NutritionField, text: string): 'valid' | 'invalid' {
  const typed = text.trim()
  if (typed === '') return 'valid'
  return /^\d+$/.test(typed) && Number(typed) <= nutritionLimit(field) ? 'valid' : 'invalid'
}

export const nutritionRequest = (texts: NutritionTexts): NutritionAmounts =>
  Object.fromEntries(NUTRITION_FIELDS.map(({ field }) => [field, nutritionAmount(texts[field])])) as NutritionAmounts

export const orderTags = (keys: string[], { tags }: DetailsChoices): string[] =>
  tags.map(({ key }) => key).filter((key) => keys.includes(key))

export const keepAvailable = (
  { category, tags }: { category: string | null; tags: string[] },
  choices: DetailsChoices,
): { category: string | null; tags: string[] } => ({
  category: choices.categories.some(({ key }) => key === category) ? category : null,
  tags: orderTags(tags, choices),
})
