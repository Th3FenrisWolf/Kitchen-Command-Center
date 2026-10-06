import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  RECENTLY_VIEWED_KEY,
  readRecentlyViewed,
  recordRecentlyViewed,
  useRecentlyViewed,
} from '~/Components/Header/useRecentlyViewed'

let stored: Map<string, string>

function onBrowser({ blocked = false } = {}) {
  const refuse = () => {
    throw new DOMException('Storage is blocked.', 'SecurityError')
  }
  vi.stubGlobal('window', {
    localStorage: {
      getItem: blocked ? refuse : (key: string) => stored.get(key) ?? null,
      setItem: blocked ? refuse : (key: string, value: string) => stored.set(key, value),
    },
  })
}

const page = (name: string) => ({ name, url: `/recipes/${name.toLowerCase().replaceAll(' ', '-')}/` })

beforeEach(() => {
  stored = new Map()
  onBrowser()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('recently viewed recipes', () => {
  it('keeps the last three, the latest first', () => {
    for (const name of ['Weeknight Tacos', 'Legendary Lasagna', 'Shakshuka', 'Loaded Nachos']) {
      recordRecentlyViewed(page(name))
    }

    expect(readRecentlyViewed().map((seen) => seen.name)).toEqual(['Loaded Nachos', 'Shakshuka', 'Legendary Lasagna'])
  })

  it('moves a page seen again to the front instead of listing it twice', () => {
    recordRecentlyViewed(page('Weeknight Tacos'))
    recordRecentlyViewed(page('Shakshuka'))
    recordRecentlyViewed(page('Weeknight Tacos'))

    expect(readRecentlyViewed().map((seen) => seen.name)).toEqual(['Weeknight Tacos', 'Shakshuka'])
  })

  it('stores them as name and address under its own key', () => {
    recordRecentlyViewed(page('Shakshuka'))

    expect(JSON.parse(stored.get(RECENTLY_VIEWED_KEY)!)).toEqual([{ name: 'Shakshuka', url: '/recipes/shakshuka/' }])
  })

  it('reads nothing, and records nothing, where storage is blocked', () => {
    onBrowser({ blocked: true })

    expect(() => recordRecentlyViewed(page('Shakshuka'))).not.toThrow()
    expect(readRecentlyViewed()).toEqual([])
  })

  it('ignores whatever else the key holds', () => {
    stored.set(RECENTLY_VIEWED_KEY, '{not json')
    expect(readRecentlyViewed()).toEqual([])

    stored.set(RECENTLY_VIEWED_KEY, JSON.stringify([{ name: 'Shakshuka' }, page('Loaded Nachos'), 'Tacos']))
    expect(readRecentlyViewed()).toEqual([page('Loaded Nachos')])
  })

  it('reads the list only when asked, so the server render never holds it', () => {
    recordRecentlyViewed(page('Shakshuka'))
    const { recent, refresh } = useRecentlyViewed()

    expect(recent.value).toEqual([])
    refresh()
    expect(recent.value).toEqual([page('Shakshuka')])
  })
})
