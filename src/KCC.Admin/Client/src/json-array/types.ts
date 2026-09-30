export interface Ingredient {
  name: string
  quantity: number | null
  unit: string
  isEyeballed: boolean
}

export interface Instruction {
  step: number
  text: string
}
