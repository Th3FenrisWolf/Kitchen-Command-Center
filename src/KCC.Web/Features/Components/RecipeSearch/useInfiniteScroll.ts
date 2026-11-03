import { computed, onBeforeUnmount, onMounted, ref, watch, type ComputedRef, type Ref, type WatchSource } from 'vue'

// Re-observes when the sentinel changes: it sits behind a v-if, so it comes and goes with the results.
// An observer reports only changes, so a sentinel still in view after a page loads would never report again.
// Re-observing after each page has the browser report it afresh once the new cards have laid out.
export function useInfiniteScroll(
  onHit: () => void,
  pageLoaded: WatchSource,
): { sentinel: ComputedRef<Ref<HTMLElement | null>> } {
  const sentinel = ref<HTMLElement | null>(null)
  let observer: IntersectionObserver | null = null

  onMounted(() => {
    if (typeof IntersectionObserver === 'undefined') {
      return
    }

    observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          onHit()
        }
      },
      { rootMargin: '400px' },
    )

    watch(
      sentinel,
      (el, prev) => {
        if (prev) {
          observer?.unobserve(prev)
        }

        if (el) {
          observer?.observe(el)
        }
      },
      { immediate: true },
    )

    watch(
      pageLoaded,
      () => {
        const el = sentinel.value
        if (el) {
          observer?.unobserve(el)
          observer?.observe(el)
        }
      },
      { flush: 'post' },
    )
  })

  onBeforeUnmount(() => observer?.disconnect())

  return { sentinel: computed(() => sentinel) }
}
