import { describe, expect, expectTypeOf, it, vi } from 'vitest'
import { bakeBlob, BLOB_TEXELS, WASH_SPAN } from '~/Bloom/bakeWashes'
import { BLOB_COUNT, BLOB_SHAPES } from '~/Bloom/blobShapes'
import * as washesModule from '~/Bloom/washes'
import {
  hash,
  planSheet,
  scheduleStarts,
  scheduleWashes,
  sheetSize,
  toFixed3,
  visitStarts,
  WASH_BLOBS,
  WASH_WINDOW_S,
  type CollectedWash,
  type PlannedWash,
  type WashProperty,
  type WashSize,
} from '~/Bloom/washes'
import { WASHES, type Wash } from '~/Types/DesignSystem'

const PROPERTY_ORDER: readonly WashProperty[] = [
  '--wash-x',
  '--wash-y',
  '--wash-dx',
  '--wash-dy',
  '--wash-d',
  '--wash-aspect',
  '--wash-s',
  '--wash-turn',
  '--wash-blob',
  '--wash-flip',
  '--wash-o3',
  '--wash-o4',
  '--wash-ink',
  '--wash-ink-2',
  '--wash-fallback-at',
  '--wash-peak',
]

const UNITLESS: readonly WashProperty[] = [
  '--wash-x',
  '--wash-y',
  '--wash-d',
  '--wash-aspect',
  '--wash-s',
  '--wash-o3',
  '--wash-o4',
  '--wash-peak',
]

const FIXED = String.raw`-?(?:[1-9]\d*)?\.\d{3}`
const INK = new RegExp(`^var\\(--color-(${WASHES.join('|')})\\)$`)
const COUNT: Record<WashSize, number | undefined> = { page: 4, reading: 3, card: undefined, none: 0 }
const SEEDS = Array.from({ length: 200 }, (_, i) => hash('seed', i))
const THOUSAND_SEEDS = Array.from({ length: 1000 }, (_, i) => hash('field', i))
const KEYS = ['/|hero', '/recipes|results', '@pad', '@pad-card-recipes', '/recipes/soup|review:Ann|2026-01-02', 'error']

function plan(seed: number, key: string, size: WashSize, chosen?: Wash): PlannedWash[] {
  const washes = planSheet({ seed, key, size, chosen })
  const expected = COUNT[size]
  if (expected === undefined) expect([1, 2]).toContain(washes.length)
  else expect(washes).toHaveLength(expected)
  return washes
}

function num(wash: PlannedWash, property: WashProperty): number {
  return Number.parseFloat(wash.style[property])
}

function inkOf(value: string): Wash {
  const match = INK.exec(value)
  expect(match, value).not.toBeNull()
  return match![1] as Wash
}

function quadrant(wash: PlannedWash): number {
  const x = num(wash, '--wash-x')
  const y = num(wash, '--wash-y')
  expect(x).not.toBe(0.5)
  expect(y).not.toBe(0.5)
  return (x < 0.5 ? 0 : 1) + (y < 0.5 ? 0 : 2)
}

function partnerOf(index: number, count: number): number | undefined {
  if (count < 3) return undefined
  if (index === 0 || index === 2) return 2 - index
  return count === 4 ? 4 - index : undefined
}

function fallbackAt(wash: PlannedWash): number {
  expect(wash.style['--wash-fallback-at']).toMatch(new RegExp(`^${FIXED}s$`))
  return num(wash, '--wash-fallback-at')
}

const ARRANGEMENT: readonly WashProperty[] = ['--wash-x', '--wash-y', '--wash-d', '--wash-turn', '--wash-ink']

function arrangement(washes: PlannedWash[]): string {
  return washes.map((w) => ARRANGEMENT.map((p) => w.style[p])).join(';')
}

function capped(wash: PlannedWash, width: number, height: number): number {
  const gap = Math.hypot(num(wash, '--wash-dx') * width, num(wash, '--wash-dy') * height)
  return Math.max(0, Math.min(num(wash, '--wash-d') * width, gap / (num(wash, '--wash-s') * WASH_SPAN)))
}

describe('hash', () => {
  it.each([
    [[''], 2872998923],
    [['a'], 444641715],
    [['@pad', 0], 2871926896],
    [['/|hero', 3], 2246521296],
    [[4294967295, '/recipes|card', 1], 1061828606],
    [[0, 'é', 2], 3351250834],
  ] as const)('pins %j to %i', (parts, expected) => {
    expect(hash(...parts)).toBe(expected)
  })

  it('is FNV-1a over the parts joined by U+001F, through the murmur3 finalizer, as a uint32', () => {
    function reference(...parts: Array<string | number>): number {
      const s = parts.map(String).join('\u001f')
      let h = 0x811c9dc5
      for (let i = 0; i < s.length; i++) {
        h ^= s.charCodeAt(i)
        h = Math.imul(h, 0x01000193)
      }
      h ^= h >>> 16
      h = Math.imul(h, 0x85ebca6b)
      h ^= h >>> 13
      h = Math.imul(h, 0xc2b2ae35)
      h ^= h >>> 16
      return h >>> 0
    }
    for (const key of KEYS) {
      for (const k of [0, 1, 2, 3]) {
        expect(hash(key, k)).toBe(reference(key, k))
        expect(hash(SEEDS[k], key, k)).toBe(reference(SEEDS[k], key, k))
      }
    }
  })
})

describe('toFixed3', () => {
  it.each([
    [0.2814, '.281'],
    [1.4061, '1.406'],
    [211.33, '211.330'],
    [41.22, '41.220'],
    [-0.433, '-.433'],
    [-1.25, '-1.250'],
    [9, '9.000'],
    [0, '.000'],
    [-0.0001, '.000'],
    [209.9996, '210.000'],
  ])('writes %d as %s', (value, expected) => {
    expect(toFixed3(value)).toBe(expected)
  })
})

describe('planSheet: determinism and format', () => {
  it('exports a 210-second window', () => {
    expect(WASH_WINDOW_S).toBe(210)
  })

  it('gives byte-identical output for the same seed, key, size and chosen colour', async () => {
    const inputs = [
      { seed: SEEDS[0], key: '/|hero', size: 'page' as const, chosen: 'peach' as const },
      { seed: SEEDS[1], key: '/recipes|results', size: 'reading' as const },
      { seed: SEEDS[2], key: '@pad-card-recipes', size: 'card' as const, chosen: 'sky' as const },
    ]
    const first = inputs.map((input) => JSON.stringify(planSheet(input)))
    const again = inputs.map((input) => JSON.stringify(planSheet(input)))
    vi.resetModules()
    const fresh = await import('~/Bloom/washes')
    const reloaded = inputs.map((input) => JSON.stringify(fresh.planSheet(input)))
    expect(first.every((json) => json.length > 2)).toBe(true)
    expect(again).toEqual(first)
    expect(reloaded).toEqual(first)
  })

  it('writes every field of a wash, in a fixed order, with its unit and three decimals', () => {
    for (const seed of SEEDS.slice(0, 50)) {
      for (const size of ['card', 'reading', 'page'] as const) {
        for (const wash of plan(seed, '/|hero', size)) {
          expect(Object.keys(wash.style)).toEqual(PROPERTY_ORDER)
          for (const p of UNITLESS) expect(wash.style[p], p).toMatch(new RegExp(`^${FIXED}$`))
          expect(wash.style['--wash-turn']).toMatch(new RegExp(`^${FIXED}deg$`))
          expect(wash.style['--wash-flip']).toMatch(/^-?1$/)
          expect(wash.style['--wash-fallback-at']).toMatch(new RegExp(`^${FIXED}s$`))
          expect(wash.style['--wash-ink']).toMatch(INK)
          expect(wash.style['--wash-ink-2']).toMatch(INK)
          for (const p of ['--wash-dx', '--wash-dy'] as const) {
            if (wash.style[p] !== '9') expect(wash.style[p], p).toMatch(new RegExp(`^${FIXED}$`))
          }
        }
      }
    }
  })

  it('names each wash hash(key, k) as eight lower-case hex digits, with no seed', () => {
    for (const key of KEYS) {
      const ids = plan(SEEDS[0], key, 'page').map((w) => w.id)
      expect(ids).toEqual([0, 1, 2, 3].map((k) => hash(key, k).toString(16).padStart(8, '0')))
      for (const id of ids) expect(id).toMatch(/^[0-9a-f]{8}$/)
      expect(new Set(ids).size).toBe(4)
      for (const seed of SEEDS.slice(1, 20)) expect(plan(seed, key, 'page').map((w) => w.id)).toEqual(ids)
    }
    expect(plan(SEEDS[3], '@pad', 'page')[0].id).toBe('ab2e2070')
    expect(plan(SEEDS[3], '/|hero', 'page')[3].id).toBe('85e731d0')
  })

  it('carries the key and the index of each wash', () => {
    const washes = plan(SEEDS[4], '/recipes|results', 'reading')
    expect(washes.map((w) => w.key)).toEqual(['/recipes|results', '/recipes|results', '/recipes|results'])
    expect(washes.map((w) => w.index)).toEqual([0, 1, 2])
  })
})

describe('planSheet and sheetSize: counts and size', () => {
  it('plans 4 washes on a page, 3 on a reading sheet and none for none', () => {
    for (const seed of SEEDS) {
      expect(planSheet({ seed, key: '/|hero', size: 'page' })).toHaveLength(4)
      expect(planSheet({ seed, key: '/|hero', size: 'reading' })).toHaveLength(3)
      expect(planSheet({ seed, key: '/|hero', size: 'none' })).toEqual([])
    }
  })

  it('plans 1 or 2 washes on a card, by the hash, and both occur across seeds', () => {
    const counts = SEEDS.map((seed) => planSheet({ seed, key: '/recipes|card:soup', size: 'card' }).length)
    expect(counts.every((n) => n === 1 || n === 2)).toBe(true)
    expect(counts).toContain(1)
    expect(counts).toContain(2)
  })

  it('never plans more than 4 washes', () => {
    const lengths = SEEDS.flatMap((seed) =>
      (['card', 'reading', 'page', 'none'] as const).flatMap((size) =>
        KEYS.map((key) => planSheet({ seed, key, size }).length),
      ),
    )
    expect(Math.max(...lengths)).toBe(4)
  })

  it.each([
    [{ bloom: 'none', tear: 'hero', margin: true, wide: true }, 'none'],
    [{ bloom: 'card', tear: 'hero' }, 'card'],
    [{ bloom: 'reading', wide: true }, 'reading'],
    [{ bloom: 'page' }, 'page'],
    [{ tear: 'hero', margin: true }, 'page'],
    [{ tear: 'hero' }, 'page'],
    [{ tear: 3, margin: true }, 'reading'],
    [{ margin: true, wide: true }, 'reading'],
    [{ wide: true }, 'page'],
    [{ tear: 2 }, 'card'],
    [{}, 'card'],
  ] as const)('sizes %j as %s', (props, expected) => {
    expect(sheetSize(props)).toBe(expected)
  })
})

describe('planSheet: colour and strength', () => {
  it('inks every wash with two different brand washes, all distinct on the sheet', () => {
    for (const seed of SEEDS) {
      for (const size of ['card', 'reading', 'page'] as const) {
        const washes = plan(seed, '/recipes|results', size)
        const inks = washes.flatMap((w) => [inkOf(w.style['--wash-ink']), inkOf(w.style['--wash-ink-2'])])
        expect(new Set(inks).size).toBe(inks.length)
      }
    }
  })

  it('inks the four washes of a page with all eight brand washes between their two inks', () => {
    for (const seed of SEEDS) {
      const washes = plan(seed, '/|hero', 'page')
      const slots = [...washes.map((w) => w.style['--wash-ink']), ...washes.map((w) => w.style['--wash-ink-2'])]
      expect(slots.map(inkOf).sort()).toEqual([...WASHES].sort())
    }
  })

  it('shuffles per sheet, so wash 0 wears every colour across seeds', () => {
    const firsts = new Set(SEEDS.map((seed) => inkOf(plan(seed, '/|hero', 'page')[0].style['--wash-ink'])))
    expect(firsts.size).toBe(WASHES.length)
  })

  it('gives the chosen colour to wash 0 as its first ink', () => {
    for (const chosen of WASHES) {
      for (const seed of SEEDS.slice(0, 25)) {
        for (const size of ['card', 'reading', 'page'] as const) {
          const washes = plan(seed, '/|hero', size, chosen)
          expect(washes[0].style['--wash-ink']).toBe(`var(--color-${chosen})`)
          const inks = washes.flatMap((w) => [w.style['--wash-ink'], w.style['--wash-ink-2']])
          expect(new Set(inks).size).toBe(inks.length)
        }
      }
    }
  })

  it('peaks each wash at 0.75 to 1', () => {
    for (const seed of SEEDS) {
      for (const wash of plan(seed, '/|hero', 'page')) {
        expect(num(wash, '--wash-peak')).toBeGreaterThanOrEqual(0.75)
        expect(num(wash, '--wash-peak')).toBeLessThanOrEqual(1)
      }
    }
  })

  it('stratifies the fallback starts by the key hash, clamped to [0, 210), wash 0 first', () => {
    for (const seed of SEEDS.slice(0, 50)) {
      for (const key of [...KEYS, ...Array.from({ length: 40 }, (_, i) => `/recipes|card:${i}`)]) {
        for (const size of ['card', 'reading', 'page'] as const) {
          const washes = plan(seed, key, size)
          const n = washes.length
          const p = hash(key) / 2 ** 32
          const starts = washes.map(fallbackAt)
          starts.forEach((t, k) => {
            const low = Math.max(0, ((k + p - 0.3) / n) * 210)
            const high = Math.min(209.999, ((k + p + 0.3) / n) * 210)
            expect(t, `${key} ${size} wash ${k}`).toBeGreaterThanOrEqual(low - 0.0005)
            expect(t, `${key} ${size} wash ${k}`).toBeLessThanOrEqual(high + 0.0005)
            expect(t).toBeGreaterThanOrEqual(0)
            expect(t).toBeLessThan(210)
          })
          for (let k = 1; k < n; k++) {
            expect(starts[k]).toBeGreaterThan(starts[0])
            expect(starts[k] - starts[k - 1]).toBeGreaterThanOrEqual((0.4 / n) * 210 - 0.001)
          }
        }
      }
    }
  })

  it('writes a late sheet’s starts exactly, to three decimals, with wash 0 the earliest', () => {
    const washes = planSheet({
      seed: SEEDS[5],
      key: '/recipes/soup|review:Ann',
      size: 'page',
      starts: [150.1234, 12.5, 99.9996, 61],
    })
    expect(washes).toHaveLength(4)
    const written = washes.map((w) => w.style['--wash-fallback-at'])
    expect(written[0]).toBe('12.500s')
    expect([...written].sort()).toEqual(['100.000s', '12.500s', '150.123s', '61.000s'])

    const card = planSheet({ seed: SEEDS[6], key: '/recipes/soup|review:Bo', size: 'reading', starts: [200.0004, 3.25, 47] })
    expect(card.map((w) => w.style['--wash-fallback-at'])[0]).toBe('3.250s')
    expect(card.map((w) => w.style['--wash-fallback-at']).sort()).toEqual(['200.000s', '3.250s', '47.000s'])
  })
})

describe('planSheet: variety and cover', () => {
  it('gives 20 seeds of one key 20 distinct arrangements', () => {
    for (const size of ['card', 'reading', 'page'] as const) {
      const seen = new Set(SEEDS.slice(0, 20).map((seed) => arrangement(plan(seed, '/|hero', size))))
      expect(seen.size).toBe(20)
    }
  })

  it('gives six distinct keys on one seed six distinct arrangements', () => {
    for (const size of ['card', 'reading', 'page'] as const) {
      const seen = new Set(KEYS.map((key) => arrangement(plan(SEEDS[7], key, size))))
      expect(seen.size).toBe(KEYS.length)
    }
  })

  it('puts two washes in opposite halves of a hashed diagonal', () => {
    const diagonals = new Set<number>()
    for (const seed of SEEDS) {
      const washes = plan(seed, '/recipes|card:soup', 'card')
      if (washes.length !== 2) continue
      const [a, b] = washes.map(quadrant)
      expect(a + b).toBe(3)
      diagonals.add(Math.min(a, b))
    }
    expect([...diagonals].sort()).toEqual([0, 1])
  })

  it('gives three or four washes one quadrant each, with 0 and 2, 1 and 3 opposite, rotated by the hash', () => {
    const firsts = new Set<number>()
    for (const seed of SEEDS) {
      for (const size of ['reading', 'page'] as const) {
        const quadrants = plan(seed, '/|hero', size).map(quadrant)
        expect(new Set(quadrants).size).toBe(quadrants.length)
        expect(quadrants[0] + quadrants[2]).toBe(3)
        if (quadrants.length === 4) expect(quadrants[1] + quadrants[3]).toBe(3)
        firsts.add(quadrants[0])
      }
    }
    expect(firsts.size).toBe(4)
  })

  it('keeps every centre within the field and its edge bands and every shape in its range, drawing every baked blob', () => {
    const blobs = new Set<string>()
    for (const seed of SEEDS) {
      for (const size of ['card', 'reading', 'page'] as const) {
        for (const wash of plan(seed, '/recipes|results', size)) {
          for (const p of ['--wash-x', '--wash-y'] as const) {
            expect(num(wash, p)).toBeGreaterThanOrEqual(-0.02)
            expect(num(wash, p)).toBeLessThanOrEqual(1.02)
          }
          expect(num(wash, '--wash-d')).toBeGreaterThanOrEqual(0.9)
          expect(num(wash, '--wash-d')).toBeLessThanOrEqual(1.25)
          const aspect = num(wash, '--wash-aspect')
          expect(aspect).toBeGreaterThanOrEqual(0.68)
          expect(aspect).toBeLessThanOrEqual(1.32)
          expect(num(wash, '--wash-s')).toBeCloseTo(Math.sqrt(1 + 1 / aspect ** 2), 2)
          expect(num(wash, '--wash-turn')).toBeGreaterThanOrEqual(0)
          expect(num(wash, '--wash-turn')).toBeLessThan(360)
          expect(wash.style['--wash-blob']).toMatch(new RegExp(`^var\\(--wash-blob-[0-${WASH_BLOBS - 1}]\\)$`))
          blobs.add(wash.style['--wash-blob'])
        }
      }
    }
    expect(blobs.size).toBe(WASH_BLOBS)
  })
})

describe('planSheet: two deep at most', () => {
  const FIELDS = [
    ['a 280x120 card', 280, 120],
    ['a 700x2400 reading sheet', 700, 2400],
    ['a 1300x400 hero', 1300, 400],
  ] as const

  it('exports no bleed reach in pixels', () => {
    expect(washesModule).not.toHaveProperty('WASH_REACH_PX')
  })

  it('reads the padded span from the bake', () => {
    expect(WASH_SPAN).toBe(1.12)
  })

  it('points each paired wash at its partner and parks an unpaired one at 9, 9', () => {
    for (const seed of SEEDS) {
      for (const size of ['card', 'reading', 'page'] as const) {
        const washes = plan(seed, '/|hero', size)
        washes.forEach((wash, k) => {
          const partner = partnerOf(k, washes.length)
          if (partner === undefined) {
            expect(wash.style['--wash-dx']).toBe('9')
            expect(wash.style['--wash-dy']).toBe('9')
            return
          }
          const other = washes[partner]
          expect(num(wash, '--wash-dx')).toBeCloseTo(num(other, '--wash-x') - num(wash, '--wash-x'), 6)
          expect(num(wash, '--wash-dy')).toBeCloseTo(num(other, '--wash-y') - num(wash, '--wash-y'), 6)
        })
      }
    }
  })

  it.each(FIELDS)(
    'keeps each pair’s circles disjoint under the kit’s cap on %s, padded box included',
    (_, width, height) => {
      let pairs = 0
      for (const seed of THOUSAND_SEEDS) {
        for (const size of ['card', 'reading', 'page'] as const) {
          const washes = plan(seed, '/|hero', size)
          for (const [a, b] of [
            [0, 2],
            [1, 3],
          ]) {
            if (b >= washes.length) continue
            const [wa, wb] = [capped(washes[a], width, height), capped(washes[b], width, height)]
            if (wa === 0 || wb === 0) continue
            pairs++
            const ra = (wa * num(washes[a], '--wash-s') * WASH_SPAN) / 2
            const rb = (wb * num(washes[b], '--wash-s') * WASH_SPAN) / 2
            const distance = Math.hypot(
              (num(washes[b], '--wash-x') - num(washes[a], '--wash-x')) * width,
              (num(washes[b], '--wash-y') - num(washes[a], '--wash-y')) * height,
            )
            expect(ra + rb, `seed ${seed} ${size} pair ${a}-${b}`).toBeLessThanOrEqual(distance + 1e-6)
          }
        }
      }
      expect(pairs).toBeGreaterThan(100)
    },
  )
})

const BLOB = /^var\(--wash-blob-(\d+)\)$/
const SHEET_FIELDS = [
  ['card', 'a 280x120 card', 280, 120, '/recipes|card:soup', '/recipes|card:'],
  ['reading', 'a 700x2400 reading sheet', 700, 2400, '/recipes/soup|review:Ann', '/recipes/soup|review:'],
  ['page', 'a 1300x400 hero', 1300, 400, '/|hero', undefined],
] as const
const BLOB_MARGIN = (WASH_SPAN - 1) / 2
const RETENTION_GRID = 80

function blobOf(wash: PlannedWash): number {
  const match = BLOB.exec(wash.style['--wash-blob'])
  expect(match, wash.style['--wash-blob']).not.toBeNull()
  return Number(match![1])
}

function flipOf(wash: PlannedWash): number {
  expect(wash.style['--wash-flip'], `wash ${wash.id}`).toMatch(/^-?1$/)
  return Number(wash.style['--wash-flip'])
}

function cutByEdge(wash: PlannedWash, width: number, height: number): boolean {
  const w = capped(wash, width, height)
  const h = w / num(wash, '--wash-aspect')
  const { cx, cy, rx, ry } = BLOB_SHAPES[blobOf(wash)]
  const localX = (flipOf(wash) * cx + (1 - flipOf(wash)) / 2 - 0.5) * w
  const localY = (cy - 0.5) * h
  const turn = (num(wash, '--wash-turn') * Math.PI) / 180
  const [cos, sin] = [Math.cos(turn), Math.sin(turn)]
  const centreX = num(wash, '--wash-x') * width + localX * cos - localY * sin
  const centreY = num(wash, '--wash-y') * height + localX * sin + localY * cos
  const reachX = Math.hypot(rx * w * cos, ry * h * sin)
  const reachY = Math.hypot(rx * w * sin, ry * h * cos)
  return centreX - reachX < 0 || centreX + reachX > width || centreY - reachY < 0 || centreY + reachY > height
}

function bilinear(texels: Float64Array, u: number, v: number): number {
  const fx = u * BLOB_TEXELS - 0.5
  const fy = v * BLOB_TEXELS - 0.5
  const i = Math.max(0, Math.min(BLOB_TEXELS - 2, Math.floor(fx)))
  const j = Math.max(0, Math.min(BLOB_TEXELS - 2, Math.floor(fy)))
  const tx = Math.max(0, Math.min(1, fx - i))
  const ty = Math.max(0, Math.min(1, fy - j))
  const at = (a: number, b: number) => texels[b * BLOB_TEXELS + a]!
  const top = at(i, j) * (1 - tx) + at(i + 1, j) * tx
  const bottom = at(i, j + 1) * (1 - tx) + at(i + 1, j + 1) * tx
  return top * (1 - ty) + bottom * ty
}

// Aspect, size, turn and the mirror move the gradient and the mask together, so the content box's unit square holds
// the whole measure.
function retention(texels: Float64Array, o3: number, o4: number): number {
  const [reachX, reachY] = [Math.min(o3, 1 - o3), Math.min(o4, 1 - o4)]
  let kept = 0
  let total = 0
  for (let j = 0; j < RETENTION_GRID; j++) {
    for (let i = 0; i < RETENTION_GRID; i++) {
      const u = (i + 0.5) / RETENTION_GRID
      const v = (j + 0.5) / RETENTION_GRID
      const ink = Math.max(0, 1 - Math.hypot((u - o3) / reachX, (v - o4) / reachY))
      if (ink === 0) continue
      kept += ink * bilinear(texels, (u + BLOB_MARGIN) / WASH_SPAN, (v + BLOB_MARGIN) / WASH_SPAN)
      total += ink
    }
  }
  return kept / total
}

function quantile(values: readonly number[], p: number): number {
  const ordered = [...values].sort((a, b) => a - b)
  return ordered[Math.floor(p * (ordered.length - 1))]!
}

describe('planSheet: the mirror, the second ink and the edge cut', { timeout: 60_000 }, () => {
  it('drops the ellipse offset from WashProperty and adds the mirror', () => {
    expectTypeOf<'--wash-o1' | '--wash-o2'>().not.toExtend<WashProperty>()
    expectTypeOf<'--wash-flip'>().toExtend<WashProperty>()
  })

  it('writes --wash-flip as draw(flip) < .5 ? -1 : 1, and draws both', () => {
    const flips = new Set<string>()
    for (const seed of SEEDS) {
      for (const key of KEYS) {
        for (const size of ['card', 'reading', 'page'] as const) {
          for (const wash of plan(seed, key, size)) {
            const draw = hash(seed, key, wash.index, 'flip') / 2 ** 32
            expect(wash.style['--wash-flip'], `seed ${seed} ${key} wash ${wash.index}`).toBe(draw < 0.5 ? '-1' : '1')
            flips.add(wash.style['--wash-flip'])
          }
        }
      }
    }
    expect([...flips].sort()).toEqual(['-1', '1'])
  })

  it('writes no --wash-o1 or --wash-o2', () => {
    for (const seed of SEEDS) {
      for (const size of ['card', 'reading', 'page'] as const) {
        for (const wash of plan(seed, '/|hero', size)) {
          expect(wash.style).not.toHaveProperty('--wash-o1')
          expect(wash.style).not.toHaveProperty('--wash-o2')
        }
      }
    }
  })

  it('draws every one of the baked blobs', () => {
    expect(WASH_BLOBS).toBe(BLOB_COUNT)
    const drawn = new Set<number>()
    for (const seed of SEEDS) {
      for (const size of ['card', 'reading', 'page'] as const) {
        for (const wash of plan(seed, '/recipes|results', size)) drawn.add(blobOf(wash))
      }
    }
    expect([...drawn].sort()).toEqual(Array.from({ length: BLOB_COUNT }, (_, b) => b))
  })

  it('centres the second ink within a quarter of its blob’s radii of the blob’s ellipse centre', () => {
    for (const seed of THOUSAND_SEEDS) {
      for (const [size, , , , key] of SHEET_FIELDS) {
        for (const wash of plan(seed, key, size)) {
          const { cx, cy, rx, ry } = BLOB_SHAPES[blobOf(wash)]
          const where = `seed ${seed} ${key} wash ${wash.index}`
          expect(Math.abs(num(wash, '--wash-o3') - cx), where).toBeLessThanOrEqual(0.25 * rx + 0.0005)
          expect(Math.abs(num(wash, '--wash-o4') - cy), where).toBeLessThanOrEqual(0.25 * ry + 0.0005)
        }
      }
    }
  })

  it('places the second ink by the existing o3 and o4 draws', () => {
    for (const seed of SEEDS) {
      for (const [size, , , , key] of SHEET_FIELDS) {
        for (const wash of plan(seed, key, size)) {
          const { cx, cy, rx, ry } = BLOB_SHAPES[blobOf(wash)]
          const u = hash(seed, key, wash.index, 'o3') / 2 ** 32
          const v = hash(seed, key, wash.index, 'o4') / 2 ** 32
          expect(wash.style['--wash-o3']).toBe(toFixed3(cx + 0.25 * rx * (2 * u - 1)))
          expect(wash.style['--wash-o4']).toBe(toFixed3(cy + 0.25 * ry * (2 * v - 1)))
        }
      }
    }
  })

  it.each(SHEET_FIELDS)(
    'cuts at least three quarters of the %s washes by an edge of %s, over 1,000 seeds',
    (size, _, width, height, key) => {
      let drawn = 0
      let cut = 0
      for (const seed of THOUSAND_SEEDS) {
        for (const wash of plan(seed, key, size)) {
          if (capped(wash, width, height) === 0) continue
          drawn++
          if (cutByEdge(wash, width, height)) cut++
        }
      }
      expect(drawn).toBeGreaterThan(900)
      expect(cut / drawn).toBeGreaterThanOrEqual(0.75)
    },
  )

  it('seldom repeats a blob at the same place on neighbouring sheets, over 1,000 seeds', () => {
    let compared = 0
    let repeats = 0
    for (const seed of THOUSAND_SEEDS) {
      for (const [size, , , , , prefix] of SHEET_FIELDS) {
        if (prefix === undefined) continue
        const sheets = Array.from({ length: 6 }, (_, i) => plan(seed, `${prefix}${i}`, size))
        for (let i = 1; i < sheets.length; i++) {
          for (const a of sheets[i - 1]) {
            for (const b of sheets[i]) {
              compared++
              const same =
                blobOf(a) === blobOf(b) &&
                flipOf(a) === flipOf(b) &&
                Math.abs(num(a, '--wash-x') - num(b, '--wash-x')) <= 0.1 &&
                Math.abs(num(a, '--wash-y') - num(b, '--wash-y')) <= 0.1
              if (same) repeats++
            }
          }
        }
      }
    }
    expect(compared).toBeGreaterThan(10_000)
    expect(repeats / compared).toBeLessThanOrEqual(0.05)
  })

  it('keeps the second ink under its blob over the displaced bake: median at least .80, 10th percentile at least .70', () => {
    const bakes = Array.from({ length: BLOB_COUNT }, (_, b) => bakeBlob(b))
    const kept: number[] = []
    for (const seed of THOUSAND_SEEDS) {
      for (const [size, , , , key] of SHEET_FIELDS) {
        for (const wash of plan(seed, key, size)) {
          kept.push(retention(bakes[blobOf(wash)], num(wash, '--wash-o3'), num(wash, '--wash-o4')))
        }
      }
    }
    expect(quantile(kept, 0.5)).toBeGreaterThanOrEqual(0.8)
    expect(quantile(kept, 0.1)).toBeGreaterThanOrEqual(0.7)
  })
})

const TODAY_SEEDS = [3141592653, 271828182, 1618033988] as const

const TODAY_PLANS = [
  {
    seed: 3141592653,
    key: '/recipes|card:soup',
    size: 'card',
    ids: ['886671c7', '172d7917'],
    starts: ['38.544s', '159.078s'],
  },
  {
    seed: 3141592653,
    key: '/recipes|card:stew',
    size: 'card',
    ids: ['c11565d5', '122471a0'],
    starts: ['62.587s', '163.415s'],
  },
  {
    seed: 3141592653,
    key: '/recipes/soup|review:Ann',
    size: 'reading',
    ids: ['101489a2', '1438513c', 'a6de7681'],
    starts: ['24.497s', '76.572s', '154.867s'],
  },
  {
    seed: 3141592653,
    key: '/|hero',
    size: 'page',
    ids: ['1302f2ea', '2ef615c9', '593762d0', '85e731d0'],
    starts: ['41.433s', '113.132s', '162.499s', '203.075s'],
  },
  { seed: 271828182, key: '/recipes|card:soup', size: 'card', ids: ['886671c7'], starts: ['114.658s'] },
  { seed: 271828182, key: '/recipes|card:stew', size: 'card', ids: ['c11565d5'], starts: ['155.938s'] },
  {
    seed: 271828182,
    key: '/recipes/soup|review:Ann',
    size: 'reading',
    ids: ['101489a2', '1438513c', 'a6de7681'],
    starts: ['16.733s', '87.376s', '153.774s'],
  },
  {
    seed: 271828182,
    key: '/|hero',
    size: 'page',
    ids: ['1302f2ea', '2ef615c9', '593762d0', '85e731d0'],
    starts: ['58.884s', '100.828s', '142.088s', '201.273s'],
  },
  {
    seed: 1618033988,
    key: '/recipes|card:soup',
    size: 'card',
    ids: ['886671c7', '172d7917'],
    starts: ['73.058s', '170.081s'],
  },
  {
    seed: 1618033988,
    key: '/recipes|card:stew',
    size: 'card',
    ids: ['c11565d5', '122471a0'],
    starts: ['99.387s', '188.003s'],
  },
  {
    seed: 1618033988,
    key: '/recipes/soup|review:Ann',
    size: 'reading',
    ids: ['101489a2', '1438513c', 'a6de7681'],
    starts: ['28.217s', '86.927s', '168.520s'],
  },
  {
    seed: 1618033988,
    key: '/|hero',
    size: 'page',
    ids: ['1302f2ea', '2ef615c9', '593762d0', '85e731d0'],
    starts: ['49.595s', '104.704s', '142.614s', '204.233s'],
  },
] as const

const TODAY_VISITS = [
  { seed: 3141592653, key: '/recipes|card:soup', count: 2, starts: [97.807, 97.807] },
  { seed: 3141592653, key: '/recipes|card:stew', count: 2, starts: [5.114, 70.944] },
  { seed: 3141592653, key: '/recipes/soup|review:Ann', count: 3, starts: [19.983, 32.607, 165.434] },
  { seed: 3141592653, key: '/|hero', count: 4, starts: [5.114, 5.114, 123.615, 138.387] },
  { seed: 3141592653, key: '@pad', count: 4, slots: [1, 5, 9, 13], starts: [19.983, 70.944, 123.615, 177.474] },
  { seed: 271828182, key: '/recipes|card:soup', count: 1, starts: [96.679] },
  { seed: 271828182, key: '/recipes|card:stew', count: 1, starts: [71.292] },
  { seed: 271828182, key: '/recipes/soup|review:Ann', count: 3, starts: [21.303, 34.668, 163.881] },
  { seed: 271828182, key: '/|hero', count: 4, starts: [6.109, 6.109, 123.368, 137.453] },
  { seed: 271828182, key: '@pad', count: 4, slots: [1, 5, 9, 13], starts: [21.303, 71.292, 123.368, 175.833] },
  { seed: 1618033988, key: '/recipes|card:soup', count: 2, starts: [97.568, 97.568] },
  { seed: 1618033988, key: '/recipes|card:stew', count: 2, starts: [7.255, 73.829] },
  { seed: 1618033988, key: '/recipes/soup|review:Ann', count: 3, starts: [19.941, 34.775, 163.927] },
  { seed: 1618033988, key: '/|hero', count: 4, starts: [7.255, 7.255, 123.204, 136.206] },
  { seed: 1618033988, key: '@pad', count: 4, slots: [1, 5, 9, 13], starts: [19.941, 73.829, 123.204, 176.041] },
] as const

const TODAY_SCHEDULES = [
  {
    seed: 3141592653,
    starts: [
      { id: '101489a2', at: 14.45 },
      { id: 'ab2e2070', at: 19.983 },
      { id: 'c11565d5', at: 26.153 },
      { id: '1438513c', at: 37.856 },
      { id: '647d796e', at: 70.944 },
      { id: '1302f2ea', at: 75.728 },
      { id: '122471a0', at: 94.663 },
      { id: 'a6de7681', at: 106.366 },
      { id: '2ef615c9', at: 118.069 },
      { id: 'eccda58a', at: 123.615 },
      { id: '593762d0', at: 144.238 },
      { id: '886671c7', at: 155.94 },
      { id: '172d7917', at: 174.876 },
      { id: '8a6af31e', at: 177.474 },
      { id: '85e731d0', at: 186.579 },
    ],
  },
  {
    seed: 271828182,
    starts: [
      { id: '101489a2', at: 3.089 },
      { id: '1302f2ea', at: 14.791 },
      { id: 'ab2e2070', at: 21.303 },
      { id: '886671c7', at: 52.663 },
      { id: '2ef615c9', at: 64.366 },
      { id: '647d796e', at: 71.292 },
      { id: '1438513c', at: 113.94 },
      { id: 'eccda58a', at: 123.368 },
      { id: '593762d0', at: 132.876 },
      { id: '85e731d0', at: 144.579 },
      { id: '8a6af31e', at: 175.833 },
      { id: 'a6de7681', at: 182.45 },
      { id: 'c11565d5', at: 194.153 },
    ],
  },
  {
    seed: 1618033988,
    starts: [
      { id: 'c11565d5', at: 0.433 },
      { id: '101489a2', at: 12.135 },
      { id: 'ab2e2070', at: 19.941 },
      { id: '1302f2ea', at: 31.071 },
      { id: '886671c7', at: 42.774 },
      { id: '2ef615c9', at: 61.71 },
      { id: '647d796e', at: 73.829 },
      { id: '172d7917', at: 80.645 },
      { id: '1438513c', at: 92.348 },
      { id: 'eccda58a', at: 123.204 },
      { id: '593762d0', at: 130.22 },
      { id: '122471a0', at: 141.923 },
      { id: 'a6de7681', at: 160.858 },
      { id: '85e731d0', at: 172.561 },
      { id: '8a6af31e', at: 176.041 },
    ],
  },
] as const

function collectedFrom(seed: number): CollectedWash[] {
  const washes = TODAY_PLANS.filter((p) => p.seed === seed).flatMap((p) =>
    planSheet({ seed, key: p.key, size: p.size }).map((w) => ({ id: w.id, key: w.key, index: w.index })),
  )
  const pad = [1, 5, 9, 13].map((slot, k) => ({ id: washId('@pad', k), key: '@pad', index: k, slot }))
  return [...washes, ...pad]
}

describe('planSheet: edge-placed washes', () => {
  const axisKeys = ['/recipes|card:soup', '/recipes/soup|review:Ann', '/|hero', '@pad']
  const inBand = (far: boolean, v: number) => (far ? v >= 0.84 && v <= 1.02 : v >= -0.02 && v <= 0.16)
  const inHalf = (far: boolean, v: number) => (far ? v >= 0.55 && v <= 0.9 : v >= 0.1 && v <= 0.45)

  it('pushes exactly one axis into its band and keeps the other in its half', () => {
    const pushed = new Set<number>()
    for (const seed of SEEDS) {
      for (const key of axisKeys) {
        for (const size of ['card', 'reading', 'page'] as const) {
          plan(seed, key, size).forEach((wash) => {
            const x = num(wash, '--wash-x')
            const y = num(wash, '--wash-y')
            const pushX = hash(seed, key, wash.index, 'axis') & 1
            pushed.add(pushX)
            const farX = x > 0.5
            const farY = y > 0.5
            const where = `seed ${seed} ${key} ${size} wash ${wash.index} at ${x}, ${y}`
            if (pushX) {
              expect(inBand(farX, x), where).toBe(true)
              expect(inHalf(farY, y), where).toBe(true)
            } else {
              expect(inHalf(farX, x), where).toBe(true)
              expect(inBand(farY, y), where).toBe(true)
            }
          })
        }
      }
    }
    expect([...pushed].sort()).toEqual([0, 1])
  })

  it('puts a one-wash card in the quarter hash(seed, key, corner) & 3, never the middle', () => {
    const corners = new Set<number>()
    let lone = 0
    for (const seed of THOUSAND_SEEDS) {
      for (const key of ['/recipes|card:soup', '/recipes|card:stew', '@pad-card-recipes']) {
        const washes = plan(seed, key, 'card')
        if (washes.length !== 1) continue
        lone++
        const corner = hash(seed, key, 'corner') & 3
        expect(quadrant(washes[0]), `seed ${seed} ${key}`).toBe(corner)
        corners.add(corner)
      }
    }
    expect(lone).toBeGreaterThan(100)
    expect([...corners].sort()).toEqual([0, 1, 2, 3])
  })

  it('draws --wash-d from 0.90 to 1.25', () => {
    const ds: number[] = []
    for (const seed of SEEDS) {
      for (const size of ['card', 'reading', 'page'] as const)
        ds.push(...plan(seed, '/|hero', size).map((w) => num(w, '--wash-d')))
    }
    expect(Math.min(...ds)).toBeGreaterThanOrEqual(0.9)
    expect(Math.max(...ds)).toBeLessThanOrEqual(1.25)
    expect(Math.min(...ds)).toBeLessThan(1)
    expect(Math.max(...ds)).toBeGreaterThan(1.15)
  })
})

describe('wash ids and starts: the values recorded before the bleed work', () => {
  it('keeps the planner ids and fallback starts on card, reading and hero fields', () => {
    for (const p of TODAY_PLANS) {
      const washes = planSheet({ seed: p.seed, key: p.key, size: p.size })
      expect(
        washes.map((w) => w.id),
        `${p.seed} ${p.key}`,
      ).toEqual(p.ids)
      expect(
        washes.map((w) => w.style['--wash-fallback-at']),
        `${p.seed} ${p.key}`,
      ).toEqual(p.starts)
    }
  })

  it('keeps the visit starts', () => {
    for (const v of TODAY_VISITS) {
      const slots = 'slots' in v ? v.slots : undefined
      expect(visitStarts(v.seed, v.key, v.count, slots), `${v.seed} ${v.key}`).toEqual(v.starts)
    }
  })

  it('keeps the schedule', () => {
    for (const schedule of TODAY_SCHEDULES) {
      expect(TODAY_SEEDS).toContain(schedule.seed)
      expect(scheduleStarts(schedule.seed, collectedFrom(schedule.seed))).toEqual(schedule.starts)
    }
  })
})

const RULE = new RegExp(`\\.kcc-wash\\[data-wash="([0-9a-f]{8})"\\]\\{--wash-at:(${FIXED})s\\}`, 'y')
const VISIT_LATTICE = 16
const PHI = (1 + Math.sqrt(5)) / 2
const GOLDEN_STEP = 0x9e3779b9
const ROUNDING = 0.001
const PAD_SLOTS = [1, 5, 9, 13]

function washId(key: string, k: number): string {
  return hash(key, k).toString(16).padStart(8, '0')
}

function sheetOf(key: string, count: number, slots?: readonly number[]): CollectedWash[] {
  return Array.from({ length: count }, (_, k) => ({ id: washId(key, k), key, index: k, slot: slots?.[k] }))
}

function pageOf(page: string, washes: number): CollectedWash[] {
  const sheets: CollectedWash[] = []
  for (let i = 0; sheets.length < washes; i++) {
    sheets.push(...sheetOf(`${page}|sheet-${i}`, Math.min(1 + (i % 4), washes - sheets.length)))
  }
  return sheets
}

function parseSchedule(css: string): Map<string, number> {
  const starts = new Map<string, number>()
  RULE.lastIndex = 0
  while (RULE.lastIndex < css.length) {
    const at = RULE.lastIndex
    const match = RULE.exec(css)
    expect(match, `rule at ${at} of ${css}`).not.toBeNull()
    expect(starts.has(match![1])).toBe(false)
    starts.set(match![1], Number.parseFloat(match![2]))
  }
  return starts
}

function startsOf(seed: number, washes: readonly CollectedWash[]): Map<string, number> {
  const starts = parseSchedule(scheduleWashes(seed, washes))
  expect(starts.size).toBe(washes.length)
  return starts
}

function blockRadius(washes: number): number {
  let beats = 64
  while (beats < 2 * washes + 32 && beats < 512) beats *= 2
  return Math.floor((0.75 * WASH_WINDOW_S * 1000) / beats) / 1000
}

function visitBeatsOf(seed: number): number[] {
  const lattice = Array.from({ length: VISIT_LATTICE }, (_, j) => sheetOf(`@lattice-${j}`, 1, [j])).flat()
  const starts = startsOf(seed, lattice)
  return lattice.map((w) => starts.get(w.id)!)
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

function shuffled<T>(items: readonly T[], seed: number): T[] {
  const copy = [...items]
  for (let i = copy.length - 1; i > 0; i--) {
    const j = hash(seed, 'shuffle', i) % (i + 1)
    ;[copy[i], copy[j]] = [copy[j], copy[i]]
  }
  return copy
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

const PAGE_SIZES = [1, 2, 7, 16, 17, 50, 120, 240, 300] as const

describe('scheduleWashes: the even spread', { timeout: 30_000 }, () => {
  it('writes nothing for a page with no washes', () => {
    expect(scheduleWashes(1, [])).toBe('')
  })

  it('writes one `.kcc-wash[data-wash="<id>"]{--wash-at:<t>s}` rule per wash, keyed by the wash id', () => {
    const washes = pageOf('/recipes', 12)
    const starts = parseSchedule(scheduleWashes(7, washes))
    expect([...starts.keys()].sort()).toEqual(washes.map((w) => w.id).sort())
    const planned = planSheet({ seed: 7, key: '/recipes|hero', size: 'page' })
    expect([...parseSchedule(scheduleWashes(7, planned)).keys()].sort()).toEqual(planned.map((w) => w.id).sort())
  })

  it.each(PAGE_SIZES)('starts every wash of a %i-wash page inside [0, 209.999]', (n) => {
    for (const seed of SEEDS) {
      for (const at of startsOf(seed, pageOf('/p', n)).values()) {
        expect(at).toBeGreaterThanOrEqual(0)
        expect(at).toBeLessThanOrEqual(209.999)
      }
    }
  })

  it.each(PAGE_SIZES)(
    'gives a %i-wash page the first n golden-ratio candidates frac(c + j / φ) × 210s clear of the visit beats',
    (n) => {
      for (const seed of SEEDS.slice(0, 50)) {
        const washes = [...pageOf('/p', n), ...sheetOf('@pad', 4, PAD_SLOTS)]
        const page = startsOf(seed, washes)
        const expected = candidates(seed, '/p', n, n)
        expect(sorted(washes.filter((w) => w.key !== '@pad').map((w) => page.get(w.id)!))).toEqual(sorted(expected))
      }
    },
  )

  it('gives the first candidate to the wash of highest hash priority hash(seed, key, k)', () => {
    for (const seed of SEEDS.slice(0, 50)) {
      const washes = Array.from({ length: 30 }, (_, i) => sheetOf(`/p|card-${i}`, 1)).flat()
      const first = [...washes].sort((a, b) => hash(seed, a.key, a.index) - hash(seed, b.key, b.index))[0]
      expect(startsOf(seed, washes).get(first.id)).toBe(candidates(seed, '/p', 30, 1)[0])
    }
  })

  it('takes the phase c from the visit seed and the page, so two pages of one visit differ', () => {
    const recipes = sorted(startsOf(9, pageOf('/recipes', 12)).values())
    const kitchen = sorted(startsOf(9, pageOf('/kitchen', 12)).values())
    expect(recipes).not.toEqual(kitchen)
  })

  // Three-distance theorem: the first M points of frac(c + j / φ) cut the circle into gaps of at most three lengths;
  // the longest is under φ³ / √5 ≈ 1.894 of 1 / M, the shortest over 1 / (√5 M). A skipped candidate lies within the
  // block radius r of a visit beat, so a quiet stretch is at most two gaps of the full candidate set around one
  // blocked interval: 2 × 1.894 × 210 / n + 2r. The 210s window cuts the circle's wrap gap in two, which only shortens it.
  const quietBound = (n: number, radius: number) => (2 * PHI ** 3 * WASH_WINDOW_S) / (Math.sqrt(5) * n) + 2 * radius
  const gapBound = (n: number, radius: number) => Math.min(radius, WASH_WINDOW_S / (Math.sqrt(5) * 2 * n)) - ROUNDING

  it.each([6, 12, 50, 200])(
    'leaves no quiet stretch longer than 2 × 1.894 × 210 / n + 2r on a %i-wash page with the pad, over 1,000 seeds',
    (n) => {
      const washes = [...pageOf('/p', n), ...sheetOf('@pad', 4, PAD_SLOTS)]
      const bound = quietBound(n, blockRadius(n))
      for (const seed of THOUSAND_SEEDS) {
        expect(longestQuiet(startsOf(seed, washes).values()), `seed ${seed}`).toBeLessThanOrEqual(bound + ROUNDING)
      }
    },
  )

  it.each([2, 6, 12, 50, 200])(
    'keeps every two starts of a %i-wash page with the pad at least min(r, 210 / (2√5 n)) apart, over 1,000 seeds',
    (n) => {
      const washes = [...pageOf('/p', n), ...sheetOf('@pad', 4, PAD_SLOTS)]
      const bound = gapBound(n, blockRadius(n))
      for (const seed of THOUSAND_SEEDS) {
        expect(closest(startsOf(seed, washes).values()), `seed ${seed}`).toBeGreaterThanOrEqual(bound)
      }
    },
  )

  it('never puts two washes of a 2,000-wash page on the same instant', () => {
    const washes = [...pageOf('/p', 2000), ...sheetOf('@pad', 4, PAD_SLOTS)]
    for (const seed of SEEDS.slice(0, 10)) {
      const starts = startsOf(seed, washes)
      expect(new Set(starts.values()).size).toBe(washes.length)
      expect(closest(starts.values()), `seed ${seed}`).toBeGreaterThanOrEqual(gapBound(2000, blockRadius(2000)))
    }
  })

  it.each(PAGE_SIZES)('never starts a page wash within the block radius of a visit beat on a %i-wash page', (n) => {
    const radius = blockRadius(n)
    for (const seed of SEEDS) {
      const visit = visitBeatsOf(seed)
      const starts = [...startsOf(seed, pageOf('/p', n)).values()]
      const nearest = Math.min(...starts.flatMap((at) => visit.map((g) => Math.abs(at - g))))
      expect(nearest, `seed ${seed}`).toBeGreaterThan(radius)
    }
  })

  it('gives shuffled input identical CSS', () => {
    for (const seed of SEEDS) {
      const washes = [...pageOf('/p', 50), ...sheetOf('@pad', 4, PAD_SLOTS)]
      expect(scheduleWashes(seed, shuffled(washes, seed))).toBe(scheduleWashes(seed, washes))
    }
  })
})

describe('scheduleWashes: global washes and the sheet order', () => {
  it('gives a global key its wash-slot beats on the visit lattice, at the same times on two pages', () => {
    const pad = sheetOf('@pad', 4, [3, 9, 12, 0])
    const card = sheetOf('@pad-card-recipes', 2, [6, 14])
    for (const seed of SEEDS) {
      const visit = visitBeatsOf(seed)
      for (let j = 0; j < VISIT_LATTICE; j++) {
        expect(visit[j]).toBeGreaterThanOrEqual(((j + 0.35) * WASH_WINDOW_S) / VISIT_LATTICE - 0.001)
        expect(visit[j]).toBeLessThanOrEqual(((j + 0.65) * WASH_WINDOW_S) / VISIT_LATTICE + 0.001)
      }
      const home = startsOf(seed, [...pageOf('/', 20), ...pad, ...card])
      const recipe = startsOf(seed, [...pageOf('/recipes/soup', 50), ...card, ...pad])
      for (const wash of [...pad, ...card]) expect(recipe.get(wash.id)).toBe(home.get(wash.id))
      expect(sorted(pad.map((w) => home.get(w.id)!))).toEqual(sorted([3, 9, 12, 0].map((j) => visit[j])))
      expect(sorted(card.map((w) => home.get(w.id)!))).toEqual(sorted([6, 14].map((j) => visit[j])))
    }
  })

  it('starts wash 0 of every sheet first', () => {
    for (const seed of SEEDS) {
      const washes = [...pageOf('/p', 50), ...sheetOf('@pad', 4, [3, 9, 12, 0])]
      const starts = startsOf(seed, washes)
      const sheets = new Map<string, number[]>()
      for (const w of washes) sheets.set(w.key, [...(sheets.get(w.key) ?? []), starts.get(w.id)!])
      for (const [key, times] of sheets) {
        expect(times[0], `seed ${seed} ${key}`).toBe(Math.min(...times))
      }
    }
  })
})
