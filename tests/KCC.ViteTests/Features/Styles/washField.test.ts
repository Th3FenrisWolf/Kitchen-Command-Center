import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

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
  'max(0px, min(calc(var(--wash-d) * 100cqw), calc(hypot(calc(var(--wash-dx) * 100cqw), calc(var(--wash-dy) * 100cqh)) / (var(--wash-s) * var(--wash-span)))))'
const SPAN_PADDING = 'calc(var(--wash-h) * (var(--wash-span) - 1) / 2) calc(var(--wash-w) * (var(--wash-span) - 1) / 2)'

const BOX = /^(border|padding|content)-box$/
const OPACITY = 'calc(var(--wash-strength) * var(--wash-peak) * var(--wash-gain))'

const layersOf = (declarations: Record<string, string>) =>
  splitTop(declarations.background ?? '', ',').map((layer) => layer.trim())

function layerBoxes(declarations: Record<string, string>) {
  const listed = (property: string) => splitTop(declarations[property] ?? '', ',').map((value) => value.trim())
  const origins = listed('background-origin')
  const clips = listed('background-clip')
  return layersOf(declarations).map((layer, i) => {
    const tokens = splitTop(layer, ' ')
      .map((token) => token.trim())
      .filter((token) => BOX.test(token))
    return {
      origin: origins[i % origins.length] || tokens[0] || 'padding-box',
      clip: clips[i % clips.length] || tokens[1] || tokens[0] || 'border-box',
    }
  })
}

const alphas = (background: string, ink: string) =>
  [...background.matchAll(new RegExp(`oklch\\(from var\\(${ink}\\) ([^/]+)/ ([\\d.]+)\\)`, 'g'))].map(
    ([, channels, alpha]) => ({ channels: channels!.trim(), alpha: Number(alpha) }),
  )

const keyframeEnd = (name: string) => new RegExp(`@keyframes ${name}\\s*\\{[\\s\\S]*?to\\s*\\{([^}]*)\\}`).exec(kit)?.[1]

const SECOND_INK =
  'radial-gradient(closest-side at calc(var(--wash-o3)*100%) calc(var(--wash-o4)*100%),oklch(from var(--wash-tint-2) l c h/.26),oklch(from var(--wash-tint-2) l c h/0))'

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
    expect(wash.opacity).toBe(OPACITY)
    expect(bloomEnd.opacity).toBe(OPACITY)
  })

  it('fades to the same opacity under reduced motion', () => {
    expect(squash(keyframeEnd('kcc-bloom'))).toContain(squash(`opacity:${OPACITY}`))
    expect(squash(keyframeEnd('kcc-bloom-fade'))).toContain(squash(`opacity:${OPACITY}`))
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

  it('takes its bled shape from the baked blob over the whole padded box, and its chalk tiled at its own pixel size', () => {
    expect(squash(wash['mask-image'])).toBe('var(--wash-chalk),var(--wash-blob)')
    expect(squash(wash['mask-composite'])).toBe('intersect,add')
    expect(squash(wash['mask-mode'])).toBe('alpha')
    expect(squash(wash['mask-repeat'])).toBe('repeat,no-repeat')
    expect(squash(wash['mask-position'])).toBe('00,center')
    expect(squash(wash['mask-size'])).toBe('128px128px,100%100%')
    expect(wash['border-radius']).toBeUndefined()
  })

  it('mirrors as a whole by --wash-flip', () => {
    expect(squash(wash.transform)).toBe('scale(var(--wash-flip),1)')
  })

  it('saturates each ink by the ramp’s --wash-sat, with the saturate() matrix in linear light and no tone', () => {
    const tint = (ink: string) =>
      [
        `color(from var(${ink}) srgb-linear `,
        `clamp(0,((0.213+0.787*var(--wash-sat))*r+(0.715-0.715*var(--wash-sat))*g+(0.072-0.072*var(--wash-sat))*b),1)`,
        `clamp(0,((0.213-0.213*var(--wash-sat))*r+(0.715+0.285*var(--wash-sat))*g+(0.072-0.072*var(--wash-sat))*b),1)`,
        `clamp(0,((0.213-0.213*var(--wash-sat))*r+(0.715-0.715*var(--wash-sat))*g+(0.072+0.928*var(--wash-sat))*b),1))`,
      ].join('')
    expect(squash(wash['--wash-tint'])).toBe(squash(tint('--wash-ink')))
    expect(squash(wash['--wash-tint-2'])).toBe(squash(tint('--wash-ink-2')))
    expect(`${wash['--wash-tint']}${wash['--wash-tint-2']}`).not.toContain('--wash-tone')
  })

  it('declares no mix-blend-mode or isolation on a wash', () => {
    const blending = rules.filter(
      (rule) =>
        rule.selectors.some((selector) => selector.includes('.kcc-wash') && !selector.includes('.kcc-washes')) &&
        (rule.declarations['mix-blend-mode'] !== undefined || rule.declarations.isolation !== undefined),
    )
    expect(blending).toEqual([])
  })

  it('carries no border-radius, will-change, filter, blend or isolation of its own', () => {
    for (const property of ['filter', 'mix-blend-mode', 'isolation', 'will-change', 'border-radius'])
      expect(wash[property], property).toBeUndefined()
  })

  it('names none of the retired planner or bleed properties', () => {
    expect(kit).not.toMatch(/--wash-(?:o1|o2|bleed-step|grain)(?![\w-])/)
  })

  it('caps its width against its pair partner and takes its height from its aspect', () => {
    expect(squash(wash['--wash-w'])).toBe(squash(CAPPED_WIDTH))
    expect(squash(wash['--wash-h'])).toBe('calc(var(--wash-w)/var(--wash-aspect))')
    expect(wash.width).toBe('var(--wash-w)')
    expect(wash['aspect-ratio']).toBe('var(--wash-aspect)')
  })

  it('pads the box its width and aspect size by --wash-span, so the padded box scales with the wash', () => {
    expect(wash['box-sizing']).toBe('content-box')
    expect(squash(wash.padding)).toBe(squash(SPAN_PADDING))
  })

  it('lays the second ink on top in the content box and the first ink flat under it, as a gradient layer clipped to the border box', () => {
    const layers = layersOf(wash)
    const boxes = layerBoxes(wash)
    const flat = 'oklch(from var(--wash-tint) l c h / .34)'
    expect(layers).toHaveLength(2)
    for (const layer of layers) expect(layer).toMatch(/^(?:radial|linear)-gradient\(/)
    expect(squash(layers[0])?.replace(/(?<!\d)0\./g, '.')).toContain(squash(SECOND_INK))
    expect(squash(layers[0])).toContain('no-repeat')
    expect(boxes[0]).toEqual({ origin: 'content-box', clip: 'content-box' })
    expect(squash(layers[1])?.replace(/(?<!\d)0\./g, '.')).toBe(squash(`linear-gradient(${flat},${flat})`))
    expect(boxes[1]!.clip).toBe('border-box')
    expect(wash['background-color']).toBeUndefined()
  })

  it.each(['Styles/Torn/Kit.css', 'Styles/Torn/Tokens.css'])('names no --wash-reach in %s', (path) => {
    expect(read(path)).not.toContain('--wash-reach')
  })

  it('draws its first ink flat at .34, as one colour twice, and its second at .26 fading to nothing, as contrast.test.ts models them', () => {
    const background = wash.background ?? wash['background-image'] ?? ''
    expect(alphas(background, '--wash-tint').map(({ alpha }) => alpha)).toEqual([0.34, 0.34])
    expect(alphas(background, '--wash-tint-2').map(({ alpha }) => alpha)).toEqual([0.26, 0])
  })

  it('fades each ink by alpha alone, keeping its lightness, chroma and hue', () => {
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
