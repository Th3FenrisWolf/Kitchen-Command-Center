import { renderSsr } from '../../../support/ssr'
import { describe, expect, it } from 'vitest'
import RecipeFilters from '~/Components/RecipeSearch/RecipeFilters.vue'
import type { RecipeFacets, RecipeTaxonomy } from '~/Types/Recipe'

interface Over {
  facets?: Partial<RecipeFacets>
  options?: Partial<RecipeTaxonomy>
  selected?: Partial<RecipeTaxonomy>
  timeMin?: number
}

const render = ({ facets, options, selected, timeMin = 0 }: Over = {}) =>
  renderSsr(RecipeFilters, {
    facets: { category: {}, diet: {}, style: {}, ...facets },
    options: { categories: [], diets: [], styles: [], ...options },
    selected: { categories: [], diets: [], styles: [], ...selected },
    timeMin,
    timeMax: 60,
  })

// Each `:disabled` input renders the boolean attribute; range-slider inputs never do.
const countDisabled = (html: string) => (html.match(/\sdisabled/g) ?? []).length

const rowFor = (html: string, label: string) =>
  html.match(new RegExp(`<li[^>]*>(?:(?!</li>).)*${label}.*?</li>`, 's'))?.[0] ?? ''

const groupFor = (html: string, legend: string) =>
  html.match(new RegExp(`<legend class="kcc-kick">\\s*${legend}\\s*</legend>.*?</fieldset>`, 's'))?.[0] ?? ''

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
    const html = await render({ options: { categories: ['Breakfast'], diets: ['Vegan'], styles: ['Spicy'] } })

    expect(html).toMatch(/<legend class="kcc-kick">\s*Category\s*<\/legend>/)
    expect(html).toMatch(/<legend class="kcc-kick">\s*Diets\s*<\/legend>/)
    expect(html).toMatch(/<legend class="kcc-kick">\s*Styles\s*<\/legend>/)
    expect(html).toMatch(/<legend class="kcc-kick">\s*TotalTime\s*<\/legend>/)
  })

  it('prints the options as a checklist on the rule, ticked boxes filled', async () => {
    const html = await render({
      options: { categories: ['Breakfast', 'Dessert'] },
      facets: { category: { Breakfast: 4, Dessert: 2 } },
      selected: { categories: ['Dessert'] },
    })

    expect(html).toContain('<ul class="kcc-check">')
    expect(rowFor(html, 'Breakfast')).toContain('class="kcc-box"')
    expect(rowFor(html, 'Dessert')).toContain('kcc-box kcc-box--on')
    // The native checkbox stays for behaviour and assistive tech; the box is the printed mark.
    expect(rowFor(html, 'Dessert')).toContain('type="checkbox"')
    expect(rowFor(html, 'Breakfast')).toMatch(/<span class="kcc-q">4<\/span>/)
  })

  it('hangs the box, the text and the count straight off the row, with nothing wrapping them', async () => {
    const html = await render({ options: { categories: ['Breakfast'] }, facets: { category: { Breakfast: 4 } } })

    // The kit's row is a three-column grid of direct children of the li, and `.kcc-check li.kcc-done`
    // excludes the box and the count by child combinator: a wrapper element breaks both.
    expect(html).not.toContain('class="contents"')
    expect(rowFor(html, 'Breakfast')).toMatch(
      /^<li[^>]*><input id="([^"]+)" type="checkbox" class="sr-only"><label for="\1" class="kcc-box"><\/label><label for="\1">Breakfast<\/label><span class="kcc-q">4<\/span><\/li>$/,
    )
  })

  it('sets the chosen range in Sono and the track ends in kicks', async () => {
    const html = await render({ timeMin: 15 })

    expect(html).toContain('kcc-num')
    expect(html).toContain('15 Min OrMore')
    expect(html).toContain('kcc-range-track')
  })
})

describe('RecipeFilters groups', () => {
  it('lists diets and styles apart', async () => {
    const html = await render({
      options: { diets: ['Vegan'], styles: ['Spicy'] },
      facets: { diet: { Vegan: 3 }, style: { Spicy: 1 } },
    })

    expect(groupFor(html, 'Diets')).toContain('>Vegan<')
    expect(groupFor(html, 'Diets')).not.toContain('Spicy')
    expect(groupFor(html, 'Styles')).toContain('>Spicy<')
    expect(rowFor(html, 'Vegan')).toContain('<span class="kcc-q">3</span>')
    expect(rowFor(html, 'Spicy')).toContain('<span class="kcc-q">1</span>')
  })

  it('keeps the order the options arrive in', async () => {
    const html = await render({
      options: { categories: ['Dinner', 'Breakfast'] },
      facets: { category: { Breakfast: 4, Dinner: 5 } },
    })

    expect(html.indexOf('>Dinner<')).toBeLessThan(html.indexOf('>Breakfast<'))
  })

  it('leaves out a group with nothing to offer', async () => {
    const html = await render({ options: { categories: ['Breakfast'] } })

    expect(html).toMatch(/<legend class="kcc-kick">\s*Category\s*<\/legend>/)
    expect(html).not.toMatch(/>\s*Diets\s*</)
    expect(html).not.toMatch(/>\s*Styles\s*</)
  })
})

describe('RecipeFilters zero-result options', () => {
  it('renders every option even when the current results omit some (no rows dropped)', async () => {
    const html = await render({
      options: { categories: ['Breakfast', 'Dessert', 'Dinner'] },
      facets: { category: { Breakfast: 4 } }, // Dessert + Dinner have no matches right now
    })

    expect(html).toContain('Breakfast')
    expect(html).toContain('Dessert')
    expect(html).toContain('Dinner')
  })

  it('disables the options that have no matches in the current result set', async () => {
    const html = await render({
      options: { categories: ['Breakfast', 'Dessert', 'Dinner'] },
      facets: { category: { Breakfast: 4 } },
    })

    expect(countDisabled(html)).toBe(2)
    expect(html).toContain('cursor-not-allowed')
    expect(rowFor(html, 'Dinner')).toContain('opacity-40')
  })

  it('does not disable options that still have matches', async () => {
    const html = await render({
      options: { categories: ['Breakfast', 'Dessert'] },
      facets: { category: { Breakfast: 4, Dessert: 2 } },
    })

    expect(countDisabled(html)).toBe(0)
  })

  it('keeps a selected option interactive even when it drops to zero matches', async () => {
    const html = await render({
      options: { categories: ['Breakfast', 'Dessert'] },
      facets: { category: { Breakfast: 4 } }, // Dessert is 0 now...
      selected: { categories: ['Dessert'] }, // ...but it's selected, so it must stay toggleable
    })

    expect(countDisabled(html)).toBe(0)
    expect(html).toContain('checked')
  })
})
