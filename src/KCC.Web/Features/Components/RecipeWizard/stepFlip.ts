import type { InjectionKey, Ref } from 'vue'
import { listTearFor } from '~/Utilities/BrandColor'

export interface StepSheet {
  step: Readonly<Ref<number>>
  stripFailed: Ref<boolean>
}

export const STEP_SHEET: InjectionKey<StepSheet> = Symbol('stepSheet')

const turnsOver = (tear: number): boolean => tear % 2 === 1

// A step's blank turns against the hero's. The failed photo strip's sheet lies between the two, so it takes that
// turn and the step takes the hero's.
export function stepFlips(step: number, followsFailedStrip = false): boolean {
  return turnsOver(listTearFor(step - 1)) !== followsFailedStrip
}

export function failedStripSheet(step: number): { tear: ReturnType<typeof listTearFor>; flip: boolean } {
  const tear = listTearFor(step)
  return { tear, flip: turnsOver(tear) }
}
