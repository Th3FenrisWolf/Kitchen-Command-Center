import { describe, expect, it } from 'vitest'
import KccSheet from '~/Components/Sheet/KccSheet.vue'
import { renderSsr } from '../../../support/ssr'

const render = (props: Record<string, unknown> = {}) => renderSsr(KccSheet, props, { default: () => 'Hello' })

describe('KccSheet', () => {
  // Two comment artifacts are baked into this markup. `<slot />` is always wrapped in `<!--[-->`/`<!--]-->`
  // Fragment anchors — unconditional in dev and prod (`ssrRenderSlot` and the `Fragment` case in
  // `@vue/server-renderer`), and hydration needs them. A false `v-if` leaves `<!--v-if-->` here because the
  // `<component :is>` root resolves to an element, so `normalizeChildren` calls the slot without `_push` and
  // the subtree renders via vnodes; a production compile writes that comment as `<!---->`. Both are
  // invisible; the assertions include them rather than fight them.
  it('renders slip › torn › sheet with the default tear', async () => {
    const html = await render()
    expect(html).toContain(
      '<div class="kcc-slip kcc-tear-1"><div class="kcc-torn"><div class="kcc-sheet"><!--v-if--><!--[-->Hello<!--]--></div></div><!--v-if--><span class="kcc-blank" aria-hidden="true"><span></span></span></div>',
    )
  })

  it('pools the wash under the content with its placement', async () => {
    const html = await render({ wash: 'peach', at: { x: '88%', y: '18%', w: '38%', h: '60%' } })
    expect(html).toContain(
      '<span class="kcc-wash" style="--c:var(--color-peach);--x:88%;--y:18%;--w:38%;--h:60%;" aria-hidden="true"></span><!--[-->Hello<!--]-->',
    )
  })

  it('keeps the label outside the torn wrapper', async () => {
    const html = await render({ label: 'Identity · 01', icon: 'fa-duotone fa-scissors' })
    // `label` now renders through `<slot name="label">{{ label }}</slot>`, so its fallback content picks up
    // the same `<!--[-->`/`<!--]-->` Fragment anchors as any other slot output (`ssrRenderSlot` wraps
    // unconditionally, whether it falls back or a caller slot runs).
    expect(html).toContain(
      '</div></div><span class="kcc-label"><i class="fa-duotone fa-scissors" aria-hidden="true"></i><!--[-->Identity · 01<!--]--></span><span class="kcc-blank" aria-hidden="true"><span></span></span></div>',
    )
  })

  it('offers no way to tape a sheet', async () => {
    expect(await render({ label: 'Identity · 01', tape: true })).not.toContain('kcc-tape')
  })

  it('takes the element, tear and padding it is given', async () => {
    const html = await render({ as: 'article', tear: 'hero', pad: '48px', labelRight: true, label: 'Method' })
    expect(html).toContain('<article class="kcc-slip kcc-slip--wide kcc-tear-hero">')
    expect(html).toContain('<div class="kcc-sheet" style="--pad:48px;">')
    // The `<!--v-if-->` is the icon's placeholder: this call passes no `icon`. The Fragment anchors around
    // `Method` are the `label` slot's fallback content (see the comment above).
    expect(html).toContain('<span class="kcc-label kcc-label--right"><!--v-if--><!--[-->Method<!--]--></span>')
  })

  it('lies flat when crisp', async () => {
    const html = await render({ crisp: true, tear: 2 })
    expect(html).toContain('<div class="kcc-slip kcc-tear-2" style="--r:0;">')
  })

  it('renders rich content passed to the label slot', async () => {
    const html = await renderSsr(
      KccSheet,
      { icon: 'fa-duotone fa-hourglass-half' },
      { default: () => 'Hello', label: () => 'Soon' },
    )
    expect(html).toMatch(
      /<span class="kcc-label"><i class="fa-duotone fa-hourglass-half" aria-hidden="true"><\/i>(?:<!--\[-->)?Soon(?:<!--\]-->)?<\/span>/,
    )
  })

  describe('the blank', () => {
    const BLANK = '<span class="kcc-blank" aria-hidden="true"><span></span></span>'
    const count = (html: string, needle: string) => html.split(needle).length - 1

    it('lies under every sheet once, as the last child of the slip, after the label', async () => {
      const html = await render({ label: 'Method' })
      expect(count(html, 'kcc-blank')).toBe(1)
      expect(html.endsWith(`</span>${BLANK}</div>`)).toBe(true)
      expect(html.indexOf('kcc-label')).toBeLessThan(html.indexOf('kcc-blank'))
    })

    it('is left out of a sheet that is not piled', async () => {
      expect(await render({ piled: false })).not.toContain('kcc-blank')
    })

    it('makes a hero-torn sheet and a wide sheet wide, and no other', async () => {
      expect(await render({ tear: 'hero' })).toContain('class="kcc-slip kcc-slip--wide kcc-tear-hero"')
      expect(await render({ wide: true, tear: 3 })).toContain('class="kcc-slip kcc-slip--wide kcc-tear-3"')
      expect(await render({ tear: 3 })).not.toContain('kcc-slip--wide')
      expect(await render()).not.toContain('kcc-slip--wide')
    })

    it('turns the blank the other way only when flipped', async () => {
      expect(await render({ flip: true, tear: 4 })).toContain('class="kcc-slip kcc-slip--flip kcc-tear-4"')
      expect(await render({ tear: 4 })).not.toContain('kcc-slip--flip')
      expect(await render({ tear: 'hero' })).not.toContain('kcc-slip--flip')
    })
  })
})
