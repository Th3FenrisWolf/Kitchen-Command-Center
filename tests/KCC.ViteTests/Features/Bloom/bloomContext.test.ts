import { afterEach, describe, expect, it, vi } from 'vitest'
import { createApp } from 'vue'
import { BLOOM_KEY, createBloom, useBloom, type Bloom } from '~/Bloom/bloomContext'
import { hash, scheduleWashes, WASH_WINDOW_S, type CollectedWash } from '~/Bloom/washes'

const PHI = (1 + Math.sqrt(5)) / 2
const GOLDEN_STEP = 0x9e3779b9
const THOUSAND_SEEDS = Array.from({ length: 1000 }, (_, i) => hash('late', i))
const RULE = /\.kcc-wash\[data-wash="([0-9a-f]{8})"\]\{--wash-at:(\.\d{3}|[1-9]\d*\.\d{3})s\}/g
const PAGE = '/recipes/brown-butter-gnocchi'
const ROUNDING = 0.001
const PAD_SLOTS = [1, 5, 9, 13]

function sheetOf(key: string, count: number, slots?: readonly number[]): CollectedWash[] {
  return Array.from({ length: count }, (_, k) => ({
    id: hash(key, k).toString(16).padStart(8, '0'),
    key,
    index: k,
    slot: slots?.[k],
  }))
}

function pageOf(page: string, washes: number): CollectedWash[] {
  const sheets: CollectedWash[] = []
  for (let i = 0; sheets.length < washes; i++) {
    sheets.push(...sheetOf(`${page}|sheet-${i}`, Math.min(1 + (i % 4), washes - sheets.length)))
  }
  return sheets
}

function blockRadius(washes: number): number {
  let beats = 64
  while (beats < 2 * washes + 32 && beats < 512) beats *= 2
  return Math.floor((0.75 * WASH_WINDOW_S * 1000) / beats) / 1000
}

function scheduledTimes(seed: number, washes: readonly CollectedWash[]): number[] {
  return [...scheduleWashes(seed, washes).matchAll(RULE)].map((m) => Number.parseFloat(m[2]))
}

function visitBeatsOf(seed: number): number[] {
  return scheduledTimes(seed, Array.from({ length: 16 }, (_, j) => sheetOf(`@lattice-${j}`, 1, [j])).flat())
}

function candidates(seed: number, page: string, washes: number, count: number): number[] {
  const phase = hash(seed, page)
  const visit = visitBeatsOf(seed)
  const radius = blockRadius(washes)
  const taken: number[] = []
  for (let j = 0; taken.length < count; j++) {
    const at = Math.floor((((phase + Math.imul(j, GOLDEN_STEP)) >>> 0) * WASH_WINDOW_S * 1000) / 2 ** 32) / 1000
    if (!visit.some((g) => Math.abs(at - g) <= radius + ROUNDING / 2)) taken.push(at)
  }
  return taken
}

function hydrated(seed: number, page: string, washes: readonly CollectedWash[]): Bloom {
  const bloom = createBloom(seed, page)
  for (const wash of washes) bloom.collect(wash)
  bloom.markHydrated()
  return bloom
}

function drawMix(bloom: Bloom, seed: number, total: number): number[] {
  const starts: number[] = []
  for (let i = 0; starts.length < total; i++) {
    const size = Math.min(1 + (hash(seed, 'mix', i) % 4), total - starts.length)
    const sheet = bloom.lateStarts(size)
    expect(sheet).toHaveLength(size)
    starts.push(...sheet!)
  }
  return starts
}

function sorted(values: Iterable<number>): number[] {
  return [...values].sort((a, b) => a - b)
}

function longestQuiet(starts: Iterable<number>): number {
  const times = [0, ...sorted(starts), WASH_WINDOW_S]
  let longest = 0
  for (let i = 1; i < times.length; i++) longest = Math.max(longest, times[i] - times[i - 1])
  return longest
}

function closest(starts: Iterable<number>): number {
  const times = sorted(starts)
  let gap = Infinity
  for (let i = 1; i < times.length; i++) gap = Math.min(gap, times[i] - times[i - 1])
  return gap
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('createBloom: keys', () => {
  it('qualifies a page key as `${page}|${key}` and leaves a global `@` key as it is', () => {
    const bloom = createBloom(5, PAGE)
    expect(bloom.seed).toBe(5)
    expect(bloom.page).toBe(PAGE)
    expect(bloom.claimKey('variant-instructions')).toBe(`${PAGE}|variant-instructions`)
    expect(bloom.claimKey('@pad')).toBe('@pad')
    expect(bloom.claimKey('@pad-card-recipes')).toBe('@pad-card-recipes')
  })

  it('suffixes a repeated key ~2, ~3 in claim order, with a development warning', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    const bloom = createBloom(5, PAGE)
    expect(bloom.claimKey('review:Ann|2026-01-02')).toBe(`${PAGE}|review:Ann|2026-01-02`)
    expect(warn).not.toHaveBeenCalled()
    expect(bloom.claimKey('review:Ann|2026-01-02')).toBe(`${PAGE}|review:Ann|2026-01-02~2`)
    expect(bloom.claimKey('review:Ann|2026-01-02')).toBe(`${PAGE}|review:Ann|2026-01-02~3`)
    expect(bloom.claimKey('@pad')).toBe('@pad')
    expect(bloom.claimKey('@pad')).toBe('@pad~2')
    expect(warn).toHaveBeenCalledTimes(3)
    expect(String(warn.mock.calls[0][0])).toContain('review:Ann|2026-01-02')
  })

  it('keeps repeats separate per bloom', () => {
    expect(createBloom(1, '/a').claimKey('x')).toBe('/a|x')
    expect(createBloom(1, '/a').claimKey('x')).toBe('/a|x')
  })

  it('hands a released key to the next sheet that claims it, with no suffix or warning', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    const bloom = createBloom(5, PAGE)
    const first = bloom.claimKey('reviews')
    bloom.releaseKey(first)
    expect(bloom.claimKey('reviews')).toBe(first)
    bloom.releaseKey(bloom.claimKey('@pad'))
    expect(bloom.claimKey('@pad')).toBe('@pad')
    expect(warn).not.toHaveBeenCalled()
  })

  it('reuses the lowest free suffix after a repeat is released', () => {
    vi.spyOn(console, 'warn').mockImplementation(() => {})
    const bloom = createBloom(5, PAGE)
    const base = bloom.claimKey('x')
    const second = bloom.claimKey('x')
    expect(bloom.claimKey('x')).toBe(`${base}~3`)
    bloom.releaseKey(second)
    expect(bloom.claimKey('x')).toBe(`${base}~2`)
    bloom.releaseKey(base)
    expect(bloom.claimKey('x')).toBe(base)
    expect(bloom.claimKey('x')).toBe(`${base}~4`)
  })
})

describe('createBloom: collection and lag', () => {
  it('records the washes collected during the render', () => {
    const bloom = createBloom(5, PAGE)
    const washes = [...sheetOf(`${PAGE}|hero`, 4), ...sheetOf('@pad', 2, [1, 7])]
    for (const wash of washes) bloom.collect(wash)
    expect(bloom.collected()).toEqual(washes)
  })

  it('has no lag before hydration and the seconds since page load after it', () => {
    const now = vi.spyOn(performance, 'now').mockReturnValue(12_500)
    const bloom = createBloom(5, PAGE)
    expect(bloom.lag()).toBe(0)
    bloom.markHydrated()
    expect(bloom.lag()).toBe(12.5)
    now.mockReturnValue(40_250)
    expect(bloom.lag()).toBe(40.25)
  })
})

describe('createBloom: late starts', () => {
  it('offers no late starts before hydration', () => {
    const bloom = createBloom(5, PAGE)
    expect(bloom.lateStarts(2)).toBeUndefined()
  })

  it.each([0, 2, 12, 50])('continues a %i-wash page’s golden-ratio sequence from index n, in sheet order', (n) => {
    for (const seed of THOUSAND_SEEDS.slice(0, 50)) {
      const washes = [...pageOf(PAGE, n), ...sheetOf('@pad', 4, PAD_SLOTS)]
      const bloom = hydrated(seed, PAGE, washes)
      const starts = [...bloom.lateStarts(3)!, ...bloom.lateStarts(1)!, ...bloom.lateStarts(4)!]
      expect(starts).toEqual(candidates(seed, PAGE, n, n + 8).slice(n))
      for (const t of starts) {
        expect(t).toBeGreaterThanOrEqual(0)
        expect(t).toBeLessThanOrEqual(209.999)
      }
    }
  })

  it('takes the phase from the visit seed and the page, so two empty pages of one visit differ', () => {
    expect(hydrated(9, '/recipes', []).lateStarts(4)).not.toEqual(hydrated(9, '/kitchen', []).lateStarts(4))
  })

  // The bounds of washes.test.ts for N = n + 20 starts, with the block radius of the n page washes.
  it.each([0, 2, 12, 50, 200])(
    'keeps a %i-wash page, the pad and 20 late washes in any mix of sheets within the quiet-stretch and gap bounds, over 1,000 seeds',
    (n) => {
      const radius = blockRadius(n)
      const total = n + 20
      const quiet = (2 * PHI ** 3 * WASH_WINDOW_S) / (Math.sqrt(5) * total) + 2 * radius + ROUNDING
      const gap = Math.min(radius, WASH_WINDOW_S / (Math.sqrt(5) * 2 * total)) - ROUNDING
      const washes = [...pageOf(PAGE, n), ...sheetOf('@pad', 4, PAD_SLOTS)]
      for (const seed of THOUSAND_SEEDS) {
        const starts = [...scheduledTimes(seed, washes), ...drawMix(hydrated(seed, PAGE, washes), seed, 20)]
        expect(longestQuiet(starts), `seed ${seed}`).toBeLessThanOrEqual(quiet)
        expect(closest(starts), `seed ${seed}`).toBeGreaterThanOrEqual(gap)
      }
    },
  )

  it('never starts a late wash within the block radius of a visit beat', () => {
    for (const n of [0, 12, 50]) {
      const radius = blockRadius(n)
      for (const seed of THOUSAND_SEEDS.slice(0, 100)) {
        const visit = visitBeatsOf(seed)
        for (const t of hydrated(seed, PAGE, pageOf(PAGE, n)).lateStarts(40)!) {
          for (const g of visit) expect(Math.abs(t - g), `n ${n} seed ${seed}`).toBeGreaterThan(radius)
        }
      }
    }
  })
})

describe('createBloom: visit starts', () => {
  it('offers none before hydration', () => {
    expect(createBloom(5, PAGE).visitStarts('@pad', 4, PAD_SLOTS)).toBeUndefined()
  })

  it('gives a global sheet the visit beats of its slots, in order, and leaves the page sequence alone', () => {
    for (const seed of THOUSAND_SEEDS.slice(0, 50)) {
      const washes = pageOf(PAGE, 6)
      const bloom = hydrated(seed, PAGE, washes)
      const beats = visitBeatsOf(seed)
      const starts = bloom.visitStarts('@late', 4, [9, 1, 13, 5])
      expect(starts).toEqual([1, 5, 9, 13].map((slot) => beats[slot]))
      expect(bloom.lateStarts(2)).toEqual(candidates(seed, PAGE, 6, 8).slice(6))
    }
  })

  it('falls back to the key hash when a global sheet names no slots', () => {
    const seed = THOUSAND_SEEDS[3]
    const bloom = hydrated(seed, PAGE, [])
    const beats = visitBeatsOf(seed)
    expect(bloom.visitStarts('@late', 2)).toEqual(
      [0, 1].map((index) => beats[hash('@late', index) % 16]).sort((a, b) => a - b),
    )
  })
})

describe('useBloom', () => {
  it('returns the provided bloom', () => {
    const bloom = createBloom(3, '/recipes')
    const app = createApp({})
    app.provide(BLOOM_KEY, bloom)
    expect(app.runWithContext(() => useBloom())).toBe(bloom)
  })

  it('defaults to seed 0, page "", no lag and no collection without a provided bloom', () => {
    const bloom = createApp({}).runWithContext(() => useBloom())
    expect(bloom.seed).toBe(0)
    expect(bloom.page).toBe('')
    expect(bloom.lag()).toBe(0)
    bloom.collect(sheetOf('/a|x', 1)[0])
    expect(bloom.collected()).toEqual([])
    expect(bloom.lateStarts(1)).toBeUndefined()
  })
})
