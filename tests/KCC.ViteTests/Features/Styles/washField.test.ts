import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const kit = readFileSync(
  fileURLToPath(new URL('../../../../src/KCC.Web/Features/Styles/Torn/Kit.css', import.meta.url)),
  'utf8',
).replace(/\/\*[\s\S]*?\*\//g, '')

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

const rules: Rule[] = []
{
  const open: string[] = []
  let text = ''
  for (const char of kit) {
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
}

const squash = (value: string | undefined) => value?.replace(/\s+/g, '')

const declared = (selector: string) =>
  Object.assign(
    {},
    ...rules.filter((rule) => !rule.inQuery && rule.selectors.includes(selector)).map((rule) => rule.declarations),
  ) as Record<string, string>

const field = declared('.kcc-washes')
const wash = declared('.kcc-wash')

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
  it('rests invisible until its bloom starts', () => {
    expect(wash.opacity).toBe('0')
  })

  it('blooms with fill mode both, delayed by its arrival less the visit and the late mount', () => {
    expect(wash.animation).toMatch(/^kcc-bloom\s/)
    expect(wash.animation?.split(' ')).toContain('both')
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

  it('bleeds through #kcc-bloom and takes its saturation from the ramp', () => {
    expect(wash.filter).toBe('url(#kcc-bloom) saturate(var(--wash-sat))')
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
    expect(squash(wash.width)).toBe(squash(CAPPED_WIDTH))
    expect(wash['aspect-ratio']).toBe('var(--wash-aspect)')
  })

  it('draws its first ink at most .345 at the rim and its second at most .26, as contrast.test.ts models them', () => {
    const background = wash.background ?? wash['background-image'] ?? ''
    const first = alphas(background, '--wash-ink')
    const second = alphas(background, '--wash-ink-2')
    expect(first.length).toBeGreaterThan(1)
    expect(second.length).toBeGreaterThan(0)
    expect(Math.max(...first.map(({ alpha }) => alpha))).toBe(0.345)
    expect(Math.max(...second.map(({ alpha }) => alpha))).toBe(0.26)
  })

  it('lifts the rim by alpha alone, never by a darker colour', () => {
    const background = wash.background ?? wash['background-image'] ?? ''
    const channels = [...alphas(background, '--wash-ink'), ...alphas(background, '--wash-ink-2')].map(
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
