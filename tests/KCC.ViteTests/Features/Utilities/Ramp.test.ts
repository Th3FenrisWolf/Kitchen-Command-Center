import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { applyRamp, deviceRamp, rampFor } from '~/Utilities/Ramp'
import { FakeRoot } from '../../support/root'

let root: FakeRoot
let frames: FrameRequestCallback[]

beforeEach(() => {
  root = new FakeRoot()
  frames = []
  vi.stubGlobal('document', { documentElement: root })
  vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => frames.push(callback))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

function runFrame() {
  frames.splice(0).forEach((frame) => frame(0))
}

function onDevice({
  stored = null,
  dark = false,
  blocked = false,
}: { stored?: string | null; dark?: boolean; blocked?: boolean } = {}) {
  vi.stubGlobal('window', {
    localStorage: {
      getItem: () => {
        if (blocked) {
          throw new DOMException('Storage is blocked.', 'SecurityError')
        }
        return stored
      },
    },
    matchMedia: (query: string) => ({ matches: dark && query === '(prefers-color-scheme: dark)' }),
  })
}

describe('applyRamp', () => {
  it('switches the ramp and holds the motion freeze until a frame has rendered with it', () => {
    applyRamp('dark')

    expect(root.getAttribute('data-theme')).toBe('dark')
    expect(root.getAttribute('data-theme-switching')).toBe('')

    runFrame()
    expect(root.getAttribute('data-theme-switching')).toBe('')

    runFrame()
    expect(root.getAttribute('data-theme-switching')).toBeNull()
  })

  it('releases the freeze only once the latest of two overlapping switches has rendered', () => {
    applyRamp('dark')
    runFrame()

    applyRamp('light')
    runFrame()

    expect(root.getAttribute('data-theme')).toBe('light')
    expect(root.getAttribute('data-theme-switching')).toBe('')

    while (frames.length > 0) {
      runFrame()
    }
    expect(root.getAttribute('data-theme-switching')).toBeNull()
  })
})

describe('deviceRamp', () => {
  it("takes the header toggle's stored choice first", () => {
    onDevice({ stored: 'light', dark: true })
    expect(deviceRamp()).toBe('light')
  })

  it('follows the operating system when nothing is stored', () => {
    onDevice({ dark: true })
    expect(deviceRamp()).toBe('dark')
  })

  it('leads light when nothing says otherwise', () => {
    onDevice()
    expect(deviceRamp()).toBe('light')
  })

  it('falls through to the operating system when storage is blocked', () => {
    onDevice({ blocked: true, dark: true })
    expect(deviceRamp()).toBe('dark')
  })

  it('ignores a stored value that is not a ramp', () => {
    onDevice({ stored: 'sepia', dark: true })
    expect(deviceRamp()).toBe('dark')
  })
})

describe('rampFor', () => {
  it('maps a saved Light or Dark straight to its ramp, whatever the device prefers', () => {
    onDevice({ stored: 'dark', dark: true })
    expect(rampFor('Light')).toBe('light')

    onDevice({ stored: 'light' })
    expect(rampFor('Dark')).toBe('dark')
  })

  it('asks the device for Device', () => {
    onDevice({ dark: true })
    expect(rampFor('Device')).toBe('dark')
  })
})
