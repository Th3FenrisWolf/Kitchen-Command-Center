import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeCard from '~/Components/Recipe/RecipeCard.vue'
import { hitToCard } from '~/Components/Recipe/recipeCardModel'
import { sheetTearFor, tileTearFor, washFor } from '~/Utilities/BrandColor'
import type { RecipeSearchHit } from '~/Types/Recipe'

const NAME = 'Weeknight Chicken Piccata'

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

// Matches what useResourceStrings hands the card when a key has no value: the key itself.
const rs = (key: string) => key

const countStars = (html: string) => (html.match(/fa-star\b/g) ?? []).length

// The opening tag of the first element carrying `needle`, so an assertion about one element's classes and
// hooks does not also pin the order Vue happens to print its attributes in.
const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

// Renders through the search page's own mapper, so these cover the RecipeSearchHit -> card
// hop as well as the markup the shared card produces from it.
const render = (recipe: RecipeSearchHit, props: Record<string, unknown> = {}) =>
  renderSsr(RecipeCard, { card: hitToCard(recipe, rs), ...props })

describe('RecipeCard slip', () => {
  it('is an article slip torn on a preset the recipe picks for itself', async () => {
    const html = await render(hit())
    const root = tagWith(html, 'kcc-slip')
    expect(root).toContain('<article')
    expect(root).toContain('kcc-recipe')
    expect(root).toContain(`kcc-tear-${sheetTearFor(NAME)}`)
  })

  it('takes the tear a grid hands it, so two neighbours never share one', async () => {
    const html = await render(hit(), { tear: 4 })
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-tear-4')
  })

  it('makes the whole sheet one link, with the card hooks on it', async () => {
    const html = await render(hit())
    const anchor = tagWith(html, 'kcc-torn block')
    expect(html.match(/<a\b/g)).toHaveLength(1)
    expect(anchor).toContain('<a href="/recipes/chicken-piccata"')
    expect(anchor).toContain('data-testid="recipe-card"')
    expect(anchor).toContain(`data-recipe-name="${NAME}"`)
    // The fibre-and-fall filter is on the link and the clip on the sheet inside it, so the focus ring is
    // whole. `<!--[-->` is the anchor Vue prints around a slot's content.
    expect(html).toMatch(/<a[^>]*class="kcc-torn block"[^>]*><!--\[--><div class="kcc-sheet">/)
  })

  it('pins the tile and its tape outside the link, so neither is clipped by the tear', async () => {
    const html = await render(hit())
    expect(html.indexOf('kcc-tilewrap')).toBeGreaterThan(html.indexOf('</a>'))
    expect(html).toContain(`kcc-tear-tile-${tileTearFor(NAME)}`)
    expect(html).toContain(`--c:var(--color-${washFor(NAME)})`)
    expect(html).toContain('<span class="kcc-tape" aria-hidden="true"></span>')
  })

  it('leaves row alignment to the grid', async () => {
    const html = await render(hit())
    expect(html).not.toMatch(/subgrid|row-span/)
  })
})

describe('RecipeCard stat', () => {
  it('raises the rating to the top-right, with the review count in soft ink', async () => {
    const html = await render(hit())
    const stat = tagWith(html, 'kcc-stat')
    expect(stat).toContain('data-testid="recipe-card-rating"')
    expect(stat).toContain('data-average-rating="4.8"')
    expect(countStars(html)).toBe(1)
    expect(html).toMatch(
      /<i class="fa-duotone fa-star"[^>]*><\/i>\s*<span class="kcc-num">4\.8<\/span>\s*<span class="text-ink-soft">· 12<\/span>/,
    )
  })

  it('shows the clock and the fastest time, and no fake number, when nothing is rated', async () => {
    const html = await render(hit({ averageRating: null, reviewCount: 0 }))
    expect(countStars(html)).toBe(0)
    expect(html).not.toContain('4.8')
    expect(html).not.toContain('recipe-card-rating')
    expect(html).toMatch(/<i class="fa-duotone fa-clock"[^>]*><\/i>\s*<span class="kcc-num">30m<\/span>/)
    // The rating is missing, not zero, and the meta line says so.
    expect(html).toContain('<span>NoRatingsYet</span>')
  })
})

describe('RecipeCard body', () => {
  it('sets the name at heading size over a Sono meta line of category, variants and time', async () => {
    const html = await render(hit())
    expect(html).toContain(`<h3 class="kcc-h4">${NAME}</h3>`)
    const meta = html.match(/<p class="kcc-meta">.*?<\/p>/s)?.[0] ?? ''
    expect(meta).toContain('<span>Mains</span>')
    expect(meta).toMatch(/<span class="kcc-num"><i class="fa-solid fa-layer-group"[^>]*><\/i>\s*5<\/span>/)
    expect(meta).toMatch(/<span class="kcc-num"><i class="fa-solid fa-clock"[^>]*><\/i>\s*30m<\/span>/)
  })

  it('renders the tags as hairline badges', async () => {
    const html = await render(hit())
    const badges = html.match(/<div class="kcc-badges[^"]*">.*?<\/div>/s)?.[0] ?? ''
    expect(badges).toContain('<span class="kcc-badge"><!--[-->Gluten-Free<!--]--></span>')
  })
})
