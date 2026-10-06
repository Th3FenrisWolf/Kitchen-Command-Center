import type { CardId } from './usePad'

export const PRESS_DELAY_MS = 80
export const PRESS_SLOP_PX = 8

export interface Peeks {
  peek(id: CardId): void
  unpeek(id: CardId): void
}

interface Press {
  id: CardId
  x: number
  y: number
  timer: ReturnType<typeof setTimeout>
}

// A mouse reaches for a menu by hovering it. A finger has no hover, so holding still on a menu stands in for it,
// and a press that starts a scroll moves or is cancelled before the delay runs out.
export function usePressToPeek(peeks: Peeks) {
  let press: Press | null = null

  const reducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches

  function release() {
    if (press) {
      clearTimeout(press.timer)
      press = null
    }
  }

  function cancel() {
    if (press) {
      const { id } = press
      release()
      peeks.unpeek(id)
    }
  }

  function enter(id: CardId, event: PointerEvent) {
    if (event.pointerType === 'mouse') {
      peeks.peek(id)
    }
  }

  function leave(id: CardId, event: PointerEvent) {
    if (event.pointerType === 'mouse') {
      peeks.unpeek(id)
    } else {
      cancel()
    }
  }

  function down(id: CardId, event: PointerEvent) {
    if (event.pointerType === 'mouse' || reducedMotion()) {
      return
    }

    cancel()
    press = { id, x: event.clientX, y: event.clientY, timer: setTimeout(() => peeks.peek(id), PRESS_DELAY_MS) }
  }

  function move(event: PointerEvent) {
    if (press && Math.hypot(event.clientX - press.x, event.clientY - press.y) > PRESS_SLOP_PX) {
      cancel()
    }
  }

  return { enter, leave, down, move, up: release, cancel }
}
