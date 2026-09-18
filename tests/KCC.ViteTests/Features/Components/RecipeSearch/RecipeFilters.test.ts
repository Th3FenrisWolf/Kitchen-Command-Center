import { renderSsr } from '../../../support/renderSsr'
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
  renderSsr(RecipeFilters, {
    categoryFacets: {},
    dietFacets: {},
    categoryOptions: [],
    dietOptions: [],
    selectedCategories: [],
    selectedDiets: [],
    timeMin: 0,
    timeMax: 60,
    ...over,
  })

// Each `:disabled` input renders the boolean attribute; range-slider inputs never do.
const countDisabled = (html: string) => (html.match(/\sdisabled/g) ?? []).length

const rowFor = (html: string, label: string) =>
  html.match(new RegExp(`<li[^>]*>(?:(?!</li>).)*${label}.*?</li>`, 's'))?.[0] ?? ''

describe('RecipeFilters sheet', () => {
  it('is a crisp labelled sheet on its own tear', async () => {
    const html = await render()

    expect(html).toContain('kcc-slip kcc-tear-5')
    // Crisp: a panel read at arm's length while ticking boxes does not tilt.
    expect(html).toContain('--r:0')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-sliders" aria-hidden="true"></i>')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('keeps the panel heading for assistive tech and resets through a text button', async () => {
    const html = await render()

    expect(html).toMatch(/<h2 class="sr-only">\s*Filters\s*<\/h2>/)
    expect(html).toContain('kcc-btn kcc-btn--text')
    expect(html).toContain('Reset')
  })

  it('titles each group with a kick legend', async () => {
    const html = await render()

    expect(html).toMatch(/<legend class="kcc-kick">\s*Category\s*<\/legend>/)
    expect(html).toMatch(/<legend class="kcc-kick">\s*Dietary\s*<\/legend>/)
    expect(html).toMatch(/<legend class="kcc-kick">\s*TotalTime\s*<\/legend>/)
  })

  it('prints the options as a checklist on the rule, ticked boxes filled', async () => {
    const html = await render({
      categoryOptions: ['Breakfast', 'Dessert'],
      categoryFacets: { Breakfast: 4, Dessert: 2 },
      selectedCategories: ['Dessert'],
    })

    expect(html).toContain('<ul class="kcc-check">')
    expect(rowFor(html, 'Breakfast')).toContain('class="kcc-box"')
    expect(rowFor(html, 'Dessert')).toContain('kcc-box kcc-box--on')
    // The native checkbox stays for behaviour and assistive tech; the box is the printed mark.
    expect(rowFor(html, 'Dessert')).toContain('type="checkbox"')
    expect(rowFor(html, 'Breakfast')).toMatch(/<span class="kcc-q">4<\/span>/)
  })

  it('sets the chosen range in Sono and the track ends in kicks', async () => {
    const html = await render({ timeMin: 15 })

    expect(html).toContain('kcc-num')
    expect(html).toContain('15 Min OrMore')
    expect(html).toContain('kcc-range-track')
  })
})

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
    expect(rowFor(html, 'Dinner')).toContain('opacity-40')
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
