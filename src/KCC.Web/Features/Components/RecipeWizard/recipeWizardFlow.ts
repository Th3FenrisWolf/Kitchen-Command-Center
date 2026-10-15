import type { Ingredient } from '~/Types/Recipe'
import type { EncodedPhoto } from './photoEncoding'
import type { ReadRequest, ReadResponse, ReadResult, RecipeChoice } from './recipeImportApi'
import { recipeWizardKeys } from './recipeWizardKeys'
import { readErrorKey, restingUntil, type ReadErrorKey } from './wizardErrors'
import {
  emptyIngredient,
  emptyInstruction,
  emptyValues,
  hasName,
  hasText,
  isFilled,
  recipeRequest,
  valuesFromRead,
  variantRequest,
  type MarkedValue,
  type RecipeRequest,
  type VariantRequest,
  type WizardValues,
} from './wizardValues'

export type WizardMode = 'recipe' | 'variant'

export type WizardStart = 'photos' | 'typing'

export type StepId =
  'start' | 'photos' | 'reading' | 'match' | 'basics' | 'variant' | 'ingredients' | 'instructions' | 'review' | 'submitted'

export type PhotoSource = 'camera' | 'device'

export type PhotoErrorKey = 'TooManyPhotos' | 'UnsupportedFile'

export type ReviewSection = 'basics' | 'variant' | 'ingredients' | 'instructions'

export type SingleValue = Exclude<keyof WizardValues, 'ingredients' | 'instructions' | 'marks'>

export type ValueEdit<K extends SingleValue> = { [F in K]: [field: F, value: WizardValues[F]] }[K]

export interface WizardRecipe {
  key: string
  name: string
}

export type RecipeLevelValues = Pick<WizardValues, 'recipeName' | 'recipeDescription' | 'marks'>

export interface Submission {
  url: string
  body: RecipeRequest | VariantRequest
}

export interface WizardPhoto {
  id: string
  blob: Blob
  url: string
  source: PhotoSource
}

export interface WizardFlow {
  flow: WizardMode
  mode: WizardMode
  recipe: WizardRecipe | null
  setAside: RecipeLevelValues | null
  matchSettled: boolean
  importEnabled: boolean
  availableAgainOn: string | null
  start: WizardStart | null
  step: StepId
  returnTo: 'review' | null
  values: WizardValues
  photos: WizardPhoto[]
  pendingPhotos: number
  photoErrors: PhotoErrorKey[]
  read: ReadResponse | null
  readError: ReadErrorKey | null
  noRecipeFound: boolean
  editedSinceRead: boolean
  confirmingRead: boolean
}

export interface WizardFlowOptions {
  flow: WizardMode
  recipe?: WizardRecipe
  importEnabled: boolean
  availableAgainOn?: string | null
}

export interface StepProgress {
  current: number
  total: number
}

export const MAX_PHOTOS = 10

const TYPING_STEPS = {
  recipe: ['basics', 'variant', 'ingredients', 'instructions', 'review'],
  variant: ['variant', 'ingredients', 'instructions', 'review'],
} as const satisfies Record<WizardMode, readonly StepId[]>

const PHOTO_STEPS: readonly StepId[] = ['start', 'photos', 'reading', 'review']

const MATCHED_PHOTO_STEPS: readonly StepId[] = ['start', 'photos', 'reading', 'match', 'review']

const PHOTO_STRIP_STEPS: readonly StepId[] = [
  'reading',
  'match',
  'basics',
  'variant',
  'ingredients',
  'instructions',
  'review',
]

const RECIPE_MARKS: readonly MarkedValue[] = ['recipe.name', 'recipe.description']

const MARKED_VALUES: Record<SingleValue, MarkedValue> = {
  recipeName: 'recipe.name',
  recipeDescription: 'recipe.description',
  variantName: 'variant.name',
  variantDescription: 'variant.description',
  prepTime: 'variant.prepTime',
  cookTime: 'variant.cookTime',
  servings: 'variant.servings',
}

const PROCEED_RULES: Partial<Record<StepId, (values: WizardValues) => boolean>> = {
  basics: ({ recipeName, recipeDescription }) => isFilled(recipeName) && isFilled(recipeDescription),
  variant: ({ variantName }) => isFilled(variantName),
  ingredients: ({ ingredients }) => ingredients.some(hasName),
  instructions: ({ instructions }) => instructions.some(hasText),
}

let photosAdded = 0

export function createWizardFlow({ flow, recipe, importEnabled, availableAgainOn }: WizardFlowOptions): WizardFlow {
  return {
    flow,
    mode: flow,
    recipe: recipe ?? null,
    setAside: null,
    matchSettled: false,
    importEnabled,
    availableAgainOn: importEnabled ? (availableAgainOn ?? null) : null,
    start: importEnabled ? null : 'typing',
    step: importEnabled ? 'start' : TYPING_STEPS[flow][0],
    returnTo: null,
    values: emptyValues(),
    photos: [],
    pendingPhotos: 0,
    photoErrors: [],
    read: null,
    readError: null,
    noRecipeFound: false,
    editedSinceRead: false,
    confirmingRead: false,
  }
}

export function stepOrder(wizard: WizardFlow): StepId[] {
  const { importEnabled, mode, start } = wizard
  if (start === 'photos') return [...(offersMatch(wizard) ? MATCHED_PHOTO_STEPS : PHOTO_STEPS)]
  return importEnabled ? ['start', ...TYPING_STEPS[mode]] : [...TYPING_STEPS[mode]]
}

export const offersMatch = ({ flow, read }: WizardFlow): boolean => flow === 'recipe' && Boolean(read?.match)

export function chooseTyping(wizard: WizardFlow): void {
  wizard.start = 'typing'
  moveTo(wizard, TYPING_STEPS[wizard.mode][0])
  wizard.returnTo = null
}

export function choosePhotos(wizard: WizardFlow): void {
  if (wizard.availableAgainOn) return

  wizard.start = 'photos'
  changePhotos(wizard)
}

export const isImport = ({ start }: WizardFlow): boolean => start === 'photos'

export const canProceed = (wizard: WizardFlow): boolean => PROCEED_RULES[wizard.step]?.(wizard.values) ?? false

export const canGoBack = (wizard: WizardFlow): boolean => previousStep(wizard) !== undefined

export function next(wizard: WizardFlow): void {
  if (!canProceed(wizard)) return
  if (wizard.returnTo === 'review') {
    backToReview(wizard)
    return
  }

  const step = neighbour(wizard, 1)
  if (step) moveTo(wizard, step)
}

export function back(wizard: WizardFlow): void {
  if ((wizard.step === 'review' || wizard.step === 'match') && isImport(wizard)) {
    changePhotos(wizard)
    return
  }

  const step = previousStep(wizard)
  if (step) moveTo(wizard, step)
}

export function edit(wizard: WizardFlow, section: ReviewSection): void {
  if (wizard.step !== 'review' || (section === 'basics' && wizard.mode !== 'recipe')) return

  moveTo(wizard, section)
  wizard.returnTo = 'review'
}

export function backToReview(wizard: WizardFlow): void {
  moveTo(wizard, 'review')
  wizard.returnTo = null
}

export function changePhotos(wizard: WizardFlow): void {
  moveTo(wizard, 'photos')
  wizard.returnTo = wizard.read ? 'review' : null
}

export function progress({ mode, step }: WizardFlow): StepProgress | null {
  const steps: readonly StepId[] = TYPING_STEPS[mode]
  const at = steps.indexOf(step)
  return at < 0 ? null : { current: at + 1, total: steps.length }
}

export function addPhotos(wizard: WizardFlow, encoded: EncodedPhoto[], source: PhotoSource): void {
  const refusals = new Set<PhotoErrorKey>()
  for (const photo of encoded) {
    const refusal = keepPhoto(wizard, photo, source)
    if (refusal) refusals.add(refusal)
  }
  wizard.photoErrors = [...refusals]
}

export async function addPendingPhotos(
  wizard: WizardFlow,
  pending: Promise<EncodedPhoto | null>[],
  source: PhotoSource,
): Promise<void> {
  wizard.pendingPhotos += pending.length
  try {
    const arrived = (await Promise.allSettled(pending)).flatMap((settled) =>
      settled.status === 'fulfilled' && settled.value ? [settled.value] : [],
    )
    if (arrived.length) addPhotos(wizard, arrived, source)
  } finally {
    wizard.pendingPhotos -= pending.length
  }
}

export function removePhoto(wizard: WizardFlow, id: string): void {
  const at = wizard.photos.findIndex((photo) => photo.id === id)
  if (at < 0) return

  const [removed] = wizard.photos.splice(at, 1)
  URL.revokeObjectURL(removed.url)
  wizard.photoErrors = []
}

export const canStartRead = (wizard: WizardFlow): boolean =>
  (wizard.step === 'photos' || (wizard.step === 'review' && isImport(wizard))) &&
  wizard.availableAgainOn === null &&
  wizard.photos.length > 0 &&
  wizard.pendingPhotos === 0

export function startRead(wizard: WizardFlow, confirmed = false): ReadRequest | null {
  if (!canStartRead(wizard)) return null
  if (wizard.editedSinceRead && !confirmed) {
    wizard.confirmingRead = true
    return null
  }

  if (wizard.step === 'review') wizard.returnTo = 'review'
  moveTo(wizard, 'reading')
  wizard.photoErrors = []
  wizard.readError = null
  wizard.noRecipeFound = false

  const request: ReadRequest = { flow: wizard.flow, photos: wizard.photos.map(({ blob }) => blob) }
  if (wizard.flow === 'variant' && wizard.recipe) request.recipeKey = wizard.recipe.key
  return request
}

export function finishRead(wizard: WizardFlow, result: ReadResult): void {
  if (result.success) {
    applyRead(wizard, result.data)
    return
  }

  moveTo(wizard, 'photos')
  if ('aborted' in result) return

  const day = restingUntil(result)
  if (day) {
    wizard.availableAgainOn = day
    return
  }

  const key = readErrorKey(result)
  wizard.readError = key === recipeWizardKeys.ImportUnavailable ? recipeWizardKeys.ReadFailed : key
}

export function keepEdits(wizard: WizardFlow): void {
  wizard.confirmingRead = false
}

export function applyRead(wizard: WizardFlow, read: ReadResponse): void {
  if (read.outcome === 'noRecipe') {
    wizard.noRecipeFound = true
    moveTo(wizard, 'photos')
    return
  }

  wizard.values = valuesFromRead(read)
  wizard.read = read
  wizard.editedSinceRead = false
  wizard.matchSettled = false
  if (wizard.flow === 'recipe') {
    wizard.mode = 'recipe'
    wizard.recipe = null
    wizard.setAside = null
  }

  if (offersMatch(wizard)) {
    moveTo(wizard, 'match')
    wizard.returnTo = null
  } else {
    backToReview(wizard)
  }
}

export function acceptMatch(wizard: WizardFlow): void {
  const match = wizard.read?.match
  if (match) chooseRecipe(wizard, { key: match.recipeKey, name: match.recipeName })
}

export function pickRecipe(wizard: WizardFlow, recipe: RecipeChoice): void {
  if (wizard.read?.recipes?.some(({ key }) => key === recipe.key)) chooseRecipe(wizard, recipe)
}

export function keepNewRecipe(wizard: WizardFlow): void {
  if (wizard.step !== 'match') return

  if (wizard.setAside) {
    const { recipeName, recipeDescription, marks } = wizard.setAside
    Object.assign(wizard.values, { recipeName, recipeDescription, marks: { ...wizard.values.marks, ...marks } })
  }
  wizard.mode = 'recipe'
  wizard.recipe = null
  wizard.setAside = null
  wizard.matchSettled = true
  backToReview(wizard)
}

export function changeMatch(wizard: WizardFlow): void {
  if (wizard.step === 'review' && offersMatch(wizard)) moveTo(wizard, 'match')
}

export const submissionFor = ({ mode, recipe, values }: WizardFlow): Submission =>
  mode === 'variant' && recipe
    ? { url: `/api/recipes/${recipe.key}/variants`, body: variantRequest(values) }
    : { url: '/api/recipes', body: recipeRequest(values) }

export const showsPhotoStrip = ({ photos, step }: WizardFlow): boolean =>
  photos.length > 0 && PHOTO_STRIP_STEPS.includes(step)

export const hasUnsavedWork = ({ photos, pendingPhotos, read, step }: WizardFlow): boolean =>
  step !== 'submitted' && (photos.length > 0 || pendingPhotos > 0 || read !== null)

export function editValue<K extends SingleValue>(wizard: WizardFlow, field: K, value: WizardValues[K]): void {
  const { values } = wizard
  values[field] = value
  values.marks = Object.fromEntries(Object.entries(values.marks).filter(([path]) => path !== MARKED_VALUES[field]))
  wizard.editedSinceRead = true
}

export function editIngredient(wizard: WizardFlow, index: number, change: Partial<Ingredient>): void {
  const row = wizard.values.ingredients[index]
  Object.assign(row, change)
  delete row.mark
  wizard.editedSinceRead = true
}

export function editInstruction(wizard: WizardFlow, index: number, text: string): void {
  const row = wizard.values.instructions[index]
  row.text = text
  delete row.mark
  wizard.editedSinceRead = true
}

export function addIngredient(wizard: WizardFlow): void {
  wizard.values.ingredients.push(emptyIngredient())
  wizard.editedSinceRead = true
}

export function removeIngredient(wizard: WizardFlow, index: number): void {
  wizard.values.ingredients.splice(index, 1)
  wizard.editedSinceRead = true
}

export function addInstruction(wizard: WizardFlow): void {
  wizard.values.instructions.push(emptyInstruction())
  wizard.editedSinceRead = true
}

export function removeInstruction(wizard: WizardFlow, index: number): void {
  wizard.values.instructions.splice(index, 1)
  wizard.editedSinceRead = true
}

function keepPhoto(wizard: WizardFlow, encoded: EncodedPhoto, source: PhotoSource): PhotoErrorKey | null {
  if (wizard.photos.length >= MAX_PHOTOS) return recipeWizardKeys.TooManyPhotos
  if ('refusal' in encoded) return encoded.refusal

  const { blob } = encoded
  wizard.photos.push({ id: `photo-${++photosAdded}`, blob, url: URL.createObjectURL(blob), source })
  return null
}

function chooseRecipe(wizard: WizardFlow, recipe: WizardRecipe): void {
  if (wizard.step !== 'match' || !offersMatch(wizard)) return

  if (wizard.mode === 'recipe') wizard.setAside = setRecipeValuesAside(wizard.values)
  wizard.mode = 'variant'
  wizard.recipe = { key: recipe.key, name: recipe.name }
  wizard.matchSettled = true
  backToReview(wizard)
}

function setRecipeValuesAside(values: WizardValues): RecipeLevelValues {
  const marks = Object.entries(values.marks)
  const isRecipeMark = ([path]: [string, unknown]) => RECIPE_MARKS.includes(path as MarkedValue)
  const aside: RecipeLevelValues = {
    recipeName: values.recipeName,
    recipeDescription: values.recipeDescription,
    marks: Object.fromEntries(marks.filter(isRecipeMark)),
  }

  values.recipeName = ''
  values.recipeDescription = ''
  values.marks = Object.fromEntries(marks.filter((mark) => !isRecipeMark(mark)))
  return aside
}

function moveTo(wizard: WizardFlow, step: StepId): void {
  wizard.step = step === 'review' && offersMatch(wizard) && !wizard.matchSettled ? 'match' : step
  wizard.confirmingRead = false
}

const previousStep = (wizard: WizardFlow): StepId | undefined =>
  wizard.step === 'match' && !isImport(wizard) ? 'instructions' : neighbour(wizard, -1)

function neighbour(wizard: WizardFlow, offset: 1 | -1): StepId | undefined {
  const order = walkableSteps(wizard)
  const at = order.indexOf(wizard.step)
  return at < 0 ? undefined : order[at + offset]
}

const walkableSteps = (wizard: WizardFlow): StepId[] => stepOrder(wizard).filter((step) => step !== 'reading')
