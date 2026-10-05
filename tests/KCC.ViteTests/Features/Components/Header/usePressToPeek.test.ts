import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { CardId } from '~/Components/Header/usePad'
import { PRESS_DELAY_MS, PRESS_SLOP_PX, usePressToPeek } from '~/Components/Header/usePressToPeek'

let peeked: CardId | null

const peeks = {
  peek: (id: CardId) => {
    peeked = id
  },
  unpeek: (id: CardId) => {
    if (peeked === id) peeked = null
  },
}

const pointer = (pointerType: string, clientX = 100, clientY = 20) => ({ pointerType, clientX, clientY }) as PointerEvent

function onDevice({ reducedMotion = false } = {}) {
  vi.stubGlobal('window', { matchMedia: () => ({ matches: reducedMotion }) })
}

beforeEach(() => {
  peeked = null
  vi.useFakeTimers()
  onDevice()
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('usePressToPeek with a mouse', () => {
  it('peeks while the pointer is over the menu, and puts the card back when it leaves', () => {
    const press = usePressToPeek(peeks)

    press.enter('recipes', pointer('mouse'))
    expect(peeked).toBe('recipes')

    press.leave('recipes', pointer('mouse'))
    expect(peeked).toBeNull()
  })

  it('ignores the pointer pressing down: the click that follows opens the card', () => {
    const press = usePressToPeek(peeks)

    press.down('recipes', pointer('mouse'))
    vi.advanceTimersByTime(PRESS_DELAY_MS)

    expect(peeked).toBeNull()
  })
})

describe.each(['touch', 'pen'])('usePressToPeek with %s', (pointerType) => {
  it('peeks once the press has held still for the delay', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType))
    vi.advanceTimersByTime(PRESS_DELAY_MS - 1)
    expect(peeked).toBeNull()

    vi.advanceTimersByTime(1)
    expect(peeked).toBe('menu')
  })

  it('never peeks on a tap that lifts before the delay', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType))
    vi.advanceTimersByTime(PRESS_DELAY_MS - 10)
    press.up()
    vi.advanceTimersByTime(PRESS_DELAY_MS)

    expect(peeked).toBeNull()
  })

  it('keeps the peek through the lift, for the click to open', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType))
    vi.advanceTimersByTime(PRESS_DELAY_MS)
    press.up()
    press.leave('menu', pointer(pointerType))

    expect(peeked).toBe('menu')
  })

  it('stays put for a wobble within the slop', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType, 100, 20))
    press.move(pointer(pointerType, 100 + PRESS_SLOP_PX, 20))
    vi.advanceTimersByTime(PRESS_DELAY_MS)

    expect(peeked).toBe('menu')
  })

  it('puts the card back when the press moves past the slop, as a scroll starting on the pad does', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType, 100, 20))
    vi.advanceTimersByTime(PRESS_DELAY_MS)
    press.move(pointer(pointerType, 100, 20 + PRESS_SLOP_PX + 1))

    expect(peeked).toBeNull()
  })

  it('never peeks when the press moves past the slop before the delay', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType, 100, 20))
    press.move(pointer(pointerType, 100 + PRESS_SLOP_PX + 1, 20))
    vi.advanceTimersByTime(PRESS_DELAY_MS)

    expect(peeked).toBeNull()
  })

  it('puts the card back when the browser cancels the press', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType))
    vi.advanceTimersByTime(PRESS_DELAY_MS)
    press.cancel()

    expect(peeked).toBeNull()
  })

  it('puts the card back when the press slides off the menu', () => {
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType))
    vi.advanceTimersByTime(PRESS_DELAY_MS)
    press.leave('menu', pointer(pointerType))

    expect(peeked).toBeNull()
  })

  it('never peeks under reduced motion: a tap simply opens', () => {
    onDevice({ reducedMotion: true })
    const press = usePressToPeek(peeks)

    press.down('menu', pointer(pointerType))
    vi.advanceTimersByTime(PRESS_DELAY_MS)

    expect(peeked).toBeNull()
  })
})
