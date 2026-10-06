export const MAX_TIME = 60

export type RecipeSortKey = 'relevant' | 'rated' | 'variants' | 'recent'
export type RecipeViewMode = 'grid' | 'list'

export interface RecipeFilterState {
  query: string
  categories: string[]
  diets: string[]
  styles: string[]
  timeMin: number
  timeMax: number
  sort: RecipeSortKey
}

export interface RecipeSearchState extends RecipeFilterState {
  view: RecipeViewMode
}

export interface FilterChip {
  label: string
  kind: 'query' | 'category' | 'diet' | 'style' | 'time'
  value?: string
}

export function defaultState(): RecipeSearchState {
  return { query: '', categories: [], diets: [], styles: [], timeMin: 0, timeMax: MAX_TIME, sort: 'relevant', view: 'grid' }
}

export function isTimeActive(min: number, max: number): boolean {
  return min > 0 || max < MAX_TIME
}

export type ResourceResolver = (key: string) => string

export function timeRangeLabel(min: number, max: number, t: ResourceResolver): string {
  if (!isTimeActive(min, max)) {
    return 'Any'
  }
  const unit = t('Min')
  if (max >= MAX_TIME) {
    return `${min} ${unit} ${t('OrMore')}`
  }
  if (min <= 0) {
    return `${max} ${unit} ${t('OrLess')}`
  }
  return `${min}–${max} ${unit}`
}

export function activeFilterCount(s: RecipeFilterState): number {
  return s.categories.length + s.diets.length + s.styles.length + (isTimeActive(s.timeMin, s.timeMax) ? 1 : 0)
}

export function chipsFor(s: RecipeFilterState, t: ResourceResolver): FilterChip[] {
  const chips: FilterChip[] = []
  if (s.query.trim()) {
    chips.push({ label: `“${s.query.trim()}”`, kind: 'query' })
  }
  s.categories.forEach((c) => chips.push({ label: c, kind: 'category', value: c }))
  s.diets.forEach((d) => chips.push({ label: d, kind: 'diet', value: d }))
  s.styles.forEach((style) => chips.push({ label: style, kind: 'style', value: style }))
  if (isTimeActive(s.timeMin, s.timeMax)) {
    chips.push({ label: timeRangeLabel(s.timeMin, s.timeMax, t), kind: 'time' })
  }
  return chips
}

export function filterParams(s: RecipeFilterState): URLSearchParams {
  const p = new URLSearchParams()
  if (s.query.trim()) {
    p.set('query', s.query.trim())
  }
  s.categories.forEach((c) => p.append('category', c))
  s.diets.forEach((d) => p.append('diet', d))
  s.styles.forEach((style) => p.append('style', style))
  if (s.timeMin > 0) {
    p.set('timeMin', String(s.timeMin))
  }
  if (s.timeMax < MAX_TIME) {
    p.set('timeMax', String(s.timeMax))
  }
  if (s.sort !== 'relevant') {
    p.set('sort', s.sort)
  }
  return p
}

export function buildSearchParams(s: RecipeFilterState, page: number, pageSize: number): URLSearchParams {
  const p = filterParams(s)
  p.set('page', String(page))
  p.set('pageSize', String(pageSize))
  return p
}
