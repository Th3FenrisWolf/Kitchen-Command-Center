import { WASHES, type Wash } from '~/Types/DesignSystem'
import type { Tear } from '~/Components/Sheet/KccSheet.vue'

// Preset counts, matching SHEET_TEARS (minus the hero) and TILE_TEARS in Features/Torn/tears.ts; kept as
// literals so the runtime bundle does not pull the generator in.
const SHEET_TEAR_COUNT = 6
const TILE_TEAR_COUNT = 3

function colorIndexFor(text: string, paletteSize: number): number {
  let hash = 0
  for (let i = 0; i < text.length; i++) {
    hash = (hash * 31 + text.charCodeAt(i)) >>> 0
  }

  return hash % paletteSize
}

export function washFor(text: string): Wash {
  return WASHES[colorIndexFor(text, WASHES.length)]!
}

export function sheetTearFor(text: string): Exclude<Tear, 'hero'> {
  return (colorIndexFor(`sheet:${text}`, SHEET_TEAR_COUNT) + 1) as Exclude<Tear, 'hero'>
}

export function listTearFor(index: number): Exclude<Tear, 'hero'> {
  return ((index % SHEET_TEAR_COUNT) + 1) as Exclude<Tear, 'hero'>
}

export function tileTearFor(text: string): 1 | 2 | 3 {
  return (colorIndexFor(`tile:${text}`, TILE_TEAR_COUNT) + 1) as 1 | 2 | 3
}
