import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SCROLL_SLOP_PX, TUCK_BELOW_PX, useTuckOnScroll } from '~/Components/Header/useTuckOnScroll'

let scrollY: number
let mainWrites: Array<[string, string]>
let mainTucked: boolean
let scrollPadding: string | undefined
let rootWrites: string[]
let reducedMotion: boolean

beforeEach(() => {
  scrollY = 0
  mainWrites = []
  mainTucked = false
  scrollPadding = undefined
  rootWrites = []
  reducedMotion = false
  vi.stubGlobal('window', {
    get scrollY() {
      return scrollY
    },
    matchMedia: () => ({ matches: reducedMotion }),
  })
  vi.stubGlobal('document', {
    documentElement: {
      style: {
        setProperty: (name: string) => rootWrites.push(name),
        set scrollPaddingTop(value: string) {
          scrollPadding = value
        },
      },
    },
    querySelector: (selector: string) =>
      selector === 'main'
        ? {
            style: {
              setProperty: (name: string, value: string) => mainWrites.push([name, value]),
            },
            classList: {
              toggle: (token: string, force: boolean) => {
                if (token === 'is-tucked') mainTucked = force
                return force
              },
            },
          }
        : null,
  })
})

afterEach(() => {
  vi.unstubAllGlobals()
})

const padHeight = () => mainWrites.filter(([name]) => name === '--pad-h').at(-1)?.[1]

function tuckOn(height = 102) {
  const closed = { count: 0 }
  const tuck = useTuckOnScroll(() => closed.count++)
  tuck.start()
  tuck.measure(height)
  const scrollTo = (y: number) => {
    scrollY = y
    tuck.onScroll()
  }
  return { tuck, scrollTo, closed }
}

describe('useTuckOnScroll', () => {
  it('tells the sticky neighbours how tall the pad is', () => {
    tuckOn(102)

    expect(padHeight()).toBe('102px')
    expect(mainTucked).toBe(false)
  })

  it('writes the height only when the pad is measured, so a tuck or a show restyles only what its class names', () => {
    const { tuck, scrollTo } = tuckOn(102)
    const measured = mainWrites.length

    scrollTo(600)
    tuck.show()
    scrollTo(1200)
    scrollTo(1000)

    expect(mainWrites).toHaveLength(measured)
    expect(mainWrites.map(([name]) => name)).not.toContain('--pad-offset')
  })

  it('writes the height again when the pad resizes', () => {
    const { tuck } = tuckOn(102)

    tuck.measure(120)

    expect(padHeight()).toBe('120px')
  })

  it('keeps every custom property off the root so a tuck restyles no more than the page content', () => {
    const { scrollTo } = tuckOn(102)
    scrollTo(600)

    expect(rootWrites).toEqual([])
  })

  it('keeps the scroll padding on the root in step with the offset', () => {
    const { scrollTo } = tuckOn(102)
    expect(scrollPadding).toBe('102px')

    scrollTo(600)

    expect(scrollPadding).toBe('0px')
  })

  it('tucks away going down past the top of the page, closing any open card first', () => {
    const { tuck, scrollTo, closed } = tuckOn()

    scrollTo(TUCK_BELOW_PX + SCROLL_SLOP_PX + 1)

    expect(tuck.tucked.value).toBe(true)
    expect(closed.count).toBe(1)
    expect(mainTucked).toBe(true)
  })

  it('stays while the page is still near its top', () => {
    const { tuck, scrollTo } = tuckOn()

    scrollTo(TUCK_BELOW_PX)

    expect(tuck.tucked.value).toBe(false)
  })

  it('adds up a slow scroll until it passes the slop', () => {
    const { tuck, scrollTo } = tuckOn()
    scrollTo(400)
    scrollTo(300)

    scrollTo(301)
    scrollTo(302)
    scrollTo(303)
    expect(tuck.tucked.value).toBe(false)

    scrollTo(304)
    expect(tuck.tucked.value).toBe(true)
  })

  it('comes back on any scroll up past the slop', () => {
    const { tuck, scrollTo } = tuckOn(102)
    scrollTo(600)

    scrollTo(600 - SCROLL_SLOP_PX)
    expect(tuck.tucked.value).toBe(true)

    scrollTo(600 - SCROLL_SLOP_PX - 1)
    expect(tuck.tucked.value).toBe(false)
    expect(mainTucked).toBe(false)
  })

  it('stays pinned under reduced motion', () => {
    reducedMotion = true
    const { tuck, scrollTo, closed } = tuckOn()

    scrollTo(900)

    expect(tuck.tucked.value).toBe(false)
    expect(closed.count).toBe(0)
  })

  it('starts counting from where a reload left the page, so arriving mid-page does not tuck it', () => {
    scrollY = 1200
    const { tuck, scrollTo } = tuckOn()

    scrollTo(1201)

    expect(tuck.tucked.value).toBe(false)
  })

  it('shows itself when asked, as focus moving into the pad does', () => {
    const { tuck, scrollTo } = tuckOn(102)
    scrollTo(600)

    tuck.show()

    expect(tuck.tucked.value).toBe(false)
    expect(mainTucked).toBe(false)
  })

  it('stays tucked with no scroll padding when the pad is measured while tucked', () => {
    const { tuck, scrollTo } = tuckOn(102)
    scrollTo(600)

    tuck.measure(120)

    expect(mainTucked).toBe(true)
    expect(scrollPadding).toBe('0px')
    expect(padHeight()).toBe('120px')
  })
})
