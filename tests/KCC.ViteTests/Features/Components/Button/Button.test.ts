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

  it('disables a button natively', async () => {
    const html = await render({ disabled: true })
    expect(html).toContain('<button')
    expect(html).toContain('disabled')
  })

  it('carries no Softbound hook', async () => {
    const html = await render()
    expect(html).not.toContain('data-ink')
    expect(html).not.toContain('sk-')
  })
})
