import type { RecipeSearchHit, VariantSummary } from '~/Types/Recipe'

/**
 * Resolver returned by `provideResourceStrings` / `useResourceStrings`. Each page owns its own
 * resolver (scoped by prefix, e.g. `RecipeSearch` vs `RecipeDetail`), so localized text is resolved
 * here in the page-level wrappers and passed to the shared cards as plain strings — the shared
 * components stay presentation-only and never touch the resource-string context.
 */
type Resolve = (key: string) => string

/** One inline meta item on a card's meta line: optional leading icon + text (e.g. "🕐 30m"). */
export interface RecipeCardMeta {
  icon?: string
  text: string
}

/** Compact rating shown on cards / featured blocks. `count === 0` renders `emptyLabel` instead. */
export interface RecipeCardRating {
  average: number
  count: number
  emptyLabel: string
}

/**
 * Normalized, presentation-only shape shared by the recipe (search) and variant (detail) cards.
 * Each page maps its own type (`RecipeSearchHit` / `VariantSummary`) into this; optional fields are
 * rendered only when present, so a single card component drives both pages without style drift.
 */
export interface RecipeCardModel {
  href: string
  name: string
  /** AccentTile seed (brand color + fallback alt text). */
  seed: string
  icon?: string
  image?: string
  /** Small uppercase line above the name (search: category). */
  eyebrow?: string
  /** Compact rating; omit to hide the rating entirely (variant cards show none today). */
  rating?: RecipeCardRating
  /** Inline meta chips on the meta line (search: variant count + fastest time). */
  meta?: RecipeCardMeta[]
  /** Secondary text line (variant: "By Alex · 30 min"). */
  subtitle?: string
  /** Longer description paragraph (variant grid). */
  description?: string
  tags: string[]
  /** Trailing stat rendered by the row layout only (variant list: total time). */
  trailingStat?: { value: string; label: string }
  /** Attributes spread onto the root anchor — the stable E2E/unit test hooks. */
  dataAttrs?: Record<string, string>
}

/** Normalized shape for the featured / spotlight block. */
export interface FeaturedRecipeModel {
  href: string
  name: string
  seed: string
  icon?: string
  image?: string
  /** Inline "Top ___" pill. */
  pill: { icon?: string; label: string }
  eyebrow?: string
  rating?: RecipeCardRating
  meta?: RecipeCardMeta[]
  description?: string
  tags?: string[]
  dataAttrs?: Record<string, string>
}

/** Search hit → grid/list card. */
export function hitToCard(hit: RecipeSearchHit, rs: Resolve): RecipeCardModel {
  return {
    href: hit.slug,
    name: hit.name,
    seed: hit.name,
    icon: hit.icon,
    eyebrow: hit.category,
    rating: { average: hit.averageRating ?? 0, count: hit.reviewCount, emptyLabel: rs('NoRatingsYet') },
    meta: [
      { icon: 'fa-solid fa-layer-group', text: String(hit.variantCount) },
      { icon: 'fa-solid fa-clock', text: `${hit.fastestTime}m` },
    ],
    tags: hit.tags,
    dataAttrs: { 'data-testid': 'recipe-card', 'data-recipe-name': hit.name },
  }
}

/** Variant summary → grid/list card. */
export function variantToCard(variant: VariantSummary, rs: Resolve): RecipeCardModel {
  const time = `${variant.totalTime} ${rs('Min')}`
  return {
    href: variant.slug,
    name: variant.name,
    seed: variant.name,
    icon: variant.icon,
    image: variant.image,
    subtitle: variant.authorName ? `${rs('By')} ${variant.authorName} · ${time}` : time,
    description: variant.description,
    tags: variant.tags,
    trailingStat: { value: `${variant.totalTime}${rs('Min')}`, label: rs('Total') },
    dataAttrs: { 'data-variant-name': variant.name },
  }
}

/** Search hit → featured (spotlight) block. */
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

/** Variant summary → featured (top variant) block. */
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
