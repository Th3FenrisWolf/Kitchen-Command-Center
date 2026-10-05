import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useRecipeSearch } from '~/Pages/RecipeSearch/useRecipeSearch'
import type { RecipeFilterState } from '~/Pages/RecipeSearch/recipeSearchCriteria'
import type { RecipeSearchResponse } from '~/Types/Recipe'

const response = (): RecipeSearchResponse => ({
  total: 0,
  page: 0,
  pageSize: 12,
  results: [],
  facets: { category: {}, diet: {}, style: {} },
  spotlight: null,
})

const filters = (over: Partial<RecipeFilterState> = {}): RecipeFilterState => ({
  query: '',
  categories: [],
  diets: [],
  styles: [],
  timeMin: 0,
  timeMax: 60,
  sort: 'relevant',
  ...over,
})

let fetchMock: ReturnType<typeof vi.fn>

beforeEach(() => {
  fetchMock = vi.fn(async () => ({ json: async () => response() }))
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => vi.unstubAllGlobals())

// A change fetches on the next microtask, so a test that changes the state awaits it before the stub goes: Node's
// own fetch would reject the relative URL after the test had finished.
describe('useRecipeSearch', () => {
  it('starts from the filters the server read from the address, in the grid', () => {
    const { state } = useRecipeSearch(response(), filters({ query: 'soup', styles: ['Spicy'], sort: 'rated' }))

    expect(state).toEqual({ ...filters({ query: 'soup', styles: ['Spicy'], sort: 'rated' }), view: 'grid' })
  })

  it('ticks boxes on lists of its own, never on the filters it was given', async () => {
    const given = filters({ diets: ['Vegan'] })
    const { state } = useRecipeSearch(response(), given)

    state.diets.push('Keto')
    await nextTick()

    expect(given.diets).toEqual(['Vegan'])
  })

  it('asks again from the first page when a style is ticked', async () => {
    const { state } = useRecipeSearch(response(), filters())

    state.styles.push('Spicy')
    await nextTick()

    expect(fetchMock).toHaveBeenCalledWith('/api/recipes/search?style=Spicy&page=0&pageSize=12', expect.anything())
  })

  it('does not ask again when only the view changes', async () => {
    const { state } = useRecipeSearch(response(), filters())

    state.view = 'list'
    await nextTick()

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
