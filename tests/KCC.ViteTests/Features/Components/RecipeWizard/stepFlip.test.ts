import { describe, expect, it } from 'vitest'
import { stepFlips } from '~/Components/RecipeWizard/stepFlip'
import { listTearFor } from '~/Utilities/BrandColor'

const COUNTED_STEPS = [1, 2, 3, 4, 5]

describe('stepFlips', () => {
  it.each(COUNTED_STEPS)('turns step %i over exactly when its tear is odd', (step) => {
    expect(stepFlips(step)).toBe(listTearFor(step - 1) % 2 === 1)
  })

  it('turns the odd-torn steps over and leaves the even-torn ones', () => {
    expect(COUNTED_STEPS.map(stepFlips)).toEqual([true, false, true, false, true])
  })
})
