import { describe, expect, it, vi } from 'vitest'
import type { ReadRequest, ReadResult } from '~/Components/RecipeWizard/recipeImportApi'
import { startReadSession } from '~/Components/RecipeWizard/readSession'

const REQUEST: ReadRequest = { flow: 'recipe', photos: [new Blob(['page'], { type: 'image/jpeg' })] }

const RESULT: ReadResult = { success: false, data: null, aborted: true }

function deferred<T>() {
  let resolve: (value: T) => void = () => {}
  const promise = new Promise<T>((settle) => {
    resolve = settle
  })
  return { promise, resolve }
}

const tasksRun = () => new Promise((done) => setTimeout(done, 0))

function fakeRead() {
  const answer = deferred<ReadResult>()
  const read = vi.fn((_request: ReadRequest, _signal: AbortSignal) => answer.promise)
  return { read, answer }
}

const fakeWakeLock = (granted: Promise<void> = Promise.resolve()) => ({
  request: vi.fn(() => granted),
  release: vi.fn(() => Promise.resolve()),
})

describe('startReadSession', () => {
  it('reads the request it is given, and hands back what the read answered', async () => {
    const { read, answer } = fakeRead()

    const session = startReadSession(REQUEST, fakeWakeLock(), read)
    answer.resolve(RESULT)

    expect(await session.result).toBe(RESULT)
    expect(read).toHaveBeenCalledWith(REQUEST, expect.any(AbortSignal))
  })

  it('holds a screen wake lock from the start of the read', async () => {
    const wakeLock = fakeWakeLock()

    startReadSession(REQUEST, wakeLock, fakeRead().read)
    await tasksRun()

    expect(wakeLock.request).toHaveBeenCalledOnce()
    expect(wakeLock.release).not.toHaveBeenCalled()
  })

  it('lets the screen sleep once the read ends', async () => {
    const wakeLock = fakeWakeLock()
    const { read, answer } = fakeRead()

    const session = startReadSession(REQUEST, wakeLock, read)
    answer.resolve(RESULT)
    await session.result
    await tasksRun()

    expect(wakeLock.release).toHaveBeenCalledOnce()
  })

  it('lets the screen sleep as soon as the read ends, before the wake lock is granted', async () => {
    const wakeLock = fakeWakeLock(deferred<void>().promise)
    const { read, answer } = fakeRead()

    const session = startReadSession(REQUEST, wakeLock, read)
    answer.resolve(RESULT)
    await session.result

    expect(wakeLock.release).toHaveBeenCalledOnce()
  })

  it('cancels the request through the signal it reads under', () => {
    const { read } = fakeRead()

    const session = startReadSession(REQUEST, fakeWakeLock(), read)
    const signal = read.mock.calls[0]![1]
    expect(signal.aborted).toBe(false)

    session.cancel()
    expect(signal.aborted).toBe(true)
  })
})
