import { describe, expect, it } from 'vitest'
import { reviewsTrailingSign, reviewsTrailingTear, siblingTearOffset } from '~/Components/VariantDetail/reviewsTrailingSign'

describe('reviewsTrailingSign', () => {
  describe('signed out', () => {
    const signedOut = (loaded: number) => reviewsTrailingSign({ isAuthenticated: false, loaded })

    it('ends on the summary or the empty sheet in its place, turning -, before any review loads', () => {
      expect(signedOut(0)).toBe(-1)
    })

    it('ends on the last loaded review, the list turning + - + from the first', () => {
      expect([1, 2, 3, 4, 10, 11, 12].map(signedOut)).toEqual([1, -1, 1, -1, -1, 1, -1])
    })
  })

  describe('signed in, ending on the form that opposes the sheet before it', () => {
    const signedIn = (loaded: number) => reviewsTrailingSign({ isAuthenticated: true, loaded })

    it('turns + under the summary or the empty sheet before any review loads', () => {
      expect(signedIn(0)).toBe(1)
    })

    it('turns against the last loaded review', () => {
      expect([1, 2, 3, 4, 10, 11, 12].map(signedIn)).toEqual([-1, 1, -1, 1, 1, -1, 1])
    })
  })
})

describe('reviewsTrailingTear', () => {
  describe('signed out', () => {
    const signedOut = (loaded: number) => reviewsTrailingTear({ isAuthenticated: false, loaded })

    it('ends on the 5 of the summary or the empty sheet before any review loads', () => {
      expect(signedOut(0)).toBe(5)
    })

    it('ends on the last loaded review’s tear, cycling from 1', () => {
      expect([1, 2, 3, 4, 5, 6, 7].map(signedOut)).toEqual([1, 2, 3, 4, 5, 6, 1])
    })
  })

  describe('signed in, ending on the form', () => {
    const signedIn = (loaded: number) => reviewsTrailingTear({ isAuthenticated: true, loaded })

    it('takes a 2, clear of the first sibling’s 1', () => {
      expect([0, 1, 3, 7].map(signedIn)).toEqual([2, 2, 2, 2])
    })

    it('takes a 3 when the last loaded review is itself a 2', () => {
      expect([2, 8].map(signedIn)).toEqual([3, 3])
    })
  })
})

describe('siblingTearOffset', () => {
  it('moves the siblings two on only when the reviews end on a 1, the first sibling’s own tear', () => {
    expect([1, 2, 3, 4, 5, 6].map(siblingTearOffset)).toEqual([2, 0, 0, 0, 0, 0])
  })
})
