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

export interface VariantSummary {
  name: string
  description: string
  slug: string
  image?: string
  icon?: string
  authorName?: string
  tags: string[]
  totalTime: number
  publishedDate: string
}

export interface Breadcrumb {
  linkText: string
  url: string
}

export interface Nutrition {
  calories?: number | null
  proteinG?: number | null
  carbsG?: number | null
  fatG?: number | null
  fiberG?: number | null
  sugarG?: number | null
  sodiumMg?: number | null
}

export interface RecipeVariant {
  name: string
  description: string
  images: string[]
  prepTime?: number
  cookTime?: number
  servings?: number
  difficulty?: string
  calories?: number | null
  proteinG?: number | null
  carbsG?: number | null
  fatG?: number | null
  fiberG?: number | null
  sugarG?: number | null
  sodiumMg?: number | null
  tags: string[]
  ingredients: Ingredient[]
  instructions: Instruction[]
  slug: string
  recipeSlug: string
  recipeName: string
  siblingVariants: VariantSummary[]
}

export interface RecipeSummary {
  name: string
  description: string
  image?: string
  icon?: string
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
  variants: VariantSummary[]
}

export interface SiblingVariant {
  name: string
  slug: string
  icon?: string
}
