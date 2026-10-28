import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { GRAIN_TILE_PX } from '~/Bloom/bakeWashes'

const read = (path: string) =>
  readFileSync(fileURLToPath(new URL(`../../../../src/KCC.Web/Features/${path}`, import.meta.url)), 'utf8')
const uncommented = (css: string) => css.replace(/\/\*[\s\S]*?\*\//g, '')
const kit = uncommented(read('Styles/Torn/Kit.css'))

type Rule = { selectors: string[]; inQuery: boolean; declarations: Record<string, string> }

const splitTop = (text: string, separator: string) => {
  const parts: string[] = []
  let depth = 0
  let part = ''
  for (const char of text) {
    if (char === '(') depth++
    else if (char === ')') depth--
    else if (char === separator && depth === 0) {
      parts.push(part)
      part = ''
      continue
    }
    part += char
  }
  parts.push(part)
  return parts
}

function rulesOf(css: string): Rule[] {
  const rules: Rule[] = []
  const open: string[] = []
  let text = ''
  for (const char of css) {
    if (char === '{') {
      open.push(text.trim().replace(/\s+/g, ' '))
      text = ''
    } else if (char === '}') {
      const prelude = open.pop()!
      const declarations = Object.fromEntries(
        splitTop(text, ';')
          .map((declaration) => declaration.split(/:(.*)/s).map((part) => part.trim().replace(/\s+/g, ' ')))
          .filter(([property, value]) => property && value && !property.includes(' ')),
      )
      if (Object.keys(declarations).length)
        rules.push({
          selectors: splitTop(prelude, ',').map((selector) => selector.trim()),
          inQuery: open.some((at) => /^@(media|container|supports)/.test(at)),
          declarations,
        })
      text = ''
    } else if (char === ';' && open.length === 0) {
      text = ''
    } else {
      text += char
    }
  }
  return rules
}

const rules = rulesOf(kit)

const squash = (value: string | undefined) => value?.replace(/\s+/g, '')

const declared = (selector: string) =>
  Object.assign(
    {},
    ...rules.filter((rule) => !rule.inQuery && rule.selectors.includes(selector)).map((rule) => rule.declarations),
  ) as Record<string, string>

const field = declared('.kcc-washes')
const wash = declared('.kcc-wash')
const bloomStart = declared('from')
const bloomEnd = declared('to')

const DELAY = 'calc(var(--wash-at, var(--wash-fallback-at)) - var(--wash-elapsed, 0s) - var(--wash-lag, 0s))'
const CAPPED_WIDTH =
  'max(0px, min(calc(var(--wash-d) * 100cqw), calc((hypot(calc(var(--wash-dx) * 100cqw), calc(var(--wash-dy) * 100cqh)) - 2 * var(--wash-reach)) / var(--wash-s))))'

const alphas = (background: string, ink: string) =>
  [...background.matchAll(new RegExp(`oklch\\(from var\\(${ink}\\) ([^/]+)/ ([\\d.]+)\\)`, 'g'))].map(
    ([, channels, alpha]) => ({ channels: channels!.trim(), alpha: Number(alpha) }),
  )

describe('the wash field', () => {
  it('lies over the whole sheet, under its content, and takes no pointer', () => {
    expect(field.position).toBe('absolute')
    expect(field.inset).toBe('0')
    expect(field['z-index']).toBe('0')
    expect(field['pointer-events']).toBe('none')
  })

  it('clips its washes without becoming a scroll container', () => {
    expect(field.overflow).toBe('clip')
    const hidden = rules.filter(
      (rule) =>
        rule.selectors.some((selector) => selector.includes('.kcc-washes')) &&
        /hidden/.test(rule.declarations.overflow ?? ''),
    )
    expect(hidden).toEqual([])
  })

  it('is a size container, so a wash measures the real sheet', () => {
    expect(field['container-type']).toBe('size')
  })

  it('blends onto the paper as one group', () => {
    expect(field['mix-blend-mode']).toBe('var(--wash-blend)')
  })

  it('lifts every other child of a sheet above it', () => {
    expect(declared('.kcc-sheet > :where(:not(.kcc-washes))')['z-index']).toBe('1')
    expect(rules.some((rule) => rule.selectors.includes('.kcc-sheet > :where(:not(.kcc-wash))'))).toBe(false)
  })
})

describe('a wash', () => {
  it('rests at the end of its bloom, so a finished bloom leaves no animation applied', () => {
    expect(wash.opacity).toBe('calc(var(--wash-strength) * var(--wash-peak))')
    expect(bloomEnd.opacity).toBe(wash.opacity)
  })

  it('blooms with fill mode backwards from invisible, delayed by its arrival less the visit and the late mount', () => {
    expect(wash.animation).toBe('kcc-bloom var(--wash-bloom) var(--wash-ease) backwards')
    expect(bloomStart.opacity).toBe('0')
    const delay = wash['animation-delay'] ?? wash.animation
    expect(squash(delay)).toContain(squash(DELAY))
  })

  it('never asks for will-change', () => {
    const asking = rules.filter(
      (rule) =>
        rule.selectors.some((selector) => selector.includes('.kcc-wash')) && rule.declarations['will-change'] !== undefined,
    )
    expect(asking).toEqual([])
  })

  it('carries no filter of any kind, so no wash is drawn offscreen and no SVG filter runs while it blooms', () => {
    const washRules = [kit, uncommented(read('Styles/Torn/Washes.css'))]
      .flatMap(rulesOf)
      .filter((rule) => rule.selectors.some((selector) => /\.kcc-wash(?![\w-])/.test(selector)))
    expect(washRules.length).toBeGreaterThan(0)
    expect(washRules.filter((rule) => rule.declarations.filter !== undefined)).toEqual([])
    expect(read('Pages/Shared/Layout.cshtml')).not.toMatch(/<filter id="kcc-bloom"/)
  })

  it('takes its bled shape and its grain from the baked masks, the grain tiled at its own pixel size', () => {
    expect(squash(wash['mask-image'])).toBe('var(--wash-grain),var(--wash-blob)')
    expect(squash(wash['mask-composite'])).toBe('intersect,add')
    expect(squash(wash['mask-mode'])).toBe('alpha')
    expect(squash(wash['mask-repeat'])).toBe('repeat,no-repeat')
    expect(squash(wash['mask-position'])).toBe('00,center')
    expect(squash(wash['mask-size'])).toBe(
      `${GRAIN_TILE_PX}px${GRAIN_TILE_PX}px,calc(var(--wash-w)*var(--wash-blob-span))calc(var(--wash-h)*var(--wash-blob-span))`,
    )
    expect(wash['border-radius']).toBeUndefined()
  })

  it('saturates each ink by the ramp’s --wash-sat and tones it by the grain, with the saturate() matrix in linear light', () => {
    const tint = (ink: string) =>
      [
        `color(from var(${ink}) srgb-linear `,
        `clamp(0,var(--wash-tone)*((0.213+0.787*var(--wash-sat))*r+(0.715-0.715*var(--wash-sat))*g+(0.072-0.072*var(--wash-sat))*b),1)`,
        `clamp(0,var(--wash-tone)*((0.213-0.213*var(--wash-sat))*r+(0.715+0.285*var(--wash-sat))*g+(0.072-0.072*var(--wash-sat))*b),1)`,
        `clamp(0,var(--wash-tone)*((0.213-0.213*var(--wash-sat))*r+(0.715-0.715*var(--wash-sat))*g+(0.072+0.928*var(--wash-sat))*b),1))`,
      ].join('')
    expect(squash(wash['--wash-tint'])).toBe(squash(tint('--wash-ink')))
    expect(squash(wash['--wash-tint-2'])).toBe(squash(tint('--wash-ink-2')))
  })

  it('declares no mix-blend-mode or isolation on a wash', () => {
    const blending = rules.filter(
      (rule) =>
        rule.selectors.some((selector) => selector.includes('.kcc-wash') && !selector.includes('.kcc-washes')) &&
        (rule.declarations['mix-blend-mode'] !== undefined || rule.declarations.isolation !== undefined),
    )
    expect(blending).toEqual([])
  })

  it('caps its width against its pair partner, bleed included, and takes its height from its aspect', () => {
    expect(squash(wash['--wash-w'])).toBe(squash(CAPPED_WIDTH))
    expect(squash(wash['--wash-h'])).toBe('calc(var(--wash-w)/var(--wash-aspect))')
    expect(wash.width).toBe('var(--wash-w)')
    expect(wash['aspect-ratio']).toBe('var(--wash-aspect)')
  })

  it('holds its bleed in a padding of --wash-reach around the box its width and aspect size', () => {
    expect(wash['box-sizing']).toBe('content-box')
    expect(wash.padding).toBe('var(--wash-reach)')
  })

  it('draws its first ink at most .345 at the rim and its second at most .26, as contrast.test.ts models them', () => {
    const background = wash.background ?? wash['background-image'] ?? ''
    const first = alphas(background, '--wash-tint')
    const second = alphas(background, '--wash-tint-2')
    expect(first.length).toBeGreaterThan(1)
    expect(second.length).toBeGreaterThan(0)
    expect(Math.max(...first.map(({ alpha }) => alpha))).toBe(0.345)
    expect(Math.max(...second.map(({ alpha }) => alpha))).toBe(0.26)
  })

  it('lifts the rim by alpha alone, never by a darker colour', () => {
    const background = wash.background ?? wash['background-image'] ?? ''
    const channels = [...alphas(background, '--wash-tint'), ...alphas(background, '--wash-tint-2')].map(
      ({ channels }) => channels,
    )
    expect(new Set(channels)).toEqual(new Set(['l c h']))
  })
})

describe('a tile', () => {
  it('keeps its flat wax', () => {
    expect(declared('.kcc-tile').filter).toBe('url(#kcc-wax-flat)')
  })
})
