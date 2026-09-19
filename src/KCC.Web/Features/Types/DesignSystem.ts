/** The eight washes of the identity, without their `bg-` prefix. Fills only. */
export const WASHES = ['peach', 'yellow', 'green', 'teal', 'sky', 'lavender', 'pink', 'red'] as const

export type Wash = (typeof WASHES)[number]

/** `bg-peach` → `peach`; grounds map to `undefined` (no wash). */
export function washOf(background: BackgroundColor): Wash | undefined {
  const name = background.slice(3)
  return (WASHES as readonly string[]).includes(name) ? (name as Wash) : undefined
}

export const BRAND_BACKGROUND_COLORS = [
  'bg-rosewater',
  'bg-flamingo',
  'bg-pink',
  'bg-mauve',
  'bg-red',
  'bg-maroon',
  'bg-peach',
  'bg-yellow',
  'bg-green',
  'bg-teal',
  'bg-sky',
  'bg-sapphire',
  'bg-blue',
  'bg-lavender',
] as const

export type BrandBackgroundColor = (typeof BRAND_BACKGROUND_COLORS)[number]

// Grounds and inks of the sketch language. Both axes list them so a caller can move a colour between
// background and text with the index helpers below.
export const SURFACE_BACKGROUND_COLORS = [
  'bg-desk',
  'bg-desk-2',
  'bg-paper',
  'bg-paper-2',
  'bg-ink',
  'bg-ink-soft',
  'bg-ink-on-wash',
  'bg-marker',
  'bg-marker-ink',
] as const

export const BACKGROUND_COLORS = [...BRAND_BACKGROUND_COLORS, ...SURFACE_BACKGROUND_COLORS] as const

export type BackgroundColor = (typeof BACKGROUND_COLORS)[number]

export const BRAND_TEXT_COLORS = [
  'text-rosewater',
  'text-flamingo',
  'text-pink',
  'text-mauve',
  'text-red',
  'text-maroon',
  'text-peach',
  'text-yellow',
  'text-green',
  'text-teal',
  'text-sky',
  'text-sapphire',
  'text-blue',
  'text-lavender',
] as const

export type BrandTextColor = (typeof BRAND_TEXT_COLORS)[number]

export const SURFACE_TEXT_COLORS = [
  'text-desk',
  'text-desk-2',
  'text-paper',
  'text-paper-2',
  'text-ink',
  'text-ink-soft',
  'text-ink-on-wash',
  'text-marker',
  'text-marker-ink',
] as const

export const TEXT_COLORS = [...BRAND_TEXT_COLORS, ...SURFACE_TEXT_COLORS] as const

export type TextColor = (typeof TEXT_COLORS)[number]

// BACKGROUND_COLORS and TEXT_COLORS name the same tokens in the same order, so moving a color
// between the two axes is an index lookup. Crossing the axes by hand fails silently instead — a
// `text-*` class sets no background, and a `bg-*` class no color.
export function toBackgroundColor(textColor: TextColor): BackgroundColor {
  return BACKGROUND_COLORS[TEXT_COLORS.indexOf(textColor)]!
}

export function toTextColor(backgroundColor: BackgroundColor): TextColor {
  return TEXT_COLORS[BACKGROUND_COLORS.indexOf(backgroundColor)]!
}
