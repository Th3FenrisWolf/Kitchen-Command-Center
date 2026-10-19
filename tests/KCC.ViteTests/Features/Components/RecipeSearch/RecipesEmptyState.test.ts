import { renderSsr, tagWith } from '../../../support/ssr'
import { describe, expect, it } from 'vitest'
import RecipesEmptyState from '~/Components/RecipeSearch/RecipesEmptyState.vue'

const render = () => renderSsr(RecipesEmptyState)

describe('RecipesEmptyState', () => {
  it('is a butter-washed sheet carrying the e2e hook on its slip', async () => {
    const html = await render()

    const slip = tagWith(html, 'data-testid="recipes-empty"')
    expect(slip).toContain('kcc-slip kcc-slip--wide kcc-tear-2')
    expect(html).toContain('--c:var(--color-yellow)')
    expect(html).toContain('--x:18%')
    expect(html).not.toContain('kcc-tape')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'data-testid="recipes-empty"')).toContain('kcc-slip--wide')
  })

  it('says what happened at heading size and how to fix it in body copy', async () => {
    const html = await render()

    expect(html).toMatch(/<p class="kcc-title">\s*NoRecipesMatch\s*<\/p>/)
    expect(html).toMatch(/class="kcc-body[^"]*">\s*NoRecipesHint\s*<\/p>/)
    expect(html).toContain('fa-duotone fa-bowl-food kcc-icon-lg')
  })

  it('offers the way back as a hairline pill', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-btn--ghost')).toContain('kcc-btn kcc-btn--ghost')
    expect(html).toContain('ClearAllFilters')
  })
})
