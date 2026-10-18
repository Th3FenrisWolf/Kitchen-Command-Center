import { renderSsr, tagWith } from '../../../support/ssr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import VariantList from '~/Components/RecipeDetail/VariantList.vue'
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
  publishedDate: '2026-04-01',
  reviewCount: 0,
})

const render = (props: Record<string, unknown> = {}) => renderSsr(VariantList, { variants: NAMES.map(variant), ...props })

const BLANK = '<span class="kcc-blank" aria-hidden="true"><span></span></span>'
const rowsOf = (html: string) => html.split('<div class="kcc-slip ').slice(1)

describe('VariantList', () => {
  it('stacks the rows 36px apart', async () => {
    const stack = tagWith(await render(), 'flex flex-col')

    expect(stack).toContain('gap-y-9')
  })

  it('renders one row per variant, each keeping the variant-name hook', async () => {
    const html = await render()

    const names = [...html.matchAll(/data-variant-name="([^"]*)"/g)].map((match) => match[1])
    expect(names).toEqual(NAMES)
  })

  it('cycles the six tears down the stack so no two neighbours share one', async () => {
    const html = await render()

    const tears = [...html.matchAll(/kcc-slip[^"]*kcc-tear-(\d)/g)].map((match) => Number(match[1]))
    expect(tears).toEqual([1, 2, 3, 4, 5, 6, 1])
  })

  it('lays each row on one blank, the last child of its slip', async () => {
    const rows = rowsOf(await render())
    expect(rows).toHaveLength(NAMES.length)
    for (const row of rows) {
      expect(row.split('kcc-blank')).toHaveLength(2)
      expect(row).toMatch(new RegExp(`</a>${BLANK}</div>(?:<!--\\]-->)?(?:</div>)?$`))
    }
  })

  it('flips every row only when asked', async () => {
    const flipped = rowsOf(await render({ flip: true }))
    expect(flipped).toHaveLength(NAMES.length)
    expect(flipped.every((row) => row.slice(0, row.indexOf('"')).split(' ').includes('kcc-slip--flip'))).toBe(true)
    expect(await render()).not.toContain('kcc-slip--flip')
  })
})
