import type { VariantSummary } from '~/Types/Recipe'

export type SortKey = 'newest' | 'fastest'
export type ViewMode = 'grid' | 'list'

export interface VariantFilterState {
  search: string
  tag: string
  sort: SortKey
}

export function tagOptions(variants: VariantSummary[]): string[] {
  const set = new Set<string>()
  variants.forEach((variant) => variant.tags.forEach((tag) => set.add(tag)))
  return ['', ...[...set].sort((a, b) => a.localeCompare(b))]
}

export function sortVariants(variants: VariantSummary[], sort: SortKey): VariantSummary[] {
  const list = variants.slice()
  if (sort === 'fastest') {
    list.sort((a, b) => a.totalTime - b.totalTime)
  } else {
    list.sort((a, b) => new Date(b.publishedDate).getTime() - new Date(a.publishedDate).getTime())
  }
  return list
}

export function filterVariants(variants: VariantSummary[], state: VariantFilterState): VariantSummary[] {
  let list = variants.slice()
  if (state.tag) {
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
