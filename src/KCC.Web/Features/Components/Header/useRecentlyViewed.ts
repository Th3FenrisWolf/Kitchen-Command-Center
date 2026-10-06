import { ref } from 'vue'

export const RECENTLY_VIEWED_KEY = 'kcc-recent'
export const RECENTLY_VIEWED_KEPT = 3

export interface RecentlyViewed {
  name: string
  url: string
}

const isRecentlyViewed = (value: unknown): value is RecentlyViewed =>
  typeof (value as RecentlyViewed | null)?.name === 'string' && typeof (value as RecentlyViewed | null)?.url === 'string'

export function readRecentlyViewed(): RecentlyViewed[] {
  try {
    const stored: unknown = JSON.parse(window.localStorage.getItem(RECENTLY_VIEWED_KEY) ?? '[]')
    return Array.isArray(stored) ? stored.filter(isRecentlyViewed).slice(0, RECENTLY_VIEWED_KEPT) : []
  } catch {
    return []
  }
}

export function recordRecentlyViewed(page: RecentlyViewed) {
  try {
    const kept = readRecentlyViewed().filter((seen) => seen.url !== page.url)
    window.localStorage.setItem(RECENTLY_VIEWED_KEY, JSON.stringify([page, ...kept].slice(0, RECENTLY_VIEWED_KEPT)))
  } catch {
    // Storage is blocked: the page simply goes unremembered.
  }
}

export function useRecentlyViewed() {
  const recent = ref<RecentlyViewed[]>([])

  return {
    recent,
    refresh: () => {
      recent.value = readRecentlyViewed()
    },
  }
}
