import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeSearchHeader from '~/Components/RecipeSearch/RecipeSearchHeader.vue'

const render = (draft = '', slots?: Record<string, () => unknown>) =>
  renderSsr(RecipeSearchHeader, { draft, createRecipeUrl: '~/create-recipe' }, slots)

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('RecipeSearchHeader sheet', () => {
  it('is the green-washed Library sheet', async () => {
    const html = await render()

    expect(html).toContain('<section class="kcc-slip kcc-tear-3 my-6">')
    // One length: the sheet derives the pencil rule's offset from --pad, and a shorthand makes that calc()
    // invalid, so the ruling would fall back to the top edge.
    expect(html).toContain('style="--pad:24px;"')
    expect(html).toContain('--c:var(--color-green)')
    expect(html).toContain('--x:92%')
    expect(html).toContain('--y:100%')
    expect(html).toContain('--h:80%')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-book-open" aria-hidden="true"></i>')
    expect(html).toContain('Library')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('sets the search pill with its leading glyph and keeps the input hook', async () => {
    const html = await render()

    // Icon, input and the clear glyph share the pill's track.
    expect(tagWith(html, 'kcc-field')).toContain('grid-cols-[auto_1fr_auto]')
    expect(html).toContain('<i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>')
    const input = tagWith(html, 'data-testid="recipe-search-input"')
    expect(input).toContain('placeholder="SearchPlaceholder"')
    expect(tagWith(await render('gnocchi'), 'recipe-search-input')).toContain('value="gnocchi"')
  })

  it('submits through a pill button that keeps the submit hook', async () => {
    const html = await render()

    const submit = tagWith(html, 'data-testid="recipe-search-submit"')
    expect(submit).toContain('kcc-btn')
    expect(submit).toContain('type="submit"')
  })

  it('offers the clear glyph only once the query has text', async () => {
    expect(await render()).not.toContain('fa-xmark')
    expect(await render('gnocchi')).toContain('fa-duotone fa-xmark')
  })

  it('links to the recipe form as the one marker button, tilde resolved', async () => {
    const html = await render()

    const link = tagWith(html, 'href="/create-recipe"')
    expect(link).toContain('class="kcc-btn"')
    expect(html).toContain('CreateRecipe')
  })

  it('holds the results toolbar inside the sheet', async () => {
    const html = await render('', { default: () => 'TOOLBAR' })

    expect(html).toMatch(/kcc-sheet[\s\S]*TOOLBAR[\s\S]*<\/div>/)
  })
})
