// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, h, nextTick, ref } from 'vue'
import { ResourceString } from '~/Components/ResourceStrings'
import { laterPart } from '~/Islands/laterParts'
import CookModePending from '~/Pages/VariantDetail/CookModePending.vue'

const STRINGS = {
  'VariantDetail.CookMode': 'Cook mode',
  'VariantDetail.Close': 'Close',
  'VariantDetail.UnexpectedError': 'Something went wrong. Please try again.',
  'VariantDetail.TryAgain': 'Try again',
}

const unmounts: (() => void)[] = []

async function settle() {
  for (let i = 0; i < 4; i++) await nextTick()
}

function mountPending(loader: () => Promise<never> = () => new Promise<never>(() => undefined)) {
  const CookMode = laterPart(loader, CookModePending)
  const open = ref(true)
  const host = document.createElement('div')
  document.body.append(host)
  const app = createApp({
    render: () =>
      h(CookMode, {
        open: open.value,
        resourceStrings: STRINGS,
        onClose: () => (open.value = false),
      }),
  })
  app.component('ResourceString', ResourceString).mount(host)
  unmounts.push(() => app.unmount())
  return { open, dialog: () => document.querySelector<HTMLElement>('[role="dialog"]') }
}

const press = (key: string, shiftKey = false) => {
  const event = new KeyboardEvent('keydown', { key, shiftKey, bubbles: true, cancelable: true })
  document.dispatchEvent(event)
  return event
}

describe('cook mode while its chunk is on the way', () => {
  beforeEach(() => vi.useFakeTimers())

  afterEach(() => {
    vi.useRealTimers()
    unmounts.splice(0).forEach((unmount) => unmount())
    vi.restoreAllMocks()
    document.body.innerHTML = ''
  })

  it('is a busy modal dialog named cook mode', async () => {
    const { dialog } = mountPending()
    await settle()

    expect(dialog()?.getAttribute('aria-modal')).toBe('true')
    expect(dialog()?.getAttribute('aria-busy')).toBe('true')
    expect(dialog()?.getAttribute('aria-label')).toBe('Cook mode')
  })

  it('closes on Escape', async () => {
    const { open, dialog } = mountPending()
    await settle()

    expect(press('Escape').defaultPrevented).toBe(true)
    await settle()

    expect(open.value).toBe(false)
    expect(dialog()).toBeNull()
  })

  it('closes from its close control', async () => {
    const { open } = mountPending()
    await settle()

    document.querySelector<HTMLButtonElement>('[data-test="cook-close"]')!.click()
    await settle()

    expect(open.value).toBe(false)
  })

  it('moves focus to its close control on open', async () => {
    mountPending()
    await settle()

    expect(document.activeElement).toBe(document.querySelector('[data-test="cook-close"]'))
  })

  it('keeps the kit focus ring on its panel and sets the Retry row a rule below the error well', async () => {
    vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    mountPending(() => Promise.reject(new Error('chunk blocked')))
    await vi.runAllTimersAsync()
    await settle()

    expect(document.querySelector('[role="dialog"] [tabindex="-1"]')?.classList).not.toContain('outline-none')
    expect(document.querySelector('[data-test="later-part-retry"]')?.closest('.kcc-row')?.classList).toContain('mt-rule-1')
  })

  it('keeps focus inside the dialog', async () => {
    mountPending()
    await settle()
    const close = document.querySelector<HTMLButtonElement>('[data-test="cook-close"]')!
    close.focus()

    expect(press('Tab').defaultPrevented).toBe(true)
    expect(document.activeElement).toBe(close)
  })

  it('shows the error state with a Retry that loads cook mode again, once its chunk never arrives', async () => {
    vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const loader = vi.fn(() => Promise.reject(new Error('chunk blocked')))
    const { dialog } = mountPending(loader)
    await vi.runAllTimersAsync()
    await settle()

    expect(dialog()?.hasAttribute('aria-busy')).toBe(false)
    expect(dialog()?.querySelector('[role="alert"]')?.textContent).toContain('Something went wrong')
    const retry = document.querySelector<HTMLButtonElement>('[data-test="later-part-retry"]')!
    expect(retry.textContent).toContain('Try again')

    retry.click()
    await settle()

    expect(loader).toHaveBeenCalledTimes(4)
  })
})
