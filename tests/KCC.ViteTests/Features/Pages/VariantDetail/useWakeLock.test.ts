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

// Minimal hand-rolled document stub: an EventTarget that also carries a mutable
// visibilityState. Enough for useWakeLock's addEventListener/visibilitychange use.
class FakeDocument extends EventTarget {
  visibilityState: 'visible' | 'hidden' = 'visible'
}

let fakeDocument: FakeDocument

beforeEach(() => {
  fakeDocument = new FakeDocument()
  vi.stubGlobal('document', fakeDocument)
  // Default: API present but each test overrides navigator.wakeLock as needed.
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
})
