import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeSearchView from '~/Pages/RecipeSearch/RecipeSearchView.Component.vue'
import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumbs.Component.vue'
import type { RecipeSearchHit, RecipeSearchResponse } from '~/Types/Recipe'

const NAMES = [
  'Brown Butter Gnocchi',
  'Egg Skillet',
  'Mac & Cheese',
  'Miso Soup',
  'Pesto Pasta',
  'Ramen Night',
  'Tomato Soup',
]

const hit = (name: string, index = 0): RecipeSearchHit => ({
  name,
  slug: `/recipes/${index}`,
  icon: 'fa-solid fa-drumstick-bite',
  category: 'Mains',
  startedBy: 'Dana Whitfield',
  tags: [],
  averageRating: null,
  reviewCount: 0,
  variantCount: 2,
  fastestTime: 20 + index,
})

// Only the keys this page's own markup composes; the rest fall back to their key, as they do in the app.
const STRINGS = {
  'RecipeSearch.Recipe': 'Recipe',
  'RecipeSearch.Recipes': 'Recipes',
  'RecipeSearch.ResultsFor': 'results for',
  'RecipeSearch.LoadingMore': 'Loading more',
  'RecipeSearch.Filters': 'Filters',
}

const response = (over: Partial<RecipeSearchResponse> = {}): RecipeSearchResponse => ({
  total: NAMES.length,
  page: 0,
  pageSize: 12,
  results: NAMES.map(hit),
  facets: { category: { Mains: 7 }, diet: { Vegetarian: 3 } },
  spotlight: null,
  ...over,
})

const render = (over: Partial<RecipeSearchResponse> = {}) =>
  renderSsr(
    RecipeSearchView,
    {
      initial: response(over),
      createRecipeUrl: '~/create-recipe',
      breadcrumbs: [
        { linkText: 'Home', url: '/' },
        { linkText: 'Recipes', url: '/recipes' },
      ],
      resourceStrings: STRINGS,
    },
    undefined,
    { Breadcrumbs },
  )

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('RecipeSearchView library', () => {
  it('opens the results with a section name and the count set in Sono', async () => {
    const html = await render()

    expect(html).toContain('<div class="kcc-secname">')
    expect(html).toMatch(/<div class="kcc-secname"><h2>\s*Recipes\s*<\/h2>/)
    expect(html).toMatch(/<p class="kcc-kick"><span class="kcc-num">7<\/span> Recipes<\/p>/)
  })

  it('counts a lone result in the singular', async () => {
    const html = await render({ total: 1, results: [hit(NAMES[0]!)] })

    expect(html).toMatch(/<p class="kcc-kick"><span class="kcc-num">1<\/span> Recipe<\/p>/)
  })

  it('lays the slips out on the rule, 28px between columns and 36px between rows', async () => {
    const grid = tagWith(await render(), 'repeat\\(auto-fit')

    expect(grid).toContain('grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))]')
    expect(grid).toContain('gap-x-7')
    expect(grid).toContain('gap-y-9')
    expect(grid).not.toContain('row-span')
  })

  it('cycles the six tears down the grid so no two neighbours share one', async () => {
    const html = await render()

    const tears = [...html.matchAll(/kcc-recipe[^"]*kcc-tear-(\d)/g)].map((m) => Number(m[1]))
    expect(tears).toEqual([1, 2, 3, 4, 5, 6, 1])
  })

  it('pins the spotlight above the grid, 48px clear of the count line', async () => {
    const html = await render({ spotlight: hit('Top Rated Thing', 9) })

    expect(tagWith(html, 'kcc-tear-hero')).toContain('mt-12')
  })

  it('opens with the trail on the rule', async () => {
    const html = await render()

    expect(tagWith(html, 'aria-label="Breadcrumb"')).toContain('mt-6')
  })

  it('keeps the results section live and the filter panel addressable', async () => {
    const html = await render()

    const section = tagWith(html, 'aria-live="polite"')
    expect(section).toContain('aria-busy="false"')

    const aside = tagWith(html, 'id="recipe-filters"')
    expect(aside).toContain('lg:sticky')
    expect(aside).not.toContain('rounded')
    expect(aside).not.toContain('bg-paper-2')
  })

  it('opens the filter panel from a ghost pill that keeps its aria wiring', async () => {
    const toggle = tagWith(await render(), 'aria-controls="recipe-filters"')

    expect(toggle).toContain('kcc-btn')
    expect(toggle).toContain('kcc-btn--ghost')
    expect(toggle).toContain('aria-expanded="false"')
    expect(toggle).toContain('lg:hidden')
  })

  it('keeps a kick line at the foot of the results while more are still coming', async () => {
    // The spinner itself only prints once a fetch is in flight, which no server render ever is.
    expect(await render({ total: 25 })).toContain('<div class="kcc-kick flex items-center justify-center py-6">')
    expect(await render()).not.toContain('kcc-kick flex items-center justify-center')
  })

  it('falls back to the washed empty sheet with nothing to list', async () => {
    const html = await render({ total: 0, results: [] })

    expect(html).toContain('data-testid="recipes-empty"')
    expect(html).not.toContain('data-testid="recipe-card"')
  })

  it('leaves no Softbound remnant on the page', async () => {
    const html = await render({ spotlight: hit('Top Rated Thing', 9), total: 25 })

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(html).not.toMatch(/\btext-(?:xs|sm)\b/)
  })
})
