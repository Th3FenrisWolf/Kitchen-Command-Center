export const MIN_RATING = 0.5

export function clampRating(value: number, max = 5): number {
  const half = Math.round(value * 2) / 2
  return Math.min(max, Math.max(MIN_RATING, half))
}

export function stepRating(current: number, delta: number, max = 5): number {
  return clampRating((current || 0) + delta, max)
}
