import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import FeaturedRecipeCard from '~/Components/Recipe/FeaturedRecipeCard.vue'
import { hitToFeatured, variantToFeatured } from '~/Components/Recipe/recipeCardModel'
import { washFor } from '~/Utilities/BrandColor'
import type { RecipeSearchHit, VariantSummary } from '~/Types/Recipe'

const recipe: RecipeSearchHit = {
  name: 'Sourdough Focaccia',
  slug: '/recipes/focaccia',
  icon: 'fa-solid fa-bread-slice',
  category: 'Breads',
  startedBy: 'Marcus Hale',
  tags: ['Vegan'],
  averageRating: 4.9,
  reviewCount: 20,
  variantCount: 6,
  fastestTime: 45,
}

const variant: VariantSummary = {
  name: 'Overnight Rye',
  description: 'A slow, sour crumb.',
  slug: '/recipes/focaccia/rye',
  icon: 'fa-solid fa-wheat-awn',
  authorName: 'Ida Soerensen',
  tags: ['Vegan', 'Overnight'],
  totalTime: 30,
  publishedDate: '2026-01-05',
  averageRating: 4.2,
  reviewCount: 0,
}

// Matches what useResourceStrings hands the card when a key has no value: the key itself.
const rs = (key: string) => key

/** The whole opening tag of the first element carrying `needle`, so nothing pins Vue's attribute order. */
const openTag = (html: string, needle: string) => {
  const at = html.indexOf(needle)
  expect(at, `${needle} is not in the render`).toBeGreaterThan(-1)
  return html.slice(html.lastIndexOf('<', at), html.indexOf('>', at) + 1)
}

/** Everything inside that element. */
const inner = (html: string, needle: string) => {
  const at = html.indexOf(needle)
  expect(at, `${needle} is not in the render`).toBeGreaterThan(-1)
  const open = html.lastIndexOf('<', at)
  const tag = html.slice(open + 1).match(/^[a-z0-9]+/i)![0]
  const start = html.indexOf('>', at) + 1
  return html.slice(start, html.indexOf(`</${tag}>`, start))
}

/** Text of every Sono-set value, in document order: the identity sets every number this way. */
const sonoValues = (html: string) =>
  [...html.matchAll(/class="[^"]*kcc-num[^"]*"[^>]*>(.*?)<\/span>/g)].map((m) => m[1]!.replace(/<[^>]*>/g, '').trim())

const count = (html: string, needle: string) => html.split(needle).length - 1

describe('FeaturedRecipeCard from a search hit', () => {
  it('renders the recipe name and its numeric rating', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    expect(html).toContain('Sourdough Focaccia')
    expect(html).toContain('4.9')
    expect(html).toContain(recipe.slug)
  })

  it('is a recipe slip torn from the hero preset', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const article = openTag(html, 'kcc-slip')
    expect(article.startsWith('<article')).toBe(true)
    expect(article).toContain('kcc-recipe')
    expect(article).toContain('kcc-tear-hero')
    expect(html).not.toContain('sk-')
  })

  it('makes the torn wrapper the one link, and pads the sheet to the hero rule', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const anchor = openTag(html, 'kcc-torn')
    expect(anchor.startsWith('<a ')).toBe(true)
    expect(anchor).toContain(`href="${recipe.slug}"`)
    expect(count(html, '<a ')).toBe(1)
    expect(openTag(html, 'kcc-sheet').replace(/\s/g, '')).toContain('--pad:48px')
  })

  it('keeps the spotlight hooks on the link, with one data-recipe-name', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const anchor = openTag(html, 'kcc-torn')
    expect(anchor).toContain('data-testid="recipe-spotlight"')
    expect(anchor).toContain('data-recipe-name="Sourdough Focaccia"')
    expect(count(html, 'data-recipe-name')).toBe(1)
  })

  it('pools the recipe wash under the left margin', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const wash = openTag(html, 'kcc-wash').replace(/\s/g, '')
    expect(wash).toContain(`--c:var(--color-${washFor(recipe.name)})`)
    expect(wash).toContain('--x:10%')
    expect(wash).toContain('--y:15%')
    expect(wash).toContain('--w:45%')
    expect(wash).toContain('--h:80%')
  })

  it('pins the large tile and its tape outside the tear, with a strip on the sheet', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const tilewrap = openTag(html, 'kcc-tilewrap').replace(/\s/g, '')
    expect(tilewrap).toContain('top:-22px')
    expect(tilewrap).toContain('left:40px')
    expect(html).toContain('kcc-tile--lg')
    expect(count(html.replace(/\s/g, ''), `--c:var(--color-${washFor(recipe.name)})`)).toBe(2)
    expect(count(html, 'kcc-tape')).toBe(2)
    expect(html.indexOf('kcc-tilewrap')).toBeGreaterThan(html.indexOf('</a>'))
  })

  it('prints the spotlight pill as the label over the right edge, outside the link', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    expect(openTag(html, 'kcc-label')).toContain('kcc-label--right')
    expect(inner(html, 'kcc-label')).toContain('TopRated')
    expect(html.indexOf('kcc-label')).toBeGreaterThan(html.indexOf('</a>'))
  })

  it('opens with a kick line of category and who started it', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const kick = inner(html, 'kcc-kick').replace(/<[^>]*>/g, '')
    expect(kick).toContain('Breads')
    expect(kick).toContain('·')
    expect(kick).toContain('StartedBy Marcus Hale')
  })

  it('sets the title at display size', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    expect(inner(html, 'kcc-h3')).toContain('Sourdough Focaccia')
    expect(openTag(html, 'kcc-h3').startsWith('<h3')).toBe(true)
  })

  it('carries the rating hooks on a star line and sets every number in Sono', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) })
    const line = openTag(html, 'recipe-card-rating')
    expect(line).toContain('data-average-rating="4.9"')
    expect(inner(html, 'recipe-card-rating')).toContain('fa-duotone fa-star')
    expect(sonoValues(html)).toEqual(['4.9', '· 20', '6 Variants', '45 min'])
  })
})

describe('FeaturedRecipeCard from a variant', () => {
  it('renders the blurb and the badges, and drops the rating line when nobody has reviewed', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: variantToFeatured(variant, rs) })
    expect(inner(html, 'kcc-body')).toContain('A slow, sour crumb.')
    expect(inner(html, 'kcc-badges')).toContain('Overnight')
    expect(html).not.toContain('recipe-card-rating')
    expect(sonoValues(html)).toEqual(['30 Min'])
  })

  it('stays out of the detail page card queries: no spotlight hooks', async () => {
    const html = await renderSsr(FeaturedRecipeCard, { card: variantToFeatured(variant, rs) })
    expect(html).not.toContain('data-recipe-name')
    expect(html).not.toContain('data-testid')
    expect(inner(html, 'kcc-kick').replace(/<[^>]*>/g, '')).toContain('By Ida Soerensen')
  })
})
