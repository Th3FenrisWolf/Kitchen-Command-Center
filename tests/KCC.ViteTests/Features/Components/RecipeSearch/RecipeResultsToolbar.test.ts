import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import RecipeResultsToolbar from '~/Components/RecipeSearch/RecipeResultsToolbar.vue'

const render = (over: Record<string, unknown> = {}) =>
  renderSsr(RecipeResultsToolbar, { heading: '42 Recipes', sort: 'relevant', view: 'grid', ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('RecipeResultsToolbar', () => {
  it('sets the count line at heading size and keeps it an h2', async () => {
    const html = await render()

    expect(html).toMatch(/<h2 class="kcc-h4[^"]*">42 Recipes<\/h2>/)
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('names the sort group with a kick and prints both groups as segmented pills', async () => {
    const html = await render()

    expect(html).toMatch(/<span class="kcc-kick">\s*Sort\s*<\/span>/)
    expect(html.match(/class="kcc-seg"/g)).toHaveLength(2)
    expect(tagWith(html, 'aria-label="Sort"')).toContain('role="radiogroup"')
    expect(tagWith(html, 'aria-label="View"')).toContain('role="radiogroup"')
  })

  it('keeps every segment hook and marks the current one', async () => {
    const html = await render({ view: 'list' })

    for (const hook of ['sort-relevant', 'sort-rated', 'sort-variants', 'sort-recent', 'view-grid', 'view-list']) {
      expect(html).toContain(`data-testid="${hook}"`)
    }
    expect(tagWith(html, 'data-testid="view-list"')).toContain('aria-checked="true"')
    expect(tagWith(html, 'data-testid="sort-relevant"')).toContain('aria-checked="true"')
  })

  it('draws the view glyphs duotone like every other icon', async () => {
    const html = await render()

    expect(html).toContain('fa-duotone fa-table-cells-large')
    expect(html).toContain('fa-duotone fa-list')
  })
})
