import type { VariantSummary } from '~/Types/Recipe'
import { sortVariants } from './variantFilters'

export function featuredVariant(variants: VariantSummary[]): VariantSummary | undefined {
  if (variants.length === 0) return undefined
  const rated = variants.filter((variant) => (variant.reviewCount ?? 0) > 0)
  if (rated.length > 0) {
    return sortVariants(rated, 'rating')[0]
  }
  return sortVariants(variants, 'newest')[0]
}

export function averageMinutes(variants: VariantSummary[]): number | null {
  if (variants.length === 0) return null
  const total = variants.reduce((sum, variant) => sum + variant.totalTime, 0)
  return Math.round(total / variants.length)
}

export function contributorCount(variants: VariantSummary[]): number {
  const names = new Set<string>()
  variants.forEach((variant) => {
    const name = variant.authorName?.trim()
    if (name) names.add(name)
  })
  return names.size
}
