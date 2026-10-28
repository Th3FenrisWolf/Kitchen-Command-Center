// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const events = vi.hoisted(() => [] as string[])

vi.mock('virtual:vue-devtools-path:overlay.js', () => ({}))
vi.mock('~/DevTools/RuntimeErrorOverlay', () => ({}))
vi.mock('~/GlobalComponents', () => ({ registerGlobalComponents: () => {} }))
vi.mock('~/Bloom/visitHeartbeat', () => ({ startVisitHeartbeat: () => {} }))
vi.mock('~/App.vue', async () => {
  const { h } = await import('vue')
  return {
    default: {
      render: () => {
        events.push('rendered')
        return h('p', 'mounted')
      },
    },
  }
})

describe('Main', () => {
  beforeEach(() => {
    vi.resetModules()
    events.length = 0
    document.body.innerHTML = `
      <div id="app"><p>mounted</p></div>
      <script id="server-content" type="application/json">${JSON.stringify({ bloom: { seed: 1, page: '/' } })}</script>`
  })

  afterEach(() => {
    vi.restoreAllMocks()
    document.body.innerHTML = ''
  })

  it('marks kcc:hydrated once the app has mounted', async () => {
    vi.spyOn(performance, 'mark').mockImplementation((name) => {
      events.push(name)
      return {} as PerformanceMark
    })

    await import('~/Main')

    expect(events).toEqual(['rendered', 'kcc:hydrated'])
  })
})
