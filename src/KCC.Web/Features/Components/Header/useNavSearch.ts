import { ref, watch } from 'vue'
import type { RecipeSearchHit, RecipeSearchResponse } from '~/Types/Recipe'

export const SEARCH_DEBOUNCE_MS = 150
export const SEARCH_RESULTS = 4

export function useNavSearch() {
  const query = ref('')
  const results = ref<RecipeSearchHit[]>([])
  const total = ref(0)
  const failed = ref(false)
  const searched = ref('')

  let timer: ReturnType<typeof setTimeout> | undefined
  let inFlight: AbortController | undefined

  async function search(text: string) {
    const request = new AbortController()
    inFlight = request
    try {
      const params = new URLSearchParams({ query: text, pageSize: String(SEARCH_RESULTS) })
      const response = await fetch(`/api/recipes/search?${params}`, {
        headers: { Accept: 'application/json' },
        signal: request.signal,
      })
      if (!response.ok) {
        throw new Error(`The search answered ${response.status}.`)
      }

      const answer = (await response.json()) as RecipeSearchResponse
      results.value = answer.results
      total.value = answer.total
      failed.value = false
    } catch {
      if (request.signal.aborted) {
        return
      }

      failed.value = true
    }
    searched.value = text
  }

  watch(query, (next) => {
    clearTimeout(timer)
    inFlight?.abort()
    const text = next.trim()
    if (!text) {
      results.value = []
      total.value = 0
      failed.value = false
      searched.value = ''
      return
    }

    timer = setTimeout(() => void search(text), SEARCH_DEBOUNCE_MS)
  })

  return { query, results, total, failed, searched }
}
