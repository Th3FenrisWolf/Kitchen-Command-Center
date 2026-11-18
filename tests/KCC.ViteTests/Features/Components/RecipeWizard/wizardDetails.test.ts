import { describe, expect, it } from 'vitest'
import {
  NUTRITION_FIELDS,
  keepAvailable,
  nutritionAmount,
  nutritionLimit,
  nutritionRequest,
  nutritionState,
  orderTags,
  type DetailsChoices,
} from '~/Components/RecipeWizard/wizardDetails'

const CHOICES: DetailsChoices = {
  categories: [
    { key: 'cat-breakfast', name: 'Breakfast' },
    { key: 'cat-dinner', name: 'Dinner' },
  ],
  tags: [
    { key: 'tag-vegan', name: 'Vegan', kind: 'Diet' },
    { key: 'tag-spicy', name: 'Spicy', kind: 'Style' },
    { key: 'tag-keto', name: 'Keto', kind: 'Diet' },
  ],
}

describe('NUTRITION_FIELDS', () => {
  it('lists the eight aliases with their units and limits', () => {
    expect(NUTRITION_FIELDS.map(({ field, unit, limit }) => [field, unit, limit])).toEqual([
      ['calories', 'Kcal', 10000],
      ['proteinG', 'Grams', 1000],
      ['carbsG', 'Grams', 1000],
      ['fatG', 'Grams', 1000],
      ['saturatedFatG', 'Grams', 1000],
      ['fiberG', 'Grams', 1000],
      ['sugarG', 'Grams', 1000],
      ['sodiumMg', 'Milligrams', 50000],
    ])
  })

  it('names each field by its label key', () => {
    expect(NUTRITION_FIELDS.map(({ label }) => label)).toEqual([
      'Calories',
      'Protein',
      'Carbohydrates',
      'Fat',
      'SaturatedFat',
      'Fiber',
      'Sugar',
      'Sodium',
    ])
  })

  it.each([
    ['calories', 10000],
    ['fatG', 1000],
    ['sodiumMg', 50000],
  ] as const)('gives %s the limit %i', (field, limit) => {
    expect(nutritionLimit(field)).toBe(limit)
  })
})

describe('nutritionState', () => {
  it.each(['', '   ', '0', '450', ' 450 ', '10000'])('accepts %j for calories', (text) => {
    expect(nutritionState('calories', text)).toBe('valid')
  })

  it.each(['-1', '1.5', 'abc', '10001', '1e3', '+5', '4 5'])('rejects %j for calories', (text) => {
    expect(nutritionState('calories', text)).toBe('invalid')
  })

  it.each([
    ['proteinG', '1000', 'valid'],
    ['proteinG', '1001', 'invalid'],
    ['sodiumMg', '50000', 'valid'],
    ['sodiumMg', '50001', 'invalid'],
  ] as const)('holds %s to its limit: %s is %s', (field, text, state) => {
    expect(nutritionState(field, text)).toBe(state)
  })
})

describe('nutritionAmount and nutritionRequest', () => {
  it.each([
    ['', null],
    ['  ', null],
    ['0', 0],
    ['450', 450],
    [' 12 ', 12],
  ])('reads %j as %s', (text, amount) => {
    expect(nutritionAmount(text)).toBe(amount)
  })

  it('maps the texts to integers or null under the eight request keys', () => {
    expect(
      nutritionRequest({
        calories: '450',
        proteinG: '0',
        carbsG: '',
        fatG: ' 7 ',
        saturatedFatG: '',
        fiberG: '',
        sugarG: '',
        sodiumMg: '380',
      }),
    ).toEqual({
      calories: 450,
      proteinG: 0,
      carbsG: null,
      fatG: 7,
      saturatedFatG: null,
      fiberG: null,
      sugarG: null,
      sodiumMg: 380,
    })
  })
})

describe('orderTags', () => {
  it("keeps the chosen keys in the lists' order", () => {
    expect(orderTags(['tag-keto', 'tag-vegan'], CHOICES)).toEqual(['tag-vegan', 'tag-keto'])
  })

  it('drops a key the lists lack', () => {
    expect(orderTags(['tag-gone', 'tag-spicy'], CHOICES)).toEqual(['tag-spicy'])
  })
})

describe('keepAvailable', () => {
  it('keeps a category and tags the new lists still hold', () => {
    expect(keepAvailable({ category: 'cat-dinner', tags: ['tag-keto', 'tag-vegan'] }, CHOICES)).toEqual({
      category: 'cat-dinner',
      tags: ['tag-vegan', 'tag-keto'],
    })
  })

  it('drops a category and a tag the new lists lack', () => {
    expect(keepAvailable({ category: 'cat-gone', tags: ['tag-gone', 'tag-spicy'] }, CHOICES)).toEqual({
      category: null,
      tags: ['tag-spicy'],
    })
  })

  it('keeps no category when there is none', () => {
    expect(keepAvailable({ category: null, tags: [] }, CHOICES)).toEqual({ category: null, tags: [] })
  })
})
