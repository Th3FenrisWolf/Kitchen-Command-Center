export const WASHES = ['peach', 'yellow', 'green', 'teal', 'sky', 'lavender', 'pink', 'red'] as const
export type Wash = (typeof WASHES)[number]

/** Wash fills, as utilities. Fills only: no wash is ever a text colour. */
export const BRAND_BACKGROUND_COLORS = WASHES.map((wash) => `bg-${wash}` as const) as readonly `bg-${Wash}`[]
export type BrandBackgroundColor = `bg-${Wash}`

export const GROUND_BACKGROUND_COLORS = ['bg-desk', 'bg-desk-2', 'bg-paper', 'bg-paper-2', 'bg-marker'] as const
export const BACKGROUND_COLORS = [...BRAND_BACKGROUND_COLORS, ...GROUND_BACKGROUND_COLORS] as const
export type BackgroundColor = (typeof BACKGROUND_COLORS)[number]

/** The only text colours: ink on paper and desk, marker-ink inside a marker fill or a wash tile. */
export const TEXT_COLORS = ['text-ink', 'text-ink-soft', 'text-marker-ink'] as const
export type TextColor = (typeof TEXT_COLORS)[number]

/** `bg-peach` → `peach`; grounds map to `undefined` (no wash). */
export function washOf(background: BackgroundColor): Wash | undefined {
  const name = background.slice(3)
  return (WASHES as readonly string[]).includes(name) ? (name as Wash) : undefined
}
