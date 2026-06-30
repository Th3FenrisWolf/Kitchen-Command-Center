import type { VariantSummary } from '~/Types/Recipe'
import { sortVariants } from './variantFilters'

/** The variant a recipe page features (currently the newest). */
export function featuredVariant(variants: VariantSummary[]): VariantSummary | undefined {
  if (variants.length === 0) return undefined
  return sortVariants(variants, 'newest')[0]
}

/** Mean total time across variants, rounded; null for an empty list. */
export function averageMinutes(variants: VariantSummary[]): number | null {
  if (variants.length === 0) return null
  const total = variants.reduce((sum, variant) => sum + variant.totalTime, 0)
  return Math.round(total / variants.length)
}

/** Count of distinct, non-blank author names. */
export function contributorCount(variants: VariantSummary[]): number {
  const names = new Set<string>()
  variants.forEach((variant) => {
    const name = variant.authorName?.trim()
    if (name) names.add(name)
  })
  return names.size
}
