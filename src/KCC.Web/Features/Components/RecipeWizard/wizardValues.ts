import type { Ingredient, Instruction } from '~/Types/Recipe'
import type { ReadDetails, ReadIngredient, ReadResponse } from './recipeImportApi'
import { recipeWizardKeys } from './recipeWizardKeys'
import {
  NUTRITION_FIELDS,
  nutritionRequest,
  type Difficulty,
  type NutritionAmounts,
  type NutritionField,
  type NutritionTexts,
} from './wizardDetails'

export type MarkKind = 'estimate' | 'unclear' | 'written' | 'translated' | 'suggested' | 'calculated'

export type MarkCode = 'shortened' | 'outOfRange' | 'translated' | 'printedAs'

export interface Mark {
  kind: MarkKind
  reason: string | null
  code: MarkCode | null
  arg: string | null
}

export type MarkedValue =
  | 'recipe.name'
  | 'recipe.description'
  | 'variant.name'
  | 'variant.description'
  | 'variant.prepTime'
  | 'variant.cookTime'
  | 'variant.servings'
  | 'recipe.category'
  | 'variant.difficulty'
  | `variant.tag.${string}`
  | `variant.nutrition.${NutritionField}`

export interface WizardIngredient extends Ingredient {
  mark?: Mark
}

export interface WizardInstruction {
  text: string
  mark?: Mark
}

export interface WizardValues {
  recipeName: string
  recipeDescription: string
  variantName: string
  variantDescription: string
  prepTime: number | undefined
  cookTime: number | undefined
  servings: number | undefined
  ingredients: WizardIngredient[]
  instructions: WizardInstruction[]
  category: string | null
  tags: string[]
  difficulty: Difficulty | null
  nutrition: NutritionTexts
  proposedCategory: string
  proposedTags: string
  marks: Partial<Record<MarkedValue, Mark>>
}

export interface RequestIngredient extends Omit<Ingredient, 'quantity'> {
  quantity: number | null
}

export interface VariantRequest {
  variantName: string
  variantDescription: string
  prepTime: number | null
  cookTime: number | null
  servings: number | null
  ingredients: RequestIngredient[]
  instructions: Required<Instruction>[]
  tags: string[]
  difficulty: Difficulty | null
  nutrition: NutritionAmounts
  proposedTags: string | null
}

export interface RecipeRequest {
  recipeName: string
  recipeDescription: string
  category: string | null
  proposedCategory: string | null
  firstVariant: VariantRequest
}

export const emptyIngredient = (): WizardIngredient => ({ name: '', unit: '', isEyeballed: false })

export const emptyInstruction = (): WizardInstruction => ({ text: '' })

export function emptyValues(): WizardValues {
  return {
    recipeName: '',
    recipeDescription: '',
    variantName: '',
    variantDescription: '',
    prepTime: 0,
    cookTime: 0,
    servings: 0,
    ingredients: [emptyIngredient()],
    instructions: [emptyInstruction()],
    category: null,
    tags: [],
    difficulty: null,
    nutrition: Object.fromEntries(NUTRITION_FIELDS.map(({ field }) => [field, ''])) as NutritionTexts,
    proposedCategory: '',
    proposedTags: '',
    marks: {},
  }
}

export function valuesFromRead(read: ReadResponse): WizardValues {
  const { recipe, variant, ingredients, instructions, details } = read
  return {
    recipeName: recipe.name.value,
    recipeDescription: recipe.description.value,
    variantName: variant.name.value,
    variantDescription: variant.description.value,
    prepTime: variant.prepTime.value ?? 0,
    cookTime: variant.cookTime.value ?? 0,
    servings: variant.servings.value ?? 0,
    ingredients: ingredients.length ? ingredients.map(ingredientFromRead) : [emptyIngredient()],
    instructions: instructions.length
      ? instructions.map(({ text, mark }) => withMark({ text }, mark))
      : [emptyInstruction()],
    category: details.category.value,
    tags: details.tags.map(({ key }) => key),
    difficulty: details.difficulty.value,
    nutrition: nutritionFromRead(details.nutrition),
    proposedCategory: '',
    proposedTags: '',
    marks: marksFromRead(read),
  }
}

function ingredientFromRead({ name, quantity, unit, isEyeballed, mark }: ReadIngredient): WizardIngredient {
  return withMark(quantity === null ? { name, unit, isEyeballed } : { name, quantity, unit, isEyeballed }, mark)
}

const nutritionFromRead = (read: ReadDetails['nutrition']): NutritionTexts =>
  Object.fromEntries(NUTRITION_FIELDS.map(({ field }) => [field, read[field].value?.toString() ?? ''])) as NutritionTexts

function marksFromRead({ recipe, variant, details }: ReadResponse): WizardValues['marks'] {
  const marks: [MarkedValue, Mark | null][] = [
    ['recipe.name', recipe.name.mark],
    ['recipe.description', recipe.description.mark],
    ['variant.name', variant.name.mark],
    ['variant.description', variant.description.mark],
    ['variant.prepTime', variant.prepTime.mark],
    ['variant.cookTime', variant.cookTime.mark],
    ['variant.servings', variant.servings.mark],
    ['recipe.category', details.category.mark],
    ['variant.difficulty', details.difficulty.mark],
    ...details.tags.map(({ key, mark }): [MarkedValue, Mark | null] => [`variant.tag.${key}`, mark]),
    ...NUTRITION_FIELDS.map(({ field }): [MarkedValue, Mark | null] => [
      `variant.nutrition.${field}`,
      details.nutrition[field].mark,
    ]),
  ]
  return Object.fromEntries(marks.filter((entry): entry is [MarkedValue, Mark] => Boolean(entry[1])))
}

const withMark = <Row extends object>(row: Row, mark: Mark | null): Row & { mark?: Mark } => (mark ? { ...row, mark } : row)

export const isFilled = (text: string): boolean => text.trim() !== ''

export const hasName = (ingredient: Ingredient): boolean => isFilled(ingredient.name)

export const hasText = (instruction: { text: string }): boolean => isFilled(instruction.text)

export function validIngredients<T extends Ingredient>(list: T[]): T[] {
  return list.filter(hasName)
}

export function validInstructions<T extends { text: string }>(list: T[]): T[] {
  return list.filter(hasText)
}

export const stepLabelKey = (count: number) => (count === 1 ? recipeWizardKeys.Step : recipeWizardKeys.Steps)

export function variantRequest(values: WizardValues): VariantRequest {
  return {
    variantName: values.variantName.trim(),
    variantDescription: values.variantDescription.trim(),
    prepTime: values.prepTime || null,
    cookTime: values.cookTime || null,
    servings: values.servings || null,
    ingredients: validIngredients(values.ingredients).map(({ name, quantity, unit, isEyeballed }) => ({
      name,
      quantity: quantity || null,
      unit,
      isEyeballed,
    })),
    instructions: validInstructions(values.instructions).map(({ text }, index) => ({ step: index + 1, text: text.trim() })),
    tags: values.tags,
    difficulty: values.difficulty,
    nutrition: nutritionRequest(values.nutrition),
    proposedTags: values.proposedTags.trim() || null,
  }
}

export function recipeRequest(values: WizardValues): RecipeRequest {
  return {
    recipeName: values.recipeName.trim(),
    recipeDescription: values.recipeDescription.trim(),
    category: values.category,
    proposedCategory: values.proposedCategory.trim() || null,
    firstVariant: variantRequest(values),
  }
}
