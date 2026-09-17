import { describe, expect, it } from 'vitest'
import KccButton from '~/Components/Button/Button.vue'
import { renderSsr } from '../../../support/renderSsr'

const render = (props: Record<string, unknown> = {}) => renderSsr(KccButton, props, { default: () => 'Go' })

describe('KccButton', () => {
  it('is the marker pill by default: no modifier class', async () => {
    const html = await render()
    expect(html).toContain('class="kcc-btn"')
  })

  it('takes the ghost and large modifiers', async () => {
    const html = await render({ variant: 'ghost', size: 'lg' })
    expect(html).toContain('class="kcc-btn kcc-btn--ghost kcc-btn--lg"')
  })

  it('takes the ink modifier', async () => {
    const html = await render({ variant: 'ink' })
    expect(html).toContain('kcc-btn--ink')
  })

  it('takes the text modifier', async () => {
    const html = await render({ variant: 'text' })
    expect(html).toContain('kcc-btn--text')
  })

  it('renders as an anchor with an href and no type attribute', async () => {
    const html = await render({ as: 'a', href: '/x' })
    expect(html).toContain('<a')
    expect(html).toContain('href="/x"')
    expect(html).not.toContain('type=')
  })

  it('marks a disabled anchor aria-disabled rather than disabled', async () => {
    const html = await render({ as: 'a', href: '/x', disabled: true })
    expect(html).toContain('aria-disabled="true"')
  })

  it('disables a button natively, not with aria-disabled', async () => {
    const html = await render({ disabled: true })
    expect(html).toContain('<button class="kcc-btn" type="button" disabled')
    expect(html).not.toContain('aria-disabled')
  })

  it('passes a submit type through and never gives a button an href', async () => {
    const html = await render({ type: 'submit', href: '/ignored' })
    expect(html).toContain('type="submit"')
    expect(html).not.toContain('href=')
  })

  it.each(['marker', 'ghost', 'ink', 'text'] as const)('carries no Softbound hook as %s', async (variant) => {
    const html = await render({ variant })
    expect(html).not.toContain('data-ink')
    expect(html).not.toContain('sk-')
  })
})
