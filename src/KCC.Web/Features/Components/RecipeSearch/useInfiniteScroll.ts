import { onBeforeUnmount, onMounted, ref, watch, type Ref } from 'vue'

/** Calls `onHit` when the sentinel scrolls into view. No-op during SSR. Re-observes when the
 *  sentinel element changes (it is behind a v-if, so it is created/destroyed as results change). */
export function useInfiniteScroll(onHit: () => void) {
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
  })

  onBeforeUnmount(() => observer?.disconnect())

  return { sentinel } as { sentinel: Ref<HTMLElement | null> }
}
