import { computed, onBeforeUnmount, onMounted, ref, watch, type Ref, type StyleValue, type WatchSource } from 'vue'
import { paintFrames } from './paintFrames'

export interface NextPageLayout {
  cards: DOMRect[]
  rowGap: number
  next: number
  sentinelPadding: number
}

export function nextPageHeight({ cards, rowGap, next, sentinelPadding }: NextPageLayout): number {
  const tallest = Math.max(0, ...cards.map((card) => card.height))
  if (next <= 0 || !tallest) {
    return 0
  }

  const firstRow = cards[0].top
  const columns = Math.max(1, cards.filter((card) => Math.abs(card.top - firstRow) < 1).length)
  const rows = Math.ceil((cards.length + next) / columns) - Math.ceil(cards.length / columns)
  return rows * (tallest + rowGap) + sentinelPadding
}

const px = (value: string) => parseFloat(value) || 0

// The space for the next page sits in the sentinel, below the cards, and a page lands into it. The region's
// min-height holds its height through the landing, so what follows it stays put however tall the page turns out.
// Space is added or given back only while the edge it moves is below the screen: a reader resting on the footer
// would see it move. A load waits only for the frame that tries to place the space. Where it cannot be placed, the
// page loads at once, as it always has, and the landing moves the footer.
export function useReservedSpace(
  region: Ref<HTMLElement | null>,
  sentinel: Ref<HTMLElement | null>,
  nextCount: () => number,
  results: Ref<readonly unknown[]>,
  layoutChanged: WatchSource,
) {
  const reserve = ref(0)
  let hold = 0
  // Set on the element itself: any style binding makes the server print style="" or reorder the region's classes.
  const setHold = (height: number) => {
    hold = height
    if (region.value) {
      region.value.style.minHeight = height ? `${height}px` : ''
    }
  }
  const settled = ref(0)
  let pending = false
  let measured = false
  let waiting = false

  const measure = () => {
    const area = region.value
    if (!area) {
      return null
    }

    const top = area.getBoundingClientRect().top
    const end = sentinel.value
    if (!end) {
      const last = area.lastElementChild
      return {
        top,
        bottom: area.getBoundingClientRect().bottom,
        natural: last ? last.getBoundingClientRect().bottom - top : 0,
        space: 0,
      }
    }

    const cards = end.previousElementSibling
    const style = getComputedStyle(end)
    const padding = px(style.paddingTop) + px(style.paddingBottom)
    const space = nextPageHeight({
      cards: cards ? [...cards.children].map((card) => card.getBoundingClientRect()) : [],
      rowGap: cards ? px(getComputedStyle(cards).rowGap) : 0,
      next: nextCount(),
      sentinelPadding: padding,
    })
    const cardsEnd = end.getBoundingClientRect().top - top
    return { top, bottom: area.getBoundingClientRect().bottom, natural: cardsEnd + padding, space, cardsEnd }
  }

  const update = () => {
    place()
    measured = true
    if (waiting) {
      waiting = false
      settled.value++
    }
  }

  const place = () => {
    const layout = measure()
    if (!layout) {
      return
    }

    const target = layout.space ? (layout.cardsEnd ?? 0) + layout.space : 0
    if (target === hold && layout.space === reserve.value) {
      pending = false
      return
    }

    // A hold the results already fill moves nothing when it goes, so the last page leaves no min-height behind.
    const filled = !target && hold <= layout.natural
    const edgeAfter = layout.top + Math.max(layout.natural, target)
    if (!filled && Math.min(layout.bottom, edgeAfter) < innerHeight) {
      pending = true
      return
    }

    setHold(target)
    reserve.value = layout.space
    pending = false
    if (layout.space) {
      waiting = true
    }
  }

  // A first page shorter than the screen has no edge below it to place space past, so it loads without the frame.
  const ready = () => {
    const end = sentinel.value
    if (measured || !end || end.getBoundingClientRect().top + scrollY < innerHeight) {
      return true
    }

    waiting = true
    schedule()
    return false
  }

  const { nextFrame: schedule, afterPaint: scheduleAfterPaint, cancel } = paintFrames(update)

  const onScroll = () => {
    if (pending) {
      schedule()
    }
  }

  watch(
    results,
    (now, before) => {
      const appended = now.length > before.length && now[before.length - 1] === before[before.length - 1]
      setHold(appended && (reserve.value || hold) && region.value ? region.value.getBoundingClientRect().height : 0)
      reserve.value = 0
      measured = false
    },
    { flush: 'pre' },
  )

  watch(
    layoutChanged,
    () => {
      setHold(0)
      reserve.value = 0
      measured = false
    },
    { flush: 'pre' },
  )

  watch(results, scheduleAfterPaint, { flush: 'post' })
  watch(layoutChanged, schedule, { flush: 'post' })

  onMounted(() => {
    schedule()
    addEventListener('scroll', onScroll, { passive: true })
    addEventListener('resize', schedule, { passive: true })
  })

  onBeforeUnmount(() => {
    cancel()
    removeEventListener('scroll', onScroll)
    removeEventListener('resize', schedule)
  })

  // An attribute object rather than a style binding, which the server prints as an empty style="".
  const sentinelAttrs = computed<{ style?: StyleValue }>(() =>
    reserve.value ? { style: { minHeight: `${reserve.value}px`, alignItems: 'flex-end' } } : {},
  )

  return { sentinelAttrs, settled, ready }
}
