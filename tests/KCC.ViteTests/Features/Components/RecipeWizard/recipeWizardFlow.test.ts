import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { EncodedPhoto } from '~/Components/RecipeWizard/photoEncoding'
import type { ReadResponse, ReadResult } from '~/Components/RecipeWizard/recipeImportApi'
import {
  acceptMatch,
  addIngredient,
  addInstruction,
  addPendingPhotos,
  addPhotos,
  applyRead,
  back,
  backToReview,
  canGoBack,
  canProceed,
  canStartRead,
  changeMatch,
  changePhotos,
  choosePhotos,
  chooseTyping,
  createWizardFlow,
  edit,
  editIngredient,
  editInstruction,
  editValue,
  finishRead,
  hasUnsavedWork,
  isImport,
  keepEdits,
  keepNewRecipe,
  next,
  offersMatch,
  pickRecipe,
  progress,
  removeIngredient,
  removeInstruction,
  removePhoto,
  showsPhotoStrip,
  startRead,
  stepOrder,
  submissionFor,
  type StepId,
  type WizardFlow,
} from '~/Components/RecipeWizard/recipeWizardFlow'
import { validIngredients, validInstructions, type Mark, type WizardValues } from '~/Components/RecipeWizard/wizardValues'

const GNOCCHI = { key: '11111111-2222-3333-4444-555555555555', name: 'Brown Butter Gnocchi' }

const recipeWizard = (importEnabled: boolean) => createWizardFlow({ flow: 'recipe', importEnabled })
const variantWizard = (importEnabled: boolean) => createWizardFlow({ flow: 'variant', recipe: GNOCCHI, importEnabled })

const filled = (wizard: WizardFlow) => {
  Object.assign(wizard.values, {
    recipeName: 'Pancakes',
    recipeDescription: 'Fluffy weekend pancakes.',
    variantName: 'Buttermilk',
    ingredients: [{ name: 'flour', quantity: 2, unit: 'cups', isEyeballed: false }],
    instructions: [{ text: 'Whisk the dry ingredients.' }],
  })
  return wizard
}

const on = (wizard: WizardFlow, step: StepId) => {
  wizard.step = step
  return wizard
}

const stepsVisited = (wizard: WizardFlow, move: (wizard: WizardFlow) => void) => {
  const visited = [wizard.step]
  for (let moves = 0; moves < 10; moves++) {
    move(wizard)
    if (wizard.step === visited.at(-1)) break
    visited.push(wizard.step)
  }
  return visited
}

describe('recipeWizardFlow step order', () => {
  it('opens recipe mode on the start step while the import is enabled', () => {
    const wizard = recipeWizard(true)

    expect(wizard).toMatchObject({ flow: 'recipe', mode: 'recipe', recipe: null, start: null, step: 'start' })
    expect(stepOrder(wizard)).toEqual(['start', 'basics', 'variant', 'ingredients', 'instructions', 'review'])
  })

  it('opens recipe mode on the basics step, typing, when the import is not enabled', () => {
    const wizard = recipeWizard(false)

    expect(wizard).toMatchObject({ mode: 'recipe', start: 'typing', step: 'basics' })
    expect(stepOrder(wizard)).toEqual(['basics', 'variant', 'ingredients', 'instructions', 'review'])
  })

  it('opens variant mode for its recipe on the start step while the import is enabled', () => {
    const wizard = variantWizard(true)

    expect(wizard).toMatchObject({ flow: 'variant', mode: 'variant', recipe: GNOCCHI, start: null, step: 'start' })
    expect(stepOrder(wizard)).toEqual(['start', 'variant', 'ingredients', 'instructions', 'review'])
  })

  it('opens variant mode on the variant step, typing, when the import is not enabled', () => {
    const wizard = variantWizard(false)

    expect(wizard).toMatchObject({ mode: 'variant', recipe: GNOCCHI, start: 'typing', step: 'variant' })
    expect(stepOrder(wizard)).toEqual(['variant', 'ingredients', 'instructions', 'review'])
  })

  it.each([
    ['recipe', 'basics'],
    ['variant', 'variant'],
  ] as const)(
    'opens %s mode on the start step while the import is unavailable, and offers only typing there',
    (flow, firstTypingStep) => {
      const wizard = createWizardFlow({ flow, recipe: GNOCCHI, importEnabled: true, availableAgainOn: '2026-11-01' })

      expect(wizard).toMatchObject({ step: 'start', availableAgainOn: '2026-11-01' })
      choosePhotos(wizard)
      expect(wizard).toMatchObject({ start: null, step: 'start' })
      chooseTyping(wizard)
      expect(wizard).toMatchObject({ start: 'typing', step: firstTypingStep })
    },
  )

  it('holds no unavailable day while the import is available, or not enabled', () => {
    expect(recipeWizard(true).availableAgainOn).toBeNull()
    expect(
      createWizardFlow({ flow: 'recipe', importEnabled: false, availableAgainOn: '2026-11-01' }).availableAgainOn,
    ).toBeNull()
  })

  it('opens the first typing step of the mode when the member chooses to type it in', () => {
    const recipe = recipeWizard(true)
    const variant = variantWizard(true)

    chooseTyping(recipe)
    chooseTyping(variant)

    expect(recipe).toMatchObject({ start: 'typing', step: 'basics' })
    expect(variant).toMatchObject({ start: 'typing', step: 'variant' })
  })

  it('starts each wizard on values of its own', () => {
    const first = recipeWizard(false)
    const second = recipeWizard(false)

    first.values.ingredients[0]!.name = 'flour'

    expect(second.values.ingredients[0]!.name).toBe('')
  })
})

describe('recipeWizardFlow Next and Back', () => {
  it('walks recipe mode forward through each typing step to the review, and no further', () => {
    expect(stepsVisited(filled(recipeWizard(false)), next)).toEqual([
      'basics',
      'variant',
      'ingredients',
      'instructions',
      'review',
    ])
  })

  it('walks variant mode forward through each typing step to the review, and no further', () => {
    expect(stepsVisited(filled(variantWizard(false)), next)).toEqual(['variant', 'ingredients', 'instructions', 'review'])
  })

  it('walks back from the review to the start step while the import is enabled', () => {
    const recipe = on(filled(recipeWizard(true)), 'review')
    const variant = on(filled(variantWizard(true)), 'review')

    expect(stepsVisited(recipe, back)).toEqual(['review', 'instructions', 'ingredients', 'variant', 'basics', 'start'])
    expect(stepsVisited(variant, back)).toEqual(['review', 'instructions', 'ingredients', 'variant', 'start'])
  })

  it('stops at the first typing step when the import is not enabled', () => {
    const recipe = on(filled(recipeWizard(false)), 'review')
    const variant = on(filled(variantWizard(false)), 'review')

    expect(stepsVisited(recipe, back)).toEqual(['review', 'instructions', 'ingredients', 'variant', 'basics'])
    expect(stepsVisited(variant, back)).toEqual(['review', 'instructions', 'ingredients', 'variant'])
  })

  it('offers Back on every step but the first of the order', () => {
    const enabled = filled(recipeWizard(true))
    const notEnabled = filled(recipeWizard(false))

    expect(canGoBack(enabled)).toBe(false)
    expect(canGoBack(on(enabled, 'basics'))).toBe(true)
    expect(canGoBack(notEnabled)).toBe(false)
    expect(canGoBack(on(notEnabled, 'variant'))).toBe(true)
    expect(canGoBack(on(notEnabled, 'review'))).toBe(true)
  })

  it('holds Next on a step that is not filled in', () => {
    const wizard = recipeWizard(false)

    next(wizard)

    expect(wizard.step).toBe('basics')
  })

  it('leaves the values as they were on Next and Back', () => {
    const wizard = filled(recipeWizard(true))
    const before = structuredClone(wizard.values)

    chooseTyping(wizard)
    next(wizard)
    back(wizard)
    back(wizard)

    expect(wizard.values).toEqual(before)
  })
})

describe('recipeWizardFlow canProceed', () => {
  it.each([
    ['', '', false],
    ['Pancakes', '', false],
    ['', 'Fluffy weekend pancakes.', false],
    ['   ', 'Fluffy weekend pancakes.', false],
    ['Pancakes', '  ', false],
    ['Pancakes', 'Fluffy weekend pancakes.', true],
  ])('asks the basics step for a recipe name and a description: %j and %j give %s', (name, description, expected) => {
    const wizard = recipeWizard(false)
    Object.assign(wizard.values, { recipeName: name, recipeDescription: description })

    expect(canProceed(wizard)).toBe(expected)
  })

  it.each([
    ['', false],
    ['   ', false],
    ['Buttermilk', true],
  ])('asks the variant step for a variant name only: %j gives %s', (name, expected) => {
    const recipe = on(recipeWizard(false), 'variant')
    const variant = variantWizard(false)
    recipe.values.variantName = name
    variant.values.variantName = name

    expect(canProceed(recipe)).toBe(expected)
    expect(canProceed(variant)).toBe(expected)
  })

  it('asks the ingredients step for at least one named ingredient', () => {
    const wizard = on(recipeWizard(false), 'ingredients')

    wizard.values.ingredients = [{ name: '  ', quantity: 2, unit: 'cups', isEyeballed: false }]
    expect(canProceed(wizard)).toBe(false)

    wizard.values.ingredients.push({ name: 'flour', unit: '', isEyeballed: true })
    expect(canProceed(wizard)).toBe(true)
  })

  it('asks the instructions step for at least one step with text', () => {
    const wizard = on(recipeWizard(false), 'instructions')

    wizard.values.instructions = [{ text: '' }, { text: '   ' }]
    expect(canProceed(wizard)).toBe(false)

    wizard.values.instructions.push({ text: 'Whisk the dry ingredients.' })
    expect(canProceed(wizard)).toBe(true)
  })

  it('offers no Next on the start step or the review', () => {
    expect(canProceed(filled(recipeWizard(true)))).toBe(false)
    expect(canProceed(on(filled(recipeWizard(true)), 'review'))).toBe(false)
  })
})

describe('recipeWizardFlow progress', () => {
  it('counts five steps in recipe mode, on the editing steps and the review', () => {
    const wizard = recipeWizard(true)
    const steps: StepId[] = ['basics', 'variant', 'ingredients', 'instructions', 'review']

    expect(steps.map((step) => progress(on(wizard, step)))).toEqual([
      { current: 1, total: 5 },
      { current: 2, total: 5 },
      { current: 3, total: 5 },
      { current: 4, total: 5 },
      { current: 5, total: 5 },
    ])
  })

  it('counts four steps in variant mode, on the editing steps and the review', () => {
    const wizard = variantWizard(true)
    const steps: StepId[] = ['variant', 'ingredients', 'instructions', 'review']

    expect(steps.map((step) => progress(on(wizard, step)))).toEqual([
      { current: 1, total: 4 },
      { current: 2, total: 4 },
      { current: 3, total: 4 },
      { current: 4, total: 4 },
    ])
  })

  it.each(['start', 'photos', 'reading', 'match', 'submitted'] as StepId[])('shows no progress on the %s step', (step) => {
    expect(progress(on(recipeWizard(true), step))).toBeNull()
    expect(progress(on(variantWizard(true), step))).toBeNull()
  })
})

const ESTIMATE: Mark = { kind: 'estimate', reason: 'Not printed', code: null, arg: null }
const UNCLEAR: Mark = { kind: 'unclear', reason: 'Printed as 2 to 3', code: null, arg: null }
const WRITTEN: Mark = { kind: 'written', reason: 'Not in the photos', code: null, arg: null }
const TRANSLATED: Mark = { kind: 'translated', reason: null, code: 'translated', arg: 'Italian' }
const SHORTENED: Mark = { kind: 'unclear', reason: null, code: 'shortened', arg: null }

const PANCAKES: ReadResponse = {
  outcome: 'recipe',
  recipe: {
    name: { value: 'Pancakes', mark: TRANSLATED },
    description: { value: 'Fluffy weekend pancakes.', mark: WRITTEN },
  },
  variant: {
    name: { value: 'Buttermilk', mark: null },
    description: { value: 'Tangy and tall.', mark: WRITTEN },
    prepTime: { value: 10, mark: ESTIMATE },
    cookTime: { value: 15, mark: null },
    servings: { value: 4, mark: SHORTENED },
  },
  ingredients: [
    { name: 'flour', quantity: 1.5, unit: 'cups', isEyeballed: false, mark: null },
    { name: 'garlic', quantity: 2, unit: 'cloves', isEyeballed: false, mark: UNCLEAR },
    { name: 'salt', quantity: null, unit: '', isEyeballed: true, mark: null },
  ],
  instructions: [
    { text: 'Whisk the dry ingredients.', mark: null },
    { text: 'Fry for 3 minutes a side.', mark: UNCLEAR },
  ],
  match: null,
  possibleDuplicate: null,
  otherRecipes: ['Maple Syrup'],
  recipes: [],
}

const PANCAKES_VALUES: WizardValues = {
  recipeName: 'Pancakes',
  recipeDescription: 'Fluffy weekend pancakes.',
  variantName: 'Buttermilk',
  variantDescription: 'Tangy and tall.',
  prepTime: 10,
  cookTime: 15,
  servings: 4,
  ingredients: [
    { name: 'flour', quantity: 1.5, unit: 'cups', isEyeballed: false },
    { name: 'garlic', quantity: 2, unit: 'cloves', isEyeballed: false, mark: UNCLEAR },
    { name: 'salt', unit: '', isEyeballed: true },
  ],
  instructions: [{ text: 'Whisk the dry ingredients.' }, { text: 'Fry for 3 minutes a side.', mark: UNCLEAR }],
  marks: {
    'recipe.name': TRANSLATED,
    'recipe.description': WRITTEN,
    'variant.description': WRITTEN,
    'variant.prepTime': ESTIMATE,
    'variant.servings': SHORTENED,
  },
}

const empty = <T>(value: T) => ({ value, mark: null })

const NO_RECIPE: ReadResponse = {
  outcome: 'noRecipe',
  recipe: { name: empty(''), description: empty('') },
  variant: {
    name: empty(''),
    description: empty(''),
    prepTime: empty(null),
    cookTime: empty(null),
    servings: empty(null),
  },
  ingredients: [],
  instructions: [],
  match: null,
  possibleDuplicate: null,
  otherRecipes: [],
  recipes: [],
}

const answered = (data: ReadResponse): ReadResult => ({ success: true, data, errorMessage: null })
const failed = (status: number, code: string | null = null, body: Record<string, unknown> | null = null): ReadResult => ({
  success: false,
  data: null,
  errorMessage: 'The request could not be completed.',
  status,
  code,
  body,
})
const ceilingReached = (availableAgainOn?: string) =>
  failed(409, 'ceilingReached', { error: 'Photo import is resting.', code: 'ceilingReached', availableAgainOn })
const CANCELLED: ReadResult = { success: false, data: null, aborted: true }

const photo = (page = 'page') => ({ blob: new Blob([page], { type: 'image/jpeg' }) })

const pages = (count: number, first = 1) => Array.from({ length: count }, (_, index) => photo(`page ${first + index}`))

const UNSUPPORTED = { refusal: 'UnsupportedFile' } as const

const withPhotos = (wizard: WizardFlow, count = 1) => {
  choosePhotos(wizard)
  addPhotos(wizard, pages(count), 'device')
  return wizard
}

const readThrough = (wizard: WizardFlow, result: ReadResult, confirmed = false) => {
  expect(startRead(wizard, confirmed)).not.toBeNull()
  finishRead(wizard, result)
  return wizard
}

const imported = (wizard: WizardFlow = recipeWizard(true)) => readThrough(withPhotos(wizard), answered(PANCAKES))

let objectUrls = 0

beforeEach(() => {
  objectUrls = 0
  vi.spyOn(URL, 'createObjectURL').mockImplementation(() => `blob:https://kitchen.test/photo-${++objectUrls}`)
  vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('recipeWizardFlow photo path', () => {
  it.each([
    ['recipe', recipeWizard],
    ['variant', variantWizard],
  ] as const)('walks %s mode from the start step through the photos and the read to the review', (_mode, wizardFor) => {
    const wizard = wizardFor(true)

    choosePhotos(wizard)
    expect(wizard).toMatchObject({ start: 'photos', step: 'photos' })
    expect(stepOrder(wizard)).toEqual(['start', 'photos', 'reading', 'review'])

    addPhotos(wizard, [photo()], 'device')
    startRead(wizard)
    expect(wizard.step).toBe('reading')

    finishRead(wizard, answered(PANCAKES))
    expect(wizard.step).toBe('review')
  })

  it("builds each read from the import's photos in order, with the page's flow and, in the variant flow only, the recipe key", () => {
    const recipe = withPhotos(recipeWizard(true), 2)
    const variant = withPhotos(variantWizard(true), 2)

    const recipeRead = startRead(recipe)
    const variantRead = startRead(variant)

    expect(recipeRead).toMatchObject({ flow: 'recipe' })
    expect(recipeRead).not.toHaveProperty('recipeKey')
    expect(recipeRead!.photos.map((blob) => recipe.photos.findIndex((p) => p.blob === blob))).toEqual([0, 1])
    expect(variantRead).toMatchObject({ flow: 'variant', recipeKey: GNOCCHI.key })
    expect(variantRead!.photos.map((blob) => variant.photos.findIndex((p) => p.blob === blob))).toEqual([0, 1])
  })

  it('starts a read only from the photo step, with at least one photo', () => {
    const noPhotos = recipeWizard(true)
    choosePhotos(noPhotos)
    const reading = withPhotos(recipeWizard(true))
    startRead(reading)

    expect(canStartRead(noPhotos)).toBe(false)
    expect(startRead(noPhotos)).toBeNull()
    expect(noPhotos.step).toBe('photos')
    expect(canStartRead(reading)).toBe(false)
    expect(startRead(reading)).toBeNull()
    expect(canStartRead(withPhotos(recipeWizard(true)))).toBe(true)
  })

  it('steps back from the photo step to the start step, and from the review past the reading step to the photo step', () => {
    expect(stepsVisited(imported(), back)).toEqual(['review', 'photos', 'start'])
  })

  it('offers no Back and no Next while a read runs', () => {
    const wizard = withPhotos(recipeWizard(true))
    startRead(wizard)

    expect(canGoBack(wizard)).toBe(false)
    expect(canProceed(wizard)).toBe(false)
    back(wizard)
    next(wizard)
    expect(wizard.step).toBe('reading')
  })
})

describe('recipeWizardFlow applyRead', () => {
  it('fills every value from the read, with each mark on its value or its row', () => {
    const wizard = recipeWizard(true)

    applyRead(wizard, PANCAKES)

    expect(wizard.values).toEqual(PANCAKES_VALUES)
    expect(Object.keys(wizard.values.marks).sort()).toEqual(Object.keys(PANCAKES_VALUES.marks).sort())
    expect(wizard.values.ingredients.map((row) => 'mark' in row)).toEqual([false, true, false])
    expect(wizard.values.instructions.map((row) => 'mark' in row)).toEqual([false, true])
  })

  it('keeps the read, with the other recipes it saw in the photos', () => {
    const wizard = recipeWizard(true)

    applyRead(wizard, PANCAKES)

    expect(wizard.read).toEqual(PANCAKES)
    expect(wizard.read?.otherRecipes).toEqual(['Maple Syrup'])
  })

  it('gives a time or a serving count that the read left empty the zero a new wizard starts with', () => {
    const wizard = recipeWizard(true)

    applyRead(wizard, { ...PANCAKES, variant: { ...PANCAKES.variant, prepTime: empty(null), servings: empty(null) } })

    expect(wizard.values).toMatchObject({ prepTime: 0, cookTime: 15, servings: 0 })
  })

  it('leaves one blank row, as a new wizard has, where the read found no ingredient or no step', () => {
    const noIngredients = recipeWizard(true)
    const noSteps = recipeWizard(true)

    applyRead(noIngredients, { ...PANCAKES, ingredients: [] })
    applyRead(noSteps, { ...PANCAKES, instructions: [] })

    expect(noIngredients.values.ingredients).toEqual([{ name: '', unit: '', isEyeballed: false }])
    expect(noSteps.values.instructions).toEqual([{ text: '' }])
  })

  it('replaces the values of an earlier read', () => {
    const wizard = recipeWizard(true)
    applyRead(wizard, PANCAKES)

    applyRead(wizard, {
      ...PANCAKES,
      ingredients: [{ name: 'oats', quantity: 1, unit: 'cup', isEyeballed: false, mark: null }],
    })

    expect(wizard.values.ingredients).toEqual([{ name: 'oats', quantity: 1, unit: 'cup', isEyeballed: false }])
  })
})

describe('recipeWizardFlow row marks', () => {
  it("keeps each ingredient's mark on its own row when another row is removed or added", () => {
    const wizard = imported()

    removeIngredient(wizard, 0)
    addIngredient(wizard)

    expect(wizard.values.ingredients.map(({ name, mark }) => [name, mark])).toEqual([
      ['garlic', UNCLEAR],
      ['salt', undefined],
      ['', undefined],
    ])
  })

  it("keeps each step's mark on its own row when another step is removed or added", () => {
    const wizard = imported()

    addInstruction(wizard)
    removeInstruction(wizard, 0)

    expect(wizard.values.instructions.map(({ text, mark }) => [text, mark])).toEqual([
      ['Fry for 3 minutes a side.', UNCLEAR],
      ['', undefined],
    ])
  })

  it('keeps each mark on its own row when the review drops a blank row', () => {
    const wizard = imported()
    wizard.values.ingredients.unshift({ name: ' ', unit: '', isEyeballed: false })
    wizard.values.instructions.unshift({ text: '' })

    expect(validIngredients(wizard.values.ingredients).map(({ name, mark }) => [name, mark])).toEqual([
      ['flour', undefined],
      ['garlic', UNCLEAR],
      ['salt', undefined],
    ])
    expect(validInstructions(wizard.values.instructions).map(({ text, mark }) => [text, mark])).toEqual([
      ['Whisk the dry ingredients.', undefined],
      ['Fry for 3 minutes a side.', UNCLEAR],
    ])
  })
})

describe('recipeWizardFlow photo strip', () => {
  it.each(['reading', 'match', 'basics', 'variant', 'ingredients', 'instructions', 'review'] as StepId[])(
    'shows the view-only photo strip on the %s step while the import holds photos',
    (step) => {
      expect(showsPhotoStrip(on(withPhotos(recipeWizard(true)), step))).toBe(true)
    },
  )

  it.each(['start', 'photos', 'submitted'] as StepId[])('shows no photo strip on the %s step', (step) => {
    expect(showsPhotoStrip(on(withPhotos(recipeWizard(true)), step))).toBe(false)
  })

  it('shows no photo strip while the import holds no photo', () => {
    const wizard = imported()
    wizard.photos.map(({ id }) => id).forEach((id) => removePhoto(wizard, id))

    expect(showsPhotoStrip(on(wizard, 'review'))).toBe(false)
    expect(showsPhotoStrip(on(wizard, 'ingredients'))).toBe(false)
  })
})

describe('recipeWizardFlow photos', () => {
  it('adds up to ten photos, each with an object URL of its own and where it came from', () => {
    const wizard = recipeWizard(true)
    choosePhotos(wizard)

    addPhotos(wizard, [photo('shutter')], 'camera')
    addPhotos(wizard, pages(9, 2), 'device')

    expect(wizard.photos).toHaveLength(10)
    expect(new Set(wizard.photos.map(({ id }) => id)).size).toBe(10)
    expect(new Set(wizard.photos.map(({ url }) => url)).size).toBe(10)
    expect(wizard.photos.map(({ source }) => source)).toEqual(['camera', ...Array<string>(9).fill('device')])
    expect(URL.createObjectURL).toHaveBeenCalledWith(wizard.photos[0]!.blob)
    expect(wizard.photos[0]!.url).toBe('blob:https://kitchen.test/photo-1')
    expect(wizard.photoErrors).toEqual([])
  })

  it('refuses an 11th photo with TooManyPhotos, and makes no object URL for it', () => {
    const wizard = withPhotos(recipeWizard(true), 10)
    vi.mocked(URL.createObjectURL).mockClear()

    addPhotos(wizard, [photo('page 11')], 'device')

    expect(wizard.photos).toHaveLength(10)
    expect(wizard.photoErrors).toEqual(['TooManyPhotos'])
    expect(URL.createObjectURL).not.toHaveBeenCalled()
  })

  it('refuses a photo that the encoder could not use with UnsupportedFile', () => {
    const wizard = withPhotos(recipeWizard(true))

    addPhotos(wizard, [UNSUPPORTED], 'device')

    expect(wizard.photos).toHaveLength(1)
    expect(wizard.photoErrors).toEqual(['UnsupportedFile'])
  })

  it.each([
    ['before', [UNSUPPORTED, photo('page 2')]],
    ['after', [photo('page 2'), UNSUPPORTED]],
  ])('keeps UnsupportedFile for a file it cannot use %s a photo it can in one pick, and adds that photo', (_order, pick) => {
    const wizard = withPhotos(recipeWizard(true))

    addPhotos(wizard, pick, 'device')

    expect(wizard.photos).toHaveLength(2)
    expect(wizard.photoErrors).toEqual(['UnsupportedFile'])
  })

  it('keeps each refusal of a pick that holds files it cannot use and passes the limit, once each', () => {
    const wizard = withPhotos(recipeWizard(true), 5)
    const usable = pages(7, 6)

    addPhotos(wizard, [UNSUPPORTED, UNSUPPORTED, ...usable], 'device')

    expect(wizard.photos.slice(5).map(({ blob }) => blob)).toEqual(usable.slice(0, 5).map(({ blob }) => blob))
    expect(wizard.photoErrors).toEqual(['UnsupportedFile', 'TooManyPhotos'])
  })

  it('takes a removed photo out of the import and revokes its object URL', () => {
    const wizard = withPhotos(recipeWizard(true), 3)
    const [first, second, third] = wizard.photos

    removePhoto(wizard, second!.id)

    expect(wizard.photos).toEqual([first, third])
    expect(URL.revokeObjectURL).toHaveBeenCalledTimes(1)
    expect(URL.revokeObjectURL).toHaveBeenCalledWith(second!.url)
  })

  it('clears the photo messages once a photo is removed, or the next pick or capture is added', () => {
    const removed = withPhotos(recipeWizard(true), 9)
    const picked = withPhotos(recipeWizard(true))
    const captured = withPhotos(recipeWizard(true))
    addPhotos(removed, [UNSUPPORTED, ...pages(2, 10)], 'device')
    addPhotos(picked, [UNSUPPORTED], 'device')
    addPhotos(captured, [UNSUPPORTED], 'device')

    removePhoto(removed, removed.photos[0]!.id)
    addPhotos(picked, [photo('page 2')], 'device')
    addPhotos(captured, [photo('shutter')], 'camera')

    expect(removed.photoErrors).toEqual([])
    expect(picked.photoErrors).toEqual([])
    expect(captured.photoErrors).toEqual([])
  })

  it('keeps only the refusals of the latest pick', () => {
    const wizard = withPhotos(recipeWizard(true), 9)
    addPhotos(wizard, [UNSUPPORTED], 'device')

    addPhotos(wizard, pages(2, 10), 'device')

    expect(wizard.photoErrors).toEqual(['TooManyPhotos'])
  })
})

interface PendingPhoto {
  photo: Promise<EncodedPhoto | null>
  arrive: (encoded: EncodedPhoto | null) => void
  fail: (reason: unknown) => void
}

function pendingPhoto(): PendingPhoto {
  let arrive!: PendingPhoto['arrive']
  let fail!: PendingPhoto['fail']
  const photo = new Promise<EncodedPhoto | null>((resolve, reject) => {
    arrive = resolve
    fail = reject
  })
  return { photo, arrive, fail }
}

describe('recipeWizardFlow pending photos', () => {
  const SETTLED: [string, (pending: PendingPhoto) => void, number][] = [
    ['with a photo', ({ arrive }) => arrive(photo('page 2')), 2],
    ['with none', ({ arrive }) => arrive(null), 1],
    ['rejected', ({ fail }) => fail(new Error('The encoder threw.')), 1],
  ]

  it('starts no read while a photo is still being taken or encoded', () => {
    const wizard = withPhotos(recipeWizard(true))

    void addPendingPhotos(wizard, [pendingPhoto().photo], 'camera')

    expect(canStartRead(wizard)).toBe(false)
    expect(startRead(wizard)).toBeNull()
    expect(wizard.step).toBe('photos')
  })

  it.each(SETTLED)('starts reads again once the pending photo settles %s', async (_outcome, settle, photos) => {
    const wizard = withPhotos(recipeWizard(true))
    const pending = pendingPhoto()
    const adding = addPendingPhotos(wizard, [pending.photo], 'camera')

    settle(pending)
    await adding

    expect(wizard.photos).toHaveLength(photos)
    expect(canStartRead(wizard)).toBe(true)
    expect(startRead(wizard)).not.toBeNull()
  })

  it('waits for both of two overlapping adds', async () => {
    const wizard = withPhotos(recipeWizard(true))
    const shutter = pendingPhoto()
    const pick = pendingPhoto()
    const capturing = addPendingPhotos(wizard, [shutter.photo], 'camera')
    const picking = addPendingPhotos(wizard, [pick.photo], 'device')

    shutter.arrive(photo('shutter'))
    await capturing
    expect(canStartRead(wizard)).toBe(false)

    pick.arrive(photo('page 3'))
    await picking
    expect(canStartRead(wizard)).toBe(true)
    expect(wizard.photos.map(({ source }) => source)).toEqual(['device', 'camera', 'device'])
  })

  it('adds a whole pick at once, keeping each refusal of it', async () => {
    const wizard = withPhotos(recipeWizard(true), 8)
    const pick = [UNSUPPORTED, ...pages(3, 9)].map((encoded) => Promise.resolve(encoded))

    await addPendingPhotos(wizard, pick, 'device')

    expect(wizard.photos).toHaveLength(10)
    expect(wizard.photoErrors).toEqual(['UnsupportedFile', 'TooManyPhotos'])
  })
})

describe('recipeWizardFlow read outcomes', () => {
  it('stays on the photo step after a noRecipe read, with the photos and the values as they were', () => {
    const wizard = imported()
    const photos = [...wizard.photos]

    readThrough(on(wizard, 'photos'), answered(NO_RECIPE))

    expect(wizard).toMatchObject({ step: 'photos', noRecipeFound: true, readError: null })
    expect(wizard.photos).toEqual(photos)
    expect(wizard.values).toEqual(PANCAKES_VALUES)
    expect(wizard.read).toEqual(PANCAKES)
  })

  it.each([
    [502, 'readFailed', 'ReadFailed'],
    [504, 'readTimedOut', 'ReadTimedOut'],
    [0, null, 'ReadFailed'],
    [500, null, 'ReadFailed'],
  ])('keeps the photos after a failed read (%i %s) and offers Try again and Type it in', (status, code, key) => {
    const wizard = withPhotos(recipeWizard(true), 2)
    const photos = [...wizard.photos]

    readThrough(wizard, failed(status, code))

    expect(wizard).toMatchObject({ step: 'photos', readError: key, noRecipeFound: false })
    expect(wizard.photos).toEqual(photos)
    expect(canStartRead(wizard)).toBe(true)

    const tryAgain = structuredClone(wizard)
    expect(startRead(tryAgain)).not.toBeNull()
    expect(tryAgain).toMatchObject({ step: 'reading', readError: null })

    chooseTyping(wizard)
    expect(wizard).toMatchObject({ start: 'typing', step: 'basics' })
    expect(wizard.photos).toEqual(photos)
  })

  it('keeps the photo step and its photos after a 409 ceilingReached, with the import unavailable until its day, Read recipe off and Type it in left', () => {
    const wizard = withPhotos(recipeWizard(true), 2)
    const photos = [...wizard.photos]

    readThrough(wizard, ceilingReached('2026-11-01'))

    expect(wizard).toMatchObject({ step: 'photos', availableAgainOn: '2026-11-01', readError: null, noRecipeFound: false })
    expect(wizard.photos).toEqual(photos)
    expect(canStartRead(wizard)).toBe(false)
    expect(startRead(wizard)).toBeNull()
    expect(wizard.step).toBe('photos')

    chooseTyping(wizard)
    expect(wizard).toMatchObject({ start: 'typing', step: 'basics' })
    expect(wizard.photos).toEqual(photos)
  })

  it('keeps Read recipe off after a 409 once more photos are added', () => {
    const wizard = withPhotos(recipeWizard(true))
    readThrough(wizard, ceilingReached('2026-11-01'))

    addPhotos(wizard, pages(1, 2), 'device')

    expect(canStartRead(wizard)).toBe(false)
  })

  it('shows a 409 ceilingReached that names no day as a failed read', () => {
    const wizard = readThrough(withPhotos(recipeWizard(true)), ceilingReached())

    expect(wizard).toMatchObject({ step: 'photos', readError: 'ReadFailed', availableAgainOn: null })
  })

  it('shows no message after a cancelled read, and keeps the photos', () => {
    const wizard = withPhotos(recipeWizard(true))
    readThrough(wizard, failed(502, 'readFailed'))
    const photos = [...wizard.photos]

    readThrough(wizard, CANCELLED)

    expect(wizard).toMatchObject({ step: 'photos', readError: null, noRecipeFound: false })
    expect(wizard.photos).toEqual(photos)
  })

  it('clears the outcome of the last read, and the photo message, when the next read starts', () => {
    const wizard = withPhotos(recipeWizard(true))
    readThrough(wizard, answered(NO_RECIPE))
    addPhotos(wizard, [UNSUPPORTED], 'device')

    startRead(wizard)

    expect(wizard).toMatchObject({ step: 'reading', readError: null, noRecipeFound: false, photoErrors: [] })
  })
})

describe('recipeWizardFlow editedSinceRead', () => {
  const EDITS: [string, (wizard: WizardFlow) => void][] = [
    ['typing a recipe name', (wizard) => editValue(wizard, 'recipeName', 'Pancakes for two')],
    ['typing a recipe description', (wizard) => editValue(wizard, 'recipeDescription', 'Fluffy.')],
    ['typing a variant name', (wizard) => editValue(wizard, 'variantName', 'Sourdough')],
    ['typing a variant description', (wizard) => editValue(wizard, 'variantDescription', 'Tangier.')],
    ['changing the prep time', (wizard) => editValue(wizard, 'prepTime', 12)],
    ['changing the cook time', (wizard) => editValue(wizard, 'cookTime', 20)],
    ['changing the servings', (wizard) => editValue(wizard, 'servings', 6)],
    ['typing an ingredient name', (wizard) => editIngredient(wizard, 0, { name: 'oat flour' })],
    ['changing a quantity', (wizard) => editIngredient(wizard, 0, { quantity: 3 })],
    ['changing a unit', (wizard) => editIngredient(wizard, 0, { unit: 'grams' })],
    ['eyeballing an ingredient', (wizard) => editIngredient(wizard, 0, { isEyeballed: true })],
    ['typing a step', (wizard) => editInstruction(wizard, 0, 'Rest the batter.')],
    ['adding an ingredient row', addIngredient],
    ['removing an ingredient row', (wizard) => removeIngredient(wizard, 1)],
    ['adding a step row', addInstruction],
    ['removing a step row', (wizard) => removeInstruction(wizard, 1)],
  ]

  it('starts false', () => {
    expect(recipeWizard(true).editedSinceRead).toBe(false)
    expect(variantWizard(false).editedSinceRead).toBe(false)
  })

  it.each(EDITS)('turns true after %s, counted from the last applied read', (_edit, edit) => {
    const wizard = imported()
    expect(wizard.editedSinceRead).toBe(false)

    edit(wizard)

    expect(wizard.editedSinceRead).toBe(true)
  })

  it.each(EDITS.filter(([edit]) => !edit.startsWith('removing')))(
    'turns true after %s, counted from the wizard start',
    (_edit, edit) => {
      const wizard = recipeWizard(true)
      chooseTyping(wizard)

      edit(wizard)

      expect(wizard.editedSinceRead).toBe(true)
    },
  )

  it('is cleared by applying a read, and counts again from it', () => {
    const wizard = withPhotos(recipeWizard(true))
    editValue(wizard, 'recipeName', 'Pancakes')

    readThrough(wizard, answered(PANCAKES), true)
    expect(wizard.editedSinceRead).toBe(false)

    addIngredient(wizard)
    expect(wizard.editedSinceRead).toBe(true)
  })

  it('is left as it was by a noRecipe read', () => {
    const edited = imported()
    editValue(edited, 'variantName', 'Sourdough')
    const untouched = imported()

    readThrough(on(edited, 'photos'), answered(NO_RECIPE), true)
    readThrough(on(untouched, 'photos'), answered(NO_RECIPE))

    expect(edited.editedSinceRead).toBe(true)
    expect(untouched.editedSinceRead).toBe(false)
  })

  it('is never set by Next, Back, the start choice or a photo added or removed', () => {
    const typed = filled(recipeWizard(true))
    chooseTyping(typed)
    next(typed)
    next(typed)
    back(typed)
    back(typed)
    back(typed)
    const photos = withPhotos(typed, 2)
    removePhoto(photos, photos.photos[0]!.id)
    const read = imported()
    back(read)
    addPhotos(read, [photo('page 2')], 'device')
    removePhoto(read, read.photos[0]!.id)

    expect(typed.editedSinceRead).toBe(false)
    expect(read.editedSinceRead).toBe(false)
  })
})

describe('recipeWizardFlow hasUnsavedWork', () => {
  it('is false while the import holds no photo and no read values, typed values or not', () => {
    const wizard = recipeWizard(true)
    expect(hasUnsavedWork(wizard)).toBe(false)

    chooseTyping(wizard)
    editValue(wizard, 'recipeName', 'Pancakes')

    expect(hasUnsavedWork(wizard)).toBe(false)
  })

  it('is true while the import holds a photo', () => {
    expect(hasUnsavedWork(withPhotos(recipeWizard(true)))).toBe(true)
    expect(hasUnsavedWork(readThrough(withPhotos(variantWizard(true)), failed(502, 'readFailed')))).toBe(true)
  })

  it('is true while a photo is pending and the import holds none, and false again once every pending photo is dropped', async () => {
    const wizard = recipeWizard(true)
    choosePhotos(wizard)
    const shutter = pendingPhoto()
    const firstPage = pendingPhoto()
    const secondPage = pendingPhoto()

    const capturing = addPendingPhotos(wizard, [shutter.photo], 'camera')
    const picking = addPendingPhotos(wizard, [firstPage.photo, secondPage.photo], 'device')
    expect(hasUnsavedWork(wizard)).toBe(true)

    shutter.arrive(null)
    await capturing
    expect(hasUnsavedWork(wizard)).toBe(true)

    firstPage.arrive(UNSUPPORTED)
    secondPage.fail(new Error('The encoder threw.'))
    await picking
    expect(wizard.photos).toEqual([])
    expect(hasUnsavedWork(wizard)).toBe(false)
  })

  it('is true while the wizard holds read values, with every photo removed', () => {
    const wizard = imported()
    wizard.photos.map(({ id }) => id).forEach((id) => removePhoto(wizard, id))

    expect(hasUnsavedWork(wizard)).toBe(true)
  })

  it('is false once the recipe is submitted', () => {
    expect(hasUnsavedWork(on(imported(), 'submitted'))).toBe(false)
  })
})

const typedReview = (wizard: WizardFlow = recipeWizard(true)) => {
  chooseTyping(filled(wizard))
  stepsVisited(wizard, next)
  return wizard
}

describe('recipeWizardFlow step links', () => {
  it.each([
    ['basics', 'basics'],
    ['variant', 'variant'],
    ['ingredients', 'ingredients'],
    ['instructions', 'instructions'],
  ] as const)('opens the %s section from the review on its step, set to return there', (section, step) => {
    const wizard = typedReview()
    expect(wizard).toMatchObject({ step: 'review', returnTo: null })

    edit(wizard, section)

    expect(wizard).toMatchObject({ step, returnTo: 'review' })
  })

  it('opens the steps of an imported recipe from its review as well', () => {
    const wizard = imported(variantWizard(true))

    edit(wizard, 'ingredients')

    expect(wizard).toMatchObject({ step: 'ingredients', returnTo: 'review' })
    expect(showsPhotoStrip(wizard)).toBe(true)
  })

  it('opens no basics step in variant mode, which has none', () => {
    const wizard = typedReview(variantWizard(true))

    edit(wizard, 'basics')

    expect(wizard).toMatchObject({ step: 'review', returnTo: null })
  })

  it('returns straight to the review with the changes kept, from Back to review', () => {
    const wizard = typedReview()
    edit(wizard, 'ingredients')
    editIngredient(wizard, 0, { name: 'oat flour' })

    backToReview(wizard)

    expect(wizard).toMatchObject({ step: 'review', returnTo: null })
    expect(wizard.values.ingredients[0]!.name).toBe('oat flour')
  })

  it('returns straight to the review from Next on a step opened there', () => {
    const wizard = typedReview()
    edit(wizard, 'basics')

    next(wizard)

    expect(wizard).toMatchObject({ step: 'review', returnTo: null })
  })

  it('keeps the return to the review across Back on a step opened there, so Next still returns to it', () => {
    const wizard = typedReview()
    edit(wizard, 'ingredients')

    back(wizard)

    expect(wizard).toMatchObject({ step: 'variant', returnTo: 'review' })
    next(wizard)
    expect(wizard.step).toBe('review')
  })

  it('holds Next on a step opened from the review until it is filled in', () => {
    const wizard = typedReview()
    edit(wizard, 'variant')
    editValue(wizard, 'variantName', ' ')

    next(wizard)

    expect(wizard).toMatchObject({ step: 'variant', returnTo: 'review' })
  })

  it('walks Next through the typing steps as before once the member has returned to the review', () => {
    const wizard = typedReview()
    edit(wizard, 'basics')
    backToReview(wizard)
    back(wizard)
    back(wizard)

    next(wizard)

    expect(wizard.step).toBe('instructions')
  })
})

describe('recipeWizardFlow marks after an edit', () => {
  it.each([
    ['recipeName', 'Pancakes for two', 'recipe.name'],
    ['recipeDescription', 'Fluffy.', 'recipe.description'],
    ['variantDescription', 'Tangier.', 'variant.description'],
    ['prepTime', 12, 'variant.prepTime'],
    ['servings', 6, 'variant.servings'],
  ] as const)('removes the mark of %s once it is edited, and no other', (field, value, path) => {
    const wizard = imported()

    editValue(wizard, field, value)

    const { [path]: _removed, ...others } = PANCAKES_VALUES.marks
    expect(wizard.values.marks).toEqual(others)
    expect(wizard.values.ingredients[1]!.mark).toEqual(UNCLEAR)
    expect(wizard.values.instructions[1]!.mark).toEqual(UNCLEAR)
  })

  it.each([
    ['name', { name: 'garlic, minced' }],
    ['quantity', { quantity: 3 }],
    ['unit', { unit: 'heads' }],
    ['eyeball', { isEyeballed: true }],
  ] as const)("removes a marked ingredient's mark once its %s is edited, and no other mark", (_field, change) => {
    const wizard = imported()
    wizard.values.ingredients[0]!.mark = ESTIMATE

    editIngredient(wizard, 1, change)

    expect(wizard.values.ingredients.map(({ mark }) => mark)).toEqual([ESTIMATE, undefined, undefined])
    expect(wizard.values.instructions[1]!.mark).toEqual(UNCLEAR)
    expect(wizard.values.marks).toEqual(PANCAKES_VALUES.marks)
  })

  it("removes a marked step's mark once its text is edited, and no other mark", () => {
    const wizard = imported()
    wizard.values.instructions[0]!.mark = WRITTEN

    editInstruction(wizard, 1, 'Fry for 2 minutes a side.')

    expect(wizard.values.instructions.map(({ mark }) => mark)).toEqual([WRITTEN, undefined])
    expect(wizard.values.ingredients[1]!.mark).toEqual(UNCLEAR)
    expect(wizard.values.marks).toEqual(PANCAKES_VALUES.marks)
  })
})

describe('recipeWizardFlow reading again', () => {
  const asks = (wizard: WizardFlow) => {
    const step = wizard.step

    expect(startRead(wizard)).toBeNull()
    expect(wizard).toMatchObject({ step, confirmingRead: true })
  }

  const readsAtOnce = (wizard: WizardFlow) => {
    expect(startRead(wizard)).not.toBeNull()
    expect(wizard).toMatchObject({ step: 'reading', confirmingRead: false })
  }

  it('asks first on the photo step when values were typed before a first read', () => {
    const wizard = recipeWizard(true)
    chooseTyping(wizard)
    editValue(wizard, 'recipeName', 'Pancakes')
    back(wizard)
    withPhotos(wizard)

    asks(wizard)
    expect(startRead(wizard, true)).not.toBeNull()
    expect(wizard).toMatchObject({ step: 'reading', confirmingRead: false })
  })

  it.each([
    ['an ingredient row added', addIngredient],
    ['an ingredient row removed', (wizard: WizardFlow) => removeIngredient(wizard, 0)],
    ['a step row added', addInstruction],
    ['a step row removed', (wizard: WizardFlow) => removeInstruction(wizard, 0)],
  ])('asks first on Read again from the review after %s, with nothing else changed', (_change, change) => {
    const wizard = imported()
    change(wizard)

    asks(wizard)
  })

  it("asks first on the photo step's Read recipe after an edit and Change photos", () => {
    const wizard = imported()
    editValue(wizard, 'variantName', 'Sourdough')
    changePhotos(wizard)
    addPhotos(wizard, [photo('page 2')], 'device')

    asks(wizard)
  })

  it('asks nothing in an untouched wizard', () => {
    readsAtOnce(withPhotos(recipeWizard(true)))
    readsAtOnce(withPhotos(variantWizard(true)))
  })

  it('asks nothing when no value changed since the last applied read, photos added or not', () => {
    const fromReview = imported()
    const withAnotherPhoto = imported()
    changePhotos(withAnotherPhoto)
    addPhotos(withAnotherPhoto, [photo('page 2')], 'device')

    readsAtOnce(fromReview)
    readsAtOnce(withAnotherPhoto)
  })

  it('keeps the edits, the step and no pending question when the member cancels the warning', () => {
    const wizard = imported()
    editValue(wizard, 'recipeName', 'Pancakes for two')
    startRead(wizard)

    keepEdits(wizard)

    expect(wizard).toMatchObject({ step: 'review', confirmingRead: false, editedSinceRead: true })
    expect(wizard.values.recipeName).toBe('Pancakes for two')
  })

  it('drops a pending question when the member moves to another step', () => {
    const wizard = imported()
    addIngredient(wizard)
    startRead(wizard)

    edit(wizard, 'ingredients')

    expect(wizard.confirmingRead).toBe(false)
  })

  it('reads again from the review of an import only', () => {
    const typed = withPhotos(recipeWizard(true))
    chooseTyping(filled(typed))
    stepsVisited(typed, next)

    expect(canStartRead(imported())).toBe(true)
    expect(typed).toMatchObject({ start: 'typing', step: 'review' })
    expect(typed.photos).toHaveLength(1)
    expect(canStartRead(typed)).toBe(false)
    expect(startRead(typed)).toBeNull()
    expect(typed.step).toBe('review')
  })

  it('counts an import as one started from the photos', () => {
    expect(isImport(imported())).toBe(true)
    expect(isImport(typedReview())).toBe(false)
  })
})

const UNREAD: [string, ReadResult][] = [
  ['is cancelled', CANCELLED],
  ['fails', failed(502, 'readFailed')],
  ['finds no recipe', answered(NO_RECIPE)],
  ['gets a 409', ceilingReached('2026-11-01')],
]

describe('recipeWizardFlow Change photos', () => {
  it('returns to the photo step with the photos, set to return to the review while it holds read values', () => {
    const wizard = imported()
    const photos = [...wizard.photos]

    changePhotos(wizard)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: 'review' })
    expect(wizard.photos).toEqual(photos)
  })

  it('leaves the photo step for the review, with nothing changed, from Back to review', () => {
    const wizard = imported()
    editIngredient(wizard, 1, { name: 'garlic, minced' })
    const values = structuredClone(wizard.values)
    changePhotos(wizard)

    backToReview(wizard)

    expect(wizard).toMatchObject({ step: 'review', returnTo: null, editedSinceRead: true })
    expect(wizard.values).toEqual(values)
  })

  it('does the same from Back on the review of a photos path', () => {
    const wizard = imported()

    back(wizard)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: 'review' })
  })

  it('sets no return while the wizard holds no values from an applied read', () => {
    const wizard = typedReview()
    withPhotos(wizard)
    chooseTyping(wizard)
    stepsVisited(wizard, next)

    changePhotos(wizard)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: null })
  })

  it.each(UNREAD)('lands a read from the review that %s on the photo step with the return kept', (_outcome, result) => {
    const wizard = imported()

    readThrough(wizard, result)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: 'review' })
    expect(wizard.values).toEqual(PANCAKES_VALUES)
  })

  it.each(UNREAD)(
    'lands a read from the photo step with that return that %s on the photo step with the return kept',
    (_outcome, result) => {
      const wizard = imported()
      changePhotos(wizard)

      readThrough(wizard, result)

      expect(wizard).toMatchObject({ step: 'photos', returnTo: 'review' })
    },
  )

  it('keeps the import resting after a 409 on a read from the review', () => {
    const wizard = readThrough(imported(), ceilingReached('2026-11-01'))

    expect(wizard.availableAgainOn).toBe('2026-11-01')
    expect(canStartRead(wizard)).toBe(false)
    backToReview(wizard)
    expect(canStartRead(wizard)).toBe(false)
  })

  it('sets no return on a failed first read', () => {
    expect(readThrough(withPhotos(recipeWizard(true)), failed(502, 'readFailed')).returnTo).toBeNull()
  })

  it('clears the return once a new read applies, from the review or the photo step', () => {
    const fromReview = readThrough(imported(), answered(PANCAKES))
    const fromPhotos = imported()
    changePhotos(fromPhotos)
    readThrough(fromPhotos, answered(PANCAKES))

    expect(fromReview).toMatchObject({ step: 'review', returnTo: null })
    expect(fromPhotos).toMatchObject({ step: 'review', returnTo: null })
  })

  it('drops the return when the member types the recipe in after a confirmed read from the photo step fails', () => {
    const wizard = imported()
    editValue(wizard, 'recipeName', 'Pancakes for two')
    changePhotos(wizard)
    startRead(wizard, true)
    finishRead(wizard, failed(502, 'readFailed'))

    chooseTyping(wizard)

    expect(wizard).toMatchObject({ step: 'basics', returnTo: null })
    next(wizard)
    expect(wizard.step).toBe('variant')
  })

  it('sets no return when a typed review goes back to the start and on to the photos', () => {
    const wizard = typedReview()
    edit(wizard, 'basics')
    back(wizard)
    expect(wizard.step).toBe('start')

    choosePhotos(wizard)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: null })
  })

  it('keeps the return to the review when an import goes back to the start and on to the photos', () => {
    const wizard = imported()
    changePhotos(wizard)
    back(wizard)
    expect(wizard.step).toBe('start')

    choosePhotos(wizard)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: 'review' })
  })
})

const LASAGNA = { key: '22222222-3333-4444-5555-666666666666', name: 'Legendary Lasagna' }
const STACK = { key: '33333333-4444-5555-6666-777777777777', name: 'Fluffy Buttermilk Pancakes' }

const DUPLICATE = {
  variantKey: '44444444-5555-6666-7777-888888888888',
  variantName: 'Classic Stack',
  recipeName: STACK.name,
}

const MATCHED: ReadResponse = {
  ...PANCAKES,
  match: { recipeKey: STACK.key, recipeName: STACK.name, reason: 'Same dish with buttermilk.' },
  possibleDuplicate: DUPLICATE,
  recipes: [STACK, LASAGNA],
}

const matched = (wizard: WizardFlow = recipeWizard(true), read: ReadResponse = MATCHED) =>
  readThrough(withPhotos(wizard, 2), answered(read))

const SET_ASIDE_MARKS = (({ 'recipe.name': _name, 'recipe.description': _description, ...variantMarks }) => variantMarks)(
  PANCAKES_VALUES.marks,
)

const CHOICES: [string, (wizard: WizardFlow) => void, typeof STACK][] = [
  ['acceptMatch', acceptMatch, STACK],
  ['pickRecipe', (wizard) => pickRecipe(wizard, LASAGNA), LASAGNA],
]

const ANSWERS: [string, (wizard: WizardFlow) => void, WizardFlow['mode']][] = [
  ['acceptMatch', acceptMatch, 'variant'],
  ['pickRecipe', (wizard) => pickRecipe(wizard, LASAGNA), 'variant'],
  ['keepNewRecipe', keepNewRecipe, 'recipe'],
]

const typedOnToMatch = () => {
  const wizard = matched()
  back(wizard)
  chooseTyping(wizard)
  stepsVisited(wizard, next)
  return wizard
}

describe('recipeWizardFlow match step', () => {
  it('opens the match step after a Create Recipe read that proposes a recipe, with no progress and the photo strip', () => {
    const wizard = matched()

    expect(wizard).toMatchObject({ step: 'match', mode: 'recipe', recipe: null, returnTo: null, editedSinceRead: false })
    expect(stepOrder(wizard)).toEqual(['start', 'photos', 'reading', 'match', 'review'])
    expect(offersMatch(wizard)).toBe(true)
    expect(progress(wizard)).toBeNull()
    expect(showsPhotoStrip(wizard)).toBe(true)
    expect(wizard.values).toEqual(PANCAKES_VALUES)
  })

  it('opens the review, with no match step, after a read that proposes no recipe', () => {
    const wizard = imported()

    expect(wizard.step).toBe('review')
    expect(stepOrder(wizard)).toEqual(['start', 'photos', 'reading', 'review'])
    expect(offersMatch(wizard)).toBe(false)
  })

  it('opens no match step in the Add Variant flow, whatever the read holds', () => {
    const wizard = matched(variantWizard(true))

    expect(wizard).toMatchObject({ step: 'review', mode: 'variant', recipe: GNOCCHI })
    expect(offersMatch(wizard)).toBe(false)
  })

  it.each(CHOICES)(
    '%s switches to variant mode for its recipe on the review, setting the recipe values aside',
    (_choice, choose, recipe) => {
      const wizard = matched()
      const photos = [...wizard.photos]

      choose(wizard)

      expect(wizard).toMatchObject({ step: 'review', mode: 'variant', recipe, flow: 'recipe', returnTo: null })
      expect(wizard.values).toEqual({ ...PANCAKES_VALUES, recipeName: '', recipeDescription: '', marks: SET_ASIDE_MARKS })
      expect(wizard.photos).toEqual(photos)
      expect(progress(wizard)).toEqual({ current: 4, total: 4 })
    },
  )

  it('refuses a recipe that the read did not list', () => {
    const wizard = matched()

    pickRecipe(wizard, GNOCCHI)

    expect(wizard).toMatchObject({ step: 'match', mode: 'recipe', recipe: null })
  })

  it('takes no match choice away from the match step', () => {
    const wizard = matched()
    changePhotos(wizard)

    acceptMatch(wizard)
    pickRecipe(wizard, LASAGNA)

    expect(wizard).toMatchObject({ step: 'photos', mode: 'recipe', recipe: null })
  })

  it('keeps a new recipe on the review, with every value filled', () => {
    const wizard = matched()

    keepNewRecipe(wizard)

    expect(wizard).toMatchObject({ step: 'review', mode: 'recipe', recipe: null })
    expect(wizard.values).toEqual(PANCAKES_VALUES)
  })

  it.each(CHOICES)('restores the recipe values that %s set aside when the member keeps a new recipe', (_choice, choose) => {
    const wizard = matched()
    choose(wizard)
    changeMatch(wizard)

    keepNewRecipe(wizard)

    expect(wizard).toMatchObject({ step: 'review', mode: 'recipe', recipe: null })
    expect(wizard.values).toEqual(PANCAKES_VALUES)
  })

  it('keeps the values set aside when the member moves from one recipe to another', () => {
    const wizard = matched()
    acceptMatch(wizard)
    changeMatch(wizard)
    pickRecipe(wizard, LASAGNA)
    changeMatch(wizard)

    keepNewRecipe(wizard)

    expect(wizard.values).toEqual(PANCAKES_VALUES)
  })

  it('keeps the variant edits made after a choice when the member keeps a new recipe', () => {
    const wizard = matched()
    acceptMatch(wizard)
    editValue(wizard, 'variantName', 'Sourdough')
    changeMatch(wizard)

    keepNewRecipe(wizard)

    expect(wizard.values).toMatchObject({ recipeName: 'Pancakes', variantName: 'Sourdough' })
  })

  it.each([
    ['recipe mode', (wizard: WizardFlow) => keepNewRecipe(wizard)],
    ['variant mode', (wizard: WizardFlow) => acceptMatch(wizard)],
  ])('returns from Change on the review in %s to the match step, values unchanged', (_mode, choose) => {
    const wizard = matched()
    choose(wizard)
    const values = structuredClone(wizard.values)
    const mode = wizard.mode

    changeMatch(wizard)

    expect(wizard).toMatchObject({ step: 'match', mode, returnTo: null })
    expect(wizard.values).toEqual(values)
    expect(showsPhotoStrip(wizard)).toBe(true)
  })

  it('offers no Change on the review when the read proposed nothing, or in the Add Variant flow', () => {
    const noProposal = imported()
    const addVariant = matched(variantWizard(true))

    changeMatch(noProposal)
    changeMatch(addVariant)

    expect(noProposal.step).toBe('review')
    expect(addVariant.step).toBe('review')
  })

  it('steps back from the match step to the photo step, set to return to the review', () => {
    const wizard = matched()

    expect(canGoBack(wizard)).toBe(true)
    back(wizard)

    expect(wizard).toMatchObject({ step: 'photos', returnTo: 'review' })
  })

  it('keeps the possible duplicate with the import through every choice, in both modes', () => {
    const wizard = matched()
    const duplicates = [wizard.read?.possibleDuplicate]

    acceptMatch(wizard)
    duplicates.push(wizard.read?.possibleDuplicate)
    changeMatch(wizard)
    pickRecipe(wizard, LASAGNA)
    duplicates.push(wizard.read?.possibleDuplicate)
    changeMatch(wizard)
    keepNewRecipe(wizard)
    duplicates.push(wizard.read?.possibleDuplicate)

    expect(duplicates).toEqual([DUPLICATE, DUPLICATE, DUPLICATE, DUPLICATE])
  })

  it('never sets editedSinceRead through a match choice or Change', () => {
    const wizard = matched()

    acceptMatch(wizard)
    changeMatch(wizard)
    pickRecipe(wizard, LASAGNA)
    changeMatch(wizard)
    keepNewRecipe(wizard)
    changeMatch(wizard)

    expect(wizard.editedSinceRead).toBe(false)
  })

  it.each(ANSWERS)('keeps editedSinceRead through Change and %s once the member has edited', (_answer, answer) => {
    const wizard = matched()
    keepNewRecipe(wizard)
    editValue(wizard, 'variantName', 'Sourdough')

    changeMatch(wizard)
    answer(wizard)

    expect(wizard.editedSinceRead).toBe(true)
  })
})

describe('recipeWizardFlow unsettled match', () => {
  it('opens the match step for Back to review while the member has not answered it', () => {
    const wizard = matched()
    back(wizard)

    backToReview(wizard)

    expect(wizard).toMatchObject({ step: 'match', mode: 'recipe', returnTo: null })
  })

  it('opens the match step for Next on the last typing step while the member has not answered it', () => {
    const wizard = matched()
    back(wizard)
    chooseTyping(wizard)

    const visited = stepsVisited(wizard, next)

    expect(visited).toEqual(['basics', 'variant', 'ingredients', 'instructions', 'match'])
  })

  it('steps back from the match step on a typing path to the instructions step, leaving it unanswered', () => {
    const wizard = typedOnToMatch()

    expect(canGoBack(wizard)).toBe(true)
    back(wizard)

    expect(wizard).toMatchObject({ step: 'instructions', mode: 'recipe', matchSettled: false, returnTo: null })
    next(wizard)
    expect(wizard.step).toBe('match')
  })

  it.each(ANSWERS)(
    'steps back from Change on a typed review after %s to the instructions step, and on to the review',
    (_answer, answer, mode) => {
      const wizard = typedOnToMatch()
      answer(wizard)
      changeMatch(wizard)

      expect(wizard.step).toBe('match')
      expect(canGoBack(wizard)).toBe(true)
      back(wizard)

      expect(wizard).toMatchObject({ step: 'instructions', mode, matchSettled: true })
      next(wizard)
      expect(wizard).toMatchObject({ step: 'review', mode })
    },
  )

  it('opens the review for Back to review once the member has answered the match step', () => {
    const wizard = matched()
    keepNewRecipe(wizard)
    changeMatch(wizard)
    back(wizard)

    backToReview(wizard)

    expect(wizard).toMatchObject({ step: 'review', mode: 'recipe' })
  })

  it.each(CHOICES)('asks again after %s when a new read proposes a recipe again', (_choice, choose) => {
    const wizard = matched()
    choose(wizard)
    readThrough(wizard, answered(MATCHED))
    back(wizard)

    backToReview(wizard)

    expect(wizard).toMatchObject({ step: 'match', mode: 'recipe', recipe: null })
  })
})

describe('recipeWizardFlow submission', () => {
  it('sends recipe mode to the recipes endpoint, with the recipe values', () => {
    const wizard = matched()
    keepNewRecipe(wizard)

    expect(submissionFor(wizard)).toEqual({
      url: '/api/recipes',
      body: expect.objectContaining({
        recipeName: 'Pancakes',
        firstVariant: expect.objectContaining({ variantName: 'Buttermilk' }),
      }),
    })
  })

  it.each(CHOICES)('sends variant mode after %s to the variants of its recipe', (_choice, choose, recipe) => {
    const wizard = matched()
    choose(wizard)

    const { url, body } = submissionFor(wizard)

    expect(url).toBe(`/api/recipes/${recipe.key}/variants`)
    expect(body).toMatchObject({ variantName: 'Buttermilk', prepTime: 10 })
    expect(body).not.toHaveProperty('recipeName')
  })

  it("sends the Add Variant flow to the variants of the page's recipe", () => {
    expect(submissionFor(imported(variantWizard(true))).url).toBe(`/api/recipes/${GNOCCHI.key}/variants`)
  })
})

describe('recipeWizardFlow reading after a match choice', () => {
  const chosen = (choose: (wizard: WizardFlow) => void) => {
    const wizard = matched()
    choose(wizard)
    return wizard
  }

  it.each(CHOICES)('reads with the recipe flow and no recipe key after %s', (_choice, choose) => {
    const wizard = chosen(choose)

    const request = startRead(wizard)

    expect(request).toMatchObject({ flow: 'recipe' })
    expect(request).not.toHaveProperty('recipeKey')
  })

  it.each(CHOICES)(
    'returns to recipe mode on the match step when the new read after %s proposes a recipe again',
    (_choice, choose) => {
      const wizard = chosen(choose)

      readThrough(
        wizard,
        answered({ ...MATCHED, recipe: { ...MATCHED.recipe, name: { value: 'Pancakes II', mark: null } } }),
      )

      expect(wizard).toMatchObject({ step: 'match', mode: 'recipe', recipe: null, returnTo: null })
      expect(wizard.values.recipeName).toBe('Pancakes II')
      keepNewRecipe(wizard)
      expect(wizard.values.recipeName).toBe('Pancakes II')
    },
  )

  it.each(CHOICES)(
    'returns to recipe mode on the review when the new read after %s proposes no recipe',
    (_choice, choose) => {
      const wizard = chosen(choose)
      changePhotos(wizard)

      readThrough(wizard, answered(PANCAKES))

      expect(wizard).toMatchObject({ step: 'review', mode: 'recipe', recipe: null, returnTo: null })
      expect(wizard.values).toEqual(PANCAKES_VALUES)
      expect(submissionFor(wizard).url).toBe('/api/recipes')
    },
  )

  it.each(CHOICES)('keeps the choice of %s through a read that finds no recipe', (_choice, choose, recipe) => {
    const wizard = chosen(choose)

    readThrough(wizard, answered(NO_RECIPE))

    expect(wizard).toMatchObject({ step: 'photos', mode: 'variant', recipe, returnTo: 'review' })
  })

  const choiceThenUnread = CHOICES.flatMap(([choice, choose, recipe]) =>
    UNREAD.map(([outcome, result]) => ({ choice, choose, recipe, outcome, result })),
  )

  it.each(choiceThenUnread)(
    'keeps the choice of $choice on Back to review after a read that $outcome',
    ({ choose, recipe, result }) => {
      const wizard = chosen(choose)
      readThrough(wizard, result)

      backToReview(wizard)

      expect(wizard).toMatchObject({ step: 'review', mode: 'variant', recipe, matchSettled: true })
    },
  )

  it('keeps the page recipe of the Add Variant flow through every read', () => {
    const wizard = matched(variantWizard(true))

    readThrough(wizard, answered(PANCAKES))

    expect(wizard).toMatchObject({ mode: 'variant', recipe: GNOCCHI })
  })
})
