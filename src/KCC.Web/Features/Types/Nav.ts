export type NavSection = 'recipes' | 'kitchen'

export interface NavRow {
  label: string
  url: string
  count?: number
  icon?: string
  target?: string
}

export interface NavRecipes {
  meals: NavRow[]
  diets: NavRow[]
  quickPicks: NavRow[]
  note?: string
}

export interface KitchenSummary {
  recipes: number
  variants: number
  waiting: number
}

export interface NavMember {
  firstName: string
  memberSince: string
  kitchen?: KitchenSummary
}

export interface NavUrls {
  home: string
  library?: string
  surpriseMe: string
  currentPage: string
  signIn?: string
  register?: string
  newRecipe?: string
  account?: string
  settings?: string
  signOut?: string
}

export interface NavModel {
  currentSection?: NavSection
  recipeTotal: number
  recipes: NavRecipes
  suggestions: string[]
  member?: NavMember
  urls: NavUrls
  labels: Record<string, string>
}
