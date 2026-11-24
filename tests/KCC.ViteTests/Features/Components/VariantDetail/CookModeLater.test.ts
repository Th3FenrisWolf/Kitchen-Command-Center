// @vitest-environment happy-dom
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, h, nextTick } from 'vue'
import '~/Utilities/StringExtensions'

const chunk = vi.hoisted(() => {
  let release = () => {}
  const arrived = new Promise<void>((resolve) => (release = resolve))
  return { arrived, release: () => release(), requested: 0 }
})

vi.mock('~/Pages/VariantDetail/CookMode.vue', async (importOriginal) => {
  chunk.requested++
  const actual = await importOriginal<typeof import('~/Pages/VariantDetail/CookMode.vue')>()
  await chunk.arrived
  return actual
})

const view = readFileSync(
  join(import.meta.dirname, '../../../../../src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue'),
  'utf8',
)

const PROPS = {
  variantName: 'Weeknight',
  variantDescription: 'Fast',
  tags: [],
  ingredients: [{ name: 'Gnocchi', quantity: '1', unit: 'lb', isEyeballed: false }],
  instructions: [{ text: 'Boil the gnocchi.' }, { text: 'Brown the butter.' }],
  recipeName: 'Brown Butter Gnocchi',
  recipeSlug: 'brown-butter-gnocchi',
  siblingVariants: [],
  variantGuid: '11111111-2222-3333-4444-555555555555',
  servings: 2,
}

const flush = async () => {
  for (let i = 0; i < 10; i++) await nextTick()
}

describe('cook mode as a later part', () => {
  beforeEach(() => vi.stubGlobal('fetch', () => new Promise(() => {})))

  afterEach(() => {
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
  })

  it('reaches the variant view only through defineAsyncComponent, never a static import', () => {
    expect(view).not.toMatch(/^\s*import\s+[\w{},\s]+\s+from\s+['"][^'"]*\/CookMode\.vue['"]/m)
    expect(view).toMatch(/defineAsyncComponent|laterPart\(/)
  })

  it('opens at once with its pending sheet, then fills in when its chunk arrives', async () => {
    const { default: VariantDetailView } = await import('~/Pages/VariantDetail/VariantDetailView.Component.vue')
    const host = document.createElement('div')
    document.body.append(host)
    const app = createApp({ render: () => h(VariantDetailView, PROPS) })
    app.mount(host)
    await flush()

    expect(chunk.requested).toBe(0)

    host.querySelector<HTMLButtonElement>('[data-test="cook-mode-open-desktop"]')!.click()
    await flush()

    const pending = document.body.querySelector('.fixed.inset-0 .kcc-slip')
    expect(pending?.closest('[role="dialog"]')?.getAttribute('aria-busy')).toBe('true')
    expect(pending?.hasAttribute('data-later-pending')).toBe(true)
    expect(pending?.classList.contains('kcc-tear-3')).toBe(true)
    expect(pending?.textContent?.trim()).toBe('')

    chunk.release()
    await vi.waitFor(() => expect(document.body.querySelector('[data-test="cook-progress"]')).not.toBeNull())
    expect(document.body.querySelector('.fixed[aria-busy="true"], .fixed [aria-busy="true"]')).toBeNull()
    expect(document.body.querySelector('.fixed [data-later-pending]')).toBeNull()
    expect(document.body.querySelector('[data-test="cook-progress"]')?.textContent).toMatch(/1/)

    app.unmount()
  })
})
