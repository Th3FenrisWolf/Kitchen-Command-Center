import { describe, expect, it } from 'vitest'
import Badge from '~/Components/Badge/Badge.vue'
import { renderSsr } from '../../../support/renderSsr'

describe('Badge', () => {
  it('renders a kcc-badge pill around its slot content', async () => {
    const html = await renderSsr(Badge, {}, { default: () => 'Vegetarian' })
    expect(html).toContain('<span class="kcc-badge">')
    expect(html).toContain('Vegetarian')
  })

  it('carries no Softbound hook', async () => {
    const html = await renderSsr(Badge, {}, { default: () => 'One pan' })
    expect(html).not.toContain('sk-')
  })
})
