import { WASHES, type Wash } from '~/Types/DesignSystem'

export type WashSize = 'card' | 'reading' | 'page' | 'none'

export type WashProperty =
  | '--wash-x'
  | '--wash-y'
  | '--wash-dx'
  | '--wash-dy'
  | '--wash-d'
  | '--wash-aspect'
  | '--wash-s'
  | '--wash-turn'
  | '--wash-blob'
  | '--wash-o1'
  | '--wash-o2'
  | '--wash-o3'
  | '--wash-o4'
  | '--wash-ink'
  | '--wash-ink-2'
  | '--wash-fallback-at'
  | '--wash-peak'

export interface PlannedWash {
  id: string
  key: string
  index: number
  style: Record<WashProperty, string>
}

export interface SheetBloom {
  seed: number
  key: string
  size: WashSize
  chosen?: Wash
  starts?: readonly number[]
}

export interface SheetSizeProps {
  bloom?: WashSize
  tear?: number | 'hero'
  margin?: boolean
  wide?: boolean
}

export const WASH_WINDOW_S = 210

export const WASH_REACH_PX = 24

export const WASH_BLOBS = 8

export const LATEST_START_S = 209.999
const FALLBACK_JITTER = 0.25
const PAIRS: Record<number, readonly [number, number][]> = {
  3: [[0, 2]],
  4: [
    [0, 2],
    [1, 3],
  ],
}

export function hash(...parts: ReadonlyArray<string | number>): number {
  const text = parts.map(String).join('\u001f')
  let h = 0x811c9dc5
  for (let i = 0; i < text.length; i++) {
    h ^= text.charCodeAt(i)
    h = Math.imul(h, 0x01000193)
  }
  h ^= h >>> 16
  h = Math.imul(h, 0x85ebca6b)
  h ^= h >>> 13
  h = Math.imul(h, 0xc2b2ae35)
  h ^= h >>> 16
  return h >>> 0
}

export function toFixed3(value: number): string {
  const fixed = value.toFixed(3)
  return (fixed === '-0.000' ? '0.000' : fixed).replace(/^(-?)0\./, '$1.')
}

export function sheetSize(props: SheetSizeProps): WashSize {
  if (props.bloom) return props.bloom
  if (props.tear === 'hero') return 'page'
  if (props.margin) return 'reading'
  if (props.wide) return 'page'
  return 'card'
}

function unit(...parts: ReadonlyArray<string | number>): number {
  return hash(...parts) / 2 ** 32
}

function between(low: number, high: number, u: number): number {
  return low + (high - low) * u
}

function rounded(value: number): number {
  return Number(value.toFixed(3))
}

function washCount(sheet: SheetBloom): number {
  switch (sheet.size) {
    case 'page':
      return 4
    case 'reading':
      return 3
    case 'card':
      return 1 + (hash(sheet.seed, sheet.key, 'count') & 1)
    default:
      return 0
  }
}

function quadrantsFor(sheet: SheetBloom, count: number): number[] {
  if (count === 2) {
    const first = hash(sheet.seed, sheet.key, 'diagonal') & 1 ? 1 : 0
    const second = 3 - first
    return hash(sheet.seed, sheet.key, 'diagonal-order') & 1 ? [second, first] : [first, second]
  }
  const first = hash(sheet.seed, sheet.key, 'rotation') & 3
  const second = first ^ (hash(sheet.seed, sheet.key, 'second') & 1 ? 1 : 2)
  return [first, second, 3 - first, 3 - second].slice(0, count)
}

function centre(sheet: SheetBloom, count: number, k: number, quadrants: readonly number[]): [number, number] {
  const u = unit(sheet.seed, sheet.key, k, 'x')
  const v = unit(sheet.seed, sheet.key, k, 'y')
  if (count === 1) return [rounded(between(0.25, 0.75, u)), rounded(between(0.25, 0.75, v))]
  const quadrant = quadrants[k]
  const x = (quadrant & 1 ? 0.55 : 0.05) + 0.4 * u
  const y = (quadrant & 2 ? 0.55 : 0.05) + 0.4 * v
  return [rounded(x), rounded(y)]
}

function inkSlots(sheet: SheetBloom): Wash[] {
  const slots = [...WASHES]
  for (let i = slots.length - 1; i > 0; i--) {
    const j = hash(sheet.seed, sheet.key, 'ink', i) % (i + 1)
    ;[slots[i], slots[j]] = [slots[j], slots[i]]
  }
  if (sheet.chosen) {
    const at = slots.indexOf(sheet.chosen)
    ;[slots[0], slots[at]] = [slots[at], slots[0]]
  }
  return slots
}

function fallbackStarts(sheet: SheetBloom, count: number): number[] {
  if (sheet.starts && sheet.starts.length >= count) {
    const starts = sheet.starts.slice(0, count).map((t) => rounded(t))
    const earliest = starts.indexOf(Math.min(...starts))
    ;[starts[0], starts[earliest]] = [starts[earliest], starts[0]]
    return starts
  }
  const p = unit(sheet.key)
  return Array.from({ length: count }, (_, k) => {
    const jitter = between(-FALLBACK_JITTER, FALLBACK_JITTER, unit(sheet.seed, sheet.key, k, 'start'))
    const at = Math.round(((k + p + jitter) / count) * WASH_WINDOW_S * 1000) / 1000
    return Math.min(LATEST_START_S, Math.max(0, at))
  })
}

export function planSheet(sheet: SheetBloom): PlannedWash[] {
  const count = washCount(sheet)
  if (count === 0) return []

  const quadrants = quadrantsFor(sheet, count)
  const centres = Array.from({ length: count }, (_, k) => centre(sheet, count, k, quadrants))
  const partners = new Map<number, number>()
  for (const [a, b] of PAIRS[count] ?? []) {
    partners.set(a, b)
    partners.set(b, a)
  }
  const slots = inkSlots(sheet)
  const starts = fallbackStarts(sheet, count)

  return centres.map(([x, y], k) => {
    const draw = (label: string) => unit(sheet.seed, sheet.key, k, label)
    const aspect = rounded(between(0.68, 1.32, draw('aspect')))
    const partner = partners.get(k)
    const dx = partner === undefined ? '9' : toFixed3(centres[partner][0] - x)
    const dy = partner === undefined ? '9' : toFixed3(centres[partner][1] - y)

    return {
      id: hash(sheet.key, k).toString(16).padStart(8, '0'),
      key: sheet.key,
      index: k,
      style: {
        '--wash-x': toFixed3(x),
        '--wash-y': toFixed3(y),
        '--wash-dx': dx,
        '--wash-dy': dy,
        '--wash-d': toFixed3(between(0.45, 1.25, draw('d'))),
        '--wash-aspect': toFixed3(aspect),
        '--wash-s': toFixed3(Math.sqrt(1 + 1 / aspect ** 2)),
        '--wash-turn': `${toFixed3(between(0, 359.99, draw('turn')))}deg`,
        '--wash-blob': `var(--wash-blob-${Math.floor(draw('blob') * WASH_BLOBS)})`,
        '--wash-o1': toFixed3(between(0.28, 0.72, draw('o1'))),
        '--wash-o2': toFixed3(between(0.28, 0.72, draw('o2'))),
        '--wash-o3': toFixed3(between(0.28, 0.72, draw('o3'))),
        '--wash-o4': toFixed3(between(0.28, 0.72, draw('o4'))),
        '--wash-ink': `var(--color-${slots[k]})`,
        '--wash-ink-2': `var(--color-${slots[k + 4]})`,
        '--wash-fallback-at': `${toFixed3(starts[k])}s`,
        '--wash-peak': toFixed3(between(0.75, 1, draw('peak'))),
      },
    }
  })
}

export interface CollectedWash {
  id: string
  key: string
  index: number
  slot?: number
}

export interface ScheduledStart {
  id: string
  at: number
}

const VISIT_BEATS = 16
const BEAT_JITTER = 0.3
const BLOCK_RADIUS_BEATS = 0.75
const WINDOW_MS = WASH_WINDOW_S * 1000
const GOLDEN_STEP = 0x9e3779b9

export function isGlobalKey(key: string): boolean {
  return key.startsWith('@')
}

export function pageOfKey(key: string): string {
  return key.slice(0, key.indexOf('|'))
}

function blockRadiusMs(pageWashes: number): number {
  let beats = 64
  while (beats < 2 * pageWashes + 32 && beats < 512) beats *= 2
  return Math.floor((BLOCK_RADIUS_BEATS * WINDOW_MS) / beats)
}

function visitBeatsMs(seed: number): number[] {
  return Array.from({ length: VISIT_BEATS }, (_, j) =>
    Math.round(((j + 0.5 + BEAT_JITTER * (unit(seed, 'visit-beat', j) - 0.5)) / VISIT_BEATS) * WINDOW_MS),
  )
}

export function arrivals(seed: number, page: string, pageWashes: number): () => number {
  const visit = visitBeatsMs(seed)
  const radius = blockRadiusMs(pageWashes)
  const phase = hash(seed, page)
  let j = 0
  return () => {
    // Ends: the odd step reaches every 32-bit position, and the blocked arcs cover at most 3/8 of the window.
    for (;;) {
      const at = Math.floor((((phase + Math.imul(j++, GOLDEN_STEP)) >>> 0) * WINDOW_MS) / 2 ** 32)
      if (!visit.some((g) => Math.abs(at - g) <= radius)) return at / 1000
    }
  }
}

function priorityOrder(seed: number) {
  return (a: CollectedWash, b: CollectedWash): number =>
    hash(seed, a.key, a.index) - hash(seed, b.key, b.index) || (a.key < b.key ? -1 : a.key > b.key ? 1 : a.index - b.index)
}

function beatOf(visit: readonly number[], key: string, index: number, slot: number | undefined): number {
  const wanted = slot ?? hash(key, index)
  return visit[((wanted % VISIT_BEATS) + VISIT_BEATS) % VISIT_BEATS] / 1000
}

export function visitStarts(seed: number, key: string, count: number, slots?: readonly number[]): number[] {
  const visit = visitBeatsMs(seed)
  return Array.from({ length: count }, (_, index) => beatOf(visit, key, index, slots?.[index])).sort((a, b) => a - b)
}

export function scheduleStarts(seed: number, washes: readonly CollectedWash[]): ScheduledStart[] {
  const claims = new Map<CollectedWash, number>()
  const ranked = washes.filter((w) => !isGlobalKey(w.key)).sort(priorityOrder(seed))
  if (ranked.length > 0) {
    const next = arrivals(seed, pageOfKey(ranked[0].key), ranked.length)
    for (const wash of ranked) claims.set(wash, next())
  }
  const visit = visitBeatsMs(seed)
  for (const wash of washes) {
    if (!isGlobalKey(wash.key)) continue
    claims.set(wash, beatOf(visit, wash.key, wash.index, wash.slot))
  }

  const sheets = new Map<string, CollectedWash[]>()
  for (const wash of claims.keys()) sheets.set(wash.key, [...(sheets.get(wash.key) ?? []), wash])

  const starts: ScheduledStart[] = []
  for (const sheet of sheets.values()) {
    const times = sheet.map((w) => claims.get(w)!).sort((a, b) => a - b)
    sheet.sort((a, b) => a.index - b.index).forEach((wash, i) => starts.push({ id: wash.id, at: times[i] }))
  }
  return starts.sort((a, b) => a.at - b.at || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0))
}

export function scheduleWashes(seed: number, washes: readonly CollectedWash[]): string {
  return scheduleStarts(seed, washes)
    .map(({ id, at }) => `.kcc-wash[data-wash="${id}"]{--wash-at:${toFixed3(at)}s}`)
    .join('')
}
