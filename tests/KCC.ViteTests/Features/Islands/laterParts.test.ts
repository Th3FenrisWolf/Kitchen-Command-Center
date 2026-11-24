// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, defineComponent, h, nextTick } from 'vue'

type LaterParts = typeof import('~/Islands/laterParts')

async function fresh(): Promise<LaterParts> {
  vi.resetModules()
  return import('~/Islands/laterParts')
}

describe('later parts', () => {
  beforeEach(() => vi.useFakeTimers())

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.unstubAllEnvs()
    document.body.innerHTML = ''
  })

  it('prefetch every registered part in idle time once scheduled, and none before', async () => {
    const idle: IdleRequestCallback[] = []
    vi.stubGlobal('requestIdleCallback', (callback: IdleRequestCallback) => idle.push(callback))
    const { registerLaterPart, scheduleLaterParts } = await fresh()
    const first = vi.fn()
    const second = vi.fn()
    registerLaterPart(first)
    registerLaterPart(second)

    await vi.runAllTimersAsync()
    expect(first).not.toHaveBeenCalled()

    scheduleLaterParts()
    expect(first).not.toHaveBeenCalled()
    expect(idle.length).toBeGreaterThan(0)

    idle.splice(0).forEach((callback) => callback({ didTimeout: false, timeRemaining: () => 50 }))
    expect(first).toHaveBeenCalledOnce()
    expect(second).toHaveBeenCalledOnce()
  })

  it('fall back to a timeout where requestIdleCallback is missing', async () => {
    vi.stubGlobal('requestIdleCallback', undefined)
    const { registerLaterPart, scheduleLaterParts } = await fresh()
    const part = vi.fn()
    registerLaterPart(part)

    scheduleLaterParts()
    expect(part).not.toHaveBeenCalled()

    await vi.runAllTimersAsync()
    expect(part).toHaveBeenCalledOnce()
  })

  it('prefetch a part registered after scheduling at the next idle time too', async () => {
    const idle: IdleRequestCallback[] = []
    vi.stubGlobal('requestIdleCallback', (callback: IdleRequestCallback) => idle.push(callback))
    const { registerLaterPart, scheduleLaterParts } = await fresh()
    scheduleLaterParts()
    idle.splice(0).forEach((callback) => callback({ didTimeout: false, timeRemaining: () => 50 }))

    const late = vi.fn()
    registerLaterPart(late)
    expect(late).not.toHaveBeenCalled()

    idle.splice(0).forEach((callback) => callback({ didTimeout: false, timeRemaining: () => 50 }))
    expect(late).toHaveBeenCalledOnce()
  })

  it('keep a failed prefetch quiet', async () => {
    vi.stubGlobal('requestIdleCallback', undefined)
    const { registerLaterPart, scheduleLaterParts } = await fresh()
    registerLaterPart(() => Promise.reject(new Error('offline')))
    const after = vi.fn()
    registerLaterPart(after)

    scheduleLaterParts()
    await vi.runAllTimersAsync()

    expect(after).toHaveBeenCalledOnce()
  })
  it('prefetch a part registered more than once only once, as a remounted island registers it again', async () => {
    vi.stubGlobal('requestIdleCallback', undefined)
    const { registerLaterPart, scheduleLaterParts } = await fresh()
    const part = vi.fn()
    registerLaterPart(part)
    registerLaterPart(part)

    scheduleLaterParts()
    await vi.runAllTimersAsync()

    expect(part).toHaveBeenCalledOnce()
  })

  it('register nothing on the server, where nothing would ever prefetch it', async () => {
    vi.stubEnv('SSR', true)
    vi.stubGlobal('requestIdleCallback', undefined)
    const { registerLaterPart, scheduleLaterParts } = await fresh()
    const part = vi.fn()
    registerLaterPart(part)

    scheduleLaterParts()
    await vi.runAllTimersAsync()

    expect(part).not.toHaveBeenCalled()
  })
})

describe('a later part whose chunk never arrives', () => {
  beforeEach(() => vi.useFakeTimers())

  afterEach(() => {
    vi.useRealTimers()
    document.body.innerHTML = ''
  })

  const Arrived = defineComponent({ render: () => h('p', { 'data-arrived': '' }, 'arrived') })

  async function mountPart(loader: () => Promise<typeof Arrived>) {
    const { laterPart, useLaterPartRetry } = await fresh()
    const Pending = defineComponent({
      inheritAttrs: false,
      props: { error: { type: Error, default: undefined } },
      setup(props) {
        const retry = useLaterPartRetry()
        return () =>
          props.error
            ? h('button', { 'data-failed': '', onClick: retry }, 'Try again')
            : h('div', { 'data-later-pending': '' })
      },
    })
    const part = laterPart(loader, Pending)
    const host = document.createElement('div')
    document.body.append(host)
    createApp({ render: () => h(part) }).mount(host)
    return { host, part }
  }

  it('gives up after three attempts and shows its error state in place of the pending one', async () => {
    const loader = vi.fn(() => Promise.reject(new Error('chunk blocked')))
    const { host } = await mountPart(loader)
    expect(host.querySelector('[data-later-pending]')).not.toBeNull()

    await vi.runAllTimersAsync()

    expect(loader).toHaveBeenCalledTimes(3)
    expect(host.querySelector('[data-later-pending]')).toBeNull()
    expect(host.querySelector('[data-failed]')).not.toBeNull()
  })

  it('gives up on a load that never settles, tries again, and fails the same way', async () => {
    const loader = vi.fn(() => new Promise<typeof Arrived>(() => undefined))
    const { host } = await mountPart(loader)

    await vi.advanceTimersByTimeAsync(14_000)
    expect(loader).toHaveBeenCalledTimes(1)
    expect(host.querySelector('[data-later-pending]')).not.toBeNull()

    await vi.runAllTimersAsync()

    expect(loader).toHaveBeenCalledTimes(3)
    expect(host.querySelector('[data-later-pending]')).toBeNull()
    expect(host.querySelector('[data-failed]')).not.toBeNull()
  })

  it('settles what waits on it, so nothing that awaits the part hangs', async () => {
    const { prefetchPart, laterPart } = await fresh()
    const part = laterPart(() => Promise.reject(new Error('chunk blocked')), Arrived)
    const settled = prefetchPart(part).then(
      () => 'resolved',
      () => 'rejected',
    )

    await vi.runAllTimersAsync()

    await expect(settled).resolves.toBe('rejected')
  })

  it('loads the part again from its Retry control', async () => {
    let online = false
    const loader = vi.fn(() => (online ? Promise.resolve(Arrived) : Promise.reject(new Error('chunk blocked'))))
    const { host } = await mountPart(loader)
    await vi.runAllTimersAsync()

    online = true
    host.querySelector<HTMLButtonElement>('[data-failed]')!.click()
    await vi.runAllTimersAsync()
    await nextTick()

    expect(loader).toHaveBeenCalledTimes(4)
    expect(host.querySelector('[data-arrived]')).not.toBeNull()
  })
})
