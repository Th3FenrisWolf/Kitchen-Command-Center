// @vitest-environment happy-dom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createSSRApp, h, type Component } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { ResourceString } from '~/Components/ResourceStrings'
import type { IslandPayload } from '~/Types/ContentRegions'
import { wizardStrings } from '../../../support/recipeWizard'

const PAGE = '/recipes/create-recipe/'
const IMPORT_OFF = { enabled: false, available: false, maxImageEdge: 0 }

const SLOW_CHUNK_MS = 50

// A fresh module graph per side also means a fresh bloom key, so each side imports its bloom after its wizard.
// A step's chunk reaches a phone well after the island's own, so each one here arrives late.
async function freshView(view: string): Promise<Component> {
  vi.resetModules()
  for (const step of ['BasicsStep', 'VariantStep'])
    vi.doMock(`~/Components/RecipeWizard/${step}.vue`, async (original) => {
      await new Promise((resolve) => setTimeout(resolve, SLOW_CHUNK_MS))
      return original()
    })
  const views: Record<string, () => Promise<{ default: Component }>> = {
    CreateRecipeView: () => import('~/Pages/CreateRecipe/CreateRecipeView.Component.vue'),
    AddVariantView: () => import('~/Pages/AddVariant/AddVariantView.Component.vue'),
  }
  return (await views[view]()).default
}

describe('the step a wizard opens on, with photo import off', () => {
  afterEach(() => {
    vi.doUnmock('~/Components/RecipeWizard/BasicsStep.vue')
    vi.doUnmock('~/Components/RecipeWizard/VariantStep.vue')
    document.body.innerHTML = ''
    vi.restoreAllMocks()
  })

  it.each([
    ['CreateRecipeView', {}, 'wizard-recipe-name'],
    [
      'AddVariantView',
      { recipeId: 'recipe-1', recipeName: 'Held Stew', recipeSlug: '/recipes/held-stew/' },
      'wizard-variant-name',
    ],
  ])('hydrates %s on its opening step, never on its pending sheet', async (view, own, opening) => {
    const props = { ...own, photoImport: IMPORT_OFF, resourceStrings: wizardStrings() }
    const wizardOnServer = await freshView(view)
    const { BLOOM_KEY, createBloom } = await import('~/Bloom/bloomContext')
    const server = createSSRApp({ render: () => h(wizardOnServer, props) })
    const serverBloom = createBloom(9, PAGE)
    const keys: string[] = []
    const claiming = {
      ...serverBloom,
      claimKey(key: string) {
        const claimed = serverBloom.claimKey(key)
        keys.push(claimed)
        return claimed
      },
    }
    server.component('ResourceString', ResourceString).provide(BLOOM_KEY, claiming)
    const html = await renderToString(server)
    document.body.innerHTML = `<kcc-island data-island="0">${html}</kcc-island>`
    const serverControl = document.querySelector(`[data-testid="${opening}"]`)
    const warnings = [vi.spyOn(console, 'warn'), vi.spyOn(console, 'error')]

    const wizard = await freshView(view)
    const { hydrateIslands } = await import('~/Islands/hydrate')
    const client = await import('~/Bloom/bloomContext')
    const payload: IslandPayload = {
      bloom: { seed: 9, page: PAGE, pageWashes: 0 },
      islands: [{ name: view, props, keys }],
    }
    await hydrateIslands(payload, client.createBloom(9, PAGE), () => () => Promise.resolve(wizard))

    expect(serverControl).not.toBeNull()
    expect(document.querySelector('[data-later-pending]')).toBeNull()
    const control = document.querySelector(`[data-testid="${opening}"]`)
    expect(control).toBe(serverControl)
    expect(control && '__vnode' in control, 'the opening step is hydrated when the island is').toBe(true)
    expect(warnings.flatMap((spy) => spy.mock.calls.map((call) => String(call[0])))).not.toContainEqual(
      expect.stringMatching(/[Hh]ydration/),
    )
  })
})
