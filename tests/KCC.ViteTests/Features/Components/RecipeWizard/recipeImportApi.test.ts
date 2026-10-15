import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApi } from '~/Utilities/Api'
import { readRecipe } from '~/Components/RecipeWizard/recipeImportApi'

const GNOCCHI_KEY = '11111111-2222-3333-4444-555555555555'

const NO_RECIPE = { outcome: 'noRecipe', ingredients: [], instructions: [], otherRecipes: [] }

const answer = (body: unknown) =>
  ({ ok: true, status: 200, text: () => Promise.resolve(JSON.stringify(body)) }) as unknown as Response

const page = (text: string) => new Blob([text], { type: 'image/jpeg' })

function stubFetch() {
  const fetch = vi.fn((_url: string, _init: RequestInit) => Promise.resolve(answer(NO_RECIPE)))
  vi.stubGlobal('fetch', fetch)
  return fetch
}

const sent = (fetch: ReturnType<typeof stubFetch>) => {
  const [url, init] = fetch.mock.calls[0]!
  return { url, init, form: init.body as FormData }
}

describe('readRecipe', () => {
  beforeEach(() => {
    configureApi({ antiforgeryToken: 'tok' })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('posts every photo, in order, with the flow, to the read endpoint under the caller signal', async () => {
    const fetch = stubFetch()
    const controller = new AbortController()

    await readRecipe({ flow: 'recipe', photos: [page('first page'), page('second page')] }, controller.signal)

    const { url, init, form } = sent(fetch)
    expect(url).toBe('/api/recipe-imports/read')
    expect(init.method).toBe('POST')
    expect(init.signal).toBe(controller.signal)
    expect(await Promise.all(form.getAll('photos').map((photo) => (photo as Blob).text()))).toEqual([
      'first page',
      'second page',
    ])
    expect(form.get('flow')).toBe('recipe')
    expect(form.has('recipeKey')).toBe(false)
  })

  it('posts the recipe key with the variant flow', async () => {
    const fetch = stubFetch()

    await readRecipe({ flow: 'variant', recipeKey: GNOCCHI_KEY, photos: [page('card')] }, new AbortController().signal)

    const { form } = sent(fetch)
    expect(form.get('flow')).toBe('variant')
    expect(form.get('recipeKey')).toBe(GNOCCHI_KEY)
  })

  it('returns the read as the endpoint answered it', async () => {
    stubFetch()

    const result = await readRecipe({ flow: 'recipe', photos: [page('card')] }, new AbortController().signal)

    expect(result).toEqual({ success: true, data: NO_RECIPE, errorMessage: null })
  })
})
