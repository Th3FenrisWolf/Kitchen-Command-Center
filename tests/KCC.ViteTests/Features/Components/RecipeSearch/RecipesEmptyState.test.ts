import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import RecipesEmptyState from '~/Components/RecipeSearch/RecipesEmptyState.vue'

const render = () => renderSsr(RecipesEmptyState)

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('RecipesEmptyState', () => {
  it('is a taped, butter-washed sheet carrying the e2e hook on its slip', async () => {
    const html = await render()

    const slip = tagWith(html, 'data-testid="recipes-empty"')
    expect(slip).toContain('kcc-slip kcc-tear-2')
    expect(html).toContain('--c:var(--color-yellow)')
    expect(html).toContain('--x:18%')
    expect(html).toContain('<span class="kcc-tape" aria-hidden="true"></span>')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('says what happened at heading size and how to fix it in body copy', async () => {
    const html = await render()

    expect(html).toMatch(/<p class="kcc-h4">\s*NoRecipesMatch\s*<\/p>/)
    expect(html).toMatch(/class="kcc-body[^"]*">\s*NoRecipesHint\s*<\/p>/)
    expect(html).toContain('fa-duotone fa-bowl-food')
  })

  it('offers the way back as a hairline pill', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-btn--ghost')).toContain('kcc-btn kcc-btn--ghost')
    expect(html).toContain('ClearAllFilters')
  })
})
