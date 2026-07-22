import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import RecipeFilters from '~/Components/RecipeSearch/RecipeFilters.vue'

interface Props {
  categoryFacets?: Record<string, number>
  dietFacets?: Record<string, number>
  categoryOptions?: string[]
  dietOptions?: string[]
  selectedCategories?: string[]
  selectedDiets?: string[]
}

const render = (over: Props = {}) =>
  renderToString(
    createSSRApp(RecipeFilters, {
      categoryFacets: {},
      dietFacets: {},
      categoryOptions: [],
      dietOptions: [],
      selectedCategories: [],
      selectedDiets: [],
      timeMin: 0,
      timeMax: 60,
      ...over,
    }),
  )

// Each `:disabled` input renders the boolean attribute; range-slider inputs never do.
const countDisabled = (html: string) => (html.match(/\sdisabled/g) ?? []).length

describe('RecipeFilters zero-result options', () => {
  it('renders every option even when the current results omit some (no rows dropped)', async () => {
    const html = await render({
      categoryOptions: ['Breakfast', 'Dessert', 'Dinner'],
      categoryFacets: { Breakfast: 4 }, // Dessert + Dinner have no matches right now
    })
    expect(html).toContain('Breakfast')
    expect(html).toContain('Dessert')
    expect(html).toContain('Dinner')
  })

  it('disables the options that have no matches in the current result set', async () => {
    const html = await render({
      categoryOptions: ['Breakfast', 'Dessert', 'Dinner'],
      categoryFacets: { Breakfast: 4 },
    })
    // Dessert and Dinner are greyed out + disabled; Breakfast stays interactive.
    expect(countDisabled(html)).toBe(2)
    expect(html).toContain('cursor-not-allowed')
  })

  it('does not disable options that still have matches', async () => {
    const html = await render({
      categoryOptions: ['Breakfast', 'Dessert'],
      categoryFacets: { Breakfast: 4, Dessert: 2 },
    })
    expect(countDisabled(html)).toBe(0)
  })

  it('keeps a selected option interactive even when it drops to zero matches', async () => {
    const html = await render({
      categoryOptions: ['Breakfast', 'Dessert'],
      categoryFacets: { Breakfast: 4 }, // Dessert is 0 now...
      selectedCategories: ['Dessert'], // ...but it's selected, so it must stay toggleable
    })
    expect(countDisabled(html)).toBe(0)
    expect(html).toContain('checked')
  })
})
