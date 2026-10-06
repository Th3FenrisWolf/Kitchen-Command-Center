import { effectScope, nextTick, reactive, type EffectScope } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useLibraryUrl } from '~/Pages/RecipeSearch/useLibraryUrl'
import { MAX_TIME, defaultState } from '~/Pages/RecipeSearch/recipeSearchCriteria'

let scope: EffectScope
let replaceState: ReturnType<typeof vi.fn>

beforeEach(() => {
  vi.useFakeTimers()
  replaceState = vi.fn()
})

afterEach(() => {
  scope.stop()
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

function mirror(hash = '') {
  vi.stubGlobal('window', { location: { pathname: '/recipes/', hash }, history: { state: null, replaceState } })
  const state = reactive(defaultState())
  scope = effectScope()
  scope.run(() => useLibraryUrl(state))
  return state
}

async function settle(ms: number) {
  await nextTick()
  vi.advanceTimersByTime(ms)
}

describe('useLibraryUrl', () => {
  it('writes the filters into the address once they have been still for 300ms', async () => {
    const state = mirror()

    state.categories.push('Dinner')
    await settle(299)
    expect(replaceState).not.toHaveBeenCalled()

    vi.advanceTimersByTime(1)
    expect(replaceState).toHaveBeenCalledWith(null, '', '/recipes/?category=Dinner')
  })

  it('writes a burst of changes once, as they ended', async () => {
    const state = mirror()

    state.categories.push('Dinner')
    await settle(200)
    state.styles.push('Spicy')
    await settle(300)

    expect(replaceState).toHaveBeenCalledTimes(1)
    expect(replaceState).toHaveBeenCalledWith(null, '', '/recipes/?category=Dinner&style=Spicy')
  })

  it('drops the query string once every filter is back to its default', async () => {
    const state = mirror()

    state.timeMax = 30
    await settle(300)
    state.timeMax = MAX_TIME
    await settle(300)

    expect(replaceState).toHaveBeenNthCalledWith(1, null, '', '/recipes/?timeMax=30')
    expect(replaceState).toHaveBeenNthCalledWith(2, null, '', '/recipes/')
  })

  it('keeps the hash', async () => {
    const state = mirror('#results')

    state.sort = 'rated'
    await settle(300)

    expect(replaceState).toHaveBeenCalledWith(null, '', '/recipes/?sort=rated#results')
  })

  it('leaves the address alone when only the view changes', async () => {
    const state = mirror()

    state.view = 'list'
    await settle(300)

    expect(replaceState).not.toHaveBeenCalled()
  })
})
