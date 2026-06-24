import type { RecipeVariantSummary } from '~/Types/Recipe'

export type SortKey = 'newest' | 'fastest'
export type ViewMode = 'grid' | 'list'

export interface VariantFilterState {
  search: string
  tag: string
  sort: SortKey
}

export const ALL_TAG = 'All'

export function tagOptions(variants: RecipeVariantSummary[]): string[] {
  const set = new Set<string>()
  variants.forEach((variant) => variant.tags.forEach((tag) => set.add(tag)))
  return [ALL_TAG, ...[...set].sort((a, b) => a.localeCompare(b))]
}

export function sortVariants(variants: RecipeVariantSummary[], sort: SortKey): RecipeVariantSummary[] {
  const list = variants.slice()
  if (sort === 'fastest') {
    list.sort((a, b) => a.totalTime - b.totalTime)
  } else {
    list.sort((a, b) => new Date(b.publishedDate).getTime() - new Date(a.publishedDate).getTime())
  }
  return list
}

export function filterVariants(
  variants: RecipeVariantSummary[],
  state: VariantFilterState,
): RecipeVariantSummary[] {
  let list = variants.slice()
  if (state.tag !== ALL_TAG) {
    list = list.filter((variant) => variant.tags.includes(state.tag))
  }
  const query = state.search.trim().toLowerCase()
  if (query) {
    list = list.filter((variant) =>
      `${variant.name} ${variant.authorName ?? ''} ${variant.description} ${variant.tags.join(' ')}`
        .toLowerCase()
        .includes(query),
    )
  }
  return sortVariants(list, state.sort)
}

export function featuredVariant(variants: RecipeVariantSummary[]): RecipeVariantSummary | undefined {
  if (variants.length === 0) return undefined
  return sortVariants(variants, 'newest')[0]
}

export const ACCENTS = [
  'rosewater', 'flamingo', 'pink', 'mauve', 'red', 'maroon', 'peach',
  'yellow', 'green', 'teal', 'sky', 'sapphire', 'blue', 'lavender',
] as const

export type Accent = (typeof ACCENTS)[number]

export function accentForName(name: string): Accent {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) >>> 0
  }
  return ACCENTS[hash % ACCENTS.length]
}
