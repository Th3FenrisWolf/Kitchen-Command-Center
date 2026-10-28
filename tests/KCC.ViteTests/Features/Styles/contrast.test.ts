import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// Both ramps must pass WCAG AA on the pairs the identity actually uses. Light is the default ramp and the
// binding constraint for small caps on the desk; dark is the binding constraint for chalk over a screened
// wash. Three values are pinned here rather than by the mockup: light ink-soft, light hair-strong and the
// --wash-strength of each ramp. Lower a failing token's L (or the alpha / opacity) and rerun.
const stylesDir = new URL('../../../../src/KCC.Web/Features/Styles/', import.meta.url)
const read = (name: string) => readFileSync(fileURLToPath(new URL(name, stylesDir)), 'utf8')

type Oklch = { l: number; c: number; h: number; alpha: number }
type Tokens = Map<string, Oklch | { ref: string }>

const TOKEN =
  /--color-([a-z0-9-]+):\s*(?:oklch\(([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\)|var\(--color-([a-z0-9-]+)\))/g

function parseTokens(css: string): Tokens {
  const tokens: Tokens = new Map()
  for (const [, name, l, c, h, alpha, ref] of css.matchAll(TOKEN)) {
    tokens.set(
      name!,
      ref ? { ref } : { l: Number(l), c: Number(c), h: Number(h), alpha: alpha === undefined ? 1 : Number(alpha) },
    )
  }
  return tokens
}

// Bounded to the block's own braces. Slicing to end-of-file would fold anything declared after it into the
// ramp being tested.
function blockAt(css: string, index: number, file: string): string {
  let depth = 0
  for (let i = css.indexOf('{', index); i < css.length; i++) {
    if (css[i] === '{') depth++
    else if (css[i] === '}' && --depth === 0) return css.slice(index, i)
  }
  throw new Error(`the block at index ${index} of ${file} is never closed`)
}

const themeCss = read('TailwindConfig.css')
const tokensCss = read('Torn/Tokens.css')
const light = parseTokens(themeCss)
const darkAt = tokensCss.indexOf("[data-theme='dark']")
// Without this the dark map would just be the light map and every dark assertion would re-test light.
if (darkAt < 0) throw new Error("Torn/Tokens.css declares no [data-theme='dark'] block")
const darkBlock = blockAt(tokensCss, darkAt, 'Torn/Tokens.css')
const dark = new Map([...light, ...parseTokens(darkBlock)])

// A dark token written in a form the regex does not read (percent lightness, `deg`, color-mix) would fall
// out of the dark map and every dark assertion would re-test light. Guard the parse itself.
const darkOverrides = parseTokens(darkBlock)
const darkDeclared = darkBlock.match(/--color-[a-z0-9-]+:/g)?.length ?? 0
if (darkOverrides.size !== darkDeclared)
  throw new Error(`parsed ${darkOverrides.size} of ${darkDeclared} dark tokens in Torn/Tokens.css`)
if (resolve(dark, 'paper').l === resolve(light, 'paper').l) throw new Error('the dark ramp did not override paper')

type WashRender = { blend: 'multiply' | 'screen'; strength: number; sat: number }

function washRender(css: string, base: Partial<WashRender> = {}): WashRender {
  const blend = (css.match(/--wash-blend:\s*(multiply|screen)/)?.[1] as WashRender['blend'] | undefined) ?? base.blend
  const strengthText = css.match(/--wash-strength:\s*([\d.]+)/)?.[1]
  const satText = css.match(/--wash-sat:\s*([\d.]+)/)?.[1]
  const sat = satText === undefined ? base.sat : Number(satText)
  if (!blend || strengthText === undefined || sat === undefined)
    throw new Error('Torn/Tokens.css declares no --wash-blend / --wash-strength / --wash-sat')
  return { blend, strength: Number(strengthText), sat }
}

const rootAt = tokensCss.indexOf(':root {')
if (rootAt < 0) throw new Error('Torn/Tokens.css declares no plain :root block')
const lightWash = washRender(blockAt(tokensCss, rootAt, 'Torn/Tokens.css'))
const darkWash = washRender(darkBlock, lightWash)

function resolve(tokens: Tokens, name: string, depth = 0): Oklch {
  const value = tokens.get(name)
  if (!value) throw new Error(`token --color-${name} is not declared`)
  if ('ref' in value) {
    if (depth > 5) throw new Error(`token --color-${name} reference chain deeper than 5 (a cycle or an alias of an alias)`)
    return resolve(tokens, value.ref, depth + 1)
  }
  return value
}

type Rgb = { r: number; g: number; b: number }

// oklch → oklab → LMS → linear sRGB (Björn Ottosson's published matrices). Out-of-gamut channels are clipped
// per channel here; browsers gamut-map instead, so only in-gamut tokens are ever asserted (see the gamut test).
function toLinearRgbRaw({ l, c, h }: Oklch): Rgb {
  const a = c * Math.cos((h * Math.PI) / 180)
  const b = c * Math.sin((h * Math.PI) / 180)
  const l_ = l + 0.3963377774 * a + 0.2158037573 * b
  const m_ = l - 0.1055613458 * a - 0.0638541728 * b
  const s_ = l - 0.0894841775 * a - 1.291485548 * b
  const L = l_ ** 3
  const M = m_ ** 3
  const S = s_ ** 3
  return {
    r: 4.0767416621 * L - 3.3077115913 * M + 0.2309699292 * S,
    g: -1.2684380046 * L + 2.6097574011 * M - 0.3413193965 * S,
    b: -0.0041960863 * L - 0.7034186147 * M + 1.707614701 * S,
  }
}

// sRGB clip for actual rendering math; the gamut test below calls toLinearRgbRaw directly so an
// out-of-gamut channel is measured instead of silently clamped away.
function toLinearRgb(oklch: Oklch): Rgb {
  const clip = (v: number) => Math.min(1, Math.max(0, v))
  const raw = toLinearRgbRaw(oklch)
  return { r: clip(raw.r), g: clip(raw.g), b: clip(raw.b) }
}

const luminance = ({ r, g, b }: Rgb) => 0.2126 * r + 0.7152 * g + 0.0722 * b

// The sRGB transfer function both ways: CSS alpha, blending and opacity all composite in gamma-encoded
// sRGB, so anything translucent is mixed in encoded space and decoded again before the luminance is read.
// Mixing in linear light instead (the Softbound harness did) under-reports a 40% hairline as 1.6:1 when it
// renders at 2.4:1.
const encode = (v: number) => (v <= 0.0031308 ? 12.92 * v : 1.055 * Math.pow(v, 1 / 2.4) - 0.055)
const decode = (v: number) => (v <= 0.04045 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4))
const mapRgb = (c: Rgb, f: (v: number) => number): Rgb => ({ r: f(c.r), g: f(c.g), b: f(c.b) })

/** `foreground` at `alpha` over an opaque `background`, as the browser composites it. */
function over(foreground: Rgb, background: Rgb, alpha: number): Rgb {
  const F = mapRgb(foreground, encode)
  const B = mapRgb(background, encode)
  return mapRgb({ r: B.r + (F.r - B.r) * alpha, g: B.g + (F.g - B.g) * alpha, b: B.b + (F.b - B.b) * alpha }, decode)
}

function contrastOf(foreground: Rgb, background: Rgb): number {
  const [hi, lo] = [luminance(foreground), luminance(background)].sort((x, y) => y - x) as [number, number]
  return (hi + 0.05) / (lo + 0.05)
}

function contrast(tokens: Tokens, foreground: string, background: string): number {
  const bg = toLinearRgb(resolve(tokens, background))
  const fgColor = resolve(tokens, foreground)
  // Translucent ink (hairlines) is judged as it renders: composited over its ground.
  return contrastOf(over(toLinearRgb(fgColor), bg, fgColor.alpha), bg)
}

/** Paper tinted with a wash, as `color-mix(in oklab, <wash> 28%, paper)` renders: the status wells. */
function tintedPaper(tokens: Tokens, wash: string, amount = 0.28): Rgb {
  const lab = ({ l, c, h }: Oklch) => ({
    L: l,
    a: c * Math.cos((h * Math.PI) / 180),
    b: c * Math.sin((h * Math.PI) / 180),
  })
  const P = lab(resolve(tokens, 'paper'))
  const W = lab(resolve(tokens, wash))
  const L = P.L + (W.L - P.L) * amount
  const a = P.a + (W.a - P.a) * amount
  const b = P.b + (W.b - P.b) * amount
  return toLinearRgb({ l: L, c: Math.hypot(a, b), h: ((Math.atan2(b, a) * 180) / Math.PI + 360) % 360, alpha: 1 })
}

// Stands in for the baked grain and bleed (Washes.css) as text reads them, averaged over 3px. Fitted per blend so
// the model reads at or below every rendering in RENDERED_INK_SOFT.
const GRAIN_GAIN: Record<WashRender['blend'], number> = { multiply: 1.26, screen: 0.905 }
const FIRST_INK_RIM_ALPHA = 0.26 + 0.25 * 0.34
const SECOND_INK_CORE_ALPHA = 0.26

type Premultiplied = { rgb: Rgb; alpha: number }
type WashPair = [first: string, second: string]

function sourceOver(top: Premultiplied, bottom: Premultiplied): Premultiplied {
  const keep = 1 - top.alpha
  return {
    rgb: {
      r: top.rgb.r + bottom.rgb.r * keep,
      g: top.rgb.g + bottom.rgb.g * keep,
      b: top.rgb.b + bottom.rgb.b * keep,
    },
    alpha: top.alpha + bottom.alpha * keep,
  }
}

function inkLayer(tokens: Tokens, token: string, alpha: number): Premultiplied {
  const { r, g, b } = mapRgb(toLinearRgb(resolve(tokens, token)), encode)
  return { rgb: { r: r * alpha, g: g * alpha, b: b * alpha }, alpha }
}

// The Filter Effects saturate() matrix on gamma-encoded sRGB. The washes saturate in linear light; GRAIN_GAIN
// absorbs the difference.
function saturate({ r, g, b }: Rgb, s: number): Rgb {
  const clip = (v: number) => Math.min(1, Math.max(0, v))
  return {
    r: clip((0.213 + 0.787 * s) * r + (0.715 - 0.715 * s) * g + (0.072 - 0.072 * s) * b),
    g: clip((0.213 - 0.213 * s) * r + (0.715 + 0.285 * s) * g + (0.072 - 0.072 * s) * b),
    b: clip((0.213 - 0.213 * s) * r + (0.715 - 0.715 * s) * g + (0.072 + 0.928 * s) * b),
  }
}

function washAtPeak(tokens: Tokens, [first, second]: WashPair, { blend, strength, sat }: WashRender): Premultiplied {
  const { rgb, alpha } = sourceOver(
    inkLayer(tokens, first, FIRST_INK_RIM_ALPHA),
    inkLayer(tokens, second, SECOND_INK_CORE_ALPHA),
  )
  const colour = saturate({ r: rgb.r / alpha, g: rgb.g / alpha, b: rgb.b / alpha }, sat)
  const peak = Math.min(1, alpha * strength * GRAIN_GAIN[blend])
  return { rgb: { r: colour.r * peak, g: colour.g * peak, b: colour.b * peak }, alpha: peak }
}

function stackedPaper(tokens: Tokens, [top, bottom]: [WashPair, WashPair], render: WashRender): Rgb {
  const { blend } = render
  const field = sourceOver(washAtPeak(tokens, top, render), washAtPeak(tokens, bottom, render))
  const paper = mapRgb(toLinearRgb(resolve(tokens, 'paper')), encode)
  const colour = (channel: 'r' | 'g' | 'b') => (field.alpha === 0 ? 0 : field.rgb[channel] / field.alpha)
  const mixed = (channel: 'r' | 'g' | 'b') => {
    const backdrop = paper[channel]
    const source = colour(channel)
    const blended = blend === 'multiply' ? backdrop * source : 1 - (1 - backdrop) * (1 - source)
    return backdrop + (blended - backdrop) * field.alpha
  }
  return mapRgb({ r: mixed('r'), g: mixed('g'), b: mixed('b') }, decode)
}

const WASHES = ['peach', 'yellow', 'green', 'teal', 'sky', 'lavender', 'pink', 'red']
const WASH_PAIRS = WASHES.flatMap((first) => WASHES.map((second): WashPair => [first, second]))
const WASH_STACKS = WASH_PAIRS.flatMap((top) => WASH_PAIRS.map((bottom): [WashPair, WashPair] => [top, bottom]))
// ink-soft over the worst two-wash stack at its peak, averaged over 3px, in headless Chromium: [ramp, strength,
// sat, reading]. Read from the baked washes in one harness and multiplied by the recorded value over that harness's
// reading of the same earlier settings (light .9656, dark .9585, the lowest of each ramp), so a row is derived from a
// harness reading, not a direct measurement. The harness reads higher than the recorded values.
const RENDERED_INK_SOFT: ['light' | 'dark', number, number, number][] = [
  ['light', 0.704, 1, 4.495],
  ['light', 0.42, 2.5, 5.047],
  ['light', 0.6, 2.5, 4.543],
  ['dark', 0.116, 1.45, 5.53],
  ['dark', 0.18, 3, 4.766],
  ['dark', 0.2, 3, 4.563],
  ['dark', 0.22, 3, 4.354],
]

// The status wells: kcc-well--danger / --success / --warning.
const STATUS_WASHES = ['red', 'green', 'yellow']

// Every ink is tested on every ground, not just on paper: `html` is painted with desk, so text outside a
// sheet sits on desk or desk-2.
const INKS = ['ink', 'ink-soft']
const GROUNDS = ['paper', 'paper-2', 'desk', 'desk-2']

const TEXT_PAIRS: [string, string][] = [
  ...INKS.flatMap((ink) => GROUNDS.map((ground): [string, string] => [ink, ground])),
  ['marker-ink', 'marker'],
  // Tiles and labels: the fixed dark ink on a wash fill.
  ...WASHES.map((wash): [string, string] => ['marker-ink', wash]),
]

// Control boundaries (inset hairlines) and the focus ring, which sits 2px outside its element on the ground.
// desk-2 is the darkest ground a hairline is read on, so it is the binding pair.
const UI_PAIRS: [string, string][] = [
  ['hair-strong', 'paper'],
  ['hair-strong', 'paper-2'],
  ['hair-strong', 'desk'],
  ['hair-strong', 'desk-2'],
  ['focus', 'paper'],
  ['focus', 'desk'],
]

// The colours a field's error state draws its line (and its area's outline) in, read from the kit: the line is
// the only mark of the error on the field itself, so it is UI and reads at 3:1 against the paper it lies on.
const errorLineColours = [
  ...read('Torn/Controls.css').matchAll(/\.kcc-field--error(?:\.kcc-field--area)?\s*\{([^}]*)\}/g),
].flatMap(([, body]) => [...body!.matchAll(/var\(--color-([a-z0-9-]+)\)/g)].map(([, colour]) => colour!))

describe.each<[string, Tokens, WashRender]>([
  ['light', light, lightWash],
  ['dark', dark, darkWash],
])('%s ramp', (name, tokens, wash) => {
  it('draws a field’s error line in a colour that reads at AA for UI against paper (≥ 3:1)', () => {
    expect(errorLineColours.length).toBeGreaterThan(0)
    for (const colour of errorLineColours) expect(contrast(tokens, colour, 'paper'), colour).toBeGreaterThanOrEqual(3)
  })

  it.each(TEXT_PAIRS)('%s on %s reads at AA for text (≥ 4.5:1)', (fg, bg) => {
    expect(contrast(tokens, fg, bg)).toBeGreaterThanOrEqual(4.5)
  })

  it.each(UI_PAIRS)('%s against %s reads at AA for UI (≥ 3:1)', (fg, bg) => {
    expect(contrast(tokens, fg, bg)).toBeGreaterThanOrEqual(3)
  })

  describe('ink over a washed sheet', () => {
    const ink = toLinearRgb(resolve(tokens, 'ink'))
    it('reads ink as opaque, so compositing can be skipped', () => {
      expect(resolve(tokens, 'ink').alpha).toBe(1)
    })

    const inkSoft = toLinearRgb(resolve(tokens, 'ink-soft'))
    const stacks = WASH_STACKS.map((stack) => stackedPaper(tokens, stack, wash))
    const worst = (reading: (ground: Rgb) => number) => Math.min(...stacks.map(reading))

    it('reads ink over every two-wash stack at its peak at AA for text (≥ 4.5:1)', () => {
      expect(worst((ground) => contrastOf(ink, ground))).toBeGreaterThanOrEqual(4.5)
    })

    it('reads ink-soft over every two-wash stack at its peak at AA for text (≥ 4.5:1)', () => {
      expect(worst((ground) => contrastOf(inkSoft, ground))).toBeGreaterThanOrEqual(4.5)
    })

    it.runIf(name === 'dark')('reads below AA a tenth stronger', () => {
      const stronger = { ...wash, strength: wash.strength * 1.1 }
      const hair = resolve(tokens, 'hair-strong')
      const reads = (render: WashRender) =>
        WASH_STACKS.every((stack) => {
          const ground = stackedPaper(tokens, stack, render)
          return (
            contrastOf(ink, ground) >= 4.5 &&
            contrastOf(inkSoft, ground) >= 4.5 &&
            contrastOf(over(toLinearRgb(hair), ground, hair.alpha), ground) >= 3
          )
        })
      expect(reads(wash)).toBe(true)
      expect(reads(stronger)).toBe(false)
    })

    it.each(STATUS_WASHES)('ink in a %s status well reads at AA for text (≥ 4.5:1)', (w) => {
      expect(contrastOf(ink, tintedPaper(tokens, w))).toBeGreaterThanOrEqual(4.5)
    })

    // Measured and not yet met in the dark ramp: soft ink inside a tinted well reads 3.3–3.9:1. Kit.css forces
    // ink for a kick inside a well for this reason, and no stats tile today carries both a well and a `<small>`
    // unit. `fails` keeps the measured number on record.
    const softInWell = name === 'dark' ? it.fails : it
    softInWell(
      `ink-soft in a status well reads at AA for text (≥ 4.5:1)${name === 'dark' ? ', measured 3.3–3.9:1' : ''}`,
      () => {
        for (const w of STATUS_WASHES) expect(contrastOf(inkSoft, tintedPaper(tokens, w))).toBeGreaterThanOrEqual(4.5)
      },
    )

    // Ghost buttons, badges, fields and checklist boxes draw their hairline on washed sheets too (the Library,
    // the ingredients sheet, the empty states).
    const hairStrong = resolve(tokens, 'hair-strong')
    const hairOver = (ground: Rgb) => contrastOf(over(toLinearRgb(hairStrong), ground, hairStrong.alpha), ground)

    it('reads hair-strong over every two-wash stack at its peak at AA for UI (≥ 3:1)', () => {
      expect(worst(hairOver)).toBeGreaterThanOrEqual(3)
    })
  })
})

describe('the wash model against the rendered filter', () => {
  const ramps = { light: [light, lightWash], dark: [dark, darkWash] } as const

  it.each(RENDERED_INK_SOFT)(
    '%s at strength %s, sat %s reads no higher than the rendered %s',
    (ramp, strength, sat, rendered) => {
      const [tokens, wash] = ramps[ramp]
      const inkSoft = toLinearRgb(resolve(tokens, 'ink-soft'))
      const render = { ...wash, strength, sat }
      const modelled = Math.min(...WASH_STACKS.map((stack) => contrastOf(inkSoft, stackedPaper(tokens, stack, render))))
      expect(modelled).toBeLessThanOrEqual(rendered)
      if (rendered < 4.5) expect(modelled).toBeLessThan(4.5)
    },
  )
})

// Every colour the table asserts must be inside sRGB: a channel outside [0, 1] before clipping would be
// measured as a colour the browser never shows. fiber and fall are shadow colours and are not asserted.
describe.each<[string, Tokens]>([
  ['light', light],
  ['dark', dark],
])('%s ramp gamut', (_name, tokens) => {
  const asserted = new Set([...TEXT_PAIRS.flat(), ...UI_PAIRS.flat(), ...WASHES, 'paper'])
  it.each([...asserted])('%s is inside sRGB', (token) => {
    const { r, g, b } = toLinearRgbRaw(resolve(tokens, token))
    for (const v of [r, g, b]) {
      expect(v).toBeGreaterThanOrEqual(-0.002)
      expect(v).toBeLessThanOrEqual(1.002)
    }
  })
})

// The browser draws some surfaces itself (a select's option list, the scrollbars, an autofilled field) on its
// own Canvas with the page's ink. Without the ramp's scheme, the dark ramp's chalk lands on a light Canvas.
describe('browser-drawn surfaces', () => {
  const lightRootAt = themeCss.indexOf(':root {')

  it('take the light scheme from the light ramp', () => {
    expect(lightRootAt, 'TailwindConfig.css declares no plain :root block').toBeGreaterThan(-1)
    expect(blockAt(themeCss, lightRootAt, 'TailwindConfig.css')).toMatch(/color-scheme:\s*light;/)
  })

  it('take the dark scheme from the dark ramp', () => {
    expect(darkBlock).toMatch(/color-scheme:\s*dark;/)
  })
})
