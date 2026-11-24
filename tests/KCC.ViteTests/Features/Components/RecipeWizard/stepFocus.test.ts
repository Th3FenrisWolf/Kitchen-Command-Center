// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, h, nextTick } from 'vue'
import RecipeWizard from '~/Components/RecipeWizard/RecipeWizard.vue'
import { prefetchLaterSteps } from '~/Components/RecipeWizard/wizardSteps'
import { ResourceString } from '~/Components/ResourceStrings'
import { wizardStrings } from '../../../support/recipeWizard'

const IMPORT_OFF = { enabled: false, available: false, maxImageEdge: 0 }
const RECIPE = { key: '11111111-2222-3333-4444-555555555555', name: 'Brown Butter Gnocchi' }

let frames: FrameRequestCallback[] = []

async function settle() {
  for (let i = 0; i < 5; i++) await nextTick()
  await new Promise((resolve) => setTimeout(resolve))
}

async function paintFrame() {
  const due = frames
  frames = []
  for (const frame of due) frame(performance.now())
  await new Promise((resolve) => setTimeout(resolve))
}

async function variantWizardFilledIn() {
  await prefetchLaterSteps()
  const host = document.createElement('div')
  document.body.append(host)
  const app = createApp({
    render: () =>
      h(RecipeWizard, { flow: 'variant', recipe: RECIPE, photoImport: IMPORT_OFF, resourceStrings: wizardStrings() }),
  })
  app.component('ResourceString', ResourceString).mount(host)
  await settle()

  const name = host.querySelector<HTMLInputElement>('[data-testid="wizard-variant-name"]')!
  name.value = 'First Try'
  name.dispatchEvent(new Event('input'))
  await settle()

  const next = host.querySelector<HTMLButtonElement>('button[type="submit"]')!
  return { host, next, unmount: () => (app.unmount(), host.remove()) }
}

describe('a wizard step change', () => {
  beforeEach(() => {
    frames = []
    vi.stubGlobal('requestAnimationFrame', (frame: FrameRequestCallback) => frames.push(frame))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
  })

  // Removing the focused control makes the browser recalculate style for the whole step before it can move focus.
  it('lets go of the pressed control while it is still in the page', async () => {
    const { next, unmount } = await variantWizardFilledIn()
    next.focus()
    const connectedAtBlur: boolean[] = []
    next.addEventListener('blur', () => connectedAtBlur.push(next.isConnected))

    next.click()
    await settle()

    expect(next.isConnected).toBe(false)
    expect(connectedAtBlur).toEqual([true])
    unmount()
  })

  // Focusing inside the frame that swaps the step forces a second recalculation before that frame can paint.
  it("focuses the new step's heading only after the swapped step has painted", async () => {
    const { host, next, unmount } = await variantWizardFilledIn()
    next.focus()

    next.click()
    await settle()
    const heading = host.querySelector('h2')
    expect(heading?.textContent).toContain('Ingredients')
    expect(document.activeElement).not.toBe(heading)

    await paintFrame()
    expect(document.activeElement).toBe(heading)
    unmount()
  })
})
