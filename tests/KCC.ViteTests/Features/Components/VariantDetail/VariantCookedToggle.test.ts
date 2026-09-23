import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantCookedToggle from '~/Components/VariantDetail/VariantCookedToggle.vue'

// The tally the toggle prints after a click comes from `/api/variant/{guid}/cooked`, which SSR never
// calls: every render below is the server-rendered starting state. The round trip is covered by
// VariantCookedTests in the e2e suite.
const render = (over: Record<string, unknown> = {}) =>
  renderSsr(VariantCookedToggle, {
    variantGuid: '11111111-2222-3333-4444-555555555555',
    isAuthenticated: true,
    cookedCount: 3,
    ...over,
  })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('VariantCookedToggle', () => {
  it('is one pill the cook presses, hairline until they have made it', async () => {
    const button = tagWith(await render(), 'data-testid="cooked-toggle"')

    expect(button).toMatch(/^<button /)
    expect(button).toContain('kcc-btn kcc-btn--ghost')
    expect(button).toContain('aria-pressed="false"')
  })

  it('fills with ink once it is pressed', async () => {
    const button = tagWith(await render({ hasCooked: true }), 'data-testid="cooked-toggle"')

    expect(button).toContain('kcc-btn kcc-btn--ink')
    expect(button).toContain('aria-pressed="true"')
  })

  it('keeps the tally in parentheses, set in Sono, so the e2e can read the count from it', async () => {
    const html = await render()

    expect(html).toContain('(<span class="kcc-num">3</span>)')
    expect(html).toMatch(/ICookedThis[\s\S]*\(<span class="kcc-num">3<\/span>\)/)
  })

  it('lights the burner in duotone, never as a second solid glyph', async () => {
    const html = await render({ hasCooked: true })

    expect(html).toContain('<i class="fa-duotone fa-fire-burner" aria-hidden="true"></i>')
    expect(html).not.toContain('fa-solid fa-fire-burner')
    expect(html).not.toContain('fa-regular fa-fire-burner')
  })

  it('waits for the round trip rather than taking a second click', async () => {
    // `busy` is false on the server, so the disabled attribute is absent until the click sets it.
    expect(tagWith(await render(), 'data-testid="cooked-toggle"')).not.toContain('disabled')
  })

  it('shows nothing at all to a reader who is not signed in', async () => {
    expect(await render({ isAuthenticated: false })).not.toContain('cooked-toggle')
  })

  it('carries no Softbound remnant, weight or paper radius', async () => {
    const html = await render({ hasCooked: true })

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toContain('font-bold')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toContain('bg-paper-2')
    expect(html).not.toContain('transition-colors')
  })
})
