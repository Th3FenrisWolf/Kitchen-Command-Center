import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useWakeLock } from '~/Pages/VariantDetail/useWakeLock'

interface FakeSentinel {
  released: boolean
  release: ReturnType<typeof vi.fn>
}

function makeSentinel(): FakeSentinel {
  const sentinel: FakeSentinel = {
    released: false,
    release: vi.fn(async () => {
      sentinel.released = true
    }),
  }
  return sentinel
}

function deferred<T>() {
  let resolve: (value: T) => void = () => {}
  const promise = new Promise<T>((settle) => {
    resolve = settle
  })
  return { promise, resolve }
}

const tasksRun = () => new Promise((done) => setTimeout(done, 0))

class FakeDocument extends EventTarget {
  visibilityState: 'visible' | 'hidden' = 'visible'
}

let fakeDocument: FakeDocument

beforeEach(() => {
  fakeDocument = new FakeDocument()
  vi.stubGlobal('document', fakeDocument)
  vi.stubGlobal('navigator', {})
})

function setWakeLock(value: unknown) {
  vi.stubGlobal('navigator', { wakeLock: value })
}

function setVisibility(state: 'visible' | 'hidden') {
  fakeDocument.visibilityState = state
  fakeDocument.dispatchEvent(new Event('visibilitychange'))
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('useWakeLock', () => {
  it('acquires a sentinel when supported', async () => {
    const sentinel = makeSentinel()
    const request = vi.fn(async () => sentinel)
    setWakeLock({ request })

    const wl = useWakeLock()
    await wl.request()

    expect(request).toHaveBeenCalledWith('screen')
    expect(wl.isActive.value).toBe(true)
  })

  it('is a silent no-op when the API is unsupported', async () => {
    setWakeLock(undefined)
    const wl = useWakeLock()
    await expect(wl.request()).resolves.toBeUndefined()
    expect(wl.isActive.value).toBe(false)
  })

  it('is a silent no-op when the request is denied (rejects)', async () => {
    const request = vi.fn(async () => {
      throw new DOMException('denied', 'NotAllowedError')
    })
    setWakeLock({ request })

    const wl = useWakeLock()
    await expect(wl.request()).resolves.toBeUndefined()
    expect(wl.isActive.value).toBe(false)
  })

  it('releases the sentinel and clears active state', async () => {
    const sentinel = makeSentinel()
    setWakeLock({ request: vi.fn(async () => sentinel) })

    const wl = useWakeLock()
    await wl.request()
    await wl.release()

    expect(sentinel.release).toHaveBeenCalledOnce()
    expect(wl.isActive.value).toBe(false)
  })

  it('re-acquires when the tab becomes visible again after being hidden', async () => {
    const request = vi.fn(async () => makeSentinel())
    setWakeLock({ request })

    const wl = useWakeLock()
    await wl.request()
    expect(request).toHaveBeenCalledTimes(1)

    setVisibility('hidden')
    await Promise.resolve()
    setVisibility('visible')
    await Promise.resolve()

    expect(request).toHaveBeenCalledTimes(2)
  })

  it('releases a first grant that arrives after release()', async () => {
    const sentinel = makeSentinel()
    const grant = deferred<FakeSentinel>()
    setWakeLock({ request: vi.fn(() => grant.promise) })

    const wl = useWakeLock()
    const requested = wl.request()
    await wl.release()
    grant.resolve(sentinel)
    await requested

    expect(sentinel.release).toHaveBeenCalledOnce()
    expect(wl.isActive.value).toBe(false)
  })

  it('releases a second grant that arrives after release(), once the tab was hidden and shown', async () => {
    const first = makeSentinel()
    const second = makeSentinel()
    const secondGrant = deferred<FakeSentinel>()
    const request = vi.fn().mockResolvedValueOnce(first).mockReturnValueOnce(secondGrant.promise)
    setWakeLock({ request })

    const wl = useWakeLock()
    await wl.request()
    setVisibility('hidden')
    setVisibility('visible')
    await wl.release()
    secondGrant.resolve(second)
    await tasksRun()

    expect(request).toHaveBeenCalledTimes(2)
    expect(second.release).toHaveBeenCalledOnce()
    expect(wl.isActive.value).toBe(false)
  })
})
