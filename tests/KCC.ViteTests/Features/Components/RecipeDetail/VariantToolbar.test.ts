import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantToolbar from '~/Components/RecipeDetail/VariantToolbar.vue'
import type { SortKey, ViewMode } from '~/Components/RecipeDetail/variantFilters'

interface ToolbarProps {
  search?: string
  sort?: SortKey
  view?: ViewMode
  tag?: string
  tags?: string[]
}

const render = ({
  search = '',
  sort = 'newest',
  view = 'grid',
  tag = '',
  tags = ['', 'Quick', 'Vegan'],
}: ToolbarProps = {}) => renderSsr(VariantToolbar, { search, sort, view, tag, tags })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

/** Every rendered button as its opening tag plus the text it carries, comment anchors stripped. */
const buttons = (html: string) =>
  [...html.matchAll(/<button([^>]*)>([\s\S]*?)<\/button>/g)].map((match) => ({
    open: match[1]!,
    text: match[2]!
      .replace(/<!--.*?-->/g, '')
      .replace(/<[^>]*>/g, '')
      .trim(),
  }))

describe('VariantToolbar', () => {
  it('is a printed control row, not a sheet', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toContain('kcc-sheet')
    expect(html).not.toContain('sticky')
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
  })

  it('sets the search as a pill with its leading glyph', async () => {
    const html = await render({ search: 'jalapeno' })

    expect(tagWith(html, 'kcc-field')).toContain('kcc-field')
    expect(html).toContain('<i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>')
    const input = tagWith(html, 'placeholder="SearchVariants"')
    expect(input).toContain('value="jalapeno"')
  })

  it('names the sort with a kick and keeps every segment hook', async () => {
    const html = await render({ sort: 'fastest' })

    expect(html).toMatch(/<span class="kcc-kick">\s*Sort\s*<\/span>/)
    expect(tagWith(html, 'aria-label="Sort"')).toContain('kcc-seg')
    expect(tagWith(html, 'data-testid="sort-fastest"')).toContain('aria-checked="true"')
    expect(tagWith(html, 'data-testid="sort-newest"')).toContain('aria-checked="false"')
    expect(html).toContain('data-testid="sort-rating"')
  })

  it('keeps the view toggle on duotone glyphs', async () => {
    const html = await render({ view: 'list' })

    expect(tagWith(html, 'aria-label="View"')).toContain('kcc-seg')
    expect(tagWith(html, 'data-testid="view-list"')).toContain('aria-checked="true"')
    expect(tagWith(html, 'data-testid="view-grid"')).toContain('aria-checked="false"')
    expect(html).toContain('fa-duotone fa-table-cells-large')
    expect(html).toContain('fa-duotone fa-list')
  })

  it('leaves the tag row off when the variants carry no tags to filter by', async () => {
    const html = await render({ tags: [''] })

    expect(buttons(html).filter((button) => button.open.includes('kcc-btn'))).toEqual([])
  })

  it('prints the tags as pills, the chosen one filled in ink', async () => {
    const html = await render({ tag: 'Quick' })

    const pills = buttons(html).filter((button) => button.open.includes('kcc-btn'))
    expect(pills.map((pill) => pill.text)).toEqual(['All', 'Quick', 'Vegan'])
    expect(pills.map((pill) => pill.open.includes('kcc-btn--ink'))).toEqual([false, true, false])
    expect(pills.map((pill) => pill.open.includes('aria-pressed="true"'))).toEqual([false, true, false])
  })
})
