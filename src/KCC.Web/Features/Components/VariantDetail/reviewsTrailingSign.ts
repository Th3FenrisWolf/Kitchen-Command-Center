import type { Tear } from '~/Components/Sheet/KccSheet.vue'
import { listTearFor } from '~/Utilities/BrandColor'

export type Turn = 1 | -1

export interface ReviewsSection {
  isAuthenticated: boolean
  count: number
  loaded: number
}

const COOK_NOTES: Turn = 1
const SUMMARY: Turn = -1
export const SUMMARY_TEAR = 5
const FIRST_SIBLING_TEAR = listTearFor(0)

export const tearTurn = (tear: number): Turn => (tear % 2 === 1 ? -1 : 1)

const flipped = (turn: Turn): Turn => (turn === 1 ? -1 : 1)

export function reviewsTrailingSign({ isAuthenticated, count, loaded }: ReviewsSection): Turn | null {
  const lastShown = loaded > 0 ? flipped(tearTurn(listTearFor(loaded - 1))) : count > 0 ? SUMMARY : null
  if (!isAuthenticated) return lastShown
  return flipped(lastShown ?? COOK_NOTES)
}

// The form sits under the last review slip and above the first sibling card, which is always a 1: a 2
// clears both, and a 3 clears the one case where the slip above it is itself a 2.
export const formTearAfter = (loaded: number): Exclude<Tear, 'hero'> => (loaded && listTearFor(loaded - 1) === 2 ? 3 : 2)

export function reviewsTrailingTear({ isAuthenticated, count, loaded }: ReviewsSection): number | null {
  if (isAuthenticated) return formTearAfter(loaded)
  return loaded > 0 ? listTearFor(loaded - 1) : count > 0 ? SUMMARY_TEAR : null
}

// Two on keeps every card's parity, so each card keeps its turn and the flip still holds.
export const siblingTearOffset = (trailingTear: number | null) => (trailingTear === FIRST_SIBLING_TEAR ? 2 : 0)
