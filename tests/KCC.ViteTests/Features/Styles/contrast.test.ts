import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// Both ramps must pass WCAG AA on the pairs the design actually uses (umbrella spec §9). The light ramp is
// the binding constraint: washes are far too light to be text on cream paper, which is why the *-ink tokens
// exist. This test is what tunes them — lower a failing token's L by 0.02 and rerun.
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

const dark = parseTokens(read('TailwindConfig.css'))
const tokensCss = read('Sketch/Tokens.css')
const lightAt = tokensCss.indexOf("[data-theme='light']")
// Without this the slice would silently yield an empty overlay and every light assertion below would
// re-test the dark ramp and pass.
if (lightAt < 0) throw new Error("Sketch/Tokens.css declares no [data-theme='light'] block")
const light = new Map([...dark, ...parseTokens(blockAt(tokensCss, lightAt))])

// Bounded to the light block's own braces. Slicing to end-of-file would fold anything declared after it —
// a print override, a themed kit selector — into the light ramp.
function blockAt(css: string, index: number): string {
  let depth = 0
  for (let i = css.indexOf('{', index); i < css.length; i++) {
    if (css[i] === '{') depth++
    else if (css[i] === '}' && --depth === 0) return css.slice(index, i)
  }
  throw new Error(`the block at index ${index} of Sketch/Tokens.css is never closed`)
}

function resolve(tokens: Tokens, name: string, depth = 0): Oklch {
  const value = tokens.get(name)
  if (!value) throw new Error(`token --color-${name} is not declared`)
  if ('ref' in value) {
    if (depth > 5) throw new Error(`token --color-${name} references itself`)
    return resolve(tokens, value.ref, depth + 1)
  }
  return value
}

type Rgb = { r: number; g: number; b: number }

// oklch → oklab → LMS → linear sRGB (Björn Ottosson's published matrices). Out-of-gamut channels are clipped,
// which is what a browser does too.
function toLinearRgb({ l, c, h }: Oklch): Rgb {
  const a = c * Math.cos((h * Math.PI) / 180)
  const b = c * Math.sin((h * Math.PI) / 180)
  const l_ = l + 0.3963377774 * a + 0.2158037573 * b
  const m_ = l - 0.1055613458 * a - 0.0638541728 * b
  const s_ = l - 0.0894841775 * a - 1.291485548 * b
  const L = l_ ** 3
  const M = m_ ** 3
  const S = s_ ** 3
  const clip = (v: number) => Math.min(1, Math.max(0, v))
  return {
    r: clip(4.0767416621 * L - 3.3077115913 * M + 0.2309699292 * S),
    g: clip(-1.2684380046 * L + 2.6097574011 * M - 0.3413193965 * S),
    b: clip(-0.0041960863 * L - 0.7034186147 * M + 1.707614701 * S),
  }
}

const luminance = ({ r, g, b }: Rgb) => 0.2126 * r + 0.7152 * g + 0.0722 * b

function contrast(tokens: Tokens, foreground: string, background: string): number {
  const bg = toLinearRgb(resolve(tokens, background))
  const fgColor = resolve(tokens, foreground)
  const fg = toLinearRgb(fgColor)
  // Translucent ink (outline strokes) is judged as it renders: composited over its ground.
  const a = fgColor.alpha
  const mixed: Rgb = { r: fg.r * a + bg.r * (1 - a), g: fg.g * a + bg.g * (1 - a), b: fg.b * a + bg.b * (1 - a) }
  const [hi, lo] = [luminance(mixed), luminance(bg)].sort((x, y) => y - x) as [number, number]
  return (hi + 0.05) / (lo + 0.05)
}

const WASHES = [
  'rosewater',
  'flamingo',
  'pink',
  'mauve',
  'red',
  'maroon',
  'peach',
  'yellow',
  'green',
  'teal',
  'sky',
  'sapphire',
  'blue',
  'lavender',
]

// Every ink is tested on every ground, not just on paper: `html` is painted with desk, so text outside a
// sheet — and all text until the kit's sheets exist — sits on desk or desk-2. Tuning an ink against paper
// alone leaves it failing on the ground it is most often read against.
const INKS = ['ink', 'ink-soft', 'danger-ink', 'success-ink', 'warning-ink', 'rating-ink', 'link']
const GROUNDS = ['paper', 'paper-2', 'desk', 'desk-2']

const TEXT_PAIRS: [string, string][] = [
  ...INKS.flatMap((ink) => GROUNDS.map((ground): [string, string] => [ink, ground])),
  ['marker-ink', 'marker'],
  ...WASHES.map((wash): [string, string] => ['ink-on-wash', wash]),
]

// The focus ring sits 2px outside its element, on the surrounding ground.
const UI_PAIRS: [string, string][] = [
  ['ink-line', 'paper'],
  ['ink-line', 'desk'],
  ['focus', 'paper'],
  ['focus', 'desk'],
]

describe.each<[string, Tokens]>([
  ['dark', dark],
  ['light', light],
])('%s ramp', (_name, tokens) => {
  it.each(TEXT_PAIRS)('%s on %s reads at AA for text (≥ 4.5:1)', (fg, bg) => {
    expect(contrast(tokens, fg, bg)).toBeGreaterThanOrEqual(4.5)
  })

  it.each(UI_PAIRS)('%s against %s reads at AA for UI (≥ 3:1)', (fg, bg) => {
    expect(contrast(tokens, fg, bg)).toBeGreaterThanOrEqual(3)
  })
})
