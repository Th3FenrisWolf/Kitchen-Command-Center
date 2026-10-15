import { postForm, type ApiAborted, type ApiResult } from '~/Utilities/Api'
import type { WizardMode } from './recipeWizardFlow'
import type { Mark } from './wizardValues'

export const READ_URL = '/api/recipe-imports/read'

export interface ReadRequest {
  flow: WizardMode
  recipeKey?: string
  photos: Blob[]
}

export interface ReadValue<T> {
  value: T
  mark: Mark | null
}

export interface ReadIngredient {
  name: string
  quantity: number | null
  unit: string
  isEyeballed: boolean
  mark: Mark | null
}

export interface ReadInstruction {
  text: string
  mark: Mark | null
}

export interface ReadMatch {
  recipeKey: string
  recipeName: string
  reason: string
}

export interface PossibleDuplicate {
  variantKey: string
  variantName: string
  recipeName: string
}

export interface RecipeChoice {
  key: string
  name: string
}

export interface ReadResponse {
  outcome: 'recipe' | 'noRecipe'
  recipe: {
    name: ReadValue<string>
    description: ReadValue<string>
  }
  variant: {
    name: ReadValue<string>
    description: ReadValue<string>
    prepTime: ReadValue<number | null>
    cookTime: ReadValue<number | null>
    servings: ReadValue<number | null>
  }
  ingredients: ReadIngredient[]
  instructions: ReadInstruction[]
  match: ReadMatch | null
  possibleDuplicate: PossibleDuplicate | null
  otherRecipes: string[]
  recipes: RecipeChoice[] | null
}

export type ReadResult = ApiResult<ReadResponse> | ApiAborted

export function readRecipe({ flow, recipeKey, photos }: ReadRequest, signal: AbortSignal): Promise<ReadResult> {
  const form = new FormData()
  photos.forEach((photo, index) => form.append('photos', photo, `photo-${index + 1}.jpg`))
  form.append('flow', flow)
  if (recipeKey) form.append('recipeKey', recipeKey)
  return postForm<ReadResponse>(READ_URL, form, signal)
}
