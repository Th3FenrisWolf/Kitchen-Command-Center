import type { Ingredient, Instruction } from '~/Types/Recipe'
import type { ReadIngredient, ReadResponse } from './recipeImportApi'
import { recipeWizardKeys } from './recipeWizardKeys'

export type MarkKind = 'estimate' | 'unclear' | 'written' | 'translated'

export type MarkCode = 'shortened' | 'outOfRange' | 'translated'

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
}

export interface RecipeRequest {
  recipeName: string
  recipeDescription: string
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
    marks: {},
  }
}

export function valuesFromRead(read: ReadResponse): WizardValues {
  const { recipe, variant, ingredients, instructions } = read
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
    marks: marksFromRead(read),
  }
}

function ingredientFromRead({ name, quantity, unit, isEyeballed, mark }: ReadIngredient): WizardIngredient {
  return withMark(quantity === null ? { name, unit, isEyeballed } : { name, quantity, unit, isEyeballed }, mark)
}

function marksFromRead({ recipe, variant }: ReadResponse): WizardValues['marks'] {
  const marks: [MarkedValue, Mark | null][] = [
    ['recipe.name', recipe.name.mark],
    ['recipe.description', recipe.description.mark],
    ['variant.name', variant.name.mark],
    ['variant.description', variant.description.mark],
    ['variant.prepTime', variant.prepTime.mark],
    ['variant.cookTime', variant.cookTime.mark],
    ['variant.servings', variant.servings.mark],
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
  }
}

export function recipeRequest(values: WizardValues): RecipeRequest {
  return {
    recipeName: values.recipeName.trim(),
    recipeDescription: values.recipeDescription.trim(),
    firstVariant: variantRequest(values),
  }
}
