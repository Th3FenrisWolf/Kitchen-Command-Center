import { ref } from 'vue'

export const TUCK_BELOW_PX = 150
export const SCROLL_SLOP_PX = 3

export function useTuckOnScroll(beforeTuck: () => void) {
  const tucked = ref(false)
  let anchor = 0
  let height = 0

  const reducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches

  // Sticky neighbours add this to their own top, so they sit below the pad while it shows.
  const publish = () => document.documentElement.style.setProperty('--pad-offset', `${tucked.value ? 0 : height}px`)

  function settle(next: boolean) {
    if (tucked.value !== next) {
      tucked.value = next
      publish()
    }
  }

  function start() {
    anchor = window.scrollY
  }

  function measure(next: number) {
    height = next
    publish()
  }

  function show() {
    settle(false)
  }

  // Measured from where the last move was counted, so a slow scroll still adds up past the slop.
  function onScroll() {
    const y = window.scrollY
    if (y - anchor > SCROLL_SLOP_PX) {
      anchor = y
      if (y > TUCK_BELOW_PX && !tucked.value && !reducedMotion()) {
        beforeTuck()
        settle(true)
      }
    } else if (anchor - y > SCROLL_SLOP_PX) {
      anchor = y
      settle(false)
    }
  }

  return { tucked, start, measure, show, onScroll }
}
