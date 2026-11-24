// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { IslandPayload } from '~/Types/ContentRegions'

const events = vi.hoisted(() => [] as string[])
const broken = vi.hoisted(() => new Set<string>())
const gate = vi.hoisted(() => ({ held: Promise.resolve() }))

vi.mock('virtual:vue-devtools-path:overlay.js', () => ({}))
vi.mock('~/DevTools/RuntimeErrorOverlay', () => ({}))
vi.mock('~/Bloom/visitHeartbeat', () => ({ startVisitHeartbeat: () => events.push('heartbeat') }))
vi.mock('~/Utilities/Api', () => ({ configureApi: () => events.push('api') }))
vi.mock('~/Bloom/bloomContext', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/Bloom/bloomContext')>()
  return {
    ...actual,
    createBloom: (...args: Parameters<typeof actual.createBloom>) => {
      const bloom = actual.createBloom(...args)
      const markHydrated = bloom.markHydrated
      bloom.markHydrated = (pageWashes) => {
        events.push(`markHydrated:${pageWashes}`)
        markHydrated(pageWashes)
      }
      return bloom
    },
  }
})
vi.mock('~/Islands/registry', async () => {
  const { h } = await import('vue')
  return {
    islandLoader: (name: string) => async () => {
      await gate.held
      return broken.has(name)
        ? Promise.reject(new Error(`${name} chunk failed`))
        : Promise.resolve({
            render: () => {
              events.push(`rendered:${name}`)
              return h('p', name)
            },
          })
    },
  }
})

const payload: IslandPayload = {
  isPreview: false,
  bloom: { seed: 1, page: '/', pageWashes: 4 },
  islands: [
    { name: 'AppHeader', props: {}, keys: [] },
    { name: 'RecipeSearchView', props: {}, keys: [] },
  ],
}

describe('Main', () => {
  beforeEach(() => {
    vi.resetModules()
    events.length = 0
    broken.clear()
    gate.held = Promise.resolve()
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(performance, 'mark').mockImplementation((name) => {
      events.push(name)
      return {} as PerformanceMark
    })
    document.body.innerHTML = `
      <style data-ssr-styles>p{}</style>
      <div id="app"><kcc-island data-island="0"><p>AppHeader</p></kcc-island><kcc-island data-island="1"><p>RecipeSearchView</p></kcc-island></div>
      <script id="server-content" type="application/json">${JSON.stringify(payload)}</script>
      <script id="api-config" type="application/json">{}</script>`
  })

  afterEach(() => {
    vi.restoreAllMocks()
    document.body.innerHTML = ''
  })

  const settled = () => vi.waitFor(() => expect(events).toContain('heartbeat'))

  it('configures the API, hydrates every island, then marks kcc:hydrated, the bloom and the heartbeat', async () => {
    await import('~/Main')
    await settled()

    expect(events[0]).toBe('api')
    expect(events.slice(1, 3).sort()).toEqual(['rendered:AppHeader', 'rendered:RecipeSearchView'])
    expect(events.slice(3)).toEqual(['kcc:hydrated', 'markHydrated:4', 'heartbeat'])
  })

  it('still marks the page hydrated when one island chunk fails to load, leaving its server HTML', async () => {
    broken.add('AppHeader')

    await import('~/Main')
    await settled()

    expect(events).toEqual(['api', 'rendered:RecipeSearchView', 'kcc:hydrated', 'markHydrated:4', 'heartbeat'])
    expect(document.querySelector('[data-island="0"]')?.textContent).toBe('AppHeader')
  })

  it('removes the server style block only after the last island has hydrated', async () => {
    let release = () => {}
    gate.held = new Promise((resolve) => (release = resolve))

    await import('~/Main')
    await new Promise((resolve) => setTimeout(resolve, 10))

    expect(document.querySelector('style[data-ssr-styles]')).not.toBeNull()
    release()
    await settled()
    await vi.waitFor(() => expect(document.querySelector('style[data-ssr-styles]')).toBeNull())
  })
})
