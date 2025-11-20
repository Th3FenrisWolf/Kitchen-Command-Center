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
