import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
import { hitToCard, variantToCard } from '~/Components/Recipe/recipeCardModel'
import { sheetTearFor } from '~/Utilities/BrandColor'
import type { RecipeSearchHit, VariantSummary } from '~/Types/Recipe'

const NAME = 'Weeknight Chicken Piccata'
const VARIANT = 'Sheet-Pan Piccata'

const hit = (over: Partial<RecipeSearchHit> = {}): RecipeSearchHit => ({
  name: NAME,
  slug: '/recipes/chicken-piccata',
  icon: 'fa-solid fa-drumstick-bite',
  category: 'Mains',
  startedBy: 'Dana Whitfield',
  tags: ['Gluten-Free'],
  averageRating: 4.8,
  reviewCount: 12,
  variantCount: 5,
  fastestTime: 30,
  ...over,
})

const variant = (over: Partial<VariantSummary> = {}): VariantSummary => ({
  name: VARIANT,
  description: 'Everything on one tray.',
  slug: '/recipes/chicken-piccata/sheet-pan',
  icon: 'fa-solid fa-drumstick-bite',
  authorName: 'Alex',
  tags: ['One pan'],
  totalTime: 35,
  publishedDate: '2026-01-01',
  ...over,
})

// Matches what useResourceStrings hands the row when a key has no value: the key itself.
const rs = (key: string) => key

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

const renderHit = (recipe: RecipeSearchHit) => renderSsr(RecipeCardRow, { card: hitToCard(recipe, rs) })
const renderVariant = (summary: VariantSummary) => renderSsr(RecipeCardRow, { card: variantToCard(summary, rs) })

describe('RecipeCardRow sheet', () => {
  it('is one link over a flat torn sheet, with the card hooks on it', async () => {
    const html = await renderHit(hit())
    const anchor = tagWith(html, 'kcc-slip')
    expect(html.match(/<a\b/g)).toHaveLength(1)
    expect(anchor).toContain('<a href="/recipes/chicken-piccata"')
    expect(anchor).toContain('data-testid="recipe-card"')
    expect(anchor).toContain(`data-recipe-name="${NAME}"`)
    expect(anchor).toContain('kcc-torn')
    expect(anchor).toContain(`kcc-tear-${sheetTearFor(NAME)}`)
    // A stack of rows reads as a list, so the sheets lie flat rather than each taking the preset's tilt.
    expect(anchor).toContain('style="--r:0;"')
  })

  it('pads the row tighter than a card', async () => {
    const html = await renderHit(hit())
    expect(html).toContain('<div class="kcc-sheet flex items-center gap-4" style="--pad:16px;">')
  })

  it('takes the tear a list hands it', async () => {
    const html = await renderSsr(RecipeCardRow, { card: hitToCard(hit(), rs), tear: 2 })
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-tear-2')
  })
})

describe('RecipeCardRow body', () => {
  it('sets the name at heading size and keeps the rating hook on the meta line', async () => {
    const html = await renderHit(hit())
    expect(html).toContain(`<h3 class="kcc-h4">${NAME}</h3>`)
    const rating = tagWith(html, 'data-testid="recipe-card-rating"')
    expect(rating).toContain('class="kcc-num"')
    expect(rating).toContain('data-average-rating="4.8"')
    expect(html).toMatch(/<i class="fa-duotone fa-star"[^>]*><\/i>\s*4\.8/)
  })

  it('says a recipe is unrated instead of showing it a zero', async () => {
    const html = await renderHit(hit({ averageRating: null, reviewCount: 0 }))
    expect(html).not.toContain('recipe-card-rating')
    expect(html).not.toMatch(/fa-star\b/)
    expect(html).toContain('<span>NoRatingsYet</span>')
  })

  it('closes a variant row with its total time in Sono over a kick', async () => {
    const html = await renderVariant(variant())
    expect(html).toContain(`<h3 class="kcc-h4">${VARIANT}</h3>`)
    expect(html).toContain('<span class="kcc-num block">35Min</span>')
    expect(html).toContain('<span class="kcc-kick block">Total</span>')
    expect(html).toContain('<span class="kcc-badge">')
  })
})
