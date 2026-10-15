import { describe, expect, it } from 'vitest'
import {
  emptyValues,
  recipeRequest,
  stepLabelKey,
  validIngredients,
  validInstructions,
  variantRequest,
  type Mark,
  type WizardIngredient,
  type WizardInstruction,
  type WizardValues,
} from '~/Components/RecipeWizard/wizardValues'

const UNCLEAR: Mark = { kind: 'unclear', reason: 'Printed as 2 to 3', code: null, arg: null }
const ESTIMATE: Mark = { kind: 'estimate', reason: 'Not printed', code: null, arg: null }
const TRANSLATED: Mark = { kind: 'translated', reason: null, code: 'translated', arg: 'Italian' }

const ing = (over: Partial<WizardIngredient> = {}): WizardIngredient => ({ name: '', unit: '', isEyeballed: false, ...over })
const inst = (over: Partial<WizardInstruction> = {}): WizardInstruction => ({ text: '', ...over })

const values = (over: Partial<WizardValues> = {}): WizardValues => ({ ...emptyValues(), ...over })

const pancakes = () =>
  values({
    recipeName: '  Pancakes ',
    recipeDescription: ' Fluffy weekend pancakes. ',
    variantName: ' Buttermilk ',
    variantDescription: ' Tangy and tall. ',
    prepTime: 10,
    cookTime: 15,
    servings: 4,
    ingredients: [
      ing({ name: 'flour', quantity: 1.5, unit: 'cups' }),
      ing({ name: '   ', quantity: 2, unit: 'cups' }),
      ing({ name: 'garlic', quantity: 2, unit: 'cloves', mark: UNCLEAR }),
      ing({ name: 'salt', isEyeballed: true }),
    ],
    instructions: [
      inst({ text: ' Whisk the dry ingredients. ' }),
      inst({ text: '  ' }),
      inst({ text: 'Fry.', mark: ESTIMATE }),
    ],
    marks: { 'recipe.name': TRANSLATED, 'variant.prepTime': ESTIMATE },
  })

const PANCAKES_VARIANT = {
  variantName: 'Buttermilk',
  variantDescription: 'Tangy and tall.',
  prepTime: 10,
  cookTime: 15,
  servings: 4,
  ingredients: [
    { name: 'flour', quantity: 1.5, unit: 'cups', isEyeballed: false },
    { name: 'garlic', quantity: 2, unit: 'cloves', isEyeballed: false },
    { name: 'salt', quantity: null, unit: '', isEyeballed: true },
  ],
  instructions: [
    { step: 1, text: 'Whisk the dry ingredients.' },
    { step: 2, text: 'Fry.' },
  ],
}

const onTheWire = (body: unknown) => JSON.parse(JSON.stringify(body))

describe('wizardValues', () => {
  it('starts as both wizards start today: blank text, zero numbers, one blank ingredient and one blank step', () => {
    expect(emptyValues()).toEqual({
      recipeName: '',
      recipeDescription: '',
      variantName: '',
      variantDescription: '',
      prepTime: 0,
      cookTime: 0,
      servings: 0,
      ingredients: [{ name: '', unit: '', isEyeballed: false }],
      instructions: [{ text: '' }],
      marks: {},
    })
  })

  it('gives each wizard rows of its own', () => {
    const first = emptyValues()

    first.ingredients.push(ing({ name: 'flour' }))
    first.instructions.push(inst({ text: 'Whisk.' }))

    expect(emptyValues().ingredients).toHaveLength(1)
    expect(emptyValues().instructions).toHaveLength(1)
  })

  it('maps a recipe to the body of POST /api/recipes', () => {
    expect(onTheWire(recipeRequest(pancakes()))).toEqual({
      recipeName: 'Pancakes',
      recipeDescription: 'Fluffy weekend pancakes.',
      firstVariant: PANCAKES_VARIANT,
    })
  })

  it('maps a variant to the body of POST /api/recipes/{key}/variants', () => {
    expect(onTheWire(variantRequest(pancakes()))).toEqual(PANCAKES_VARIANT)
  })

  it('drops blank rows and numbers the steps that remain from 1', () => {
    const body = variantRequest(
      values({
        ingredients: [ing(), ing({ name: 'eggs', quantity: 2 }), ing({ name: ' ' })],
        instructions: [inst({ text: ' ' }), inst({ text: 'Crack.' }), inst(), inst({ text: 'Scramble gently.' })],
      }),
    )

    expect(onTheWire(body.ingredients)).toEqual([{ name: 'eggs', quantity: 2, unit: '', isEyeballed: false }])
    expect(body.instructions).toEqual([
      { step: 1, text: 'Crack.' },
      { step: 2, text: 'Scramble gently.' },
    ])
  })

  describe('sends a zero or empty number as null', () => {
    const clearedInput = '' as unknown as number
    const blankNumbers = () =>
      values({
        prepTime: 0,
        cookTime: undefined,
        servings: 0,
        ingredients: [
          ing({ name: 'flour', quantity: clearedInput, unit: 'cups' }),
          ing({ name: 'sugar', quantity: 0, unit: 'tbsp' }),
          ing({ name: 'salt', quantity: clearedInput, isEyeballed: true }),
        ],
      })
    const sentAsNull = {
      prepTime: null,
      cookTime: null,
      servings: null,
      ingredients: [
        { name: 'flour', quantity: null, unit: 'cups', isEyeballed: false },
        { name: 'sugar', quantity: null, unit: 'tbsp', isEyeballed: false },
        { name: 'salt', quantity: null, unit: '', isEyeballed: true },
      ],
    }

    it('in the variant body', () => {
      expect(onTheWire(variantRequest(blankNumbers()))).toMatchObject(sentAsNull)
    })

    it('in the first variant of the recipe body', () => {
      expect(onTheWire(recipeRequest(blankNumbers()))).toMatchObject({ firstVariant: sentAsNull })
    })
  })

  it('keeps every mark out of both bodies', () => {
    const wire = [JSON.stringify(recipeRequest(pancakes())), JSON.stringify(variantRequest(pancakes()))]

    wire.forEach((body) => expect(body).not.toMatch(/"marks?"/))
  })

  describe('validIngredients', () => {
    it('keeps only ingredients whose name is not blank', () => {
      const list = [
        ing({ name: 'flour' }),
        ing({ name: '   ' }),
        ing({ name: '' }),
        ing({ name: 'salt', isEyeballed: true }),
      ]
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
