import { BRAND_BACKGROUND_COLORS, type BrandBackgroundColor } from '~/Types/DesignSystem'

function colorIndexFor(text: string, paletteSize: number): number {
  let hash = 0
  for (let i = 0; i < text.length; i++) {
    hash = (hash * 31 + text.charCodeAt(i)) >>> 0
  }

  return hash % paletteSize
}

/** Deterministically map any text to one of the brand accent tokens. */
export function backgroundColorFor(text: string): BrandBackgroundColor {
  return BRAND_BACKGROUND_COLORS[colorIndexFor(text, BRAND_BACKGROUND_COLORS.length)]!
}
