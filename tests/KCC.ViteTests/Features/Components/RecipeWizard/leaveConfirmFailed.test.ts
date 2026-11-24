// @vitest-environment happy-dom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createApp, h, nextTick } from 'vue'
import RecipeWizard from '~/Components/RecipeWizard/RecipeWizard.vue'
import { laterStep } from '~/Components/RecipeWizard/wizardSteps'
import { prefetchPart } from '~/Islands/laterParts'
import { ResourceString } from '~/Components/ResourceStrings'
import { wizardStrings } from '../../../support/recipeWizard'

vi.mock('~/Components/RecipeWizard/photoEncoding', async (importOriginal) => ({
  ...(await importOriginal<object>()),
  encodePhoto: () => new Promise(() => undefined),
}))

vi.mock('~/Components/RecipeWizard/LeaveConfirm.vue', () => {
  throw new Error('chunk blocked')
})

const IMPORT_ON = { enabled: true, available: true, maxImageEdge: 2000 }

async function settle() {
  for (let i = 0; i < 5; i++) await nextTick()
  await new Promise((resolve) => setTimeout(resolve))
}

describe('a guarded link clicked when the leave dialog cannot load', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    document.body.innerHTML = ''
  })

  it("navigates anyway, so the browser's own leave prompt asks instead", async () => {
    const assign = vi.spyOn(window.location, 'assign').mockImplementation(() => undefined)
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
    const unload = new Event('beforeunload', { cancelable: true })

    const click = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 })
    link.dispatchEvent(click)
    window.dispatchEvent(unload)

    expect(click.defaultPrevented).toBe(true)
    expect(unload.defaultPrevented).toBe(true)
    await vi.waitFor(() => expect(assign).toHaveBeenCalledWith(link.href), { timeout: 6000 })
    expect(document.querySelector('[data-testid="leave-dialog"]')).toBeNull()
    app.unmount()
  }, 10_000)
})
