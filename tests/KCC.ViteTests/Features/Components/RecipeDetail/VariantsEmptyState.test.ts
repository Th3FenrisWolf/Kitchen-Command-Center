import { renderSsr, tagWith } from '../../../support/ssr'
import { chosenInk, inkOf, washFields } from '../../../support/washes'
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

  it('blooms three washes as a reading sheet, the first in sky', async () => {
    const fields = washFields(await render())

    expect(fields).toHaveLength(1)
    expect(fields[0]).toHaveLength(3)
    expect(inkOf(fields[0][0])).toBe(chosenInk('sky'))
  })

  it('says what happened at heading size and leaves the hint as the one hand note', async () => {
    const html = await render()

    expect(html).toMatch(/<p class="kcc-title">\s*NoVariantsMatch\s*<\/p>/)
    expect(html).toMatch(/<p class="kcc-hand">\s*TryDifferentFilter\s*<\/p>/)
    expect((html.match(/kcc-hand/g) ?? []).length).toBe(1)
    expect(html).toContain('fa-duotone fa-bowl-food kcc-icon-lg')
  })

  it('offers the way back as a hairline pill', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-btn--ghost')).toContain('kcc-btn kcc-btn--ghost')
    expect(html).toContain('ClearFilters')
  })
})
