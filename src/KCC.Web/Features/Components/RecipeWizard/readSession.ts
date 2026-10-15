import type { UseWakeLock } from '~/Pages/VariantDetail/useWakeLock'
import { readRecipe, type ReadRequest, type ReadResult } from './recipeImportApi'

export type ScreenWakeLock = Pick<UseWakeLock, 'request' | 'release'>

export interface ReadSession {
  result: Promise<ReadResult>
  cancel: () => void
}

export function startReadSession(
  request: ReadRequest,
  wakeLock: ScreenWakeLock,
  read: typeof readRecipe = readRecipe,
): ReadSession {
  const controller = new AbortController()
  void wakeLock.request()
  const result = read(request, controller.signal).finally(() => void wakeLock.release())
  return { result, cancel: () => controller.abort() }
}
