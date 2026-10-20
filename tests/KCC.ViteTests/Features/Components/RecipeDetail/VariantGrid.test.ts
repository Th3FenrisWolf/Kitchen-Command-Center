import { renderSsr, tagWith } from '../../../support/ssr'
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

const render = (count = NAMES.length, props: Record<string, unknown> = {}) =>
  renderSsr(VariantGrid, {
    variants: NAMES.slice(0, count).map(variant),
    addVariantUrl: '/recipes/add-variant?recipe=abc',
    ...props,
  })

const BLANK = '<span class="kcc-blank" aria-hidden="true"><span></span></span>'
const slipClasses = (html: string) =>
  [...html.matchAll(/class="([^"]*)"/g)]
    .map((match) => match[1]!.split(' '))
    .filter((classes) => classes.includes('kcc-slip'))

describe('VariantGrid', () => {
  it('lays the slips out on the rule, 28px between columns and a rule and a half between rows', async () => {
    const grid = tagWith(await render(), 'repeat\\(auto-fit')

    expect(grid).toContain('grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))]')
    expect(grid).toContain('gap-x-7')
    expect(grid).toContain('gap-y-rule-1.5')
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
    expect(html).toMatch(/<span class="kcc-title">\s*[\w.]*AddVariant\s*<\/span>/)
    // The three cards took tears 1-3, so the invitation takes the fourth.
    expect(tagWith(html, 'kcc-tear-4')).toContain('kcc-slip')
    expect(html).not.toMatch(/\bborder-dashed\b/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('stretches the add-variant slip to its row and centres the plus and the label on it', async () => {
    const html = await render(3)

    expect(tagWith(html, 'kcc-tear-4')).toContain('kcc-slip--fill')
    expect(tagWith(html, 'kcc-sheet grid')).toContain('content-center')
    expect(html).toContain('fa-duotone fa-plus kcc-icon-md')
  })

  it('lays the add-variant slip on one blank, its last child', async () => {
    const html = await render(3)
    const addSlip = html.slice(html.lastIndexOf('<div', html.indexOf('href="/recipes/add-variant')))
    expect(tagWith(addSlip, 'class')).toMatch(/\bkcc-slip\b/)
    expect(addSlip.split('kcc-blank')).toHaveLength(2)
    expect(addSlip.endsWith(`</a>${BLANK}</div></div>`)).toBe(true)
  })

  it('lifts the add-variant sheet off its blank through the kit, not through utilities', async () => {
    const html = await render(3)
    expect(tagWith(html, 'kcc-tear-4')).toContain('class="kcc-tear-4 kcc-slip kcc-slip--fill kcc-slip--lift"')
    expect(html).not.toMatch(/transition-transform|hover:-translate-y-1|focus-within:-translate-y-1/)
  })

  it('flips every card and the add-variant slip together, and none unasked', async () => {
    const flipped = slipClasses(await render(3, { flip: true }))
    expect(flipped).toHaveLength(4)
    expect(flipped.every((classes) => classes.includes('kcc-slip--flip'))).toBe(true)

    const unflipped = slipClasses(await render(3))
    expect(unflipped).toHaveLength(4)
    expect(unflipped.some((classes) => classes.includes('kcc-slip--flip'))).toBe(false)
  })
})
