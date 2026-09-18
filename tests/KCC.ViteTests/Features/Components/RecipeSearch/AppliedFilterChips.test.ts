import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import AppliedFilterChips from '~/Components/RecipeSearch/AppliedFilterChips.vue'
import type { FilterChip } from '~/Pages/RecipeSearch/recipeSearchCriteria'

const chips: FilterChip[] = [
  { label: '“gnocchi”', kind: 'query' },
  { label: 'Dessert', kind: 'category', value: 'Dessert' },
]

const render = (over: FilterChip[] = chips) => renderSsr(AppliedFilterChips, { chips: over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('AppliedFilterChips', () => {
  it('prints each applied filter as a hairline badge that removes itself', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-badges')).toContain('kcc-badges')
    expect(html.match(/kcc-badge(?!s)/g)).toHaveLength(2)
    expect(html).toMatch(/<button class="kcc-badge[^"]*"[^>]*>\s*Dessert\s*<i class="fa-duotone fa-xmark"/)
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('clears everything through a text button', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-btn--text')).toContain('kcc-btn kcc-btn--text')
    expect(html).toContain('ClearAll')
  })

  it('renders nothing when no filter is applied', async () => {
    expect(await render([])).toBe('<!---->')
  })
})
