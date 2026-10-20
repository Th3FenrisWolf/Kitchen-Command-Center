import { renderSsr, tagWith, echoKey } from '../../../support/ssr'
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

const renderHit = (recipe: RecipeSearchHit) => renderSsr(RecipeCardRow, { card: hitToCard(recipe, echoKey) })
const renderVariant = (summary: VariantSummary) => renderSsr(RecipeCardRow, { card: variantToCard(summary, echoKey) })

describe('RecipeCardRow sheet', () => {
  const LIFT = 'kcc-slip--lift'
  const BLANK = '<span class="kcc-blank" aria-hidden="true"><span></span></span>'

  it('is a wide flat slip holding one link over a torn sheet, with the card hooks on the link', async () => {
    const html = await renderHit(hit())
    const slip = tagWith(html, 'kcc-slip')
    expect(slip).toContain(`class="kcc-slip kcc-slip--wide ${LIFT} kcc-tear-${sheetTearFor(NAME)}"`)
    // A stack of rows reads as a list, so the sheets lie flat rather than each taking the preset's tilt.
    expect(slip).toContain('style="--r:0;"')
    expect(slip).not.toContain('href')

    const anchor = tagWith(html, 'kcc-torn block')
    expect(html.match(/<a\b/g)).toHaveLength(1)
    expect(anchor).toContain('<a href="/recipes/chicken-piccata"')
    expect(anchor).toContain('data-testid="recipe-card"')
    expect(anchor).toContain(`data-recipe-name="${NAME}"`)
    expect(html).toMatch(/<a[^>]*class="kcc-torn block"[^>]*><div class="kcc-sheet/)

    expect(html.startsWith(slip)).toBe(true)
    expect(html.split('kcc-blank')).toHaveLength(2)
    expect(html.endsWith(`</a>${BLANK}</div>`)).toBe(true)
    expect(
      [...html.matchAll(/class="([^"]*)"/g)].some(([, list]) => /\bkcc-slip\b/.test(list!) && /\bkcc-torn\b/.test(list!)),
    ).toBe(false)
  })

  it('turns its blank the other way only when flipped', async () => {
    const flipped = await renderSsr(RecipeCardRow, { card: hitToCard(hit(), echoKey), tear: 2, flip: true })
    expect(tagWith(flipped, 'kcc-slip')).toContain(`class="kcc-slip kcc-slip--wide ${LIFT} kcc-slip--flip kcc-tear-2"`)
    expect(await renderHit(hit())).not.toContain('kcc-slip--flip')
  })

  it('applies its tile in the flow of the sheet, under one strip of tape', async () => {
    const html = await renderHit(hit())
    const sheet = html.indexOf('<div class="kcc-sheet flex items-start gap-4"')
    expect(sheet).toBeGreaterThan(-1)
    expect(html.slice(sheet)).toMatch(
      /^<div class="kcc-sheet[^"]*"[^>]*><div class="kcc-tilewrap kcc-tilewrap--flow flex-none"><div class="kcc-torn size-14"><div class="[^"]*\bkcc-tile\b[^"]*"[^>]*><i class="fa-solid fa-drumstick-bite"><\/i><\/div><\/div><span class="kcc-tape" aria-hidden="true"><\/span><\/div>/,
    )
    expect(html.split('kcc-tape')).toHaveLength(2)
  })

  it('pads the row tighter than a card', async () => {
    const html = await renderHit(hit())
    expect(html).toContain('<div class="kcc-sheet flex items-start gap-4" style="--pad:16px;">')
  })

  it('lifts its top sheet off the blank through the kit, exactly as the card does', async () => {
    const html = await renderHit(hit())
    const slip = tagWith(html, 'kcc-slip')
    expect(slip.startsWith('<div ')).toBe(true)
    expect(slip).toContain(`class="kcc-slip kcc-slip--wide ${LIFT} kcc-tear-${sheetTearFor(NAME)}"`)
    expect(html).not.toMatch(/transition-transform|hover:-translate-y-1|focus-within:-translate-y-1/)
    expect(tagWith(html, 'kcc-torn block')).not.toContain('translate')
  })

  it('takes the tear a list hands it', async () => {
    const html = await renderSsr(RecipeCardRow, { card: hitToCard(hit(), echoKey), tear: 2 })
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-tear-2')
  })
})

describe('RecipeCardRow body', () => {
  it('sets the name at heading size and keeps the rating hook on the meta line', async () => {
    const html = await renderHit(hit())
    expect(html).toContain(`<h3 class="kcc-title">${NAME}</h3>`)
    const rating = tagWith(html, 'data-testid="recipe-card-rating"')
    expect(rating).toContain('class="kcc-num"')
    expect(rating).toContain('data-average-rating="4.8"')
    expect(html).toMatch(/<i class="fa-duotone fa-star"[^>]*><\/i>\s*4\.8/)
  })

  it('starts the name, the meta and the badges each on a line of their own, top-aligned beside the tile', async () => {
    const html = await renderHit(hit())
    const column = html.slice(html.indexOf('<div class="min-w-0 flex-1">'))

    expect(column).toMatch(
      /^<div class="min-w-0 flex-1"><h3 class="kcc-title">[^<]*<\/h3><p class="kcc-meta">[\s\S]*?<\/p><div class="kcc-badges">/,
    )
  })

  it('says a recipe is unrated instead of showing it a zero', async () => {
    const html = await renderHit(hit({ averageRating: null, reviewCount: 0 }))
    expect(html).not.toContain('recipe-card-rating')
    expect(html).not.toMatch(/fa-star\b/)
    expect(html).toContain('<span>NoRatingsYet</span>')
  })

  it('closes a variant row with its total time in Sono over a kick', async () => {
    const html = await renderVariant(variant())
    expect(html).toContain(`<h3 class="kcc-title">${VARIANT}</h3>`)
    expect(html).toContain('<span class="kcc-num block">35Min</span>')
    expect(html).toContain('<span class="kcc-kick block">Total</span>')
    expect(html).toContain('<span class="kcc-badge">')
  })
})
