import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick } from 'vue'
import { SEARCH_DEBOUNCE_MS, useNavSearch } from '~/Components/Header/useNavSearch'

const hit = (name: string) => ({
  name,
  slug: `/recipes/${name.toLowerCase().replaceAll(' ', '-')}/`,
  tags: [],
  averageRating: null,
  reviewCount: 0,
  variantCount: 1,
  fastestTime: 20,
})

const answer = (names: string[], total = names.length) =>
  ({ ok: true, json: () => Promise.resolve({ results: names.map(hit), total }) }) as unknown as Response

let fetch: ReturnType<typeof vi.fn>

beforeEach(() => {
  vi.useFakeTimers()
  fetch = vi.fn()
  vi.stubGlobal('fetch', fetch)
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

function search() {
  const scope = effectScope()
  return scope.run(() => useNavSearch())!
}

async function type(query: { value: string }, text: string) {
  query.value = text
  await nextTick()
}

async function settle() {
  await vi.advanceTimersByTimeAsync(SEARCH_DEBOUNCE_MS)
  await vi.runAllTimersAsync()
}

describe('useNavSearch', () => {
  it('waits for the typing to pause, then asks for the first four results', async () => {
    fetch.mockResolvedValue(answer(['Big-Pot Chili']))
    const { query } = search()

    await type(query, 'ch')
    await type(query, 'chili')
    await vi.advanceTimersByTimeAsync(SEARCH_DEBOUNCE_MS - 1)
    expect(fetch).not.toHaveBeenCalled()

    await settle()
    expect(fetch).toHaveBeenCalledTimes(1)
    expect(fetch.mock.calls[0]![0]).toBe('/api/recipes/search?query=chili&pageSize=4')
  })

  it('holds the results, the total and the query they answer', async () => {
    fetch.mockResolvedValue(answer(['Big-Pot Chili', 'Chili Oil Noodles'], 7))
    const { query, results, total, searched, failed } = search()

    await type(query, '  chili ')
    await settle()

    expect(results.value.map((result) => result.name)).toEqual(['Big-Pot Chili', 'Chili Oil Noodles'])
    expect(total.value).toBe(7)
    expect(searched.value).toBe('chili')
    expect(failed.value).toBe(false)
  })

  it('cancels the request in flight when the query changes, without calling it a failure', async () => {
    fetch
      .mockImplementationOnce(
        (_url: string, init: RequestInit) =>
          new Promise<Response>((_resolve, reject) =>
            init.signal!.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError'))),
          ),
      )
      .mockResolvedValueOnce(answer(['Chili Oil Noodles']))
    const { query, results, searched, failed } = search()

    await type(query, 'chi')
    await settle()
    const first = fetch.mock.calls[0]![1] as RequestInit
    await type(query, 'chili oil')
    expect(first.signal!.aborted).toBe(true)
    expect(failed.value).toBe(false)
    expect(searched.value).toBe('')
    await settle()

    expect(results.value.map((result) => result.name)).toEqual(['Chili Oil Noodles'])
    expect(searched.value).toBe('chili oil')
  })

  it('forgets the results when the field is emptied', async () => {
    fetch.mockResolvedValue(answer(['Big-Pot Chili']))
    const { query, results, searched } = search()
    await type(query, 'chili')
    await settle()

    await type(query, '')

    expect(results.value).toEqual([])
    expect(searched.value).toBe('')
  })

  it('reports a search the site could not answer', async () => {
    fetch.mockResolvedValueOnce({ ok: false, status: 503 } as Response).mockRejectedValueOnce(new TypeError('offline'))
    const { query, failed, searched } = search()

    await type(query, 'chili')
    await settle()
    expect(failed.value).toBe(true)
    expect(searched.value).toBe('chili')

    await type(query, 'tacos')
    await settle()
    expect(failed.value).toBe(true)
  })

  it('clears the failure once a search answers again', async () => {
    fetch.mockRejectedValueOnce(new TypeError('offline')).mockResolvedValueOnce(answer(['Weeknight Tacos']))
    const { query, failed } = search()
    await type(query, 'tacos')
    await settle()

    await type(query, 'taco')
    await settle()

    expect(failed.value).toBe(false)
  })
})
