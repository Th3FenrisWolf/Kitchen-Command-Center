import { formatRating } from '~/Components/StarRating/starDisplay'
import type { RecipeSearchHit, SiblingVariant, VariantSummary } from '~/Types/Recipe'

// Each page resolves strings under its own prefix, so the page localizes card text and the shared cards
// never read the resource-string context.
type Resolve = (key: string) => string

export type PromotedStat = 'rating' | 'time'

export interface RecipeCardMeta {
  icon?: string
  text: string
  key?: PromotedStat
}

export interface RecipeCardNotch {
  stat: PromotedStat
  text: string
}

export interface RecipeCardRating {
  average: number
  count: number
  emptyLabel: string
}

export interface RecipeCardModel {
  href: string
  name: string
  seed: string
  icon?: string
  image?: string
  eyebrow?: string
  rating?: RecipeCardRating
  notch: RecipeCardNotch
  meta?: RecipeCardMeta[]
  subtitle?: string
  description?: string
  tags: string[]
  trailingStat?: { value: string; label: string }
  dataAttrs?: Record<string, string>
}

export interface FeaturedRecipeModel {
  href: string
  name: string
  seed: string
  icon?: string
  image?: string
  pill: { icon?: string; label: string }
  eyebrow?: string
  rating?: RecipeCardRating
  meta?: RecipeCardMeta[]
  description?: string
  tags?: string[]
  dataAttrs?: Record<string, string>
}

/**
 * The card's single headline number: its rating once it has one, and the cook time until then — so the
 * corner stat is never empty and never advertises an unrated recipe as a zero.
 */
function notchStat(rating: RecipeCardRating | undefined, time: string): RecipeCardNotch {
  return rating && rating.count > 0 ? { stat: 'rating', text: formatRating(rating.average) } : { stat: 'time', text: time }
}

export function hitToCard(hit: RecipeSearchHit, rs: Resolve): RecipeCardModel {
  const rating = { average: hit.averageRating ?? 0, count: hit.reviewCount, emptyLabel: rs('NoRatingsYet') }
  const time = `${hit.fastestTime}m`
  return {
    href: hit.slug,
    name: hit.name,
    seed: hit.name,
    icon: hit.icon,
    eyebrow: hit.category,
    rating,
    notch: notchStat(rating, time),
    meta: [
      { icon: 'fa-solid fa-layer-group', text: String(hit.variantCount) },
      { key: 'time', icon: 'fa-solid fa-clock', text: time },
    ],
    tags: hit.tags,
    dataAttrs: { 'data-testid': 'recipe-card', 'data-recipe-name': hit.name },
  }
}

export function variantToCard(variant: VariantSummary, rs: Resolve): RecipeCardModel {
  const time = `${variant.totalTime} ${rs('Min')}`
  return {
    href: variant.slug,
    name: variant.name,
    seed: variant.name,
    icon: variant.icon,
    image: variant.image,
    notch: { stat: 'time', text: time },
    meta: [{ key: 'time', icon: 'fa-solid fa-clock', text: time }],
    subtitle: variant.authorName ? `${rs('By')} ${variant.authorName}` : undefined,
    description: variant.description,
    tags: variant.tags,
    trailingStat: { value: `${variant.totalTime}${rs('Min')}`, label: rs('Total') },
    dataAttrs: { 'data-variant-name': variant.name },
  }
}

/**
 * Sibling variant → grid card. A sibling arrives without a review count, so its rating can only ever be
 * the corner stat: there is no "· N" to print beside it and no empty-rating note to fall back to. The unit
 * is the literal the variant page already prints on its stat tiles; that page carries no `Min` string.
 */
export function siblingToCard(sibling: SiblingVariant): RecipeCardModel {
  const time = `${sibling.totalTime} min`
  return {
    href: sibling.slug,
    name: sibling.name,
    seed: sibling.name,
    icon: sibling.icon,
    notch: sibling.rating > 0 ? { stat: 'rating', text: formatRating(sibling.rating) } : { stat: 'time', text: time },
    meta: [{ key: 'time', icon: 'fa-solid fa-clock', text: time }],
    tags: [],
  }
}

export function hitToFeatured(hit: RecipeSearchHit, rs: Resolve): FeaturedRecipeModel {
  const meta: RecipeCardMeta[] = [
    { icon: 'fa-solid fa-layer-group', text: `${hit.variantCount} ${rs('Variants')}` },
    { icon: 'fa-solid fa-clock', text: `${hit.fastestTime} min` },
  ]
  if (hit.startedBy) {
    meta.push({ text: `${rs('StartedBy')} ${hit.startedBy}` })
  }
  return {
    href: hit.slug,
    name: hit.name,
    seed: hit.name,
    icon: hit.icon,
    pill: { icon: 'fa-solid fa-star', label: rs('TopRated') },
    eyebrow: hit.category,
    rating: { average: hit.averageRating ?? 0, count: hit.reviewCount, emptyLabel: rs('NoRatingsYet') },
    meta,
    // Keeps the search-only spotlight hooks; variant featured (below) intentionally has none so it
    // stays out of the detail page's `[data-variant-name]` card queries.
    dataAttrs: { 'data-testid': 'recipe-spotlight', 'data-recipe-name': hit.name },
  }
}

export function variantToFeatured(variant: VariantSummary, rs: Resolve): FeaturedRecipeModel {
  const meta: RecipeCardMeta[] = [{ icon: 'fa-solid fa-clock', text: `${variant.totalTime} ${rs('Min')}` }]
  if (variant.authorName) {
    meta.push({ text: `${rs('By')} ${variant.authorName}` })
  }
  const reviewCount = variant.reviewCount ?? 0
  return {
    href: variant.slug,
    name: variant.name,
    seed: variant.name,
    icon: variant.icon,
    image: variant.image,
    pill: { icon: 'fa-solid fa-star', label: rs('TopVariant') },
    rating: reviewCount > 0 ? { average: variant.averageRating ?? 0, count: reviewCount, emptyLabel: '' } : undefined,
    meta,
    description: variant.description,
    tags: variant.tags,
  }
}
