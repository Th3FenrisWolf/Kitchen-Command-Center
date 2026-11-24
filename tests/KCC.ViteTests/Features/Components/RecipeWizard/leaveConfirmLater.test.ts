// @vitest-environment happy-dom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createApp, h, nextTick } from 'vue'
import RecipeWizard from '~/Components/RecipeWizard/RecipeWizard.vue'
import { laterStep } from '~/Components/RecipeWizard/wizardSteps'
import { prefetchPart } from '~/Islands/laterParts'
import { ResourceString } from '~/Components/ResourceStrings'
import { wizardStrings } from '../../../support/recipeWizard'

const chunk = vi.hoisted(() => {
  let arrive: () => void = () => undefined
  const arrived = new Promise<void>((resolve) => (arrive = resolve))
  return { arrived, arrive: () => arrive() }
})

vi.mock('~/Components/RecipeWizard/photoEncoding', async (importOriginal) => ({
  ...(await importOriginal<object>()),
  encodePhoto: () => new Promise(() => undefined),
}))

vi.mock('~/Components/RecipeWizard/LeaveConfirm.vue', async (importOriginal) => {
  await chunk.arrived
  return importOriginal()
})

const IMPORT_ON = { enabled: true, available: true, maxImageEdge: 2000 }

async function settle() {
  for (let i = 0; i < 5; i++) await nextTick()
  await new Promise((resolve) => setTimeout(resolve))
}

describe('a guarded link clicked before the leave dialog has arrived', () => {
  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('holds the navigation, waits for the dialog and then asks', async () => {
    // The held chunk resolves through the real module, whose first transform takes wall-clock time that a busy suite
    // stretches past any number of ticks; loaded here, its arrival settles within them.
    await vi.importActual('~/Components/RecipeWizard/LeaveConfirm.vue')
    await prefetchPart(laterStep('PhotoStep'))
    const host = document.createElement('div')
    document.body.append(host)
    const app = createApp({
      render: () => h(RecipeWizard, { flow: 'recipe', photoImport: IMPORT_ON, resourceStrings: wizardStrings() }),
    })
    app.component('ResourceString', ResourceString).mount(host)
    await settle()
    host.querySelector<HTMLButtonElement>('[data-testid="wizard-start-photos"]')!.click()
    await settle()
    const input = host.querySelector<HTMLInputElement>('[data-testid="photo-input"]')!
    Object.defineProperty(input, 'files', { value: [new File(['photo'], 'card.jpg', { type: 'image/jpeg' })] })
    input.dispatchEvent(new Event('change'))
    await settle()
    const link = document.createElement('a')
    link.href = '/recipes/'
    document.body.append(link)

    const click = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 })
    link.dispatchEvent(click)
    await settle()

    expect(click.defaultPrevented).toBe(true)
    expect(document.querySelector('[data-testid="leave-dialog"]')).toBeNull()

    chunk.arrive()
    await settle()
    await settle()

    expect(document.querySelector('[data-testid="leave-dialog"]')).not.toBeNull()
    app.unmount()
  })
})
