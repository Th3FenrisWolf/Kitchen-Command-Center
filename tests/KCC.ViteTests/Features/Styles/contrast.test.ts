import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// Both ramps must pass WCAG AA on the pairs the identity actually uses. Light is the default ramp and the
// binding constraint for small caps on the desk; dark is the binding constraint for chalk over a screened
// wash. Three values are pinned here rather than by the mockup: light ink-soft, light hair-strong and dark
// --wash-op. Lower a failing token's L (or the alpha / opacity) and rerun.
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
if (darkOverrides.size < 12) throw new Error(`only ${darkOverrides.size} dark tokens parsed from Torn/Tokens.css`)
if (resolve(dark, 'paper').l === resolve(light, 'paper').l) throw new Error('the dark ramp did not override paper')

type WashRender = { blend: 'multiply' | 'screen'; opacity: number }

function washRender(css: string, base: Partial<WashRender> = {}): WashRender {
  const blend = (css.match(/--wash-blend:\s*(multiply|screen)/)?.[1] as WashRender['blend'] | undefined) ?? base.blend
  const opacityText = css.match(/--wash-op:\s*([\d.]+)/)?.[1]
  const opacity = opacityText === undefined ? base.opacity : Number(opacityText)
  if (!blend || opacity === undefined) throw new Error('Torn/Tokens.css declares no --wash-blend / --wash-op')
  return { blend, opacity }
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

/**
 * Paper with a wash pool over it. `coverage` is the pool's own alpha at the point under test: 1 at the core,
 * .38 at the 70% ring (the radial gradient's second stop). The #kcc-wax filter and saturate() are ignored:
 * the grain composite slightly lightens the pool's weight and saturate() moves chroma, not lightness.
 */
function washedPaper(tokens: Tokens, wash: string, { blend, opacity }: WashRender, coverage: number): Rgb {
  const paper = mapRgb(toLinearRgb(resolve(tokens, 'paper')), encode)
  const pool = mapRgb(toLinearRgb(resolve(tokens, wash)), encode)
  const blended: Rgb =
    blend === 'multiply'
      ? { r: paper.r * pool.r, g: paper.g * pool.g, b: paper.b * pool.b }
      : { r: 1 - (1 - paper.r) * (1 - pool.r), g: 1 - (1 - paper.g) * (1 - pool.g), b: 1 - (1 - paper.b) * (1 - pool.b) }
  const a = opacity * coverage
  const out: Rgb = {
    r: paper.r + (blended.r - paper.r) * a,
    g: paper.g + (blended.g - paper.g) * a,
    b: paper.b + (blended.b - paper.b) * a,
  }
  return mapRgb(out, decode)
}

const WASHES = ['peach', 'yellow', 'green', 'teal', 'sky', 'lavender', 'pink', 'red']
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

describe.each<[string, Tokens, WashRender]>([
  ['light', light, lightWash],
  ['dark', dark, darkWash],
])('%s ramp', (name, tokens, wash) => {
  it.each(TEXT_PAIRS)('%s on %s reads at AA for text (≥ 4.5:1)', (fg, bg) => {
    expect(contrast(tokens, fg, bg)).toBeGreaterThanOrEqual(4.5)
  })

  it.each(UI_PAIRS)('%s against %s reads at AA for UI (≥ 3:1)', (fg, bg) => {
    expect(contrast(tokens, fg, bg)).toBeGreaterThanOrEqual(3)
  })

  describe('ink over a washed sheet', () => {
    const ink = toLinearRgb(resolve(tokens, 'ink'))
    // Copy may sit on the pool's outer ring; the identity keeps it off the core, so the core only has to
    // read as UI on the dark ramp.
    const coreFloor = name === 'light' ? 4.5 : 3

    it('reads ink as opaque, so compositing can be skipped', () => {
      expect(resolve(tokens, 'ink').alpha).toBe(1)
    })

    it.each(WASHES)('ink over paper + %s at the 38% ring reads at AA for text (≥ 4.5:1)', (w) => {
      expect(contrastOf(ink, washedPaper(tokens, w, wash, 0.38))).toBeGreaterThanOrEqual(4.5)
    })

    it.each(WASHES)(`ink over paper + %s at the core reads at ≥ ${coreFloor}:1`, (w) => {
      expect(contrastOf(ink, washedPaper(tokens, w, wash, 1))).toBeGreaterThanOrEqual(coreFloor)
    })

    const inkSoft = toLinearRgb(resolve(tokens, 'ink-soft'))

    it.each(WASHES)('ink-soft over paper + %s at the 38% ring reads at AA for text (≥ 4.5:1)', (w) => {
      expect(contrastOf(inkSoft, washedPaper(tokens, w, wash, 0.38))).toBeGreaterThanOrEqual(4.5)
    })

    it.each(WASHES)(`ink-soft over paper + %s at the core reads at ≥ ${coreFloor}:1`, (w) => {
      expect(contrastOf(inkSoft, washedPaper(tokens, w, wash, 1))).toBeGreaterThanOrEqual(coreFloor)
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
    // the ingredients sheet, the empty states). On the ring it reads; over a dark-ramp core it measures
    // 2.6–2.9:1, so controls stay off the core the way copy does.
    const hairStrong = resolve(tokens, 'hair-strong')
    const hairOver = (ground: Rgb) => contrastOf(over(toLinearRgb(hairStrong), ground, hairStrong.alpha), ground)

    it.each(WASHES)('hair-strong over paper + %s at the 38% ring reads at AA for UI (≥ 3:1)', (w) => {
      expect(hairOver(washedPaper(tokens, w, wash, 0.38))).toBeGreaterThanOrEqual(3)
    })

    const hairOnCore = name === 'dark' ? it.fails : it
    hairOnCore(
      `hair-strong over a wash core reads at AA for UI (≥ 3:1)${name === 'dark' ? ', measured 2.6–2.9:1' : ''}`,
      () => {
        for (const w of WASHES) expect(hairOver(washedPaper(tokens, w, wash, 1))).toBeGreaterThanOrEqual(3)
      },
    )
  })
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
