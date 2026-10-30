import { describe, expect, it } from 'vitest'
import { reviewsTrailingSign, reviewsTrailingTear, siblingTearOffset } from '~/Components/VariantDetail/reviewsTrailingSign'

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

describe('reviewsTrailingTear', () => {
  describe('signed out', () => {
    const signedOut = (count: number, loaded: number) => reviewsTrailingTear({ isAuthenticated: false, count, loaded })

    it('renders no sheet with no reviews', () => {
      expect(signedOut(0, 0)).toBeNull()
    })

    it('ends on the summary’s 5 before any review loads', () => {
      expect(signedOut(3, 0)).toBe(5)
    })

    it('ends on the last loaded review’s tear, cycling from 1', () => {
      expect([1, 2, 3, 4, 5, 6, 7].map((loaded) => signedOut(12, loaded))).toEqual([1, 2, 3, 4, 5, 6, 1])
    })
  })

  describe('signed in, ending on the form', () => {
    const signedIn = (count: number, loaded: number) => reviewsTrailingTear({ isAuthenticated: true, count, loaded })

    it('takes a 2, clear of the first sibling’s 1', () => {
      expect([signedIn(0, 0), signedIn(3, 0), signedIn(12, 1), signedIn(12, 3), signedIn(12, 7)]).toEqual([2, 2, 2, 2, 2])
    })

    it('takes a 3 when the last loaded review is itself a 2', () => {
      expect([signedIn(12, 2), signedIn(12, 8)]).toEqual([3, 3])
    })
  })
})

describe('siblingTearOffset', () => {
  it('moves the siblings two on only when the reviews end on a 1, the first sibling’s own tear', () => {
    expect([null, 1, 2, 3, 4, 5, 6].map(siblingTearOffset)).toEqual([0, 2, 0, 0, 0, 0, 0])
  })
})
