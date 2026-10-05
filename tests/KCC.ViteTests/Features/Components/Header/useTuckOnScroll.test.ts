import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SCROLL_SLOP_PX, TUCK_BELOW_PX, useTuckOnScroll } from '~/Components/Header/useTuckOnScroll'

let scrollY: number
let offset: string | undefined
let reducedMotion: boolean

beforeEach(() => {
  scrollY = 0
  offset = undefined
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
        setProperty: (name: string, value: string) => {
          if (name === '--pad-offset') offset = value
        },
      },
    },
  })
})

afterEach(() => {
  vi.unstubAllGlobals()
})

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

    expect(offset).toBe('102px')
  })

  it('tucks away going down past the top of the page, closing any open card first', () => {
    const { tuck, scrollTo, closed } = tuckOn()

    scrollTo(TUCK_BELOW_PX + SCROLL_SLOP_PX + 1)

    expect(tuck.tucked.value).toBe(true)
    expect(closed.count).toBe(1)
    expect(offset).toBe('0px')
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
    expect(offset).toBe('102px')
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
    expect(offset).toBe('102px')
  })

  it('reports no offset while tucked, whatever the pad measures', () => {
    const { tuck, scrollTo } = tuckOn(102)
    scrollTo(600)

    tuck.measure(120)

    expect(offset).toBe('0px')
  })
})
