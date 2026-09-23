import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeDetailView from '~/Pages/RecipeDetail/RecipeDetailView.Component.vue'
import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumbs.Component.vue'
import type { VariantSummary } from '~/Types/Recipe'

const NAMES = ['Brown Butter', 'Crispy Edge', 'Garlic Confit', 'Herb Ricotta']

const variant = (name: string, index: number): VariantSummary => ({
  name,
  description: `${name}, the way it is made here.`,
  slug: `/recipes/gnocchi/${index}`,
  icon: 'fa-duotone fa-wheat',
  authorName: index ? 'Dana Whitfield' : 'Ira Boone',
  tags: ['Vegetarian'],
  totalTime: 20 + index * 10,
  publishedDate: `2026-04-0${index + 1}`,
  reviewCount: 0,
})

// Only the keys this page's own markup composes; the rest fall back to their key, as they do in the app.
const STRINGS = {
  'RecipeDetail.AddVariant': 'Add Variant',
  'RecipeDetail.AllVariants': 'All Variants',
  'RecipeDetail.Of': 'of',
  'RecipeDetail.StartedBy': 'Started by',
  'RecipeDetail.Variants': 'Variants',
  'RecipeDetail.Fastest': 'Fastest',
  'RecipeDetail.AvgTime': 'Avg Time',
  'RecipeDetail.Contributors': 'Contributors',
  'RecipeDetail.Min': 'min',
}

const render = (over: Record<string, unknown> = {}) =>
  renderSsr(
    RecipeDetailView,
    {
      recipeName: 'Brown Butter Gnocchi',
      recipeDescription: 'Crisp-edged pillows in a nutty brown butter.',
      recipeIcon: 'fa-duotone fa-wheat',
      recipeCategory: 'Mains',
      recipeGuid: 'a1b2c3',
      startedByName: 'Ira Boone',
      addVariantUrl: '~/recipes/add-variant',
      variants: NAMES.map(variant),
      breadcrumbs: [
        { linkText: 'Home', url: '/' },
        { linkText: 'Recipes', url: '/recipes' },
        { linkText: 'Brown Butter Gnocchi', url: '/recipes/gnocchi' },
      ],
      resourceStrings: STRINGS,
      ...over,
    },
    undefined,
    { Breadcrumbs },
  )

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('RecipeDetailView', () => {
  it('opens with the trail on the rule and sets the page 72px apart', async () => {
    const html = await render()

    expect(tagWith(html, 'aria-label="Breadcrumb"')).toContain('mt-6')
    expect(html).toContain('space-y-[72px]')
  })

  it('hands the hero the one marker action, carrying the recipe to the variant form', async () => {
    const html = await render()

    const action = tagWith(html, 'href="/recipes/add-variant\\?recipe=a1b2c3"')
    expect(action).toContain('kcc-btn')
    expect(action).not.toContain('kcc-btn--')
    expect(html).toContain('Add Variant')
    // Inside the hero sheet, under the description — not floating above the trail.
    expect(html.indexOf('kcc-tear-hero')).toBeLessThan(html.indexOf('/recipes/add-variant?recipe=a1b2c3'))
  })

  it('joins the category and the author in the hero eyebrow, no glyph for the dot', async () => {
    const html = await render()

    const eyebrow = html.match(/<p class="kcc-kick">[\s\S]*?<\/p>/)?.[0] ?? ''
    expect(eyebrow).toContain('Mains')
    expect(eyebrow).toContain('·')
    expect(eyebrow).toContain('Ira Boone')
    expect(html).not.toContain('fa-dot')
  })

  it('keeps the stats on an At a glance sheet of their own', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-tear-3')).toContain('kcc-slip kcc-tear-3')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-gauge" aria-hidden="true"></i>')
    expect(html).toContain('At a glance')
    const slip = html.slice(html.indexOf('kcc-tear-3'), html.indexOf('kcc-label', html.indexOf('kcc-tear-3')))
    expect(slip).toContain('data-testid="variant-stats"')
  })

  it('counts the variants, the fastest, the average and the contributors', async () => {
    const html = await render()

    const values = [...html.matchAll(/<p class="kcc-v">([\s\S]*?)<\/p>/g)].map((match) =>
      match[1]!
        .replace(/<[^>]*>/g, '')
        .replace(/\s+/g, ' ')
        .trim(),
    )
    expect(values).toEqual(['4', '20min', '35min', '2'])
  })

  it('names the variants section and counts the shown against the whole', async () => {
    const html = await render()

    expect(html).toMatch(/<div class="kcc-secname"><h2>\s*All Variants\s*<\/h2>/)
    expect(html).toMatch(/<p class="kcc-kick"><span class="kcc-num">4<\/span> of <span class="kcc-num">4<\/span><\/p>/)
  })

  it('reads h1 then h2 then h3 down the page', async () => {
    const html = await render()

    expect(html.indexOf('<h1')).toBeGreaterThan(-1)
    expect(html.indexOf('<h1')).toBeLessThan(html.indexOf('<h2'))
    expect(html.indexOf('<h2')).toBeLessThan(html.indexOf('<h3'))
  })

  it('lists the variants under the toolbar, 36px clear of it', async () => {
    const html = await render()

    expect(html).toContain('space-y-9')
    expect(html.indexOf('data-testid="sort-newest"')).toBeLessThan(html.indexOf('data-variant-name'))
    expect((html.match(/data-variant-name=/g) ?? []).length).toBe(4)
  })

  it('falls back to the washed empty sheet for a recipe with no variants yet', async () => {
    const html = await render({ variants: [] })

    expect(html).toContain('NoVariantsMatch')
    expect(html).not.toContain('data-variant-name')
    expect(html).toContain('—')
  })

  it('leaves no Softbound remnant on the page', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\bv-ink\b/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(html).not.toMatch(/\bshadow-/)
  })
})
