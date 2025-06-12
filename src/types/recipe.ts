import { Timestamp } from 'firebase/firestore'

export interface Ingredient {
  name: string
  quantity?: number
  unit: string
  isEyeballed: boolean
}

export interface Instruction {
  step?: number
  text: string
}

export interface Recipe {
  id?: string
  title: string
  description: string
  ingredients: Ingredient[]
  instructions: Instruction[]
  createdBy: string
  createdOn: Date
}

export interface DBRecipe {
  id?: string
  title: string
  description: string
  ingredients: string
  instructions: string
  createdBy: string
  createdOn: Timestamp
}

export const mapToDBRecipe = (recipe: Recipe): DBRecipe => ({
  id: recipe.id,
  title: recipe.title,
  description: recipe.description,
  ingredients: JSON.stringify(recipe.ingredients),
  instructions: JSON.stringify(recipe.instructions),
  createdBy: recipe.createdBy,
  createdOn: Timestamp.fromDate(recipe.createdOn),
})

export const mapFromDBRecipe = (dbRecipe: DBRecipe | null): Recipe | null =>
  dbRecipe
    ? {
        id: dbRecipe.id,
        title: dbRecipe.title,
        description: dbRecipe.description,
        ingredients: JSON.parse(dbRecipe.ingredients) as Ingredient[],
        instructions: JSON.parse(dbRecipe.instructions) as Instruction[],
        createdBy: dbRecipe.createdBy,
        createdOn: dbRecipe.createdOn.toDate(),
      }
    : null
