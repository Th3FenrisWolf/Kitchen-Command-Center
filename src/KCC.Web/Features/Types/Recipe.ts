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

export interface RecipeVariantSummary {
  name: string
  description: string
  slug: string
  image?: string
  tags: string[]
}

export interface RecipeVariant {
  name: string
  description: string
  images: string[]
  prepTime?: number
  cookTime?: number
  servings?: number
  tags: string[]
  ingredients: Ingredient[]
  instructions: Instruction[]
  slug: string
  recipeSlug: string
  recipeName: string
  siblingVariants: RecipeVariantSummary[]
}

export interface RecipeSummary {
  name: string
  description: string
  image?: string
  category?: string
  slug: string
  variantCount: number
}

export interface RecipeDetail {
  name: string
  description: string
  image?: string
  category?: string
  slug: string
  variants: RecipeVariantSummary[]
}
