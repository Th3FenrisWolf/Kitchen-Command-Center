// Brand accent tokens. Keep in sync with the `bg-{…}` safelist and `--color-*`
// definitions in Features/Styles/TailwindConfig.css (the two files cannot import
// one another).
export const BRAND_COLORS = [
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
] as const

export type BrandColor = (typeof BRAND_COLORS)[number]

/** Deterministically map any text to one of the brand accent tokens. */
export function brandColorFor(text: string): BrandColor {
  let hash = 0
  for (let i = 0; i < text.length; i++) {
    hash = (hash * 31 + text.charCodeAt(i)) >>> 0
  }
  return BRAND_COLORS[hash % BRAND_COLORS.length]!
}
