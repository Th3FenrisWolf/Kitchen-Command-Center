export const MAX_TIME = 60

export type RecipeSortKey = 'relevant' | 'rated' | 'variants' | 'recent'
export type RecipeViewMode = 'grid' | 'list'

export interface RecipeSearchState {
  query: string
  categories: string[]
  diets: string[]
  timeMin: number
  timeMax: number
  sort: RecipeSortKey
  view: RecipeViewMode
}

export interface FilterChip {
  label: string
  kind: 'query' | 'category' | 'diet' | 'time'
  value?: string
}

export function defaultState(): RecipeSearchState {
  return { query: '', categories: [], diets: [], timeMin: 0, timeMax: MAX_TIME, sort: 'relevant', view: 'grid' }
}

export function isTimeActive(min: number, max: number): boolean {
  return min > 0 || max < MAX_TIME
}

export function timeRangeLabel(min: number, max: number): string {
  if (!isTimeActive(min, max)) {
    return 'Any'
  }
  const upper = max >= MAX_TIME ? `${MAX_TIME}+` : `${max}`
  return `${min}–${upper} min`
}

export function activeFilterCount(s: RecipeSearchState): number {
  return s.categories.length + s.diets.length + (isTimeActive(s.timeMin, s.timeMax) ? 1 : 0)
}

export function chipsFor(s: RecipeSearchState): FilterChip[] {
  const chips: FilterChip[] = []
  if (s.query.trim()) {
    chips.push({ label: `“${s.query.trim()}”`, kind: 'query' })
  }
  s.categories.forEach((c) => chips.push({ label: c, kind: 'category', value: c }))
  s.diets.forEach((d) => chips.push({ label: d, kind: 'diet', value: d }))
  if (isTimeActive(s.timeMin, s.timeMax)) {
    chips.push({ label: timeRangeLabel(s.timeMin, s.timeMax), kind: 'time' })
  }
  return chips
}

export function buildSearchParams(s: RecipeSearchState, page: number, pageSize: number): URLSearchParams {
  const p = new URLSearchParams()
  if (s.query.trim()) {
    p.set('q', s.query.trim())
  }
  s.categories.forEach((c) => p.append('category', c))
  s.diets.forEach((d) => p.append('diet', d))
  if (isTimeActive(s.timeMin, s.timeMax)) {
    p.set('timeMin', String(s.timeMin))
    p.set('timeMax', String(s.timeMax))
  }
  if (s.sort !== 'relevant') {
    p.set('sort', s.sort)
  }
  p.set('page', String(page))
  p.set('pageSize', String(pageSize))
  return p
}
