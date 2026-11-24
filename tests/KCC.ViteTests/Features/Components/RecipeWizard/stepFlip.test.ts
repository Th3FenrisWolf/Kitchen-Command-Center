import { describe, expect, it } from 'vitest'
import { failedStripSheet, stepFlips } from '~/Components/RecipeWizard/stepFlip'
import { listTearFor } from '~/Utilities/BrandColor'

const COUNTED_STEPS = [1, 2, 3, 4, 5]

describe('stepFlips', () => {
  it.each(COUNTED_STEPS)('turns step %i over exactly when its tear is odd', (step) => {
    expect(stepFlips(step)).toBe(listTearFor(step - 1) % 2 === 1)
  })

  it('turns the odd-torn steps over and leaves the even-torn ones', () => {
    expect(COUNTED_STEPS.map((step) => stepFlips(step))).toEqual([true, false, true, false, true])
  })

  it('turns a step the other way while the failed photo strip lies above it', () => {
    expect(COUNTED_STEPS.map((step) => stepFlips(step, true))).toEqual([false, true, false, true, false])
  })
})

const blankTurn = (tear: number, flip: boolean) => (tear % 2 === 1 ? -1 : 1) * (flip ? -1 : 1)

describe('failedStripSheet', () => {
  it.each(COUNTED_STEPS)("takes a tear other than step %i's", (step) => {
    expect(failedStripSheet(step).tear).not.toBe(listTearFor(step - 1))
  })

  it.each(COUNTED_STEPS)('turns its blank against the hero above it and step %i below it', (step) => {
    const strip = failedStripSheet(step)
    const heroTurn = -1

    expect(blankTurn(strip.tear, strip.flip)).toBe(-heroTurn)
    expect(blankTurn(listTearFor(step - 1), stepFlips(step, true))).toBe(heroTurn)
  })
})
