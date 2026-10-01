import { computed, onBeforeUnmount, onMounted, ref, watch, type ComputedRef, type Ref } from 'vue'

// Re-observes when the sentinel changes: it sits behind a v-if, so it comes and goes with the results.
export function useInfiniteScroll(onHit: () => void): { sentinel: ComputedRef<Ref<HTMLElement | null>> } {
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

  return { sentinel: computed(() => sentinel) }
}
