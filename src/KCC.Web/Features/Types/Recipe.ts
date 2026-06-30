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
  averageRating?: number
  reviewCount?: number
  cookedCount?: number
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
  timesCooked?: number
}

export interface SiblingVariant {
  name: string
  slug: string
  icon?: string
}

export interface Review {
  authorName: string
  rating: number
  text?: string
  created: string
  isMine: boolean
}

export interface MyReview {
  rating: number
  text?: string
}

export interface ReviewsResponse {
  average: number
  count: number
  total: number
  page: number
  pageSize: number
  reviews: Review[]
  myReview: MyReview | null
}

export interface CookNote {
  id: number
  authorName: string
  text: string
  created: string
  isMine: boolean
}

export interface CookNotesResponse {
  total: number
  page: number
  pageSize: number
  notes: CookNote[]
}
