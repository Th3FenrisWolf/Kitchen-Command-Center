import { describe, expect, it } from 'vitest'
import {
  formatIngredientSummary,
  formatQuantity,
  isIngredientValid,
  isInstructionValid,
  normalizeIngredient,
  parseItems,
  reconcileIncomingValue,
  serializeItems,
  stampSteps,
} from './helpers.js'
import type { Ingredient, Instruction } from './types.js'

describe('parseItems', () => {
  it('reads nothing from null, empty or blank', () => {
    expect(parseItems(null)).toEqual({ items: [], error: false })
    expect(parseItems('')).toEqual({ items: [], error: false })
    expect(parseItems('   ')).toEqual({ items: [], error: false })
  })

  it('reads a JSON array', () => {
    const result = parseItems<Ingredient>('[{"name":"Bacon","quantity":3,"unit":"Slices","isEyeballed":false}]')
    expect(result.error).toBe(false)
    expect(result.items[0].name).toBe('Bacon')
  })

  it('flags invalid JSON, and JSON that is not an array', () => {
    expect(parseItems('{not json')).toEqual({ items: [], error: true })
    expect(parseItems('{"a":1}')).toEqual({ items: [], error: true })
  })
})

describe('serializeItems', () => {
  it('turns an empty list into no value, so a mandatory property fails validation', () => {
    expect(serializeItems([])).toBeUndefined()
  })

  it('writes a list as compact JSON', () => {
    const items: Instruction[] = [{ step: 1, text: 'Boil water.' }]
    expect(serializeItems(items)).toBe('[{"step":1,"text":"Boil water."}]')
  })
})

describe('reconcileIncomingValue', () => {
  const json = '[{"step":1,"text":"Boil water."}]'

  it('keeps the rows when the value is the one the editor emitted', () => {
    expect(reconcileIncomingValue(json, json)).toBeNull()
    expect(reconcileIncomingValue(undefined, undefined)).toBeNull()
  })

  it('replaces the rows with any other value', () => {
    expect(reconcileIncomingValue<Instruction>(json, '[{"step":1,"text":"edited"}]')).toEqual({
      items: [{ step: 1, text: 'Boil water.' }],
      error: false,
    })
    expect(reconcileIncomingValue(undefined, json)).toEqual({ items: [], error: false })
    expect(reconcileIncomingValue('{not json', json)).toEqual({ items: [], error: true })
  })

  it('reads a value that arrives after the editor started', () => {
    expect(reconcileIncomingValue<Instruction>(json, undefined)?.items).toEqual([{ step: 1, text: 'Boil water.' }])
  })
})

describe('normalizeIngredient', () => {
  it('clears the quantity and unit of an eyeballed ingredient', () => {
    expect(normalizeIngredient({ name: 'Salt', quantity: 2, unit: 'Pinch', isEyeballed: true })).toEqual({
      name: 'Salt',
      quantity: null,
      unit: '',
      isEyeballed: true,
    })
  })

  it('leaves a measured ingredient alone', () => {
    const item: Ingredient = { name: 'Bacon', quantity: 3, unit: 'Slices', isEyeballed: false }
    expect(normalizeIngredient(item)).toEqual(item)
  })
})

describe('stampSteps', () => {
  it('numbers the steps by position', () => {
    expect(stampSteps([{ step: 9, text: 'b' }, { step: 4, text: 'a' }])).toEqual([
      { step: 1, text: 'b' },
      { step: 2, text: 'a' },
    ])
  })
})

describe('validation', () => {
  it('needs an ingredient name and a step text', () => {
    expect(isIngredientValid({ name: 'Bacon', quantity: 1, unit: '', isEyeballed: false })).toBe(true)
    expect(isIngredientValid({ name: '  ', quantity: 1, unit: '', isEyeballed: false })).toBe(false)
    expect(isInstructionValid({ step: 1, text: 'Stir.' })).toBe(true)
    expect(isInstructionValid({ step: 1, text: ' ' })).toBe(false)
  })
})

describe('formatQuantity', () => {
  it('writes whole numbers, common fractions as glyphs, and anything else as a decimal', () => {
    expect(formatQuantity(3)).toBe('3')
    expect(formatQuantity(0.5)).toBe('½')
    expect(formatQuantity(1.5)).toBe('1½')
    expect(formatQuantity(0.4)).toBe('0.4')
  })
})

describe('formatIngredientSummary', () => {
  it('writes a measured, an eyeballed and a unitless ingredient', () => {
    expect(formatIngredientSummary({ name: 'Bacon', quantity: 3, unit: 'Slices', isEyeballed: false })).toBe('3 Slices Bacon')
    expect(formatIngredientSummary({ name: 'Salt', quantity: null, unit: '', isEyeballed: true })).toBe('Salt — to taste')
    expect(formatIngredientSummary({ name: 'Eggs', quantity: 4, unit: '', isEyeballed: false })).toBe('4 Eggs')
  })
})
