import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import VariantDetailView from '~/Pages/VariantDetail/VariantDetailView.Component.vue'
import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumbs.Component.vue'

// Only the keys this page's own markup composes; the rest fall back to their key, as they do in the app.
const STRINGS = {
  'VariantDetail.VariantOf': 'Variant of',
  'VariantDetail.CookMode': 'Cook Mode',
  'VariantDetail.ComingSoon': 'Coming soon',
  'VariantDetail.Prep': 'Prep',
  'VariantDetail.Cook': 'Cook',
  'VariantDetail.Count': 'Servings',
  'VariantDetail.Difficulty': 'Difficulty',
  'VariantDetail.DifficultyEasy': 'Easy',
}

const render = (over: Record<string, unknown> = {}) =>
  renderSsr(
    VariantDetailView,
    {
      variantName: 'Crispy Edge Gnocchi',
      variantDescription: 'Pan-fried until the edges shatter.',
      icon: 'fa-duotone fa-pepper-hot',
      prepTime: 15,
      cookTime: 20,
      servings: 4,
      difficulty: 'easy',
      tags: ['Vegetarian'],
      ingredients: [{ name: 'Gnocchi', quantity: 500, unit: 'g', isEyeballed: false }],
      instructions: [{ text: 'Crisp the gnocchi in brown butter.' }],
      recipeName: 'Brown Butter Gnocchi',
      recipeSlug: '/recipes/gnocchi',
      createdByName: 'Ira Boone',
      siblingVariants: [{ name: 'Herb Ricotta', slug: '/recipes/gnocchi/herb-ricotta', rating: 0, totalTime: 30 }],
      variantGuid: '11111111-2222-3333-4444-555555555555',
      breadcrumbs: [
        { linkText: 'Home', url: '/' },
        { linkText: 'Brown Butter Gnocchi', url: '/recipes/gnocchi' },
      ],
      resourceStrings: STRINGS,
      ...over,
    },
    undefined,
    { Breadcrumbs },
  )

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('VariantDetailView', () => {
  it('sets the whole page on one 72px rhythm, from a single root', async () => {
    const html = await render()

    expect(html.trimStart()).toMatch(/^<div class="mt-6 space-y-\[72px\]"/)
  })

  it('opens with the trail and the desktop controls on one row', async () => {
    const html = await render({ isAuthenticated: true })

    expect(tagWith(html, 'aria-label="Breadcrumb"')).toBeTruthy()
    expect(html).toContain('hidden items-center gap-3 lg:flex')
    expect(html.indexOf('aria-label="Breadcrumb"')).toBeLessThan(html.indexOf('cook-mode-open-desktop'))
    expect(html.indexOf('cook-mode-open-desktop')).toBeLessThan(html.indexOf('cooked-toggle'))
  })

  it('opens cook mode from a hairline pill that keeps its hook, its title and its guard', async () => {
    const button = tagWith(await render(), 'data-test="cook-mode-open-desktop"')

    expect(button).toMatch(/^<button /)
    expect(button).toContain('kcc-btn kcc-btn--ghost')
    expect(button).toContain('title="Cook Mode"')
    expect(button).not.toContain('disabled')
    expect(await render()).toContain('<i class="fa-duotone fa-play" aria-hidden="true"></i>')
  })

  it('disables the cook-mode pill, with the reason in its title, when there is no method', async () => {
    const button = tagWith(await render({ instructions: [] }), 'data-test="cook-mode-open-desktop"')

    expect(button).toContain('disabled')
    expect(button).toContain('title="Coming soon"')
  })

  it('carries the recipe name in the hero eyebrow', async () => {
    const html = await render()

    expect(html).toContain('kcc-tear-hero')
    expect(html).toMatch(/<p class="kcc-kick">[\s\S]*?Variant of[\s\S]*?Brown Butter Gnocchi/)
  })

  it('keeps the stats on an At a glance sheet of their own', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-tear-3')).toContain('kcc-slip kcc-tear-3')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-gauge" aria-hidden="true"></i>')
    expect(html).toContain('At a glance')
    const slip = html.slice(html.indexOf('kcc-tear-3'), html.indexOf('kcc-label', html.indexOf('kcc-tear-3')))
    expect(slip).toContain('data-testid="variant-stats"')
  })

  it('stacks the ingredients over the method until 768px, then sets the method the wider of the two', async () => {
    const html = await render()

    expect(html).toContain('grid gap-x-7 gap-y-9 md:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]')
    expect(html.indexOf('fa-basket-shopping')).toBeLessThan(html.indexOf('fa-list-ol'))
    expect(html).not.toContain('lg:sticky')
    expect(html).not.toContain('lg:col-span-3')
  })

  it('runs the sheets down the page in one order: glance, method, nutrition, notes, reviews, siblings', async () => {
    const html = await render()
    const order = ['fa-gauge', 'fa-basket-shopping', 'fa-list-ol', 'fa-wheat', 'fa-pen-nib', 'RatingsReviews'].map(
      (needle) => html.indexOf(needle),
    )

    expect(order).toEqual([...order].sort((a, b) => a - b))
    expect(order.every((index) => index > -1)).toBe(true)
    expect(html.indexOf('RatingsReviews')).toBeLessThan(html.indexOf('OtherVariants'))
  })

  it('holds only the two sheets in that grid, nutrition following it full width', async () => {
    const html = await render()
    const grid = html.indexOf('md:grid-cols-[minmax(0,2fr)')

    // Ingredients, method, then the grid itself close before the nutrition sheet opens.
    expect(html.slice(grid, html.indexOf('fa-wheat')).match(/<\/section>/g)).toHaveLength(3)
  })

  it('leaves cook mode outside every slip, since fixed UI never lives inside a tear', async () => {
    const html = await render()

    // The dialog teleports to <body> once mounted, so the server prints no fixed panel anywhere.
    expect(html).not.toContain('role="dialog"')
    expect(html).not.toContain('fixed inset-0')
  })

  it('reads h1 then h2 down the page', async () => {
    const html = await render()

    expect(html.indexOf('<h1')).toBeGreaterThan(-1)
    expect(html.indexOf('<h1')).toBeLessThan(html.indexOf('<h2'))
  })

  it('leaves no Softbound remnant on the page', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\bv-ink\b/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(html).not.toMatch(/\bshadow-/)
    expect(html).not.toMatch(/\btext-(?:danger|warning|success|rating)-ink\b/)
  })
})
