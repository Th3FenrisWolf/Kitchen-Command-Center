// @vitest-environment happy-dom
import { createApp, nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { renderSsr, tagWith } from '../../../support/ssr'
import '~/Utilities/StringExtensions'
import RecipeSearchView from '~/Pages/RecipeSearch/RecipeSearchView.Component.vue'
import type { RecipeSearchHit, RecipeSearchResponse } from '~/Types/Recipe'
import type { RecipeFilterState } from '~/Pages/RecipeSearch/recipeSearchCriteria'

const PAGE_SIZE = 12
const CARD_HEIGHT = 400
const CARDS_TOP = 200

const hit = (index: number): RecipeSearchHit => ({
  name: `Recipe ${index}`,
  slug: `/recipes/${index}`,
  icon: 'fa-solid fa-drumstick-bite',
  category: 'Mains',
  startedBy: 'Dana Whitfield',
  tags: [],
  averageRating: null,
  reviewCount: 0,
  variantCount: 1,
  fastestTime: 20,
})

const firstPage = (total: number): RecipeSearchResponse => ({
  total,
  page: 0,
  pageSize: PAGE_SIZE,
  results: Array.from({ length: Math.min(PAGE_SIZE, total) }, (_, i) => hit(i)),
  facets: { category: {}, diet: {}, style: {} },
  spotlight: null,
})

const FILTERS: RecipeFilterState = {
  query: '',
  categories: [],
  diets: [],
  styles: [],
  timeMin: 0,
  timeMax: 60,
  sort: 'relevant',
}

const props = (total: number) => ({
  initial: firstPage(total),
  filters: FILTERS,
  options: { categories: [], diets: [], styles: [] },
  createRecipeUrl: '/create-recipe',
})

class AlwaysIntersects {
  constructor(private readonly report: (entries: { isIntersecting: boolean }[]) => void) {}
  observe() {
    requestAnimationFrame(() => setTimeout(() => this.report([{ isIntersecting: true }])))
  }
  unobserve() {}
  disconnect() {}
}

let reportHit: () => void = () => {}

class IntersectsOnCue {
  constructor(report: (entries: { isIntersecting: boolean }[]) => void) {
    reportHit = () => report([{ isIntersecting: true }])
  }
  observe() {}
  unobserve() {}
  disconnect() {}
}

const answer = (total: number, from: number, count: number, page: number): RecipeSearchResponse => ({
  ...firstPage(total),
  page,
  results: Array.from({ length: count }, (_, i) => hit(from + i)),
})

class NeverIntersects {
  observe() {}
  unobserve() {}
  disconnect() {}
}

const rect = (top: number, height: number) =>
  ({ top, bottom: top + height, height, left: 0, right: 0, width: 0, x: 0, y: top }) as DOMRect

const isCard = (el: Element) => el.hasAttribute('data-recipe-name') || !!el.querySelector(':scope > [data-recipe-name]')
const cardsOf = (grid: Element) => [...grid.children].filter(isCard)

// A grid of `columns` equal cards from CARDS_TOP; the element right after the grid starts where its last row ends.
const layOut = (columns: number) => {
  const gridBottom = (grid: Element) => CARDS_TOP + Math.ceil(cardsOf(grid).length / columns) * CARD_HEIGHT
  vi.spyOn(Element.prototype, 'getBoundingClientRect').mockImplementation(function (this: Element) {
    if (this.parentElement?.tagName === 'SECTION' && cardsOf(this).length) {
      return rect(CARDS_TOP, gridBottom(this) - CARDS_TOP)
    }

    if (isCard(this) && this.parentElement) {
      const index = cardsOf(this.parentElement).indexOf(this)
      return rect(CARDS_TOP + Math.floor(index / columns) * CARD_HEIGHT, CARD_HEIGHT)
    }

    const before = this.previousElementSibling
    if (before && cardsOf(before).length) {
      return rect(gridBottom(before), 0)
    }

    if (this.tagName === 'SECTION') {
      const grid = [...this.children].find((child) => cardsOf(child).length)
      return rect(0, grid ? gridBottom(grid) : 0)
    }

    return rect(0, 0)
  })
}

const settle = async () => {
  for (let i = 0; i < 8; i++) {
    await new Promise((resolve) => setTimeout(resolve))
    await nextTick()
  }
}

let unmount: () => void = () => {}

const mount = async (total: number) => {
  const host = document.createElement('div')
  document.body.append(host)
  const app = createApp(RecipeSearchView, props(total))
  app.component('Breadcrumbs', { render: () => null })
  app.mount(host)
  unmount = () => {
    app.unmount()
    host.remove()
  }
  await settle()
  return host
}

const sentinelOf = (host: Element) => {
  const grid = [...host.querySelectorAll('section > *')].find((child) => cardsOf(child).length)
  return grid?.nextElementSibling as HTMLElement | null
}

beforeEach(() => {
  vi.stubGlobal('IntersectionObserver', NeverIntersects)
  vi.stubGlobal('fetch', vi.fn())
  vi.stubGlobal('innerHeight', 800)
})

afterEach(() => {
  unmount()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

describe('the space search reserves for its next page', () => {
  it('is not in the server render, so the first paint and the scripts-off page are unchanged', async () => {
    const html = await renderSsr(RecipeSearchView, props(30))

    expect(html).not.toContain('min-height')
    expect(tagWith(html, 'aria-live="polite"')).not.toContain('style=')
    expect(tagWith(html, 'kcc-kick flex items-center justify-center')).not.toContain('style=')
  })

  it('holds the rows the next full page needs below the screen once the page is usable', async () => {
    layOut(3)

    const host = await mount(30)

    expect(sentinelOf(host)?.style.minHeight).toBe(`${4 * CARD_HEIGHT}px`)
  })

  it('sizes a short last page from the server total', async () => {
    layOut(3)

    const host = await mount(15)

    expect(sentinelOf(host)?.style.minHeight).toBe(`${CARD_HEIGHT}px`)
  })

  it('sizes the rows from the columns the grid lays out', async () => {
    layOut(2)

    const host = await mount(30)

    expect(sentinelOf(host)?.style.minHeight).toBe(`${6 * CARD_HEIGHT}px`)
  })

  it('holds no space when the first page is the last', async () => {
    layOut(3)

    const host = await mount(PAGE_SIZE)

    expect(sentinelOf(host)).toBeNull()
    expect(host.querySelector('section[aria-live]')?.style.minHeight).toBe('')
  })

  it('adds nothing while the end of the results is on screen, so nothing visible moves', async () => {
    layOut(3)
    vi.stubGlobal('innerHeight', 3000)

    const host = await mount(30)

    expect(sentinelOf(host)?.style.minHeight).toBe('')
    expect(host.querySelector('section[aria-live]')?.style.minHeight).toBe('')
  })

  it('loads the next page at once, as it always has, where no space can be added below the screen', async () => {
    layOut(3)
    vi.stubGlobal('IntersectionObserver', AlwaysIntersects)
    vi.stubGlobal('innerHeight', 3000)
    vi.stubGlobal('scrollY', 2000)
    const fetch = vi.fn(() => new Promise(() => {}))
    vi.stubGlobal('fetch', fetch)

    await mount(30)

    expect(fetch).toHaveBeenCalledTimes(1)
  })

  const leftoverHeights = (host: Element) => [
    host.querySelector('section[aria-live]')?.style.minHeight ?? '',
    sentinelOf(host)?.style.minHeight ?? '',
  ]

  it('gives every bit of space back once the last page lands below the screen', async () => {
    layOut(3)
    vi.stubGlobal('IntersectionObserver', IntersectsOnCue)
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ json: async () => answer(24, PAGE_SIZE, PAGE_SIZE, 1) })),
    )
    const host = await mount(24)
    expect(sentinelOf(host)?.style.minHeight).toBe(`${4 * CARD_HEIGHT}px`)

    reportHit()
    await settle()

    expect(host.querySelectorAll('[data-recipe-name]').length).toBe(24)
    expect(sentinelOf(host)).toBeNull()
    expect(leftoverHeights(host)).toEqual(['', ''])
  })

  it('gives every bit of space back once the last page lands with the end of the results on screen', async () => {
    layOut(3)
    vi.stubGlobal('IntersectionObserver', IntersectsOnCue)
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ json: async () => answer(24, PAGE_SIZE, PAGE_SIZE, 1) })),
    )
    const host = await mount(24)
    expect(sentinelOf(host)?.style.minHeight).toBe(`${4 * CARD_HEIGHT}px`)
    vi.stubGlobal('innerHeight', 6000)

    reportHit()
    await settle()

    expect(host.querySelectorAll('[data-recipe-name]').length).toBe(24)
    expect(leftoverHeights(host)).toEqual(['', ''])
  })

  it('gives every bit of space back when a new search has no further pages', async () => {
    layOut(3)
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ json: async () => answer(5, 0, 5, 0) })),
    )
    const host = await mount(30)
    expect(sentinelOf(host)?.style.minHeight).toBe(`${4 * CARD_HEIGHT}px`)

    host.querySelector<HTMLElement>('[data-testid="sort-rated"]')?.click()
    await settle()

    expect(host.querySelectorAll('[data-recipe-name]').length).toBe(5)
    expect(sentinelOf(host)).toBeNull()
    expect(leftoverHeights(host)).toEqual(['', ''])
  })
})
