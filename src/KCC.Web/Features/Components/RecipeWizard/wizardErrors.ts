import type { ApiFailure } from '~/Utilities/Api'
import { recipeWizardKeys } from './recipeWizardKeys'

export type SubmitErrorKey = 'SubmitFailed' | 'SignInAgain' | 'RecipeNotFound' | 'TooManyAttempts' | 'UnexpectedError'

export type NameField = 'recipeName' | 'variantName'

export type ReadErrorKey =
  | 'PhotosRejected'
  | 'ReadFailed'
  | 'SignInAgain'
  | 'RecipeNotFound'
  | 'ImportUnavailable'
  | 'TooManyAttempts'
  | 'ImportOff'
  | 'ReadTimedOut'

interface ReadErrorRow {
  status: number
  code?: string
  key: ReadErrorKey
}

const SUBMIT_ERRORS: Partial<Record<number, SubmitErrorKey>> = {
  400: recipeWizardKeys.SubmitFailed,
  401: recipeWizardKeys.SignInAgain,
  404: recipeWizardKeys.RecipeNotFound,
  429: recipeWizardKeys.TooManyAttempts,
}

const READ_ERRORS: ReadErrorRow[] = [
  { status: 400, code: 'invalidPhotos', key: recipeWizardKeys.PhotosRejected },
  { status: 401, key: recipeWizardKeys.SignInAgain },
  { status: 404, code: 'recipeNotFound', key: recipeWizardKeys.RecipeNotFound },
  { status: 409, code: 'ceilingReached', key: recipeWizardKeys.ImportUnavailable },
  { status: 429, key: recipeWizardKeys.TooManyAttempts },
  { status: 503, code: 'unavailable', key: recipeWizardKeys.ImportOff },
  { status: 504, code: 'readTimedOut', key: recipeWizardKeys.ReadTimedOut },
]

export const submitErrorKey = ({ status }: ApiFailure): SubmitErrorKey =>
  SUBMIT_ERRORS[status] ?? recipeWizardKeys.UnexpectedError

const isNameField = (value: unknown): value is NameField => value === 'recipeName' || value === 'variantName'

export function refusedName({ status, code, body }: ApiFailure): NameField | null {
  const field = body?.field
  return status === 400 && code === 'nameTooLong' && isNameField(field) ? field : null
}

export const readErrorKey = (failure: ApiFailure): ReadErrorKey =>
  READ_ERRORS.find(({ status, code }) => status === failure.status && (code === undefined || code === failure.code))?.key ??
  recipeWizardKeys.ReadFailed

export function restingUntil(failure: ApiFailure): string | null {
  const day = failure.body?.availableAgainOn
  return readErrorKey(failure) === recipeWizardKeys.ImportUnavailable && typeof day === 'string' && day ? day : null
}
