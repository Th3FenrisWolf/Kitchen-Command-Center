import type { Ingredient, Instruction } from '~/Types/Recipe'

/** An ingredient counts toward the variant once it has a non-blank name. */
export const hasName = (ingredient: Ingredient): boolean => ingredient.name.trim() !== ''

/** An instruction counts toward the variant once it has non-blank text. */
export const hasText = (instruction: Instruction): boolean => instruction.text.trim() !== ''

export const validIngredients = (list: Ingredient[]): Ingredient[] => list.filter(hasName)

export const validInstructions = (list: Instruction[]): Instruction[] => list.filter(hasText)

/** Resource-string key for the step counter: singular only for exactly one step. */
export const stepLabelKey = (count: number): 'Step' | 'Steps' => (count === 1 ? 'Step' : 'Steps')
