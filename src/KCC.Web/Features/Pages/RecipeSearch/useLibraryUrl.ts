import { onScopeDispose, watch } from 'vue'
import { filterParams, type RecipeFilterState } from './recipeSearchCriteria'

const WRITE_DELAY_MS = 300

export function useLibraryUrl(filters: RecipeFilterState) {
  let timer: ReturnType<typeof setTimeout> | undefined

  watch(
    () => filterParams(filters).toString(),
    (query) => {
      clearTimeout(timer)
      timer = setTimeout(() => {
        const { pathname, hash } = window.location
        window.history.replaceState(window.history.state, '', `${pathname}${query ? `?${query}` : ''}${hash}`)
      }, WRITE_DELAY_MS)
    },
  )

  onScopeDispose(() => clearTimeout(timer))
}
