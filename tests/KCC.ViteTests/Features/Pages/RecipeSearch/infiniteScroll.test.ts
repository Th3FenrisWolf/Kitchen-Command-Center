// @vitest-environment happy-dom
import { createApp, nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeSearchView from '~/Pages/RecipeSearch/RecipeSearchView.Component.vue'
import type { RecipeSearchHit, RecipeSearchResponse } from '~/Types/Recipe'
import type { RecipeFilterState } from '~/Pages/RecipeSearch/recipeSearchCriteria'

const PAGE_SIZE = 12
const TOTAL = 30

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

const pageOf = (page: number): RecipeSearchResponse => ({
  total: TOTAL,
  page,
  pageSize: PAGE_SIZE,
  results: Array.from({ length: Math.min(PAGE_SIZE, TOTAL - page * PAGE_SIZE) }, (_, i) => hit(page * PAGE_SIZE + i)),
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

// A real observer notifies once when a target is observed, then only when its intersection changes. This sentinel
// never moves out of view, as in a viewport taller than the pages loaded so far.
class SentinelAlwaysInView {
  private readonly targets = new Set<Element>()

  constructor(private readonly callback: IntersectionObserverCallback) {}

  observe(target: Element) {
    this.targets.add(target)
    setTimeout(() => {
      if (this.targets.has(target)) {
        this.callback(
          [{ isIntersecting: true, target } as IntersectionObserverEntry],
          this as unknown as IntersectionObserver,
        )
      }
    })
  }

  unobserve(target: Element) {
    this.targets.delete(target)
  }

  disconnect() {
    this.targets.clear()
  }
}

const settle = async () => {
  for (let i = 0; i < 12; i++) {
    await new Promise((resolve) => setTimeout(resolve))
    await nextTick()
  }
}

const requestedPages = (fetchMock: ReturnType<typeof vi.fn>) =>
  fetchMock.mock.calls.map(([url]) => Number(new URL(String(url), 'http://kcc.test').searchParams.get('page')))

let fetchMock: ReturnType<typeof vi.fn>
let unmount: () => void = () => {}

beforeEach(() => {
  vi.stubGlobal('IntersectionObserver', SentinelAlwaysInView)
})

afterEach(() => {
  unmount()
  vi.unstubAllGlobals()
})

const mount = async () => {
  const host = document.createElement('div')
  const app = createApp(RecipeSearchView, {
    initial: pageOf(0),
    filters: FILTERS,
    options: { categories: [], diets: [], styles: [] },
    createRecipeUrl: '/create-recipe',
  })
  app.component('Breadcrumbs', { render: () => null })
  app.mount(host)
  unmount = () => app.unmount()
  await settle()
  return host
}

describe('RecipeSearchView infinite scroll', () => {
  it('keeps loading pages while the sentinel stays in view, until every result is listed', async () => {
    fetchMock = vi.fn(async (url: string) => {
      const page = Number(new URL(url, 'http://kcc.test').searchParams.get('page'))
      return { json: async () => pageOf(page) }
    })
    vi.stubGlobal('fetch', fetchMock)

    const host = await mount()

    expect(requestedPages(fetchMock)).toEqual([1, 2])
    expect(host.querySelectorAll('[data-recipe-name]')).toHaveLength(TOTAL)
  })

  it('does not retry a page whose fetch failed while the sentinel stays in view', async () => {
    fetchMock = vi.fn(async () => {
      throw new TypeError('Failed to fetch')
    })
    vi.stubGlobal('fetch', fetchMock)
    const unhandled = vi.fn()
    process.on('unhandledRejection', unhandled)

    try {
      await mount()
    } finally {
      process.off('unhandledRejection', unhandled)
    }

    expect(requestedPages(fetchMock)).toEqual([1])
  })
})
