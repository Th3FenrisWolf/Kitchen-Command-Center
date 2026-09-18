import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import VariantGrid from '~/Components/RecipeDetail/VariantGrid.vue'
import type { VariantSummary } from '~/Types/Recipe'

const NAMES = ['Brown Butter', 'Crispy Edge', 'Garlic Confit', 'Herb Ricotta', 'Lemon Caper', 'Miso Butter', 'Nutmeg Cream']

const variant = (name: string, index: number): VariantSummary => ({
  name,
  description: `${name}, the way it is made here.`,
  slug: `/recipes/gnocchi/${index}`,
  icon: 'fa-duotone fa-wheat',
  authorName: 'Dana Whitfield',
  tags: ['Vegetarian'],
  totalTime: 20 + index,
  publishedDate: `2026-04-0${index + 1}`,
  reviewCount: 0,
})

const render = (count = NAMES.length) =>
  renderSsr(VariantGrid, {
    variants: NAMES.slice(0, count).map(variant),
    addVariantUrl: '~/recipes/add-variant?recipe=abc',
  })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('VariantGrid', () => {
  it('lays the slips out on the rule, 28px between columns and 36px between rows', async () => {
    const grid = tagWith(await render(), 'repeat\\(auto-fit')

    expect(grid).toContain('grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))]')
    expect(grid).toContain('gap-x-7')
    expect(grid).toContain('gap-y-9')
    expect(grid).not.toContain('row-span')
  })

  it('renders one card per variant, each keeping the variant-name hook', async () => {
    const html = await render(3)

    const names = [...html.matchAll(/data-variant-name="([^"]*)"/g)].map((match) => match[1])
    expect(names).toEqual(NAMES.slice(0, 3))
  })

  it('cycles the six tears down the grid so no two neighbours share one', async () => {
    const html = await render()

    const tears = [...html.matchAll(/kcc-recipe[^"]*kcc-tear-(\d)/g)].map((match) => Number(match[1]))
    expect(tears).toEqual([1, 2, 3, 4, 5, 6, 1])
  })

  it('closes the grid with a blank slip that starts a new variant, on the next tear', async () => {
    const html = await render(3)

    const link = tagWith(html, 'href="/recipes/add-variant\\?recipe=abc"')
    expect(link).toContain('kcc-torn')
    expect(html).toContain('AddVariant')
    // The three cards took tears 1-3, so the invitation takes the fourth.
    expect(tagWith(html, 'kcc-tear-4')).toContain('kcc-slip')
    expect(html).not.toMatch(/\bborder-dashed\b/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/sk-[a-z]/)
  })
})
