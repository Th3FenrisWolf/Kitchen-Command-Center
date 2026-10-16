import { listTearFor } from '~/Utilities/BrandColor'

export type Turn = 1 | -1

export interface ReviewsSection {
  isAuthenticated: boolean
  count: number
  loaded: number
}

const COOK_NOTES: Turn = 1
const SUMMARY: Turn = -1

export const tearTurn = (tear: number): Turn => (tear % 2 === 1 ? -1 : 1)

const flipped = (turn: Turn): Turn => (turn === 1 ? -1 : 1)

export function reviewsTrailingSign({ isAuthenticated, count, loaded }: ReviewsSection): Turn | null {
  const lastShown = loaded > 0 ? flipped(tearTurn(listTearFor(loaded - 1))) : count > 0 ? SUMMARY : null
  if (!isAuthenticated) return lastShown
  return flipped(lastShown ?? COOK_NOTES)
}
