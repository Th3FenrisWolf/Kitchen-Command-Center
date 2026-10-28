import { describe, expect, it } from 'vitest'
import { createSSRApp, h } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { planSheet } from '~/Bloom/washes'
import KccSheet from '~/Components/Sheet/KccSheet.vue'
import { renderSsr } from '../../../support/ssr'

const render = (props: Record<string, unknown> = {}) =>
  renderSsr(KccSheet, { washKey: 'sheet', ...props }, { default: () => 'Hello' })

const washCount = (html: string) => html.split('class="kcc-wash"').length - 1

const fieldOf = (washKey: string, size: 'card' | 'reading' | 'page') =>
  `<span class="kcc-washes" aria-hidden="true"><!--[-->${planSheet({ seed: 0, key: `|${washKey}`, size })
    .map((wash) => {
      const style = Object.entries(wash.style)
        .map(([property, value]) => `${property}:${value}`)
        .join(';')
      return `<span class="kcc-wash" data-wash="${wash.id}" style="${style}"></span>`
    })
    .join('')}<!--]--></span>`

describe('KccSheet', () => {
  // Two comment artifacts are baked into this markup. `<slot />` is always wrapped in `<!--[-->`/`<!--]-->`
  // Fragment anchors — unconditional in dev and prod (`ssrRenderSlot` and the `Fragment` case in
  // `@vue/server-renderer`), and hydration needs them. A false `v-if` leaves `<!--v-if-->` here because the
  // `<component :is>` root resolves to an element, so `normalizeChildren` calls the slot without `_push` and
  // the subtree renders via vnodes; a production compile writes that comment as `<!---->`. Both are
  // invisible; the assertions include them rather than fight them.
  it('renders slip › fall, torn › sheet with the default tear', async () => {
    const html = await render()
    expect(html).toContain(
      `<div class="kcc-slip kcc-tear-1"><span class="kcc-fall" aria-hidden="true"><span></span></span><div class="kcc-torn"><div class="kcc-sheet">${fieldOf('sheet', 'card')}<!--[-->Hello<!--]--></div></div><!--v-if--><span class="kcc-blank" aria-hidden="true"><span></span></span></div>`,
    )
  })

  describe('the washes', () => {
    it('lie in a field that is the sheet’s first child, inside its tear', async () => {
      const html = await render({ label: 'Method' })
      expect(html).toContain(`<div class="kcc-torn"><div class="kcc-sheet">${fieldOf('sheet', 'card')}<!--[-->`)
      expect(html.indexOf('kcc-washes')).toBeLessThan(html.indexOf('</div></div>'))
    })

    it('take no placement from at', async () => {
      const html = await render({ wash: 'peach', at: { x: '88%', y: '18%', w: '38%', h: '60%' } })
      expect(html).not.toContain('--x:88%')
      expect(html).not.toContain('--c:')
      expect(html).not.toMatch(/<span class="kcc-wash" style=/)
    })

    it('wear the chosen colour on wash 0', async () => {
      const html = await render({ wash: 'peach' })
      expect(html).toMatch(
        /<span class="kcc-washes" aria-hidden="true"><!--\[--><span class="kcc-wash" data-wash="[0-9a-f]{8}" style="[^"]*--wash-ink:var\(--color-peach\);/,
      )
    })

    it('number four on a hero tear, three with a margin, four on a wide sheet, else one or two', async () => {
      expect(washCount(await render({ tear: 'hero' }))).toBe(4)
      expect(washCount(await render({ margin: true }))).toBe(3)
      expect(washCount(await render({ margin: true, wide: true }))).toBe(3)
      expect(washCount(await render({ wide: true }))).toBe(4)
      const cards = await Promise.all(Array.from({ length: 12 }, (_, i) => render({ washKey: `card-${i}` })))
      expect(new Set(cards.map(washCount))).toEqual(new Set([1, 2]))
    })

    it('follow bloom over the sheet’s shape', async () => {
      expect(washCount(await render({ tear: 'hero', bloom: 'card', washKey: 'card-0' }))).toBeLessThanOrEqual(2)
      expect(washCount(await render({ bloom: 'page' }))).toBe(4)
    })

    it('leave no field with bloom none', async () => {
      expect(await render({ bloom: 'none', wash: 'peach', tear: 'hero' })).not.toContain('kcc-wash')
    })

    it('collect the slots of a global key into the provided bloom', async () => {
      const bloom = createBloom(0, '/')
      const app = createSSRApp({ render: () => h(KccSheet, { washKey: '@pad', washSlots: [0, 4, 8, 12], tear: 'hero' }) })
      app.provide(BLOOM_KEY, bloom)
      await renderToString(app)
      expect(bloom.collected().map(({ key, index, slot }) => ({ key, index, slot }))).toEqual(
        [0, 4, 8, 12].map((slot, index) => ({ key: '@pad', index, slot })),
      )
    })

    it('pass the wash key and slots through', async () => {
      const html = await render({ washKey: '@pad', washSlots: [0, 4, 8, 12], tear: 'hero' })
      const pad = planSheet({ seed: 0, key: '@pad', size: 'page' })
      expect(pad.map((wash) => `data-wash="${wash.id}"`).every((id) => html.includes(id))).toBe(true)
      expect(html).not.toContain(planSheet({ seed: 0, key: '|sheet', size: 'page' })[0].id)
    })
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

  it('rules a margin with margin', async () => {
    expect(await render({ margin: true })).toContain('<div class="kcc-sheet kcc-sheet--margin">')
    expect(await render()).not.toContain('kcc-sheet--margin')
  })

  it('marks a sheet that scrolls itself with scroll', async () => {
    expect(await render({ scroll: true })).toContain('<div class="kcc-sheet kcc-sheet--scroll">')
    expect(await render()).not.toContain('kcc-sheet--scroll')
  })

  it('draws no ruling with blank', async () => {
    expect(await render({ blank: true })).toContain('<div class="kcc-sheet kcc-sheet--blank">')
    expect(await render()).not.toContain('kcc-sheet--blank')
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
      { washKey: 'sheet', icon: 'fa-duotone fa-hourglass-half' },
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
