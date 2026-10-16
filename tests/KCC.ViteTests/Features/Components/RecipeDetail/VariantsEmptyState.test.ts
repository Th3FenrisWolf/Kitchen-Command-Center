import { renderSsr, tagWith } from '../../../support/ssr'
import { describe, expect, it } from 'vitest'
import VariantsEmptyState from '~/Components/RecipeDetail/VariantsEmptyState.vue'

const render = () => renderSsr(VariantsEmptyState)

describe('VariantsEmptyState', () => {
  it('is a powder-washed sheet on the fourth tear', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-4')
    expect(html).not.toContain('kcc-tape')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip--wide')
  })

  it('pools the wash in the top-right corner, clear of the centred copy', async () => {
    const wash = tagWith(await render(), 'kcc-wash')

    expect(wash).toContain('--c:var(--color-sky)')
    expect(wash).toContain('--x:88%')
    expect(wash).toContain('--y:14%')
  })

  it('says what happened at heading size and leaves the hint as the one hand note', async () => {
    const html = await render()

    expect(html).toMatch(/<p class="kcc-h4">\s*NoVariantsMatch\s*<\/p>/)
    expect(html).toMatch(/<p class="kcc-hand">\s*TryDifferentFilter\s*<\/p>/)
    expect((html.match(/kcc-hand/g) ?? []).length).toBe(1)
    expect(html).toContain('fa-duotone fa-bowl-food')
  })

  it('offers the way back as a hairline pill', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-btn--ghost')).toContain('kcc-btn kcc-btn--ghost')
    expect(html).toContain('ClearFilters')
  })
})
