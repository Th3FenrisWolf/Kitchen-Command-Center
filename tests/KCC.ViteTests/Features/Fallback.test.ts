// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'

const island = vi.hoisted(() => ({
  beforeHydrate: (() => Promise.resolve()) as (props: Record<string, unknown>) => Promise<unknown>,
}))

vi.mock('~/DevTools/RuntimeErrorOverlay', () => ({}))
vi.mock('~/Bloom/visitHeartbeat', () => ({ startVisitHeartbeat: () => () => undefined }))
vi.mock('~/Ssr/ServerComponents', () => {
  const WaitIsland = defineComponent({
    props: { flow: { type: String, required: true }, photoImport: { type: Object, default: undefined } },
    beforeHydrate: (props: Record<string, unknown>) => island.beforeHydrate(props),
    setup: (props) => () => h('p', { 'data-island': '' }, props.flow),
  })
  return { eagerGlobals: [{ name: 'WaitIsland', path: '', component: WaitIsland, island: true }] }
})

function serverContent() {
  const payload = {
    isPreview: false,
    bloom: { seed: 1, page: 'test' },
    headerContent: '<header></header>',
    bodyContent: '<WaitIsland flow="recipe" :photo-import="{ enabled: true }"></WaitIsland>',
    footerContent: '<footer></footer>',
  }
  document.body.innerHTML = `<div id="app"></div><script id="server-content" type="application/json">${JSON.stringify(payload)}</script>`
}

const hydrated = () => performance.getEntriesByName('kcc:hydrated').length > 0

async function settle() {
  for (let i = 0; i < 5; i++) await nextTick()
  await new Promise((resolve) => setTimeout(resolve))
}

describe('the fallback page with an island that settles something before it hydrates', () => {
  beforeEach(() => {
    vi.resetModules()
    performance.clearMarks()
    serverContent()
  })

  afterEach(() => {
    vi.restoreAllMocks()
    document.body.innerHTML = ''
  })

  it("mounts the island and marks the page hydrated only once that settles, with the island's own props", async () => {
    let settleIt: () => void = () => undefined
    const seen: Record<string, unknown>[] = []
    island.beforeHydrate = (props) => {
      seen.push(props)
      return new Promise<void>((resolve) => (settleIt = resolve))
    }

    await import('~/Fallback')
    await settle()

    expect(seen).toEqual([expect.objectContaining({ flow: 'recipe', photoImport: { enabled: true } })])
    expect(document.querySelector('[data-island]')).toBeNull()
    expect(hydrated()).toBe(false)

    settleIt()
    await settle()

    expect(document.querySelector('[data-island]')?.textContent).toBe('recipe')
    expect(hydrated()).toBe(true)
  })

  it('warns and mounts the island anyway when it fails', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    island.beforeHydrate = () => Promise.reject(new Error('chunk blocked'))

    await import('~/Fallback')
    await settle()

    expect(warn).toHaveBeenCalled()
    expect(document.querySelector('[data-island]')?.textContent).toBe('recipe')
    expect(hydrated()).toBe(true)
  })
})
