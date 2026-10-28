import { describe, expect, it } from 'vitest'
// @ts-expect-error plain JavaScript with no declarations
import * as lib from '../../scripts/perf-diff-lib.mjs'

const template = readTemplate()

function readTemplate() {
  return `<title>Round title</title><style>
</style><h1>Round title</h1><nav aria-label="Options">
      <a href="#direction">Direction</a>
    </nav>
  <section class="pv-option" id="direction">
    <header></header>
  </section>`
}

describe('perf-diff pairing', () => {
  it('pairs by case name and reports the unpaired side', () => {
    expect(lib.pairCases(['a', 'b'], ['b', 'c'])).toEqual({ paired: ['b'], onlyBefore: ['a'], onlyAfter: ['c'] })
  })

  it('groups by page type for both name styles', () => {
    const groups = lib.groupByPage(['search-3 · phone · dark', 'home/phone/light', 'search-3__desktop__light'])
    expect([...groups.keys()]).toEqual(['home', 'search-3'])
    expect(groups.get('search-3')).toHaveLength(2)
  })

  it('computes and formats the diff share', () => {
    expect(lib.diffShare(1, 4)).toBe(0.25)
    expect(lib.diffShare(0, 0)).toBe(0)
    expect(lib.formatShare(0)).toBe('0%')
    expect(lib.formatShare(0.00001)).toBe('<0.01%')
    expect(lib.formatShare(0.0123)).toBe('1.2%')
  })

  it('numbers the next round from existing look files', () => {
    expect(lib.nextRound(['look-1-home.html', 'look-3-search.html', '_round.html'])).toBe(4)
    expect(lib.nextRound([])).toBe(1)
  })

  it('renders each case with before, after, heat map and a label-only share', () => {
    const html = lib.renderRoundPage(template, {
      round: 2,
      page: 'search-3',
      cases: [{ name: 'search-3 · phone · dark', share: 0.5, before: 'b.png', after: 'a.png', heat: 'h.png' }],
    })
    expect(html).toContain('search-3 · phone · dark')
    expect(html).toContain('50.0% of pixels differ')
    for (const src of ['b.png', 'a.png', 'h.png']) expect(html).toContain(`src="${src}"`)
    expect(html).not.toContain('Round title')
  })
})

describe('scripts-off check', () => {
  it('pairs each scripts-on capture with its scripts-off capture and reports a lone side', () => {
    const { pairs, unpaired } = lib.scriptsOffPairs([
      'home__phone__chromium__light__bloomed',
      'home__phone__chromium__light__bloomed__scripts-on',
      'home__phone__chromium__light__bloomed__scripts-off',
      'recipe__phone__webkit__light__bloomed__scripts-on',
    ])
    expect(pairs).toEqual([
      {
        name: 'home__phone__chromium__light__bloomed',
        on: 'home__phone__chromium__light__bloomed__scripts-on',
        off: 'home__phone__chromium__light__bloomed__scripts-off',
      },
    ])
    expect(unpaired).toEqual(['recipe__phone__webkit__light__bloomed__scripts-on'])
  })

  it('counts every pixel whose channels differ at all, and the largest channel delta', () => {
    const on = { width: 2, height: 1, data: Uint8Array.from([10, 20, 30, 255, 0, 0, 0, 255]) }
    const off = { width: 2, height: 1, data: Uint8Array.from([10, 20, 31, 255, 0, 0, 0, 255]) }
    expect(lib.comparePixels(on, on)).toEqual({ sameSize: true, differing: 0, maxDelta: 0 })
    expect(lib.comparePixels(on, off)).toEqual({ sameSize: true, differing: 1, maxDelta: 1 })
    expect(lib.comparePixels(on, { width: 1, height: 1, data: Uint8Array.from([10, 20, 30, 255]) }).sameSize).toBe(false)
  })

  it('passes a pair only when its captures are pixel-identical', () => {
    expect(lib.scriptsOffPasses({ sameSize: true, differing: 0, maxDelta: 0 })).toBe(true)
    expect(lib.scriptsOffPasses({ sameSize: true, differing: 1, maxDelta: 1 })).toBe(false)
    expect(lib.scriptsOffPasses({ sameSize: false, differing: 0, maxDelta: 0 })).toBe(false)
  })
})
