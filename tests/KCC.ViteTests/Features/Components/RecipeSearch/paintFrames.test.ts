import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { paintFrames } from '~/Components/RecipeSearch/paintFrames'

let queued = new Map<number, () => void>()
let lastId = 0

const paintOneFrame = () => {
  const due = [...queued.values()]
  queued = new Map()
  due.forEach((callback) => callback())
}

beforeEach(() => {
  queued = new Map()
  lastId = 0
  vi.stubGlobal('requestAnimationFrame', (callback: () => void) => {
    queued.set(++lastId, callback)
    return lastId
  })
  vi.stubGlobal('cancelAnimationFrame', (id: number) => queued.delete(id))
})

afterEach(() => vi.unstubAllGlobals())

describe('work held to a painted frame', () => {
  it('runs once in the next frame however often it is asked for', () => {
    const run = vi.fn()
    const frames = paintFrames(run)

    frames.nextFrame()
    frames.nextFrame()
    paintOneFrame()

    expect(run).toHaveBeenCalledTimes(1)
  })

  it('waits for a landing to be painted before it runs', () => {
    const run = vi.fn()
    const frames = paintFrames(run)

    frames.afterPaint()
    paintOneFrame()
    expect(run).not.toHaveBeenCalled()

    paintOneFrame()
    expect(run).toHaveBeenCalledTimes(1)
  })

  it('still waits for the landing when a scroll had already asked for the next frame', () => {
    const run = vi.fn()
    const frames = paintFrames(run)

    frames.nextFrame()
    frames.afterPaint()
    paintOneFrame()
    expect(run).not.toHaveBeenCalled()

    paintOneFrame()
    expect(run).toHaveBeenCalledTimes(1)
  })

  it('keeps the wait for the landing when a scroll asks for the next frame meanwhile', () => {
    const run = vi.fn()
    const frames = paintFrames(run)

    frames.afterPaint()
    frames.nextFrame()
    paintOneFrame()

    expect(run).not.toHaveBeenCalled()
  })

  it('runs nothing once cancelled', () => {
    const run = vi.fn()
    const frames = paintFrames(run)

    frames.afterPaint()
    frames.cancel()
    paintOneFrame()
    paintOneFrame()

    expect(run).not.toHaveBeenCalled()
  })
})
