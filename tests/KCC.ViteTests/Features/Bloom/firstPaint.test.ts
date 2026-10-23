// @vitest-environment happy-dom
import { createSSRApp, h, type Component } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { scheduleWashes } from '~/Bloom/washes'
import KccSheet from '~/Components/Sheet/KccSheet.vue'

const SEED = 2671381617
const PAGE = '/recipes/brown-butter-gnocchi'

const Page: Component = {
  render: () => [
    h(KccSheet, { washKey: 'detail-hero', tear: 'hero', wash: 'peach' }, () => 'Brown butter gnocchi'),
    h(KccSheet, { washKey: 'variant-instructions', margin: true }, () => 'Method'),
    h(KccSheet, { washKey: 'variant-notes', tear: 3 }, () => 'Notes'),
  ],
}

function appFor(seed: number) {
  const bloom = createBloom(seed, PAGE)
  const app = createSSRApp(Page)
  app.provide(BLOOM_KEY, bloom)
  return { app, bloom }
}

async function serverRender() {
  const { app, bloom } = appFor(SEED)
  return { html: await renderToString(app), bloom }
}

const fieldsIn = (root: ParentNode) => [...root.querySelectorAll('.kcc-washes')].map((field) => field.outerHTML)

function hydrate(html: string, seed: number) {
  const container = document.createElement('div')
  container.innerHTML = html
  document.body.append(container)
  const before = fieldsIn(container)
  const warnings: string[] = []
  const record = (...args: unknown[]) => warnings.push(args.map(String).join(' '))
  vi.spyOn(console, 'warn').mockImplementation(record)
  vi.spyOn(console, 'error').mockImplementation(record)
  const { app, bloom } = appFor(seed)
  app.mount(container)
  bloom.markHydrated()
  return { before, after: fieldsIn(container), mismatches: warnings.filter((w) => /hydration/i.test(w)) }
}

afterEach(() => {
  vi.restoreAllMocks()
  document.body.innerHTML = ''
})

describe('the first paint', () => {
  it('renders every field with no schedule and no lag inline', async () => {
    const { html } = await serverRender()

    expect(html.match(/<span class="kcc-washes"/g)).toHaveLength(3)
    expect(html.match(/class="kcc-wash"/g)?.length).toBeGreaterThanOrEqual(8)
    expect(html).not.toContain('--wash-at')
    expect(html).not.toContain('--wash-lag')
  })

  it('schedules one rule per rendered wash', async () => {
    const { html, bloom } = await serverRender()

    const rendered = [...html.matchAll(/data-wash="([0-9a-f]{8})"/g)].map((m) => m[1])
    const ruled = [...scheduleWashes(SEED, bloom.collected()).matchAll(/\[data-wash="([0-9a-f]{8})"\]/g)].map((m) => m[1])
    expect(rendered.length).toBeGreaterThanOrEqual(8)
    expect(ruled.sort()).toEqual(rendered.sort())
  })

  it('hydrates with the same bloom without a mismatch or a changed field', async () => {
    const { html } = await serverRender()

    const { before, after, mismatches } = hydrate(html, SEED)

    expect(mismatches).toEqual([])
    expect(before).toHaveLength(3)
    expect(after).toEqual(before)
  })

  it('reports a mismatch when hydrated with another seed', async () => {
    const { html } = await serverRender()

    const { mismatches } = hydrate(html, SEED + 1)

    expect(mismatches.length).toBeGreaterThan(0)
  })
})
