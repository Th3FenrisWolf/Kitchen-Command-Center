import { ref } from 'vue'

export const TUCK_BELOW_PX = 150
export const SCROLL_SLOP_PX = 3

export function useTuckOnScroll(beforeTuck: () => void) {
  const tucked = ref(false)
  let anchor = 0
  let height = 0

  const reducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches

  let content: HTMLElement | null | undefined

  // Sticky neighbours add --pad-h to their top under main:not(.is-tucked). A tuck only toggles that class: a custom
  // property written on <main> on every tuck restyled each element in it, where a class change restyles only the
  // elements a selector names. The root takes the plain scroll-padding-top, which restyles only itself.
  const main = () => (content ??= document.querySelector<HTMLElement>('main'))

  const publish = () => {
    main()?.classList.toggle('is-tucked', tucked.value)
    document.documentElement.style.scrollPaddingTop = `${tucked.value ? 0 : height}px`
  }

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
    if (next !== height) main()?.style.setProperty('--pad-h', `${next}px`)
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
