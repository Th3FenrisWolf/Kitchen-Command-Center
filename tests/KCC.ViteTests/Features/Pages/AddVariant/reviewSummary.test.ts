import { describe, expect, it } from 'vitest'
import type { Ingredient, Instruction } from '~/Types/Recipe'
import { stepLabelKey, validIngredients, validInstructions } from '~/Pages/AddVariant/reviewSummary'

const ing = (over: Partial<Ingredient> = {}): Ingredient => ({ name: '', unit: '', isEyeballed: false, ...over })
const inst = (over: Partial<Instruction> = {}): Instruction => ({ text: '', ...over })

describe('reviewSummary', () => {
  describe('validIngredients', () => {
    it('keeps only ingredients whose name is not blank', () => {
      const list = [ing({ name: 'flour' }), ing({ name: '   ' }), ing({ name: '' }), ing({ name: 'salt', isEyeballed: true })]
      expect(validIngredients(list).map((i) => i.name)).toEqual(['flour', 'salt'])
    })
  })

  describe('validInstructions', () => {
    it('keeps only instructions whose text is not blank', () => {
      const list = [inst({ text: 'Boil the pasta' }), inst({ text: '   ' }), inst({ text: '' })]
      expect(validInstructions(list).map((i) => i.text)).toEqual(['Boil the pasta'])
    })
  })

  describe('stepLabelKey', () => {
    it('returns the singular key for exactly one step', () => {
      expect(stepLabelKey(1)).toBe('Step')
    })

    it('returns the plural key for zero or many steps', () => {
      expect(stepLabelKey(0)).toBe('Steps')
      expect(stepLabelKey(4)).toBe('Steps')
    })
  })
})
