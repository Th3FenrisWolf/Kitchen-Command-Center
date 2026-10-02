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
  saturatedFatG?: number | null
  fiberG?: number | null
  sugarG?: number | null
  sodiumMg?: number | null
}

export interface SiblingVariant {
  name: string
  slug: string
  icon?: string
  rating: number
  totalTime: number
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
  distribution: number[]
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

export interface RecipeSearchHit {
  name: string
  slug: string
  icon?: string
  category?: string
  startedBy?: string
  tags: string[]
  averageRating: number | null
  reviewCount: number
  variantCount: number
  fastestTime: number
}

export interface RecipeFacets {
  category: Record<string, number>
  diet: Record<string, number>
}

export interface RecipeSearchResponse {
  total: number
  page: number
  pageSize: number
  results: RecipeSearchHit[]
  facets: RecipeFacets
  spotlight: RecipeSearchHit | null
}
