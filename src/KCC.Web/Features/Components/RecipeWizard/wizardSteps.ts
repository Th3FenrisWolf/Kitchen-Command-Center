import type { Component, FunctionalComponent } from 'vue'
import { laterPart, prefetchPart, registerLaterPart } from '~/Islands/laterParts'
import { createWizardFlow, type WizardMode } from './recipeWizardFlow'
import StepPending from './StepPending.vue'

const loaders = import.meta.glob<Component>(['./*Step.vue', '!./StartStep.vue'], {
  import: 'default',
})

const steps = new Map(
  Object.entries(loaders).map(([path, load]) => [path.slice(2, -'.vue'.length), laterPart(load, StepPending)]),
)

// A step's prop and emit types cannot follow a name through the glob, so a later step is typed loosely and the step's
// own file stays the one place its contract is written.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type LaterStep = FunctionalComponent<Record<string, any>, Record<string, (...args: any[]) => unknown>>

export const laterStepNames: readonly string[] = [...steps.keys()]

export function laterStep(name: string): LaterStep {
  const step = steps.get(name)
  if (!step) throw new Error(`No later wizard step named ${name}: a later step is a Components/RecipeWizard/*Step.vue file.`)
  return step as LaterStep
}

const irregularNames: Record<string, string> = { photos: 'PhotoStep' }

const partOf = (step: string) => steps.get(irregularNames[step] ?? `${step.charAt(0).toUpperCase()}${step.slice(1)}Step`)

export function prefetchOpeningStep({
  flow,
  importEnabled,
}: {
  flow: WizardMode
  importEnabled: boolean
}): Promise<unknown> {
  const { step } = createWizardFlow({ flow, importEnabled, availableAgainOn: null })
  const part = partOf(step)
  return part ? prefetchPart(part) : Promise.resolve()
}

// With import off the server renders a later step first, and Vue would hydrate it only once its chunk arrived. The
// island is the page's view, so the view carries this as its `beforeHydrate`.
export const beforeWizardHydrates = (flow: WizardMode) => (props: Record<string, unknown>) =>
  prefetchOpeningStep({
    flow,
    importEnabled: (props.photoImport as { enabled?: boolean } | undefined)?.enabled ?? false,
  })

const stepsOutOfStart = ['BasicsStep', 'VariantStep']

export async function prefetchLaterSteps(): Promise<void> {
  await Promise.all(stepsOutOfStart.map((name) => prefetchPart(laterStep(name))))
  await Promise.all([...steps].filter(([name]) => !stepsOutOfStart.includes(name)).map(([, step]) => prefetchPart(step)))
}

registerLaterPart(prefetchLaterSteps)
