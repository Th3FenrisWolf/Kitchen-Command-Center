import { reactive, ref, watch } from 'vue'
import type { RecipeSearchHit, RecipeSearchResponse, RecipeFacets } from '~/Types/Recipe'
import { buildSearchParams, defaultState, type RecipeSearchState } from './recipeSearchCriteria'

const PAGE_SIZE = 12

export function useRecipeSearch(initial: RecipeSearchResponse) {
  const state = reactive<RecipeSearchState>(defaultState())
  const results = ref<RecipeSearchHit[]>([...initial.results])
  const facets = ref<RecipeFacets>(initial.facets)

  // The initial response is unfiltered, so its facet keys are the complete universe of
  // category/diet options. Capturing them once lets the filter panel render every option on
  // every search (greying out the ones with no current matches) instead of dropping rows —
  // which keeps the panel from resizing as the user filters. Filtered searches only ever
  // return a subset of these, so the option set never needs to grow.
  const categoryOptions = Object.keys(initial.facets.category)
  const dietOptions = Object.keys(initial.facets.diet)
  const total = ref(initial.total)
  const spotlight = ref<RecipeSearchHit | null>(initial.spotlight)
  const page = ref(0)
  const loading = ref(false)

  let seq = 0
  const hasMore = () => results.value.length < total.value

  async function fetchPage(nextPage: number, append: boolean) {
    const mySeq = ++seq
    loading.value = true
    try {
      const params = buildSearchParams(state, nextPage, PAGE_SIZE)
      const res = await fetch(`/api/recipes/search?${params.toString()}`, { headers: { Accept: 'application/json' } })
      const data: RecipeSearchResponse = await res.json()
      if (mySeq !== seq) {
        return // a newer request superseded this one
      }
      results.value = append ? [...results.value, ...data.results] : data.results
      facets.value = data.facets
      total.value = data.total
      page.value = data.page
      if (!append) {
        spotlight.value = data.spotlight
      }
    } finally {
      if (mySeq === seq) {
        loading.value = false
      }
    }
  }

  // Any filter/sort change resets to page 0 (replace). `view` is display-only — exclude it.
  watch(
    () =>
      [state.query, state.categories.join(','), state.diets.join(','), state.timeMin, state.timeMax, state.sort].join('|'),
    () => fetchPage(0, false),
  )

  function loadMore() {
    if (!loading.value && hasMore()) {
      fetchPage(page.value + 1, true)
    }
  }

  return { state, results, facets, categoryOptions, dietOptions, total, spotlight, loading, hasMore, loadMore }
}
