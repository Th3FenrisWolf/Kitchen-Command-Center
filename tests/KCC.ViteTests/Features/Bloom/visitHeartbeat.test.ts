import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { startVisitHeartbeat } from '~/Bloom/visitHeartbeat'

const VALUE = '1.9f3a0c71.1792000000'

function stubPage(cookie: string, protocol: 'http:' | 'https:' = 'https:') {
  const writes: string[] = []
  const page = new EventTarget()
  vi.stubGlobal('window', page)
  vi.stubGlobal('location', { protocol })
  vi.stubGlobal('document', {
    get cookie() {
      return cookie
    },
    set cookie(value: string) {
      writes.push(value)
    },
  })
  return { writes, page }
}

function attributes(write: string): string[] {
  return write.split(';').map((part) => part.trim().toLowerCase())
}

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('startVisitHeartbeat', () => {
  it('rewrites kcc-visit with the same value and a fresh 30-minute max-age every 60 seconds', () => {
    const { writes } = stubPage(`kcc-ramp=dark; kcc-visit=${VALUE}; other=1`)
    const stop = startVisitHeartbeat()
    vi.advanceTimersByTime(59_999)
    expect(writes).toEqual([])
    vi.advanceTimersByTime(1)
    expect(writes).toHaveLength(1)
    expect(writes[0].startsWith(`kcc-visit=${VALUE};`)).toBe(true)
    expect(attributes(writes[0]).slice(1).sort()).toEqual(['max-age=1800', 'path=/', 'samesite=lax', 'secure'])
    vi.advanceTimersByTime(120_000)
    expect(writes).toHaveLength(3)
    expect(new Set(writes).size).toBe(1)
    stop()
  })

  it('rewrites it on pagehide', () => {
    const { writes, page } = stubPage(`kcc-visit=${VALUE}`)
    const stop = startVisitHeartbeat()
    page.dispatchEvent(new Event('pagehide'))
    expect(writes).toHaveLength(1)
    expect(writes[0].startsWith(`kcc-visit=${VALUE};`)).toBe(true)
    stop()
  })

  it('leaves secure off over plain HTTP', () => {
    const { writes } = stubPage(`kcc-visit=${VALUE}`, 'http:')
    const stop = startVisitHeartbeat()
    vi.advanceTimersByTime(60_000)
    expect(attributes(writes[0]).slice(1).sort()).toEqual(['max-age=1800', 'path=/', 'samesite=lax'])
    stop()
  })

  it('writes nothing when there is no visit cookie', () => {
    const { writes, page } = stubPage('kcc-ramp=dark; kcc-visitor=x')
    const stop = startVisitHeartbeat()
    vi.advanceTimersByTime(180_000)
    page.dispatchEvent(new Event('pagehide'))
    expect(writes).toEqual([])
    stop()
  })

  it('stops its timer and its pagehide listener', () => {
    const { writes, page } = stubPage(`kcc-visit=${VALUE}`)
    const stop = startVisitHeartbeat()
    stop()
    vi.advanceTimersByTime(180_000)
    page.dispatchEvent(new Event('pagehide'))
    expect(writes).toEqual([])
    expect(vi.getTimerCount()).toBe(0)
  })
})
