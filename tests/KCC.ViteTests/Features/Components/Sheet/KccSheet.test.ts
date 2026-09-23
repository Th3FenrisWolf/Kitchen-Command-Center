import { describe, expect, it } from 'vitest'
import KccSheet from '~/Components/Sheet/KccSheet.vue'
import { renderSsr } from '../../../support/renderSsr'

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
      '<div class="kcc-slip kcc-tear-1"><div class="kcc-torn"><div class="kcc-sheet"><!--v-if--><!--[-->Hello<!--]--></div></div><!--v-if--><!--v-if--></div>',
    )
  })

  it('pools the wash under the content with its placement', async () => {
    const html = await render({ wash: 'peach', at: { x: '88%', y: '18%', w: '38%', h: '60%' } })
    expect(html).toContain(
      '<span class="kcc-wash" style="--c:var(--color-peach);--x:88%;--y:18%;--w:38%;--h:60%;" aria-hidden="true"></span><!--[-->Hello<!--]-->',
    )
  })

  it('keeps the label and tape outside the torn wrapper', async () => {
    const html = await render({ label: 'Identity · 01', icon: 'fa-duotone fa-scissors', tape: true })
    // `label` now renders through `<slot name="label">{{ label }}</slot>`, so its fallback content picks up
    // the same `<!--[-->`/`<!--]-->` Fragment anchors as any other slot output (`ssrRenderSlot` wraps
    // unconditionally, whether it falls back or a caller slot runs).
    expect(html).toContain(
      '</div></div><span class="kcc-label"><i class="fa-duotone fa-scissors" aria-hidden="true"></i><!--[-->Identity · 01<!--]--></span><span class="kcc-tape" aria-hidden="true"></span></div>',
    )
  })

  it('takes the element, tear and padding it is given', async () => {
    const html = await render({ as: 'article', tear: 'hero', pad: '48px', labelRight: true, label: 'Method' })
    expect(html).toContain('<article class="kcc-slip kcc-tear-hero">')
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
})
