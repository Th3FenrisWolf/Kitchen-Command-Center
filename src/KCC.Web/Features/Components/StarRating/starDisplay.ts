/** Visual state of a single star in a rating display. */
export type StarState = 'full' | 'half' | 'empty'

/** Per-star fill states for a rating value, rounded to the nearest half star and clamped to [0, max]. */
export function starStates(value: number, max: number = 5): StarState[] {
  const clamped = Math.max(0, Math.min(max, value ?? 0))
  const halves = Math.round(clamped * 2)
  return Array.from({ length: max }, (_, i) => {
    const filledHalves = halves - i * 2
    if (filledHalves >= 2) return 'full'
    if (filledHalves === 1) return 'half'
    return 'empty'
  })
}

/** One-decimal rating label, e.g. 4 -> "4.0". */
export function formatRating(value: number): string {
  return (value ?? 0).toFixed(1)
}
