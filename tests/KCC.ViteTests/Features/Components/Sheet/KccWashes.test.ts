// @vitest-environment happy-dom
import { createApp, createSSRApp, h, nextTick, ref, type Component } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it, vi } from 'vitest'
import { BLOOM_KEY, createBloom, type Bloom } from '~/Bloom/bloomContext'
import { planSheet, type PlannedWash, type WashSize } from '~/Bloom/washes'
import KccWashes from '~/Components/Sheet/KccWashes.Component.vue'

const SEED = 2671381617
const PAGE = '/recipes/brown-butter-gnocchi'

interface WashesProps {
  washKey: string
  size?: WashSize
  chosen?: string
  washSlots?: number[]
}

function renderIn(bloom: Bloom | undefined, ...fields: (WashesProps | false)[]): Promise<string> {
  const present = fields.filter((props) => props !== false)
  const app = createSSRApp({
    render: () => {
      const vnodes = present.map((props) => h(KccWashes as Component, props))
      return vnodes.length === 1 ? vnodes[0] : vnodes
    },
  })
  if (bloom) app.provide(BLOOM_KEY, bloom)
  return renderToString(app)
}

const styleOf = (wash: PlannedWash) =>
  Object.entries(wash.style)
    .map(([property, value]) => `${property}:${value}`)
    .join(';')

const washMarkup = (wash: PlannedWash) => `<span class="kcc-wash" data-wash="${wash.id}" style="${styleOf(wash)}"></span>`

// The `<!--[-->`/`<!--]-->` pair is the `v-for` Fragment's anchors, which hydration needs.
const fieldMarkup = (washes: PlannedWash[]) =>
  `<span class="kcc-washes" aria-hidden="true"><!--[-->${washes.map(washMarkup).join('')}<!--]--></span>`

const fieldFor = (html: string, id: string) => {
  const start = html.lastIndexOf('<span class="kcc-washes"', html.indexOf(`data-wash="${id}"`))
  const end = '<!--]--></span>'
  return html.slice(start, html.indexOf(end, start) + end.length)
}

const fallbackStarts = (html: string) =>
  [...html.matchAll(/--wash-fallback-at:([\d.]+)s/g)].map((match) => Number.parseFloat(match[1]))

describe('KccWashes', () => {
  it('renders one wash per planned wash, with its id and the fixed-decimal style, in a hidden field', async () => {
    const planned = planSheet({ seed: SEED, key: `${PAGE}|variant-instructions`, size: 'reading' })

    const html = await renderIn(createBloom(SEED, PAGE), { washKey: 'variant-instructions', size: 'reading' })

    expect(planned).toHaveLength(3)
    expect(html).toBe(fieldMarkup(planned))
    expect(html).toMatch(/style="--wash-x:(\.\d{3});--wash-y:\.\d{3};/)
  })

  it('renders the same markup for the same seed, page and key, and other markup for another seed', async () => {
    const field = { washKey: 'hero', size: 'page' as const }

    const first = await renderIn(createBloom(SEED, PAGE), field)
    const again = await renderIn(createBloom(SEED, PAGE), field)
    const otherSeed = await renderIn(createBloom(SEED + 1, PAGE), field)

    expect(again).toBe(first)
    expect(otherSeed).not.toBe(first)
  })

  it('takes seed 0 and no page without a provided bloom', async () => {
    const html = await renderIn(undefined, { washKey: 'hero', size: 'page' })

    expect(html).toBe(fieldMarkup(planSheet({ seed: 0, key: '|hero', size: 'page' })))
  })

  it('renders nothing for size none', async () => {
    const html = await renderIn(createBloom(SEED, PAGE), { washKey: 'bare', size: 'none' })

    expect(html).not.toContain('kcc-wash')
  })

  it('wears the chosen colour on wash 0', async () => {
    const html = await renderIn(createBloom(SEED, PAGE), { washKey: 'hero', size: 'page', chosen: 'teal' })

    expect(html).toMatch(
      /^<span class="kcc-washes" aria-hidden="true"><!--\[--><span class="kcc-wash" data-wash="[0-9a-f]{8}" style="[^"]*--wash-ink:var\(--color-teal\);/,
    )
  })

  it('collects every wash into the provided bloom, with its slot on a global key', async () => {
    const bloom = createBloom(SEED, PAGE)

    await renderIn(
      bloom,
      { washKey: 'variant-instructions', size: 'reading', washSlots: [9, 10, 11] },
      { washKey: '@pad', size: 'page', washSlots: [0, 4, 8, 12] },
    )

    const page = planSheet({ seed: SEED, key: `${PAGE}|variant-instructions`, size: 'reading' })
    const pad = planSheet({ seed: SEED, key: '@pad', size: 'page' })
    expect(bloom.collected()).toEqual([
      ...page.map(({ id, key, index }) => ({ id, key, index })),
      ...pad.map(({ id, key, index }) => ({ id, key, index, slot: [0, 4, 8, 12][index] })),
    ])
  })

  it('suffixes a repeated key on the page', async () => {
    const html = await renderIn(
      createBloom(SEED, PAGE),
      { washKey: 'review', size: 'reading' },
      { washKey: 'review', size: 'reading' },
    )

    const second = planSheet({ seed: SEED, key: `${PAGE}|review~2`, size: 'reading' })
    expect(html).toContain(fieldMarkup(second))
  })

  it('gives a sheet that unmounts and mounts again on one page its key back, with the same arrangement', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    const bloom = createBloom(SEED, PAGE)
    const shown = ref(true)
    const host = document.createElement('div')
    const app = createApp({
      render: () => (shown.value ? h(KccWashes as Component, { washKey: 'variant-reviews', size: 'reading' }) : null),
    })
    app.provide(BLOOM_KEY, bloom)
    app.mount(host)
    bloom.markHydrated()
    const arrangement = () =>
      [...host.querySelectorAll<HTMLElement>('.kcc-wash')].map(
        (wash) => `${wash.dataset.wash} ${wash.getAttribute('style')!.replace(/--wash-fallback-at:[^;]*;?/, '')}`,
      )
    const before = arrangement()

    shown.value = false
    await nextTick()
    shown.value = true
    await nextTick()

    const ids = planSheet({ seed: SEED, key: `${PAGE}|variant-reviews`, size: 'reading' }).map((w) => w.id)
    expect(before.map((line) => line.split(' ')[0])).toEqual(ids)
    expect(arrangement()).toEqual(before)
    expect(warn).not.toHaveBeenCalled()
    app.unmount()
    warn.mockRestore()
  })

  it('adds the lag and plans with the late starts when set up after hydration', async () => {
    const bloom = createBloom(SEED, PAGE)
    const twin = createBloom(SEED, PAGE)
    bloom.markHydrated()
    twin.markHydrated()

    const html = await renderIn(bloom, { washKey: 'review:42', size: 'page' })

    const planned = planSheet({ seed: SEED, key: `${PAGE}|review:42`, size: 'page', starts: twin.lateStarts(4) })
    expect(html).toMatch(/^<span class="kcc-washes" style="--wash-lag:(\d+)?\.\d{3}s;" aria-hidden="true">/)
    expect(html).toContain(planned.map(washMarkup).join(''))
    const starts = fallbackStarts(html)
    expect(starts[0]).toBe(Math.min(...starts))
    expect(bloom.collected()).toEqual([])
  })

  it('adds no lag before hydration', async () => {
    const html = await renderIn(createBloom(SEED, PAGE), { washKey: 'hero', size: 'page' })

    expect(html).not.toContain('--wash-lag')
  })

  it('keeps its markup when a conditional sibling is inserted before it', async () => {
    const later = { washKey: 'variant-reviews', size: 'reading' as const }
    const without = await renderIn(createBloom(SEED, PAGE), false, later)
    const withSibling = await renderIn(createBloom(SEED, PAGE), { washKey: 'review-form', size: 'card' }, later)

    const id = planSheet({ seed: SEED, key: `${PAGE}|variant-reviews`, size: 'reading' })[0].id
    expect(fieldFor(withSibling, id)).toBe(without)
  })

  it('renders a global key the same on every page', async () => {
    const pad = { washKey: '@pad', size: 'page' as const, washSlots: [0, 4, 8, 12] }

    const home = await renderIn(createBloom(SEED, '/'), pad)
    const recipe = await renderIn(createBloom(SEED, PAGE), pad)

    expect(recipe).toBe(home)
  })
})
