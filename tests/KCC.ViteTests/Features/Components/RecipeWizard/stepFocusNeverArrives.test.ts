// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, h, nextTick } from 'vue'
import RecipeWizard from '~/Components/RecipeWizard/RecipeWizard.vue'
import { prefetchOpeningStep } from '~/Components/RecipeWizard/wizardSteps'
import { ResourceString } from '~/Components/ResourceStrings'
import { wizardStrings } from '../../../support/recipeWizard'

vi.mock('~/Components/RecipeWizard/IngredientsStep.vue', () => new Promise(() => undefined))

const IMPORT_OFF = { enabled: false, available: false, maxImageEdge: 0 }
const RECIPE = { key: '11111111-2222-3333-4444-555555555555', name: 'Brown Butter Gnocchi' }

let frames: FrameRequestCallback[] = []

async function settle(ms = 1) {
  for (let i = 0; i < 5; i++) await nextTick()
  await vi.advanceTimersByTimeAsync(ms)
  frames.splice(0).forEach((frame) => frame(performance.now()))
  await vi.advanceTimersByTimeAsync(1)
}

describe('a wizard step that never arrives', () => {
  beforeEach(() => {
    frames = []
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    vi.stubGlobal('requestAnimationFrame', (frame: FrameRequestCallback) => frames.push(frame))
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
  })

  it('still moves focus off the body, onto the step section', async () => {
    await prefetchOpeningStep({ flow: 'variant', importEnabled: false })
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
    next.focus()

    next.click()
    await settle()
    expect(host.querySelector('[data-later-pending]')).not.toBeNull()

    await settle(60_000)

    expect(document.activeElement).toBe(host.querySelector('section'))
    app.unmount()
  })
})
