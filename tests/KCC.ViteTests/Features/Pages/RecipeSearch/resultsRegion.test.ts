import { describe, expect, it } from 'vitest'
import { resultsRegionFlips } from '~/Pages/RecipeSearch/resultsRegion'

describe('resultsRegionFlips', () => {
  it('turns the results over while the phone hides the filters between them and the header', () => {
    expect(resultsRegionFlips(false)).toBe(true)
  })

  it('leaves the results as they are once the filters open', () => {
    expect(resultsRegionFlips(true)).toBe(false)
  })
})
