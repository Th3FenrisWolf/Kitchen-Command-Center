import { describe, expect, it } from 'vitest'
import { reviewsTrailingSign } from '~/Components/VariantDetail/reviewsTrailingSign'

describe('reviewsTrailingSign', () => {
  describe('signed out', () => {
    const signedOut = (count: number, loaded: number) => reviewsTrailingSign({ isAuthenticated: false, count, loaded })

    it('renders no sheet with no reviews', () => {
      expect(signedOut(0, 0)).toBeNull()
    })

    it('ends on the summary, turning -, before any review loads', () => {
      expect(signedOut(3, 0)).toBe(-1)
    })

    it('ends on the last loaded review, the list turning + - + from the first', () => {
      expect([1, 2, 3, 4, 10, 11, 12].map((loaded) => signedOut(12, loaded))).toEqual([1, -1, 1, -1, -1, 1, -1])
    })
  })

  describe('signed in, ending on the form that opposes the sheet before it', () => {
    const signedIn = (count: number, loaded: number) => reviewsTrailingSign({ isAuthenticated: true, count, loaded })

    it('turns - over cook notes when there are no reviews', () => {
      expect(signedIn(0, 0)).toBe(-1)
    })

    it('turns + under the summary before any review loads', () => {
      expect(signedIn(3, 0)).toBe(1)
    })

    it('turns against the last loaded review', () => {
      expect([1, 2, 3, 4, 10, 11, 12].map((loaded) => signedIn(12, loaded))).toEqual([-1, 1, -1, 1, 1, -1, 1])
    })
  })
})
