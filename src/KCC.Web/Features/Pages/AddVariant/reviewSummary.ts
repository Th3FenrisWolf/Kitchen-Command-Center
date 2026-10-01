import type { Ingredient, Instruction } from '~/Types/Recipe'

export const hasName = (ingredient: Ingredient): boolean => ingredient.name.trim() !== ''

export const hasText = (instruction: Instruction): boolean => instruction.text.trim() !== ''

export const validIngredients = (list: Ingredient[]): Ingredient[] => list.filter(hasName)

export const validInstructions = (list: Instruction[]): Instruction[] => list.filter(hasText)

export const stepLabelKey = (count: number): 'Step' | 'Steps' => (count === 1 ? 'Step' : 'Steps')
