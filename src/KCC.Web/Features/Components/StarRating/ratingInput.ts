/** Lowest selectable rating (a single half-star). */
export const MIN_RATING = 0.5

/** Round to the nearest half star and clamp into [MIN_RATING, max]. */
export function clampRating(value: number, max = 5): number {
  const half = Math.round(value * 2) / 2
  return Math.min(max, Math.max(MIN_RATING, half))
}

/** Adjust an active rating by delta (e.g. ±0.5 for arrow keys), clamped to [MIN_RATING, max]. */
export function stepRating(current: number, delta: number, max = 5): number {
  return clampRating((current || 0) + delta, max)
}
