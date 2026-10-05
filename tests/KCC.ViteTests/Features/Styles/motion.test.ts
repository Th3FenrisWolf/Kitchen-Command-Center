import { readFileSync } from 'node:fs'
import { relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const read = (path: string) => readFileSync(`${WEB}${path}`, 'utf8')

const CEILING_MS = 500

const UTILITY = /(?<![\w-])duration-(?:(\d+)(?![\w-])|\[(\d*\.?\d+)(ms|s)\])/g
const DECLARATION = /(?<![\w-])(?:transition|animation)(?:-duration)?\s*:\s*([^;{}"]+)/g
const TIME = /(\d*\.?\d+)(ms|s)\b/g

const toMs = (amount: string, unit: string) => Number(amount) * (unit === 's' ? 1000 : 1)

const durations: { at: string; ms: number }[] = []
for (const file of files(`${WEB}Features`, /\.(vue|cshtml|css|ts)$/)) {
  const source = readFileSync(file, 'utf8')
  const where = (index: number) =>
    `${relative(WEB, file).replaceAll('\\', '/')}:${source.slice(0, index).split('\n').length}`

  for (const match of source.matchAll(UTILITY)) {
    const [token, whole, amount, unit] = match
    durations.push({ at: `${where(match.index)} ${token}`, ms: whole ? Number(whole) : toMs(amount!, unit!) })
  }

  for (const match of source.matchAll(DECLARATION)) {
    const value = match[1]!
    if (value.includes('infinite')) continue
    for (const [time, amount, unit] of value.matchAll(TIME)) {
      durations.push({ at: `${where(match.index)} ${time}`, ms: toMs(amount!, unit!) })
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

describe('motion', () => {
  it('finds the durations it measures', () => {
    expect(durations.length).toBeGreaterThan(3)
  })

  it('runs nothing longer than 500ms but a loop', () => {
    const tooLong = durations.filter(({ ms }) => ms > CEILING_MS).map(({ at }) => at)
    expect(tooLong, 'over the 500ms ceiling in docs/brand/kit.md → Motion').toEqual([])
  })

  it('gives a transition that names no duration the 300ms standard', () => {
    expect(read('Features/Styles/Main.css')).toMatch(/--default-transition-duration:\s*300ms;/)
  })

  it('stills every transition and animation under reduced motion', () => {
    const reduced = blockAfter(read('Features/Styles/Main.css'), '@media (prefers-reduced-motion: reduce)')
    expect(reduced).toMatch(/\*,\s*\*::before,\s*\*::after\s*\{/)
    expect(reduced).toMatch(/transition-duration:\s*0\.01ms !important;/)
    expect(reduced).toMatch(/animation-duration:\s*0\.01ms !important;/)
    expect(reduced).toMatch(/animation-iteration-count:\s*1 !important;/)
    expect(reduced).toMatch(/scroll-behavior:\s*auto !important;/)
  })

  it('freezes motion while the ramp switches', () => {
    const frozen = blockAfter(read('Features/Styles/Torn/Kit.css'), ':root[data-theme-switching] *')
    expect(frozen).toMatch(/animation:\s*none !important;/)
    expect(frozen).toMatch(/transition:\s*none !important;/)

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
