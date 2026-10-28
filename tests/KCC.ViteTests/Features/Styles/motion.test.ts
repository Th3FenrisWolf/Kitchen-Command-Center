import { readFileSync } from 'node:fs'
import { relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { WASH_WINDOW_S } from '~/Bloom/washes'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const read = (path: string) => readFileSync(`${WEB}${path}`, 'utf8')

const CEILING_MS = 500

const UTILITY = /(?<![\w-])duration-(?:(\d+)(?![\w-])|\[(\d*\.?\d+)(ms|s)\])/g
const DECLARATION = /(?<![\w-])(?:transition|animation)(?:-duration)?\s*:\s*([^;{}"]+)/g
const TIME = /(\d*\.?\d+)(ms|s)\b/g

const toMs = (amount: string, unit: string) => Number(amount) * (unit === 's' ? 1000 : 1)

const tokens = new Map<string, string>()
for (const [, name, value] of read('Features/Styles/Torn/Tokens.css').matchAll(/(--[\w-]+)\s*:\s*([^;{}]+);/g)) {
  if (!tokens.has(name!)) tokens.set(name!, value!.trim())
}
const withTokens = (value: string) => value.replace(/var\((--[\w-]+)\)/g, (whole, name: string) => tokens.get(name) ?? whole)

const BLOOM = /^kcc-bloom\s+var\(--wash-bloom\)/

const BLOOM_CURVE = { x1: 0.17, y1: 0.72, x2: 0.24, y2: 1 }
const BLOOM_STEPS = 45

const bezier = (a: number, b: number, s: number) => 3 * a * s * (1 - s) ** 2 + 3 * b * s ** 2 * (1 - s) + s ** 3

const timeAt = (progress: number) => {
  let low = 0
  let high = 1
  for (let i = 0; i < 60; i++) {
    const mid = (low + high) / 2
    if (bezier(BLOOM_CURVE.y1, BLOOM_CURVE.y2, mid) < progress) low = mid
    else high = mid
  }
  return bezier(BLOOM_CURVE.x1, BLOOM_CURVE.x2, (low + high) / 2)
}

type Stop = { progress: number; at: number }

// Each step rises the same height, so the steps crowd the start, where the curve climbs fastest, and the start reads
// smooth. A step lands where the curve crosses its midpoint, so the stair never strays half a step from the curve,
// except the first, which lands a hair after the start so a wash shows as it arrives: at 0% itself it would also
// show while the wash waits to arrive.
const staircase = (steps: number): Stop[] => {
  const stops: Stop[] = [{ progress: 0, at: 0 }]
  for (let step = 1; step <= steps; step++) {
    const at = step === 1 ? 0.01 : Number((timeAt((step - 0.5) / steps) * 100).toFixed(2))
    stops.push({ progress: Number(((step - 1) / steps).toFixed(4)), at })
    stops.push({ progress: Number((step / steps).toFixed(4)), at })
  }
  stops.push({ progress: 1, at: 100 })
  return stops
}

const stopsOf = (easing: string) =>
  (easing.replace(/\s+/g, ' ').match(/^linear\((.*)\)$/)?.[1] ?? '').split(',').map((stop) => {
    const [progress, at] = stop.trim().split(' ')
    return { progress: Number(progress), at: Number(at?.replace('%', '')) }
  })

const jumps = (stops: Stop[]) =>
  stops
    .slice(1)
    .filter((stop, i) => stop.at === stops[i]!.at)
    .map(({ at }) => at)

const durations: { at: string; ms: number; declared: string }[] = []
for (const file of files(`${WEB}Features`, /\.(vue|cshtml|css|ts)$/)) {
  const source = readFileSync(file, 'utf8')
  const where = (index: number) =>
    `${relative(WEB, file).replaceAll('\\', '/')}:${source.slice(0, index).split('\n').length}`

  for (const match of source.matchAll(UTILITY)) {
    const [token, whole, amount, unit] = match
    durations.push({
      at: `${where(match.index)} ${token}`,
      ms: whole ? Number(whole) : toMs(amount!, unit!),
      declared: token,
    })
  }

  for (const match of source.matchAll(DECLARATION)) {
    const declared = match[1]!.trim()
    if (declared.includes('infinite')) continue
    for (const [time, amount, unit] of withTokens(declared).matchAll(TIME)) {
      durations.push({ at: `${where(match.index)} ${time}`, ms: toMs(amount!, unit!), declared })
    }
  }
}

const blockAfter = (css: string, at: string) => {
  const start = css.indexOf(at)
  if (start < 0) return ''
  const open = css.indexOf('{', start)
  let depth = 0
  for (let i = open; i < css.length; i++) {
    if (css[i] === '{') depth++
    if (css[i] === '}' && --depth === 0) return css.slice(open + 1, i)
  }
  return ''
}

const keyframes = (name: string) => {
  const kit = read('Features/Styles/Torn/Kit.css')
  const at = kit.match(new RegExp(`@keyframes ${name}\\s*\\{`))
  if (!at) return undefined
  const frames = blockAfter(kit.slice(at.index), `@keyframes ${name}`)
  const declarations = (frame: string) =>
    Object.fromEntries(
      blockAfter(frames, `${frame} {`)
        .split(';')
        .map((declaration) => declaration.split(/:(.*)/s).map((part) => part.trim().replace(/\s+/g, ' ')))
        .filter(([property]) => property),
    )
  return { from: declarations('from'), to: declarations('to') }
}

const PEAK = 'calc(var(--wash-strength) * var(--wash-peak))'

const reducedWash = () => {
  const kit = read('Features/Styles/Torn/Kit.css')
  for (const match of kit.matchAll(/@media \(prefers-reduced-motion: reduce\)\s*\{/g)) {
    const block = blockAfter(kit.slice(match.index), '@media (prefers-reduced-motion: reduce)')
    if (/(?:^|[\s,}])\.kcc-wash\s*\{/.test(block)) return blockAfter(block, '.kcc-wash {')
  }
  return undefined
}

const kitFreezeSelectors = () => {
  const kit = read('Features/Styles/Torn/Kit.css')
  const start = kit.indexOf(':root[data-theme-switching]')
  const prelude = kit.slice(start, kit.indexOf('{', start))
  return prelude.split(',').map((selector) => selector.trim().replace(/\s+/g, ' '))
}

describe('motion', () => {
  it('finds the durations it measures', () => {
    expect(durations.length).toBeGreaterThan(3)
  })

  it('runs nothing longer than 500ms but a loop and the bloom', () => {
    const tooLong = durations.filter(({ ms, declared }) => ms > CEILING_MS && !BLOOM.test(declared)).map(({ at }) => at)
    expect(tooLong, 'over the 500ms ceiling in docs/brand/kit.md → Motion').toEqual([])
  })

  it('makes the bloom on .kcc-wash the one non-loop animation over 500ms', () => {
    const kit = read('Features/Styles/Torn/Kit.css')
    expect(blockAfter(kit, '.kcc-wash {')).toMatch(/animation:\s*kcc-bloom\s+var\(--wash-bloom\)/)

    const blooms = durations.filter(({ ms, declared }) => ms > CEILING_MS && BLOOM.test(declared)).map(({ at }) => at)
    expect(blooms).toHaveLength(1)
    expect(blooms[0]).toMatch(/^Features\/Styles\/Torn\/Kit\.css:\d+ 90s$/)
  })

  it('finishes the last wash at 300s: --wash-bloom plus the arrival window', () => {
    const bloom = tokens.get('--wash-bloom')?.match(/^(\d*\.?\d+)s$/)?.[1]
    expect(bloom, 'Torn/Tokens.css declares --wash-bloom in seconds').toBeDefined()
    expect(Number(bloom) + WASH_WINDOW_S).toBe(300)
  })

  it('times the bloom with --wash-ease', () => {
    expect(blockAfter(read('Features/Styles/Torn/Kit.css'), '.kcc-wash {')).toMatch(
      /animation:\s*kcc-bloom\s+var\(--wash-bloom\)\s+var\(--wash-ease\)\s+backwards;/,
    )
  })

  it('steps the bloom through 40 to 50 holds and never ramps between them', () => {
    const stops = stopsOf(tokens.get('--wash-ease') ?? '')
    expect(stops[0]).toEqual({ progress: 0, at: 0 })
    expect(stops.at(-1)).toEqual({ progress: 1, at: 100 })

    const steps = jumps(stops).length
    expect(steps).toBeGreaterThanOrEqual(40)
    expect(steps).toBeLessThanOrEqual(50)

    const ramps = stops.slice(1).filter((stop, i) => stop.at !== stops[i]!.at && stop.progress !== stops[i]!.progress)
    expect(ramps, 'between two jumps the bloom holds still').toEqual([])
  })

  it('stairs the bloom curve, front-loaded, so a fast-forwarded visit lands within one step of it', () => {
    const stops = stopsOf(tokens.get('--wash-ease') ?? '')
    expect(stops, 'regenerate --wash-ease in Torn/Tokens.css from this staircase').toEqual(staircase(BLOOM_STEPS))
    expect(jumps(stops).filter((at) => at < 100 / 6).length).toBeGreaterThan(BLOOM_STEPS / 2)
  })

  it('blooms from a faint, smaller wash to its peak at full size', () => {
    expect(keyframes('kcc-bloom'), 'Kit.css declares @keyframes kcc-bloom').toEqual({
      from: { opacity: '0', scale: '0.72' },
      to: { opacity: PEAK, scale: '1' },
    })
  })

  it('fades a wash in at full size to the same peak under reduced motion', () => {
    expect(keyframes('kcc-bloom-fade'), 'Kit.css declares @keyframes kcc-bloom-fade').toEqual({
      from: { opacity: '0' },
      to: { opacity: PEAK },
    })
    expect(
      blockAfter(read('Features/Styles/Torn/Kit.css'), '.kcc-wash {'),
      'a scale on the wash would show under the fade',
    ).not.toMatch(/(?<![\w-])scale\s*:/)
  })

  it('swaps only the keyframes of .kcc-wash under reduced motion, keeping its duration and delay', () => {
    const reduced = reducedWash()
    expect(reduced, 'Kit.css has a reduced-motion .kcc-wash rule').toBeDefined()
    const declarations = reduced!
      .split(';')
      .map((declaration) => declaration.trim().replace(/\s+/g, ' '))
      .filter(Boolean)
    expect(declarations).toEqual(['animation-name: kcc-bloom-fade'])
  })

  it('gives a transition that names no duration the 300ms standard', () => {
    expect(read('Features/Styles/Main.css')).toMatch(/--default-transition-duration:\s*300ms;/)
  })

  it('stills every transition and animation but the wash fade under reduced motion', () => {
    const reduced = blockAfter(read('Features/Styles/Main.css'), '@media (prefers-reduced-motion: reduce)')
    expect(reduced, 'the !important rule in @layer base would still the bloom fade').toMatch(
      /\*:not\(\.kcc-wash\),\s*\*::before,\s*\*::after\s*\{/,
    )
    expect(reduced).toMatch(/transition-duration:\s*0\.01ms !important;/)
    expect(reduced).toMatch(/animation-duration:\s*0\.01ms !important;/)
    expect(reduced).toMatch(/animation-iteration-count:\s*1 !important;/)
    expect(reduced).toMatch(/scroll-behavior:\s*auto !important;/)
  })

  it('freezes motion while the ramp switches', () => {
    const frozen = blockAfter(read('Features/Styles/Torn/Kit.css'), ':root[data-theme-switching] *')
    expect(frozen).toMatch(/animation:\s*none !important;/)
    expect(frozen).toMatch(/transition:\s*none !important;/)

    const selectors = kitFreezeSelectors()
    expect(selectors, 'the freeze sets animation: none, which would restart every bloom').toContain(
      ':root[data-theme-switching] *:not(.kcc-wash)',
    )
    expect(selectors.filter((selector) => selector.endsWith('*'))).toEqual([])

    const ramp = read('Features/Utilities/Ramp.ts')
    expect(ramp).toContain("setAttribute('data-theme-switching'")
    expect(ramp).toContain("removeAttribute('data-theme-switching')")
  })

  it('switches the ramp only through applyRamp', () => {
    const switchers = [...files(`${WEB}Features`, /\.(vue|ts)$/)]
      .filter((file) => readFileSync(file, 'utf8').includes("setAttribute('data-theme',"))
      .map((file) => relative(WEB, file).replaceAll('\\', '/'))

    expect(switchers, 'applyRamp in Utilities/Ramp.ts freezes motion for the switch').toEqual(['Features/Utilities/Ramp.ts'])
  })
})
